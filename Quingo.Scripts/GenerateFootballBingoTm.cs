using System.Diagnostics;
using CsvHelper.Configuration;
using CsvHelper;
using Quingo.Infrastructure.Database;
using Quingo.Scripts.Transfermarkt;
using System.Globalization;
using F23.StringSimilarity;
using Quingo.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quingo.Scripts.Transfermarkt.Dto;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Quingo.Infrastructure.Database.Repos;
using Quingo.Scripts.Models;

namespace Quingo.Scripts;

public partial class GenerateFootballBingoTm
{
    private readonly PackRepo _repo;
    private readonly TransfermarktClient _api;
    private readonly ILogger<GenerateFootballBingoTm> _logger;
    private readonly GoogleClient _google;

    private readonly JaroWinkler _jaroWinkler;

    private readonly string _csvPlayersPath = FilePath("footbingo/mar25/Players.csv");

    private readonly string _csvCountriesPath = FilePath("FootballCountries.csv");

    private readonly string _csvTeamsPath = FilePath("footbingo/mar25/Teams.csv");

    private readonly string _csvLeaguesPath = FilePath("footbingo/mar25/Leagues.csv");

    private readonly string _csvOtherPath = FilePath("FootballOther.csv");

    private readonly string _csvManagersPath = FilePath("footbingo/mar25/Managers.csv");

    private readonly string _csvTeammatesPath = FilePath("footbingo/mar25/Teammates.csv");

    private readonly string _csv50MPath = FilePath("Football50mPlus.csv");
    
    private readonly string _csvMedalistsPath = FilePath("footbingo/mar25/Medalists.csv");
    
    private readonly string _csvManagersCheckedPath = FilePath("FootballManagersReportChecked.csv");
    
    private readonly string _csvDynastyCheckedPath = FilePath("FootballRelativesReportChecked.csv");
    
    private readonly string _csvDifficultyPath = FilePath("FootballDifficulty.csv");
    
    private readonly string _csv100CapsPath = FilePath("Football100Caps.csv");
    
    private readonly string _csv100GamesCl = FilePath("Football100GamesCl.csv");
    
    private readonly string _csvDynastyPath = FilePath("footbingo/mar25/Dynasty.csv");
    
    private readonly string _csvTreblePath = FilePath("footbingo/mar25/TrebleWinners.csv");
    
    private readonly string _csvGoalscorersPath = FilePath("footbingo/mar25/Top5Goalscorers.csv");

    private static string FilePath(string filename) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "docs", filename);

    const string PackName = "Footbingo";

    const string TagPlayer = "player";
    const string TagClub = "club";
    const string TagCountry = "country";
    const string TagTrophy = "trophy";
    const string TagLeague = "league";
    private const string TagRegion = "region";
    private const string TagIgnore = "ignore";
    private const string TagOther = "other";
    private const string TagManager = "manager";
    private const string TagTeammate = "teammate";

    const string LinkPlayerClub = "played in a club";
    const string LinkPlayerCountry = "has citizenship";
    const string LinkPlayerTrophy = "won a trophy";
    const string LinkClubLeague = "league member";
    const string LinkCountryRegion = "part of region";
    const string LinkPlayerLeague = "played in a league";
    const string LinkPlayerOther = "linked to";
    const string LinkPlayerManager = "played under management";
    const string LinkPlayerTeammate = "played with";

    private readonly List<string> _tags =
    [
        TagPlayer, TagClub, TagCountry, TagTrophy, TagLeague, TagRegion, TagIgnore,
        TagOther, TagManager, TagTeammate
    ];

    private readonly List<string> _linkTypes =
    [
        LinkPlayerClub, LinkPlayerCountry, LinkPlayerTrophy, LinkClubLeague,
        LinkCountryRegion, LinkPlayerLeague, LinkPlayerOther, LinkPlayerManager, LinkPlayerTeammate
    ];

    private readonly List<string> _trophyWhitelist =
    [
        "English Champion",
        "German Champion",
        "Africa Cup winner",
        "Copa América winner",
        "Copa Libertadores winner",
        "Spanish champion",
        "French champion",
        "Champions League winner",
        "Uefa Cup winner",
        "Europa League winner",
        "Italian champion",
        "European champion",
        "World Cup winner",
        "Conference League winner",
        "European Champion Clubs' Cup winner",
        "Top goal scorer",
        "Winner Ballon d'Or",
        "Euro runner-up",
        "World Cup runner-up",
        "World Cup third place",
        "Italian cup winner",
        "Spanish cup winner",
        "German cup winner",
        "English FA Cup winner",
    ];

    private readonly Dictionary<string, string> _trophyMerge = new()
    {
        ["European Champion Clubs' Cup winner"] = "Champions League winner",
        ["Uefa Cup winner"] = "Europa League winner",
        ["Euro runner-up"] = "WC/Euro medalist",
        ["World Cup runner-up"] = "WC/Euro medalist",
        ["World Cup third place"] = "WC/Euro medalist",
    };

    private List<string> _excludePlayerClubLeagues = ["Championship"];

    private readonly Regex _excludeRegex = ExcludeRegex();

    private readonly Regex _clubRetiredRegex = ClubRetiredRegex();

    private readonly Regex _clubDuplicatesRegex = ClubDuplicatesRegex();

    public GenerateFootballBingoTm(PackRepo repo, TransfermarktClient api, ILogger<GenerateFootballBingoTm> logger,
        GoogleClient google)
    {
        _repo = repo;
        _api = api;
        _logger = logger;
        _jaroWinkler = new JaroWinkler();
        _google = google;
    }

    private List<TmPlayerModel> _players = [];

    private List<CountryModel> _countries = [];

    private List<GroupOtherModel> _groupsOther = [];

    private List<TeamModel> _teams = [];

    private List<LeagueModel> _leagues = [];

    private List<ManagerPlayerModel> _managers = [];

    private List<TeammatePlayerModel> _teammates = [];

    private List<PlayerMarketValueModel> _playerMarketValues = [];
    
    private List<MedalistModel> _medalists = [];
    
    private List<ExportManagerModel> _managersChecked = [];
    
    private List<ExportDynastyModel> _dynastyChecked = [];
    
    private List<NodeDifficultyModel> _nodeDifficulty = [];

    private List<TmPlayerModel> _100Caps = [];
    
    private List<TmPlayerModel> _100GamesCl = [];
    
    private List<PlayerUrlModel> _trebleWinners = [];
    
    private List<PlayerUrlModel> _top5Goalscorers = [];
    
    private List<PlayerUrlModel> _dynasty = [];

    private TransfermarktData _data = new();

    private ApplicationDbContext _db;

    public async Task Execute()
    {
        _players = ReadCsv<TmPlayerModel>(_csvPlayersPath);
        _countries = ReadCsv<CountryModel>(_csvCountriesPath);
        _groupsOther = ReadCsv<GroupOtherModel>(_csvOtherPath);
        _teams = ReadCsv<TeamModel>(_csvTeamsPath);
        _leagues = ReadCsv<LeagueModel>(_csvLeaguesPath);
        _managers = ReadCsv<ManagerPlayerModel>(_csvManagersPath);
        _teammates = ReadCsv<TeammatePlayerModel>(_csvTeammatesPath);
        _playerMarketValues = ReadCsv<PlayerMarketValueModel>(_csv50MPath);
        _medalists = ReadCsv<MedalistModel>(_csvMedalistsPath);
        _managersChecked = ReadCsv<ExportManagerModel>(_csvManagersCheckedPath);
        _dynastyChecked = ReadCsv<ExportDynastyModel>(_csvDynastyCheckedPath);
        _nodeDifficulty = ReadCsv<NodeDifficultyModel>(_csvDifficultyPath);
        _100Caps = ReadCsv<TmPlayerModel>(_csv100CapsPath);
        _100GamesCl = ReadCsv<TmPlayerModel>(_csv100GamesCl);
        _trebleWinners = ReadCsv<PlayerUrlModel>(_csvTreblePath);
        _top5Goalscorers = ReadCsv<PlayerUrlModel>(_csvGoalscorersPath);
        _dynasty = ReadCsv<PlayerUrlModel>(_csvDynastyPath);
        _data = LoadDataJson();

        // _data.Players = new Dictionary<string, PlayerProfileResponse>();
        // _data.PlayerTransfers = new Dictionary<string, PlayerTransfersResponse>();
        // _data.PlayerStats = new Dictionary<string, PlayerStatsResponse>();
        // _data.PlayerAchievements = new Dictionary<string, PlayerAchievementsResponse>();

        _db = await _repo.CreateDbContext();

        try
        {
            // await PrintPlayers();
            // return;
            var pack = await GetPack();
            // CheckCountries(pack);
            // CheckTeams(pack);
            // await DeleteTrophies(pack);
            // await FixLinkType(pack, TagPlayer, TagLeague, LinkClubLeague, LinkPlayerLeague);
            // await FixLinkType(pack, TagPlayer, TagOther, LinkPlayerClub, LinkPlayerOther);
            await ImportPlayers(pack, false);
            await ImportManagers(pack);
            await ImportTeammates(pack);
            await CreateOther(pack);
            // CheckMarketValueMissingPlayers(pack);
            await ImportMedalists(pack);
            // await GenerateManagersReport(pack);
            // await GenerateRelativesReport(pack);
            // await ImportPlayerManagers(pack);
            // await ImportDynasty(pack);
            // await ExportPlayersByGoalsAssists(pack);
            // ExportLinkedStats(pack);
            // await ImportDifficulty(pack);
            // await FixChampionship(pack);
            // await Import100Caps(pack);
            // await Import100GamesCl(pack);
        }
        finally
        {
            // SaveDataJson();
        }
    }

    private async Task FixChampionship(Pack pack)
    {
        var node = pack.Nodes.First(x => x.Name == "Championship" && x.HasTag(TagLeague) && x.DeletedAt == null);
        // node.NodeLinksTo.RemoveAll(x => x.NodeLinkType.Name == LinkPlayerLeague);
        // await _db.SaveChangesAsync();
        // return;

        var players = pack.Nodes.Where(x => x.HasTag(TagPlayer) && x.DeletedAt == null).ToList();
        foreach (var player in players)
        {
            var playerId = player.Meta.PropertiesDict["tm_player_id"];
            var tmStats = await GetStatsCached(playerId);
            if (tmStats.Stats?.FirstOrDefault(x => x.CompetitionID is "GB2" or "EFD2") != null)
            {
                EnsureLinkCreated(player, node, LinkPlayerLeague, pack, true);
            }
        }

        await _db.SaveChangesAsync();
    }

    private async Task ImportDifficulty(Pack pack)
    {
        foreach (var diff in _nodeDifficulty)
        {
            var node = pack.Nodes.FirstOrDefault(x => x.Name == diff.Name && x.HasTag(diff.Tag) && x.DeletedAt == null);
            if (node == null)
            {
                Console.WriteLine($"{diff.Name} {diff.Tag} not found");
                continue;
            }

            node.Difficulty = diff.DifficultyInt;
            node.CellScore = diff.ScoreInt;
        }

        await _db.SaveChangesAsync();
    }

    private async Task Import100Caps(Pack pack)
    {
        var node = EnsureNodeCreated("100+ Caps", TagOther, pack);

        foreach (var p in _100Caps)
        {
            var playerNode = pack.Nodes
                .FirstOrDefault(x => x.HasTag(TagPlayer) && x.Meta.PropertiesDict["tm_player_id"] == p.PlayerId);
            if (playerNode == null) continue;

            EnsureLinkCreated(playerNode, node, LinkPlayerOther, pack);
        }
        await _db.SaveChangesAsync();
    }
    
    private async Task Import100GamesCl(Pack pack)
    {
        var node = EnsureNodeCreated("100+ Games in CL", TagOther, pack);

        foreach (var p in _100GamesCl)
        {
            var playerNode = pack.Nodes
                .FirstOrDefault(x => x.HasTag(TagPlayer) && x.Meta.PropertiesDict["tm_player_id"] == p.PlayerId);
            if (playerNode == null) continue;

            EnsureLinkCreated(playerNode, node, LinkPlayerOther, pack);
        }
        await _db.SaveChangesAsync();
    }

    private async Task PrintPlayers()
    {
        foreach (var player in _players.Where(x => string.IsNullOrEmpty(x.Name)))
        {
            var tmPlayer = await GetPlayerProfileCached(player.PlayerId);
            Console.WriteLine(tmPlayer.Name);
        }
    }

    private async Task ExportPlayersByGoalsAssists(Pack pack)
    {
        var goals = new List<ExportPlayerGoalsModel>();
        var assists = new List<ExportPlayerGoalsModel>();
        var players = pack.Nodes.Where(x => x.HasTag(TagPlayer));
        foreach (var player in players)
        {
            var playerId = player.Meta.PropertiesDict["tm_player_id"];
            var stats = await GetStatsCached(playerId);
            var profile = await GetPlayerProfileCached(playerId);
            
            var goalCount = stats.Stats?.Where(x => !string.IsNullOrEmpty(x?.Goals))
                .Select(x => int.TryParse(x.Goals, out var i) ? i : 0).Sum() ?? 0;
            
            if (goalCount is >= 200 and < 300)
            {
                goals.Add(new ExportPlayerGoalsModel
                {
                    PlayerId = playerId,
                    Name = player.Name,
                    Url = profile.Url,
                    Goals = goalCount
                });
            }
            
            var assCount = stats.Stats?.Where(x => !string.IsNullOrEmpty(x?.Assists))
                .Select(x => int.TryParse(x.Assists, out var i) ? i : 0).Sum() ?? 0;
            
            if (assCount is >= 50 and < 100 )
            {
                assists.Add(new ExportPlayerGoalsModel
                {
                    PlayerId = playerId,
                    Name = player.Name,
                    Url = profile.Url,
                    Goals = assCount
                });
            }
        }

        var sortedGoals = goals.OrderBy(x => x.Goals).ThenBy(x => x.Name).ToList();
        SaveCsv(sortedGoals, FilePath("ExportFootballPlayerGoals.csv"));
        
        var sortedAssists = assists.OrderBy(x => x.Goals).ThenBy(x => x.Name).ToList();
        SaveCsv(sortedAssists, FilePath("ExportFootballPlayerAssists.csv"));
    }

    private async Task ImportDynasty(Pack pack)
    {
        var node = EnsureNodeCreated("Dynasty", TagOther, pack);
        // node.NodeLinksTo.RemoveAll(_ => true);
        // await _db.SaveChangesAsync();
        
        foreach (var model in _dynastyChecked.Where(x => !string.IsNullOrEmpty(x.Id) && x.ShouldAddBool))
        {
            var playerNode = pack.Nodes
                .FirstOrDefault(x => x.HasTag(TagPlayer) && x.Meta.PropertiesDict["tm_player_id"] == model.Id);
            if (playerNode == null)
            {
                Console.WriteLine($"Player not found: {model.Id} {model.Name}");
            }
            else
            {
                EnsureLinkCreated(playerNode, node, LinkPlayerOther, pack);
            }
            
        }
        await _db.SaveChangesAsync();
    }

    private async Task ImportPlayerManagers(Pack pack)
    {
        var node = EnsureNodeCreated("Manager", TagOther, pack);
        // node.NodeLinksTo.RemoveAll(_ => true);
        // await _db.SaveChangesAsync();
        foreach (var model in _managersChecked.Where(x => x.ShouldAddBool))
        {
            var playerNode = pack.Nodes
                .First(x => x.HasTag(TagPlayer) && x.Meta.PropertiesDict["tm_player_id"] == model.PlayerId);
            EnsureLinkCreated(playerNode, node, LinkPlayerOther, pack);
        }
        await _db.SaveChangesAsync();
    }

    private async Task RegeneratePlayerList(List<TmPlayerModel> players)
    {
        var result = new List<TmPlayerModel>();
        foreach (var (player, i) in players.Select((x, i) => (x, i)))
        {
            Console.WriteLine($"[{i+1}/{players.Count}] RegeneratePlayerList {player.PlayerId}");
            var tmPlayer = await GetPlayerProfileCached(player.PlayerId);
            var playerRes = new TmPlayerModel
            {
                NameRu = player.NameRu,
                Name = tmPlayer.Name,
                FullName = tmPlayer.FullName,
                NameInHomeCountry = tmPlayer.NameInHomeCountry,
                Link = tmPlayer.Url,
            };
            result.Add(playerRes);
        }
        
        SaveCsv(result, FilePath("FootballPlayersRegen.csv"));
    }

    private async Task ImportMedalists(Pack pack)
    {
        var node = EnsureNodeCreated("WC&EURO Medalist", TagOther, pack);
        var players = pack.Nodes.Where(x => x.HasTag(TagPlayer));
        var medalists = _medalists.Where(x => !string.IsNullOrEmpty(x.PlayerId)).ToList();
        foreach (var player in players)
        {
            var playerId = player.Meta.PropertiesDict["tm_player_id"];
            var medalist = medalists.FirstOrDefault(x => x.PlayerId == playerId);
            if (medalist != null)
            {
                EnsureLinkCreated(player, node, LinkPlayerOther, pack);
            }
        }

        await _db.SaveChangesAsync();
    }

    private async Task GenerateManagersReport(Pack pack)
    {
        var result = new List<ExportManagerModel>();
        var players = pack.Nodes.Where(x => x.HasTag(TagPlayer));
        foreach (var player in players)
        {
            var playerId = player.Meta.PropertiesDict["tm_player_id"];
            var profile = await GetPlayerProfileCached(playerId);
            if (profile.TrainerProfile == null) continue;
            var res = new ExportManagerModel
            {
                PlayerId = profile.Id,
                TrainerId = profile.TrainerProfile.Id,
                Position = profile.TrainerProfile.Position,
                Name = profile.Name,
                PlayerUrl = profile.Url,
                TrainerUrl = "https://www.transfermarkt.com" + profile.TrainerProfile.Url
            };
            result.Add(res);
        }

        SaveCsv(result, FilePath("FootballManagersReport.csv"));
    }

    private async Task GenerateRelativesReport(Pack pack)
    {
        var result = new List<ExportDynastyModel>();
        var players = pack.Nodes.Where(x => x.HasTag(TagPlayer));
        foreach (var player in players)
        {
            var playerId = player.Meta.PropertiesDict["tm_player_id"];
            var profile = await GetPlayerProfileCached(playerId);
            if (profile.Relatives is not { Count: > 0 }) continue;

            var res = new ExportDynastyModel
            {
                Id = profile.Id,
                Name = profile.Name,
                Url = profile.Url,
            };
            result.Add(res);
            foreach (var relative in profile.Relatives)
            {
                var relRes = new ExportDynastyModel
                {
                    RelativeId = relative.Id,
                    RelativeName = relative.Name,
                    RelativeProfileType = relative.ProfileType,
                    RelativeUrl = "https://www.transfermarkt.com" + relative.Url
                };
                result.Add(relRes);
            }
        }
        
        SaveCsv(result, FilePath("FootballRelativesReport.csv"));
    }

    private void ExportLinkedStats(Pack pack)
    {
        var res = new List<LinkedStatsModel>();
        var playersRes = new List<LinkedStatsModel>();
        foreach (var tag in pack.Tags.Where(x => x.Name != TagIgnore && x.Name != TagPlayer))
        {
            var tagNodes = pack.Nodes.Where(x => x.HasTag(tag.Id) && !x.HasTag(TagIgnore)).ToList();
            foreach (var tagNode in tagNodes)
            {
                int playersCount;
                if (tag.Name is TagLeague or TagRegion)
                {
                    var indirectCount = tagNode.NodeLinksTo.Select(x => x.NodeFrom).SelectMany(x => x.NodeLinksTo)
                        .Count(x => x.NodeFrom.HasTag(TagPlayer));
                    var directCount = tagNode.NodeLinksTo.Count(x => x.NodeFrom.HasTag(TagPlayer));
                    playersCount = indirectCount + directCount;
                }
                else
                {
                    playersCount = tagNode.NodeLinksTo.Count(x => x.NodeFrom.HasTag(TagPlayer));
                }
                
                res.Add(new LinkedStatsModel
                {
                    Type = tag.Name,
                    Name = tagNode.Name,
                    LinkedItems = playersCount
                });
            }
        }

        foreach (var playerNode in pack.Nodes.Where(x => x.HasTag(TagPlayer)))
        {
            var linksCount = playerNode.NodeLinksFrom.Count;
            playersRes.Add(new LinkedStatsModel
            {
                Type = TagPlayer,
                Name = playerNode.Name,
                LinkedItems = linksCount
            });
        }

        res = res.OrderBy(x => x.Type).ThenByDescending(x => x.LinkedItems).ThenBy(x => x.Name).ToList();
        playersRes = playersRes.OrderByDescending(x => x.LinkedItems).ThenBy(x => x.Name).ToList();
        List<LinkedStatsModel> result = [..res, ..playersRes];
        SaveCsv(result, FilePath("FootballLinkCountReport.csv"));
    }

    private void CheckMarketValueMissingPlayers(Pack pack)
    {
        var playerNodes = pack.Nodes.Where(x => x.HasTag(TagPlayer)).ToList();
        foreach (var player in _playerMarketValues)
        {
            var node = playerNodes.FirstOrDefault(x => x.Meta.PropertiesDict["tm_player_id"] == player.PlayerId);
            if (node == null)
            {
                Console.WriteLine($"{player.Player} {player.MarketValue}");
            }
        }
    }

    private async Task ImportManagers(Pack pack)
    {
        // await ClearLinks(pack, LinkPlayerManager);
        // return;
        
        var managers = _managers.GroupBy(x => x.Manager);

        foreach (var manager in managers)
        {
            var managerNode = EnsureNodeCreated(manager.Key, TagManager, pack);
            foreach (var player in manager)
            {
                var playerNode = pack.Nodes.FirstOrDefault(x => x.HasTag(TagPlayer)
                                                                && x.Meta.PropertiesDict["tm_player_id"] ==
                                                                player.PlayerId);
                if (playerNode == null) continue;

                EnsureLinkCreated(playerNode, managerNode, LinkPlayerManager, pack, true);
            }

            await _db.SaveChangesAsync();
        }
    }

    private async Task ClearLinks(Pack pack, string linkName)
    {
        var links = await _db.NodeLinks.Where(x => x.NodeLinkType.PackId == pack.Id && x.NodeLinkType.Name == linkName)
            .ToListAsync();
        _db.RemoveRange(links);
        await _db.SaveChangesAsync();
    }

    private async Task ImportTeammates(Pack pack)
    {
        // await ClearLinks(pack, LinkPlayerTeammate);
        // return;

        var tmates = _teammates.Where(x => !string.IsNullOrEmpty(x.Url))
            .GroupBy(x => x.Teammate);

        foreach (var tmate in tmates)
        {
            var tmateNode = pack.Nodes.First(x => x.HasTag(TagPlayer) && x.Name == tmate.Key);
            if (!tmateNode.HasTag(TagTeammate))
            {
                tmateNode.NodeTags.Add(new NodeTag
                {
                    Node = tmateNode,
                    Tag = pack.Tags.First(x => x.Name == TagTeammate)
                });
            }
            
            foreach (var player in tmate)
            {
                var playerNode = pack.Nodes.FirstOrDefault(x => x.HasTag(TagPlayer)
                                                                && x.Meta?.PropertiesDict.TryGetValue("tm_player_id",
                                                                    out var playerId) == true
                                                                && playerId == player.PlayerId);
                if (playerNode == null) continue;
            
                EnsureLinkCreated(playerNode, tmateNode, LinkPlayerTeammate, pack, true);
            }
            
            await _db.SaveChangesAsync();
        }
    }

    private async Task DeleteTrophies(Pack pack)
    {
        var trophies = pack.Nodes.Where(x => x.HasTag(TagTrophy));
        foreach (var trophy in trophies)
        {
            _db.Remove(trophy);
            foreach (var link in trophy.NodeLinks)
            {
                _db.Remove(link);
            }
        }

        await _db.SaveChangesAsync();
    }

    private async Task FixLinkType(Pack pack, string tagFrom, string tagTo, string linkName, string linkNameFixed)
    {
        var nodes = pack.Nodes.Where(x => x.HasTag(tagFrom));
        foreach (var node in nodes)
        {
            var updated = false;
            var links = node.NodeLinksFrom.Where(x => x.NodeLinkType.Name == linkName && x.NodeTo.HasTag(tagTo));
            foreach (var link in links)
            {
                link.NodeLinkType = pack.NodeLinkTypes.First(x => x.Name == linkNameFixed);
                updated = true;
            }

            if (updated)
            {
                await _db.SaveChangesAsync();
            }
        }
    }

    private async Task CreateOther(Pack pack)
    {
        // await CreateGroups();
        // await Create1Club(false);
        // await Create1Country(false);
        // await Create10Clubs();
        // await CreateStatsOther();
        // await CreateProfileOther();
        // await Create50Mil();
        await CreatePlayerUrlLists();

        return;

        async Task CreateGroups()
        {
            foreach (var entry in _groupsOther)
            {
                var player = pack.Nodes.FirstOrDefault(x => x.HasTag(TagPlayer)
                                                            && string.Compare(x.Name, entry.PlayerName,
                                                                CultureInfo.CurrentCulture,
                                                                CompareOptions.IgnoreNonSpace |
                                                                CompareOptions.IgnoreCase) == 0);
                if (player == null)
                {
                    continue;
                }

                var groupNode = EnsureNodeCreated(entry.GroupName, TagOther, pack);
                EnsureLinkCreated(player, groupNode, LinkPlayerOther, pack);
            }

            await _db.SaveChangesAsync();
        }

        async Task Create50Mil()
        {
            var node = EnsureNodeCreated("50+ Millions", TagOther, pack);
            var players = pack.Nodes.Where(x => x.HasTag(TagPlayer));
            foreach (var player in players)
            {
                var playerId = player.Meta.PropertiesDict["tm_player_id"];
                var profile = await GetPlayerProfileCached(playerId);
                var mvStr = profile.MarketValue;
                if (string.IsNullOrEmpty(mvStr)) continue;
                var mv = ParseMarketValue(mvStr);
                if (mv >= 50_000_000)
                {
                    EnsureLinkCreated(player, node, LinkPlayerOther, pack);
                }
            }

            await _db.SaveChangesAsync();
        }

        async Task Create1Club(bool recreate = false)
        {
            var node = EnsureNodeCreated("1 Club", TagOther, pack);
            if (recreate)
            {
                node.NodeLinksTo.RemoveAll(_ => true);
                await _db.SaveChangesAsync();
            }

            var players = pack.Nodes.Where(x => x.HasTag(TagPlayer)
                                                && x.NodeLinksFrom.Where(l => l.DeletedAt == null)
                                                    .Count(l => l.NodeTo.HasTag(TagClub)) < 2);
            foreach (var player in players)
            {
                var playerId = player.Meta.PropertiesDict["tm_player_id"];
                var clubs = await GetPlayerClubs(playerId);
                if (clubs.Count() == 1)
                {
                    EnsureLinkCreated(player, node, LinkPlayerOther, pack);
                }
            }

            await _db.SaveChangesAsync();
        }

        async Task Create1Country(bool recreate = false)
        {
            var node = EnsureNodeCreated("1 Country", TagOther, pack);
            if (recreate)
            {
                node.NodeLinksTo.RemoveAll(_ => true);
                await _db.SaveChangesAsync();
                return;
            }

            var players = pack.Nodes.Where(x => x.HasTag(TagPlayer));
            foreach (var player in players)
            {
                var playerId = player.Meta.PropertiesDict["tm_player_id"];
                var clubs = await GetPlayerClubs(playerId);
                var countries = clubs
                    .Select(x => x.League?.CountryName ?? x.AddressLine3).Where(x => !string.IsNullOrEmpty(x))
                    .Distinct();
                if (countries.Count() == 1)
                {
                    EnsureLinkCreated(player, node, LinkPlayerOther, pack);
                }
            }

            await _db.SaveChangesAsync();
        }

        async Task Create10Clubs()
        {
            var node = EnsureNodeCreated("10+ Clubs", TagOther, pack);

            var players = pack.Nodes.Where(x => x.HasTag(TagPlayer));
            foreach (var player in players)
            {
                var playerId = player.Meta.PropertiesDict["tm_player_id"];
                var clubs = await GetPlayerClubs(playerId);
                if (clubs.Count() >= 10)
                {
                    EnsureLinkCreated(player, node, LinkPlayerOther, pack);
                }
            }

            await _db.SaveChangesAsync();
        }

        async Task CreateStatsOther()
        {
            var assNode = EnsureNodeCreated("100+ Assists", TagOther, pack);
            var goalNode = EnsureNodeCreated("300+ Goals", TagOther, pack);

            var players = pack.Nodes.Where(x => x.HasTag(TagPlayer));
            foreach (var player in players)
            {
                var playerId = player.Meta.PropertiesDict["tm_player_id"];
                var stats = await GetStatsCached(playerId);

                var assCount = stats.Stats?.Where(x => !string.IsNullOrEmpty(x?.Assists))
                    .Select(x => int.TryParse(x.Assists, out var i) ? i : 0).Sum() ?? 0;
                var goalCount = stats.Stats?.Where(x => !string.IsNullOrEmpty(x?.Goals))
                    .Select(x => int.TryParse(x.Goals, out var i) ? i : 0).Sum() ?? 0;

                if (assCount >= 100)
                {
                    EnsureLinkCreated(player, assNode, LinkPlayerOther, pack);
                }

                if (goalCount >= 300)
                {
                    EnsureLinkCreated(player, goalNode, LinkPlayerOther, pack);
                }
            }

            await _db.SaveChangesAsync();
        }

        async Task CreateProfileOther()
        {
            var goalNode = EnsureNodeCreated("Goalkeeper", TagOther, pack);
            var under23Node = EnsureNodeCreated("Under 23", TagOther, pack);
            // var managerNode = EnsureNodeCreated("Manager", TagOther, pack);
            // var dynastyNode = EnsureNodeCreated("Dynasty", TagOther, pack);
            var dualCitizenshipNode = EnsureNodeCreated("Dual Citizenship", TagOther, pack);

            var players = pack.Nodes.Where(x => x.HasTag(TagPlayer)).ToList();
            var playerIds = players.Select(x => x.Meta.PropertiesDict["tm_player_id"]).ToList();
            foreach (var player in players)
            {
                var playerId = player.Meta.PropertiesDict["tm_player_id"];
                var profile = await GetPlayerProfileCached(playerId);

                if (profile.Position?.Main?.Equals("Goalkeeper", StringComparison.InvariantCultureIgnoreCase) == true)
                {
                    EnsureLinkCreated(player, goalNode, LinkPlayerOther, pack);
                }
                
                if (!string.IsNullOrEmpty(profile.Age) && int.TryParse(profile.Age, out var age) && age <= 23)
                {
                    EnsureLinkCreated(player, under23Node, LinkPlayerOther, pack);
                }
                
                // if (profile.TrainerProfile?.Position == "Manager")
                // {
                //     EnsureLinkCreated(player, managerNode, LinkPlayerOther, pack);
                // }

                // var related = profile.Relatives?.ToList();
                // if (related?.Count > 0)
                // {
                //     EnsureLinkCreated(player, dynastyNode, LinkPlayerOther, pack);
                //     foreach (var relatedPlayer in related.Where(x => x.ProfileType == "player"))
                //     {
                //         var relatedNode = players.FirstOrDefault(x =>
                //             x.Meta.PropertiesDict["tm_player_id"] == relatedPlayer.Id);
                //         if (relatedNode != null)
                //             EnsureLinkCreated(relatedNode, dynastyNode, LinkPlayerOther, pack);
                //     }
                // }

                if (profile.Citizenship?.Count > 1)
                {
                    EnsureLinkCreated(player, dualCitizenshipNode, LinkPlayerOther, pack);
                }
            }

            await _db.SaveChangesAsync();
        }

        async Task CreatePlayerUrlLists()
        {
            // var trebleWinners = EnsureNodeCreated("Treble winner", TagTrophy, pack);
            var goalscorers = EnsureNodeCreated("Top-5 Top Goalscorer", TagOther, pack);
            // var dynasty = EnsureNodeCreated("Dynasty V2", TagOther, pack);
            
            var players = pack.Nodes.Where(x => x.HasTag(TagPlayer)).ToList();
            foreach (var player in players)
            {
                var playerId = player.Meta.PropertiesDict["tm_player_id"];

                // if (_trebleWinners.FirstOrDefault(x => x.PlayerId == playerId) != null)
                // {
                //     EnsureLinkCreated(player, trebleWinners, LinkPlayerTrophy, pack, true);
                // }
                
                if (_top5Goalscorers.FirstOrDefault(x => x.PlayerId == playerId) != null)
                {
                    EnsureLinkCreated(player, goalscorers, LinkPlayerOther, pack);
                }
                //
                // if (_dynasty.FirstOrDefault(x => x.PlayerId == playerId) != null)
                // {
                //     EnsureLinkCreated(player, dynasty, LinkPlayerOther, pack);
                // }
            }
            
            await _db.SaveChangesAsync();
        }

        async Task<IEnumerable<ClubProfileResponse>> GetPlayerClubs(string playerId)
        {
            var stats = await GetStatsCached(playerId);
            var statClubIds = stats.Stats?.Where(x => !string.IsNullOrEmpty(x?.ClubID))
                .Select(x => x.ClubID).Distinct() ?? [];

            var transfers = await GetTransfersCached(playerId);

            var transferClubIds = (transfers.Transfers?.Select(x => x.From) ?? [])
                .Concat(transfers.Transfers?.Select(x => x.To) ?? [])
                .Where(x => x != null && !string.IsNullOrEmpty(x.ClubID) && !string.IsNullOrEmpty(x.ClubName)
                            && !_excludeRegex.IsMatch(x.ClubName) && !_clubDuplicatesRegex.IsMatch(x.ClubName)
                            && !_clubRetiredRegex.IsMatch(x.ClubName))
                .Select(x => x.ClubID).Distinct();

            var clubIds = statClubIds.Concat(transferClubIds).Distinct().ToList();

            foreach (var clubId in clubIds)
            {
                await GetClubCached(clubId);
            }

            var clubs = _data.Clubs
                .Where(x => clubIds.Contains(x.Key))
                .Select(x => x.Value);
            var clubsExcl = clubs
                .Where(x => !string.IsNullOrEmpty(x.Name) && x.Url?.Contains("jugend") != true &&
                            !_excludeRegex.IsMatch(x.Name)
                            && !_clubDuplicatesRegex.IsMatch(x.Name) && !_clubRetiredRegex.IsMatch(x.Name));
            return clubsExcl;
        }
    }


    private void CheckCountries(Pack pack)
    {
        foreach (var country in _countries)
        {
            var cNode = pack.Nodes.FirstOrDefault(x => x.Name == country.Country && x.HasTag(TagCountry));
            if (cNode == null)
            {
                var player = pack.Nodes.FirstOrDefault(x => x.HasTag(TagPlayer)
                                                            && x.Meta.Properties.FirstOrDefault(p =>
                                                                    p.Key == "tm_player_citizenship")?.Value
                                                                .Contains(country.Country,
                                                                    StringComparison.InvariantCultureIgnoreCase) ==
                                                            true);
                if (player != null)
                {
                    var str =
                        $"{country.Country};{player.Meta.PropertiesDict["tm_player_citizenship"]};{player.Name}";
                    Console.WriteLine(str);
                }
                else
                {
                    Console.WriteLine(country.Country);
                }
            }
        }
    }

    private void CheckTeams(Pack pack)
    {
        foreach (var team in _teams)
        {
            var cNode = pack.Nodes.FirstOrDefault(x => x.Name == team.TeamName && x.HasTag(TagClub));
            if (cNode == null)
            {
                Console.WriteLine(team.TeamName);
            }
        }
    }

    private async Task<Pack> GetPack()
    {
        var pack = await _db.Packs.FirstOrDefaultAsync(x => x.Name == PackName);
        if (pack == null)
        {
            pack = new Pack
            {
                Name = PackName,
                Description = $"Generated on {DateTime.UtcNow:g}"
            };
            _db.Packs.Add(pack);
            _logger.LogInformation("Created pack: {name}", PackName);
        }
        else
        {
            pack = await _repo.GetPack(_db, pack.Id, true);
            pack!.Description = $"Updated at {DateTime.UtcNow:g}";
        }

        foreach (var tag in _tags)
        {
            EnsureTagCreated(tag, pack);
        }

        foreach (var link in _linkTypes)
        {
            EnsureLinkTypeCreated(link, pack);
        }

        return pack;
    }

    private void EnsureTagCreated(string tagName, Pack pack)
    {
        var tag = pack.Tags.FirstOrDefault(x => x.Name == tagName);
        if (tag != null) return;

        tag = new Tag { Name = tagName, Pack = pack };
        pack.Tags.Add(tag);
        _logger.LogInformation("Created tag: {name}", tagName);
    }

    private void EnsureLinkTypeCreated(string linkName, Pack pack)
    {
        var link = pack.NodeLinkTypes.FirstOrDefault(x => x.Name == linkName);
        if (link != null) return;

        link = new NodeLinkType { Name = linkName, Pack = pack };
        pack.NodeLinkTypes.Add(link);
        _logger.LogInformation("Created link type: {name}", linkName);
    }

    private Node EnsureNodeCreated(string name, string tag, Pack pack)
    {
        var node = pack.Nodes.FirstOrDefault(x => x.Name == name && x.HasTag(tag) && x.DeletedAt != null) 
                   ?? pack.Nodes.FirstOrDefault(x => x.Name == name && x.HasTag(tag));
        if (node != null)
        {
            if (node.DeletedAt != null) 
                return null;
            return node;
        }

        node = new Node
        {
            Pack = pack,
            Name = name,
        };
        node.NodeTags.Add(new NodeTag
        {
            Node = node,
            Tag = pack.Tags.First(x => x.Name == tag)
        });
        pack.Nodes.Add(node);

        _logger.LogInformation("Created node: {name} {tag}", name, tag);

        return node;
    }

    private async Task<ClubProfileResponse> GetClubCached(string clubId)
    {
        if (_data.Clubs.TryGetValue(clubId, out var tmClub)) return tmClub;

        tmClub = await _api.GetClubProfile(clubId);
        if (tmClub == null)
        {
            //throw new Exception($"Club not found: {clubId} {clubName}");
            return null;
        }

        _data.Clubs[clubId] = tmClub;

        return tmClub;
    }

    private async Task<PlayerStatsResponse> GetStatsCached(string playerId)
    {
        if (_data.PlayerStats.TryGetValue(playerId, out var tmStats)) return tmStats;

        tmStats = await _api.GetPlayerStats(playerId);
        if (tmStats == null)
        {
            throw new Exception($"Player stats not found: {playerId}");
        }

        _data.PlayerStats[playerId] = tmStats;

        return tmStats;
    }

    private async Task<PlayerTransfersResponse> GetTransfersCached(string playerId)
    {
        if (_data.PlayerTransfers.TryGetValue(playerId, out var tmTransfers)) return tmTransfers;

        tmTransfers = await _api.GetPlayerTransfers(playerId);
        if (tmTransfers == null)
        {
            throw new Exception($"Player transfers not found: {playerId}");
        }

        _data.PlayerTransfers[playerId] = tmTransfers;

        return tmTransfers;
    }

    private async Task<PlayerProfileResponse> GetPlayerProfileCached(string playerId)
    {
        if (_data.Players.TryGetValue(playerId, out var tmPlayer))
        {
            return tmPlayer;
        }

        tmPlayer = await _api.GetPlayerProfile(playerId);
        if (tmPlayer == null)
        {
            throw new Exception($"Player not found: {playerId}");
        }
        
        _data.Players[playerId] = tmPlayer;

        return tmPlayer;
    }

    private async Task<Node> EnsureClubNodeCreated(string clubId, string clubName, Pack pack)
    {
        if (_excludeRegex.IsMatch(clubName)) return null;

        if (_clubRetiredRegex.IsMatch(clubName))
        {
            var retired = EnsureNodeCreated("Retired", TagOther, pack);
            return retired;
        }

        var team = _teams.FirstOrDefault(x => x.Id == clubId);
        if (team == null) return null;

        var tmClub = await GetClubCached(clubId);

        if (_excludeRegex.IsMatch(tmClub.Name)) return null;

        // var wlClubName = _clubsWhitelist.FirstOrDefault(x => tmClub.Name.Contains(x, StringComparison.InvariantCultureIgnoreCase));
        // if (wlClubName == null) return null;

        var node = pack.Nodes.FirstOrDefault(x => x.Name == team.TeamName && x.HasTag(TagClub) && x.DeletedAt == null);
        if (node != null) return node;

        node = new Node
        {
            Pack = pack,
            Name = team.TeamName,
            ImageUrl = tmClub.Image,
        };
        node.NodeTags.Add(new NodeTag
        {
            Node = node,
            Tag = pack.Tags.First(x => x.Name == TagClub)
        });
        pack.Nodes.Add(node);

        node.Meta ??= new Meta();
        var props = node.Meta.PropertiesDict;
        props["type"] = "club";
        props["tm_club_id"] = tmClub.Id;
        props["tm_club_name"] = tmClub.Name;
        props["tm_club_url"] = tmClub.Url;
        props["tm_updated_at"] = tmClub.UpdatedAt.ToString("u");
        node.Meta.PropertiesDict = props;

        _logger.LogInformation("Created club node: {name}", tmClub.Name);

        if (tmClub.League != null)
        {
            var leagueNode = EnsureLeagueNodeCreated(tmClub.League, pack);
            if (leagueNode != null)
                EnsureLinkCreated(node, leagueNode, LinkClubLeague, pack);
        }

        return node;
    }

    private Node EnsureLeagueNodeCreated(League league, Pack pack)
    {
        var wlLeague = _leagues.FirstOrDefault(x => x.Id == league.Id);
        if (wlLeague == null) return null;

        var leagueName = wlLeague.LeagueName;
        var node = pack.Nodes.FirstOrDefault(x => x.Name == leagueName && x.HasTag(TagLeague) && x.DeletedAt == null);
        if (node != null) return node;

        node = new Node
        {
            Pack = pack,
            Name = leagueName,
        };
        node.NodeTags.Add(new NodeTag
        {
            Node = node,
            Tag = pack.Tags.First(x => x.Name == TagLeague)
        });
        pack.Nodes.Add(node);

        node.Meta ??= new Meta();
        var props = node.Meta.PropertiesDict;
        props["type"] = "league";
        props["tm_league_id"] = league.Id;
        props["tm_league_name"] = league.Name;
        if (league.CountryID != null)
            props["tm_league_country_id"] = league.CountryID;
        if (league.CountryName != null)
            props["tm_league_country_name"] = league.CountryName;
        if (league.Tier != null)
            props["tm_league_tier"] = league.Tier;
        node.Meta.PropertiesDict = props;

        _logger.LogInformation("Created league node: {name}", league.Name);

        return node;
    }

    private Node EnsureCountryNodeCreated(string countryName, Pack pack)
    {
        var countryMap = _countries.FirstOrDefault(x =>
            string.Equals(x.Country, countryName, StringComparison.InvariantCultureIgnoreCase));
        if (countryMap == null) return null;

        var node = pack.Nodes.FirstOrDefault(x => x.Name == countryName && x.HasTag(TagCountry) && x.DeletedAt == null);
        if (node != null) return node;

        node = new Node
        {
            Pack = pack,
            Name = countryName,
        };
        node.NodeTags.Add(new NodeTag
        {
            Node = node,
            Tag = pack.Tags.First(x => x.Name == TagCountry)
        });
        pack.Nodes.Add(node);

        if (!string.IsNullOrEmpty(countryMap.Region))
        {
            var regionNode = EnsureRegionNodeCreated(countryMap.Region, pack);
            EnsureLinkCreated(node, regionNode, LinkCountryRegion, pack);
            node.NodeTags.Add(new NodeTag
            {
                Node = node,
                Tag = pack.Tags.First(x => x.Name == TagIgnore)
            });
        }

        _logger.LogInformation("Created country node: {name}", countryName);

        return node;
    }

    private Node EnsureRegionNodeCreated(string regionName, Pack pack)
    {
        var node = pack.Nodes.FirstOrDefault(x => x.Name == regionName && x.HasTag(TagRegion) && x.DeletedAt == null);
        if (node != null) return node;

        node = new Node
        {
            Pack = pack,
            Name = regionName,
        };
        node.NodeTags.Add(new NodeTag
        {
            Node = node,
            Tag = pack.Tags.First(x => x.Name == TagRegion)
        });
        pack.Nodes.Add(node);

        _logger.LogInformation("Created region node: {name}", regionName);

        return node;
    }

    private Node EnsureTrophyNodeCreated(string trophyName, Pack pack)
    {
        if (_excludeRegex.IsMatch(trophyName)) return null;
        var wlName =
            _trophyWhitelist.FirstOrDefault(x => trophyName.Equals(x, StringComparison.InvariantCultureIgnoreCase));
        if (wlName == null) return null;

        var mergeName = _trophyMerge.GetValueOrDefault(wlName);

        var name = mergeName ?? wlName;

        var node = pack.Nodes.FirstOrDefault(x => x.Name == name && x.HasTag(TagTrophy) && x.DeletedAt == null);
        if (node != null) return node;

        node = new Node
        {
            Pack = pack,
            Name = name,
        };
        node.NodeTags.Add(new NodeTag
        {
            Node = node,
            Tag = pack.Tags.First(x => x.Name == TagTrophy)
        });
        pack.Nodes.Add(node);

        _logger.LogInformation("Created trophy node: {name}", name);

        return node;
    }

    private async Task ImportPlayers(Pack pack, bool createOnly)
    {
        var checkpoint = 0;
        var foundPlayers = _players.Where(x => !string.IsNullOrEmpty(x.PlayerId)).ToList();
        foreach (var (player, idx) in foundPlayers.Select((player, idx) => (player, idx)))
        {
            _logger.LogInformation("[{idx}/{count}] Processing player {name}", idx, foundPlayers.Count,
                player.Name);
            try
            {
                var updated = await ImportPlayer(player, pack, createOnly);
                if (updated)
                {
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Saved player node: {name}", player.Name);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception");
            }
            finally
            {
                if (checkpoint == 25 || player == foundPlayers.Last())
                {
                    SaveDataJson();
                    checkpoint = 0;
                }

                checkpoint++;
            }
        }
    }

    private void SaveDataJson()
    {
        var json = JsonConvert.SerializeObject(_data);
        var path = FilePath("TmData.json");
        File.WriteAllText(path, json);
    }

    private static TransfermarktData LoadDataJson()
    {
        var path = FilePath("TmData.json");
        if (!File.Exists(path))
        {
            return new TransfermarktData();
        }
        
        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<TransfermarktData>(json);
    }

    private async Task<bool> ImportPlayer(TmPlayerModel player, Pack pack, bool createOnly)
    {
        var node = pack.Nodes.FirstOrDefault(x => x.HasTag(TagPlayer) && x.Meta?.Properties?.FirstOrDefault(p =>
            p.Key == "tm_player_id" && p.Value == player.PlayerId) != null);
        if (node != null && (createOnly || node.DeletedAt != null)) return false;

        if (node == null)
        {
            // throw new Exception("Player not found");
            node = new Node
            {
                Name = player.Name,
                Pack = pack
            };
            node.NodeTags.Add(new NodeTag
            {
                Node = node,
                Tag = pack.Tags.First(x => x.Name == TagPlayer)
            });
            pack.Nodes.Add(node);
            _logger.LogInformation("Created player node: {name}", player.Name);
        }

        await UpdatePlayer(player, pack, node);
        await UpdatePlayerTransfers(player, pack, node);
        await UpdatePlayerAchievements(player, pack, node);
        await UpdatePlayerStats(player, pack, node);
        return true;
    }

    private async Task UpdatePlayerAchievements(TmPlayerModel player, Pack pack, Node node)
    {
        if (!_data.PlayerAchievements.TryGetValue(player.PlayerId, out var tmAchievements))
        {
            tmAchievements = await _api.GetPlayerAchievements(player.PlayerId);
            if (tmAchievements == null)
            {
                throw new Exception($"Player achievements not found: {player.PlayerId} {player.Name}");
            }

            _data.PlayerAchievements[player.PlayerId] = tmAchievements;
        }

        if (tmAchievements.Achievements?.Count > 0)
        {
            foreach (var tmTrophy in tmAchievements.Achievements)
            {
                var trophyNode = EnsureTrophyNodeCreated(tmTrophy.Title, pack);
                if (trophyNode != null)
                {
                    var trophyLink = EnsureLinkCreated(node, trophyNode, LinkPlayerTrophy, pack);

                    if (trophyLink != null)
                    {
                        trophyLink.Meta ??= new Meta();
                        var linkProps = trophyLink.Meta.PropertiesDict;
                        linkProps["type"] = "trophy";
                        linkProps["tm_trophy_title"] = tmTrophy.Title;
                        linkProps["tm_trophy_count"] = tmTrophy.Count.ToString();
                        linkProps["tm_updated_at"] = tmAchievements.UpdatedAt.ToString("u");
                        trophyLink.Meta.PropertiesDict = linkProps;
                    }
                }
            }
        }
    }

    private async Task UpdatePlayerTransfers(TmPlayerModel player, Pack pack, Node node)
    {
        var tmTransfers = await GetTransfersCached(player.PlayerId);

        if (tmTransfers.Transfers?.Count > 0)
        {
            foreach (var tmTransfer in tmTransfers.Transfers)
            {
                var clubFromNode = await EnsureClubNodeCreated(tmTransfer.From.ClubID, tmTransfer.From.ClubName, pack);
                if (clubFromNode != null)
                {
                    var link = clubFromNode.HasTag(TagClub) ? LinkPlayerClub : LinkPlayerOther;
                    var clubFromLink = EnsureLinkCreated(node, clubFromNode, link, pack);
                    if (clubFromLink != null)
                    {
                        var linkProps = clubFromLink.Meta.PropertiesDict;
                        linkProps["type"] = "transfer";
                        linkProps["tm_transfer_club_id_to"] = tmTransfer.To.ClubID;
                        linkProps["tm_transfer_club_name_to"] = tmTransfer.To.ClubName;
                        linkProps["tm_transfer_club_date_end"] = tmTransfer.Date;
                        linkProps["tm_transfer_season_from"] = tmTransfer.Season;
                        linkProps["tm_transfer_upcoming_from"] = tmTransfer.Upcoming.ToString();
                        linkProps["tm_updated_at"] = tmTransfers.UpdatedAt.ToString("u");
                        clubFromLink.Meta.PropertiesDict = linkProps;
                    }
                    
                }
                else if (!_excludeRegex.IsMatch(tmTransfer.From.ClubName) &&
                         !_clubRetiredRegex.IsMatch(tmTransfer.From.ClubName))
                {
                    await EnsurePlayerLeagueLinked(tmTransfer.From.ClubID, pack, node);
                }


                var clubToNode = await EnsureClubNodeCreated(tmTransfer.To.ClubID, tmTransfer.To.ClubName, pack);
                if (clubToNode != null)
                {
                    var link = clubToNode.HasTag(TagClub) ? LinkPlayerClub : LinkPlayerOther;
                    var clubToLink = EnsureLinkCreated(node, clubToNode, link, pack);

                    if (clubToLink != null)
                    {
                        clubToLink.Meta ??= new Meta();
                        var linkProps = clubToLink.Meta.PropertiesDict;
                        linkProps["type"] = "transfer";
                        linkProps["tm_transfer_id"] = tmTransfer.Id;
                        linkProps["tm_transfer_club_id_from"] = tmTransfer.From.ClubID;
                        linkProps["tm_transfer_club_name_from"] = tmTransfer.From.ClubName;
                        linkProps["tm_transfer_club_date_start"] = tmTransfer.Date;
                        linkProps["tm_transfer_season_to"] = tmTransfer.Season;
                        linkProps["tm_transfer_upcoming_to"] = tmTransfer.Upcoming.ToString();
                        if (tmTransfer.Fee != null &&
                            !tmTransfer.Fee.Contains("free", StringComparison.InvariantCultureIgnoreCase))
                            linkProps["tm_transfer_fee"] = tmTransfer.Fee;
                        if (tmTransfer.MarketValue != null)
                            linkProps["tm_transfer_market_value"] = tmTransfer.MarketValue;
                        linkProps["tm_updated_at"] = tmTransfers.UpdatedAt.ToString("u");
                        clubToLink.Meta.PropertiesDict = linkProps;
                    }
                }
                else if (!_excludeRegex.IsMatch(tmTransfer.To.ClubName) &&
                         !_clubRetiredRegex.IsMatch(tmTransfer.To.ClubName))
                {
                    await EnsurePlayerLeagueLinked(tmTransfer.To.ClubID, pack, node);
                }
            }
        }
    }

    private async Task UpdatePlayer(TmPlayerModel player, Pack pack, Node node)
    {
        var tmPlayer = await GetPlayerProfileCached(player.PlayerId);

        var clubId = tmPlayer.Club?.Id ?? tmPlayer.Club?.LastClubId;
        var clubName = tmPlayer.Club?.Name ?? tmPlayer.Club?.LastClubName;

        if (string.IsNullOrEmpty(node.ImageUrl))
        {
            node.ImageUrl = tmPlayer.ImageURL;
        }

        if (string.IsNullOrEmpty(node.Name))
        {
            node.Name = tmPlayer.Name;
        }

        node.Meta ??= new Meta();
        var props = node.Meta.PropertiesDict;
        props["type"] = "player";
        props["tm_player_id"] = tmPlayer.Id;
        props["tm_player_url"] = tmPlayer.Url;
        props["tm_player_name"] = tmPlayer.Name;
        props["tm_player_dob"] = tmPlayer.DateOfBirth;
        props["tm_player_age"] = tmPlayer.Age;
        props["tm_player_citizenship"] =
            tmPlayer.Citizenship?.Count > 0 ? string.Join(",", tmPlayer.Citizenship) : string.Empty;
        props["tm_player_is_retired"] = tmPlayer.IsRetired.ToString();
        props["tm_player_club_id"] = clubId;
        props["tm_player_club_name"] = clubName;
        if (tmPlayer.MarketValue != null)
            props["tm_player_market_value"] = tmPlayer.MarketValue;
        if (tmPlayer.Position?.Main != null)
            props["tm_player_position_main"] = tmPlayer.Position?.Main;
        if (tmPlayer.Position?.Other?.Count > 0)
            props["tm_player_position_other"] = string.Join(",", tmPlayer.Position.Other);
        if (tmPlayer.Club?.Joined != null)
            props["tm_player_club_joined"] = tmPlayer.Club.Joined;
        if (tmPlayer.Club?.ContractExpires != null)
            props["tm_player_club_contract_expires"] = tmPlayer.Club.ContractExpires;
        props["tm_updated_at"] = tmPlayer.UpdatedAt.ToString("u");
        node.Meta.PropertiesDict = props;

        if (tmPlayer.Club != null)
        {
            var clubNode = await EnsureClubNodeCreated(clubId, clubName, pack);
            if (clubNode != null)
            {
                var link = clubNode.HasTag(TagClub) ? LinkPlayerClub : LinkPlayerOther;
                EnsureLinkCreated(node, clubNode, link, pack);
            }
        }

        if (tmPlayer.Citizenship?.Count > 0)
        {
            var country = tmPlayer.Citizenship[0];
            var countryNode = EnsureCountryNodeCreated(country, pack);
            if (countryNode != null)
            {
                EnsureLinkCreated(node, countryNode, LinkPlayerCountry, pack);
            }
        }
    }

    private async Task UpdatePlayerStats(TmPlayerModel player, Pack pack, Node node)
    {
        var tmStats = await GetStatsCached(player.PlayerId);

        foreach (var stat in tmStats.Stats.Where(x =>
                     !string.IsNullOrEmpty(x.CompetitionID) && !string.IsNullOrEmpty(x.ClubID)))
        {
            var league = _leagues.FirstOrDefault(x => x.Id == stat.CompetitionID || x.Id2 == stat.CompetitionID);
            if (league != null)
            {
                var leagueModel = new League
                {
                    Id = league.Id,
                    Name = league.LeagueName,
                };

                var leagueNode = EnsureLeagueNodeCreated(leagueModel, pack);
                EnsureLinkCreated(node, leagueNode, LinkPlayerLeague, pack);
            }

            var club = _teams.FirstOrDefault(x => x.Id == stat.ClubID);
            if (club != null)
            {
                var clubNode = await EnsureClubNodeCreated(stat.ClubID, string.Empty, pack);
                EnsureLinkCreated(node, clubNode, LinkPlayerClub, pack);
            }
            else
            {
                await EnsurePlayerLeagueLinked(stat.ClubID, pack, node);
            }
        }
    }

    private NodeLink EnsureLinkCreated(Node nodeFrom, Node nodeTo, string linkType, Pack pack, bool restoreDeleted = false)
    {
        if (nodeFrom == null || nodeTo == null || nodeFrom.DeletedAt != null || nodeTo.DeletedAt != null) 
            return null;
        
        var link = nodeFrom.NodeLinksFrom.FirstOrDefault(x => x.NodeTo.Id == nodeTo.Id);
        
        if (link?.DeletedAt != null && !restoreDeleted) 
            return null;
        
        if (link == null)
        {
            link = new NodeLink
            {
                NodeLinkType = pack.NodeLinkTypes.First(x => x.Name == linkType),
                NodeFrom = nodeFrom,
                NodeTo = nodeTo,
            };
            nodeFrom.NodeLinksFrom.Add(link);

            _logger.LogInformation("Created node link: {from} {to} {type}", nodeFrom.Name, nodeTo.Name, linkType);
        }

        if (restoreDeleted && link.DeletedAt != null && link.DeletedByUserId == null)
        {
            link.DeletedAt = null;
        }

        return link;
    }

    private async Task EnsurePlayerLeagueLinked(string clubId, Pack pack, Node node)
    {
        var team = _teams.FirstOrDefault(x => x.Id == clubId);
        if (team != null) return;

        var tmClub = await GetClubCached(clubId);

        if (tmClub?.Name == null || _excludeRegex.IsMatch(tmClub.Name)) return;

        if (tmClub.League == null) return;
        
        if (_excludePlayerClubLeagues.Contains(tmClub.League.Name)) return;

        var leagueNode = EnsureLeagueNodeCreated(tmClub.League, pack);
        if (leagueNode != null)
            EnsureLinkCreated(node, leagueNode, LinkPlayerLeague, pack);
    }

    private List<T> ReadCsv<T>(string path)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            TrimOptions = TrimOptions.Trim,
            HeaderValidated = null,
            MissingFieldFound = null,
        };
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fs);
        using var csv = new CsvReader(reader, config);

        var result = csv.GetRecords<T>().ToList();
        return result;
    }

    private void SaveCsv<T>(List<T> data, string path)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
        };
        using var writer = new StreamWriter(path);
        using var csv = new CsvWriter(writer, config);
        csv.WriteRecords(data);
    }

    private long ParseMarketValue(string input)
    {
        var inputStr = input.Replace("k", "000").Replace("m", "0000");
        long result = 0;

        var num = new string(inputStr.Where(x => char.IsDigit(x)).ToArray());

        if (num.Length > 0)
        {
            result = long.Parse(num);
        }

        return result;
    }

    [GeneratedRegex("without club|youth|unknown|u\\d+|under|academ|akadem|jgd|jugend|jong|^ban$",
        RegexOptions.IgnoreCase)]
    private static partial Regex ExcludeRegex();

    [GeneratedRegex("retired|career break", RegexOptions.IgnoreCase)]
    private static partial Regex ClubRetiredRegex();

    [GeneratedRegex("^(.+) (B|C|D|II|III|IV|2|3|4)$", RegexOptions.IgnoreCase)]
    private static partial Regex ClubDuplicatesRegex();
}