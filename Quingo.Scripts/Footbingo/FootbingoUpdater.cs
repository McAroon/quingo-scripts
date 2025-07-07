using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quingo.Infrastructure.Database;
using Quingo.Infrastructure.Database.Repos;
using Quingo.Scripts.Excel;
using Quingo.Scripts.Footbingo.Models;
using Quingo.Scripts.Transfermarkt;
using Quingo.Scripts.Transfermarkt.Dto;
using Quingo.Shared.Entities;
using static Quingo.Scripts.Footbingo.FootbingoConstants;
using static Quingo.Scripts.Footbingo.FootbingoRegex;

namespace Quingo.Scripts.Footbingo;

public class FootbingoUpdater
{
    public FootbingoUpdater(ExcelService excelService, ITransfermarktService transfermarktService,
        QuingoImportService quingoImportService, PackRepo repo, IOptions<ScriptsSettings> scriptsSettings,
        ILogger<FootbingoUpdater> logger)
    {
        _excelService = excelService;
        _transfermarktService = transfermarktService;
        _quingoImportService = quingoImportService;
        _repo = repo;
        _logger = logger;
        _scriptsSettings = scriptsSettings.Value;
    }

    private readonly ExcelService _excelService;
    private readonly ITransfermarktService _transfermarktService;
    private readonly QuingoImportService _quingoImportService;
    private readonly PackRepo _repo;
    private readonly ScriptsSettings _scriptsSettings;
    private readonly ILogger<FootbingoUpdater> _logger;

    private FootbingoExcelData _excelData;
    private ApplicationDbContext _db;
    private Pack _pack;

    public async Task Execute()
    {
        await Init();
        // await PopulateCache(false);
        await ImportPlayers();
        await ImportManagers(false);
        await ImportTeammates(false);
        await CreateOther();
    }

    private async Task Init()
    {
        _excelData = _excelService.ReadExcelFile<FootbingoExcelData>(_scriptsSettings.FootbingoExcelFile);
        _db = await _repo.CreateDbContext();
        _pack = await _quingoImportService.CreatePack(_db, PackName, Tags, LinkTypes);
    }

    private async Task PopulateCache(bool clearExistingCache)
    {
        if (clearExistingCache)
        {
            await _transfermarktService.ClearCache();
        }
        
        _logger.LogInformation("Populating cache...");
        var foundPlayers = _excelData.Players.Where(x => !string.IsNullOrEmpty(x.LinkPlayerId)).ToList();

        foreach (var (player, idx) in foundPlayers.Select((player, idx) => (player, idx)))
        {
            _logger.LogInformation("[{idx}/{count}] Populating cache for player {id} {name}", idx + 1,
                foundPlayers.Count,
                player.LinkPlayerId,
                player.Name);
        
            await _transfermarktService.GetPlayerProfile(player.LinkPlayerId);
            await _transfermarktService.GetPlayerAchievements(player.LinkPlayerId);
            await GetPlayerClubs(player.LinkPlayerId);
        }
    }

    private async Task ImportPlayers()
    {
        var foundPlayers = _excelData.Players.Where(x => !string.IsNullOrEmpty(x.LinkPlayerId)).ToList();
        foreach (var (player, idx) in foundPlayers.Select((player, idx) => (player, idx)))
        {
            _logger.LogInformation("[{idx}/{count}] Processing player {id} {name}", idx + 1, foundPlayers.Count,
                player.LinkPlayerId,
                player.Name);
            try
            {
                var updated = await ImportPlayer(player);
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
        }
    }

    private async Task<bool> ImportPlayer(TmPlayerModel player)
    {
        var node = _pack.Nodes.FirstOrDefault(x => x.HasTag(TagPlayer) &&
                                                   x.Meta?.Properties?.FirstOrDefault(p =>
                                                       p.Key == "tm_player_id" && p.Value == player.LinkPlayerId) !=
                                                   null);

        if (node != null &&
            (ImportPlayersFlags.HasFlag(FootbingoUpdateFlagsEnum.CreatePlayersOnly) || node.DeletedAt != null))
            return false;

        var created = false;
        if (node == null)
        {
            node = new Node
            {
                Name = player.Name,
                Pack = _pack
            };
            node.NodeTags.Add(new NodeTag
            {
                Node = node,
                Tag = _pack.Tags.First(x => x.Name == TagPlayer)
            });
            _pack.Nodes.Add(node);
            _logger.LogInformation("Created player node: {name}", player.Name);
            created = true;
        }

        if (created || 
            ImportPlayersFlags.HasFlag(FootbingoUpdateFlagsEnum.CreatePlayersOnly) ||
            ImportPlayersFlags.HasFlag(FootbingoUpdateFlagsEnum.UpdatePlayer))
        {
            await UpdatePlayer(player, node, _pack);
        }

        if (created || 
            ImportPlayersFlags.HasFlag(FootbingoUpdateFlagsEnum.CreatePlayersOnly) ||
            ImportPlayersFlags.HasFlag(FootbingoUpdateFlagsEnum.UpdatePlayerTransfers))
        {
            await UpdatePlayerTransfers(player, node, _pack);
        }

        if (created || 
            ImportPlayersFlags.HasFlag(FootbingoUpdateFlagsEnum.CreatePlayersOnly) ||
            ImportPlayersFlags.HasFlag(FootbingoUpdateFlagsEnum.UpdatePlayerAchievements))
        {
            await UpdatePlayerAchievements(player, node, _pack);
        }

        if (created || 
            ImportPlayersFlags.HasFlag(FootbingoUpdateFlagsEnum.CreatePlayersOnly) ||
            ImportPlayersFlags.HasFlag(FootbingoUpdateFlagsEnum.UpdatePlayerStats))
        {
            await UpdatePlayerStats(player, node, _pack);
        }

        return true;
    }

    private async Task UpdatePlayer(TmPlayerModel player, Node node, Pack pack)
    {
        var tmPlayer = await _transfermarktService.GetPlayerProfile(player.LinkPlayerId);

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
            var clubNode = await CreateClubNode(clubId, clubName, pack);
            if (clubNode != null)
            {
                var link = clubNode.HasTag(TagClub) ? LinkPlayerClub : LinkPlayerOther;
                _quingoImportService.CreateLink(node, clubNode, link, pack);
            }
        }

        if (tmPlayer.Citizenship?.Count > 0)
        {
            var country = tmPlayer.Citizenship[0];
            var countryNode = CreateCountryNode(country, pack);
            if (countryNode != null)
            {
                _quingoImportService.CreateLink(node, countryNode, LinkPlayerCountry, pack);
            }
        }
    }

    private async Task UpdatePlayerTransfers(TmPlayerModel player, Node node, Pack pack)
    {
        var tmTransfers = await _transfermarktService.GetTransfers(player.LinkPlayerId);

        if (tmTransfers.Transfers?.Count > 0)
        {
            foreach (var tmTransfer in tmTransfers.Transfers)
            {
                var clubFromNode = await CreateClubNode(tmTransfer.From.ClubID, tmTransfer.From.ClubName, pack);
                if (clubFromNode != null)
                {
                    var link = clubFromNode.HasTag(TagClub) ? LinkPlayerClub : LinkPlayerOther;
                    var clubFromLink = _quingoImportService.CreateLink(node, clubFromNode, link, pack);
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
                else if (!ExcludeRegex.IsMatch(tmTransfer.From.ClubName) &&
                         !ClubRetiredRegex.IsMatch(tmTransfer.From.ClubName))
                {
                    await CreatePlayerLeagueLink(tmTransfer.From.ClubID, pack, node);
                }


                var clubToNode = await CreateClubNode(tmTransfer.To.ClubID, tmTransfer.To.ClubName, pack);
                if (clubToNode != null)
                {
                    var link = clubToNode.HasTag(TagClub) ? LinkPlayerClub : LinkPlayerOther;
                    var clubToLink = _quingoImportService.CreateLink(node, clubToNode, link, pack);

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
                else if (!ExcludeRegex.IsMatch(tmTransfer.To.ClubName) &&
                         !ClubRetiredRegex.IsMatch(tmTransfer.To.ClubName))
                {
                    await CreatePlayerLeagueLink(tmTransfer.To.ClubID, pack, node);
                }
            }
        }
    }

    private async Task UpdatePlayerAchievements(TmPlayerModel player, Node node, Pack pack)
    {
        var tmAchievements = await _transfermarktService.GetPlayerAchievements(player.LinkPlayerId);

        if (tmAchievements.Achievements?.Count > 0)
        {
            foreach (var tmTrophy in tmAchievements.Achievements)
            {
                var trophyNode = CreateTrophyNode(tmTrophy.Title, pack);
                if (trophyNode != null)
                {
                    var trophyLink = _quingoImportService.CreateLink(node, trophyNode, LinkPlayerTrophy, pack);

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

    private async Task UpdatePlayerStats(TmPlayerModel player, Node node, Pack pack)
    {
        var tmStats = await _transfermarktService.GetStats(player.LinkPlayerId);

        foreach (var stat in tmStats.Stats.Where(x =>
                     !string.IsNullOrEmpty(x.CompetitionID) && !string.IsNullOrEmpty(x.ClubID)))
        {
            var league =
                _excelData.Leagues.FirstOrDefault(x => x.Id == stat.CompetitionID || x.Id2 == stat.CompetitionID);
            if (league != null)
            {
                var leagueModel = new League
                {
                    Id = league.Id,
                    Name = league.LeagueName,
                };

                var leagueNode = CreateLeagueNode(leagueModel, pack);
                _quingoImportService.CreateLink(node, leagueNode, LinkPlayerLeague, pack);
            }

            var club = _excelData.Teams.FirstOrDefault(x => x.Id == stat.ClubID);
            if (club != null)
            {
                var clubNode = await CreateClubNode(stat.ClubID, string.Empty, pack);
                _quingoImportService.CreateLink(node, clubNode, LinkPlayerClub, pack);
            }
            else
            {
                await CreatePlayerLeagueLink(stat.ClubID, pack, node);
            }
        }
    }

    private async Task<Node> CreateClubNode(string clubId, string clubName, Pack pack)
    {
        if (ExcludeRegex.IsMatch(clubName)) return null;

        if (ClubRetiredRegex.IsMatch(clubName))
        {
            var retired = _quingoImportService.CreateNode("Retired", TagOther, pack);
            return retired;
        }

        var team = _excelData.Teams.FirstOrDefault(x => x.Id == clubId);
        if (team == null) return null;

        var tmClub = await _transfermarktService.GetClub(clubId);

        if (ExcludeRegex.IsMatch(tmClub.Name)) return null;

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
            var leagueNode = CreateLeagueNode(tmClub.League, pack);
            if (leagueNode != null)
                _quingoImportService.CreateLink(node, leagueNode, LinkClubLeague, pack);
        }

        return node;
    }

    private Node CreateLeagueNode(League league, Pack pack)
    {
        var wlLeague = _excelData.Leagues.FirstOrDefault(x => x.Id == league.Id);
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

    private Node CreateCountryNode(string countryName, Pack pack)
    {
        var countryMap = _excelData.Countries.FirstOrDefault(x =>
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
            var regionNode = CreateRegionNode(countryMap.Region, pack);
            _quingoImportService.CreateLink(node, regionNode, LinkCountryRegion, pack);
            node.NodeTags.Add(new NodeTag
            {
                Node = node,
                Tag = pack.Tags.First(x => x.Name == TagIgnore)
            });
        }

        _logger.LogInformation("Created country node: {name}", countryName);

        return node;
    }

    private Node CreateRegionNode(string regionName, Pack pack)
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

    private Node CreateTrophyNode(string trophyName, Pack pack)
    {
        if (ExcludeRegex.IsMatch(trophyName)) return null;
        var wlName =
            TrophyWhitelist.FirstOrDefault(x => trophyName.Equals(x, StringComparison.InvariantCultureIgnoreCase));
        if (wlName == null) return null;

        var mergeName = TrophyMerge.GetValueOrDefault(wlName);

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

    private async Task CreatePlayerLeagueLink(string clubId, Pack pack, Node node)
    {
        var team = _excelData.Teams.FirstOrDefault(x => x.Id == clubId);
        if (team != null) return;

        var tmClub = await _transfermarktService.GetClub(clubId);

        if (tmClub?.Name == null || ExcludeRegex.IsMatch(tmClub.Name)) return;

        if (tmClub.League == null) return;

        if (ExcludePlayerClubLeagues.Contains(tmClub.League.Name)) return;

        var leagueNode = CreateLeagueNode(tmClub.League, pack);
        if (leagueNode != null)
            _quingoImportService.CreateLink(node, leagueNode, LinkPlayerLeague, pack);
    }

    private async Task ImportManagers(bool clearExistingLinks)
    {
        if (clearExistingLinks)
        {
            await ClearLinks(LinkPlayerManager);
        }

        var managers = _excelData.Managers.GroupBy(x => x.Manager);

        foreach (var manager in managers)
        {
            var managerNode = _quingoImportService.CreateNode(manager.Key, TagManager, _pack);
            foreach (var player in manager)
            {
                var playerNode = _pack.Nodes.FirstOrDefault(x => x.HasTag(TagPlayer) && x.DeletedAt == null
                    && x.Meta.PropertiesDict["tm_player_id"] ==
                    player.PlayerId);
                if (playerNode == null) continue;

                _quingoImportService.CreateLink(playerNode, managerNode, LinkPlayerManager, _pack, true);
            }

            await _db.SaveChangesAsync();
        }
    }

    private async Task ImportTeammates(bool clearExistingLinks)
    {
        if (clearExistingLinks)
        {
            await ClearLinks(LinkPlayerTeammate);
        }

        var tmates = _excelData.Teammates.Where(x => !string.IsNullOrEmpty(x.Url))
            .GroupBy(x => x.Teammate);

        foreach (var tmate in tmates)
        {
            var tmateNode =
                _pack.Nodes.First(x => x.HasTag(TagPlayer) && x.DeletedAt == null && x.Name == tmate.Key.Trim());

            if (!tmateNode.HasTag(TagTeammate))
            {
                tmateNode.NodeTags.Add(new NodeTag
                {
                    Node = tmateNode,
                    Tag = _pack.Tags.First(x => x.Name == TagTeammate)
                });
            }

            foreach (var player in tmate)
            {
                var playerNode = _pack.Nodes.FirstOrDefault(x => x.HasTag(TagPlayer) && x.DeletedAt == null
                    && x.Meta?.PropertiesDict.TryGetValue("tm_player_id",
                        out var playerId) == true
                    && playerId == player.PlayerId);
                if (playerNode == null) continue;

                _quingoImportService.CreateLink(playerNode, tmateNode, LinkPlayerTeammate, _pack, true);
            }

            await _db.SaveChangesAsync();
        }
    }

    private async Task ClearLinks(string linkName)
    {
        var links = await _db.NodeLinks.Where(x => x.NodeLinkType.PackId == _pack.Id && x.NodeLinkType.Name == linkName)
            .ToListAsync();
        _db.RemoveRange(links);
        await _db.SaveChangesAsync();
        _pack = await _repo.GetPack(_db, _pack.Id, true);
    }

    private async Task CreateOther()
    {
        await CreateGroups();
        await Create1Club(false);
        await Create1Country(false);
        await Create10Clubs();
        await CreateStatsOther();
        await CreateProfileOther();
        await Create50Mil(true);
        await CreatePlayerUrlLists();

        return;

        async Task CreateGroups()
        {
            _logger.LogInformation("Creating groups other");
            foreach (var entry in _excelData.GroupsOther)
            {
                var player = _pack.Nodes.FirstOrDefault(x => x.HasTag(TagPlayer) && x.DeletedAt == null
                    && string.Compare(x.Name, entry.PlayerName,
                        CultureInfo.CurrentCulture,
                        CompareOptions.IgnoreNonSpace |
                        CompareOptions.IgnoreCase) == 0);
                if (player == null)
                {
                    continue;
                }

                var groupNode = _quingoImportService.CreateNode(entry.GroupName, TagOther, _pack);
                _quingoImportService.CreateLink(player, groupNode, LinkPlayerOther, _pack);
            }

            await _db.SaveChangesAsync();
        }

        async Task Create1Club(bool recreate = false)
        {
            _logger.LogInformation("Creating 1 club other");
            var node = _quingoImportService.CreateNode("1 Club", TagOther, _pack);
            if (recreate)
            {
                node.NodeLinksTo.RemoveAll(_ => true);
                await _db.SaveChangesAsync();
            }

            var players = _pack.Nodes.Where(x => x.HasTag(TagPlayer) && x.DeletedAt == null
                                                                     && x.NodeLinksFrom.Where(l => l.DeletedAt == null)
                                                                         .Count(l => l.NodeTo.HasTag(TagClub)) < 2);
            foreach (var player in players)
            {
                var playerId = player.Meta.PropertiesDict["tm_player_id"];
                var clubs = await GetPlayerClubs(playerId);
                if (clubs.Count() == 1)
                {
                    _quingoImportService.CreateLink(player, node, LinkPlayerOther, _pack);
                }
            }

            await _db.SaveChangesAsync();
        }

        async Task Create1Country(bool recreate = false)
        {
            _logger.LogInformation("Creating 1 country other");
            var node = _quingoImportService.CreateNode("1 Country", TagOther, _pack);
            if (recreate)
            {
                node.NodeLinksTo.RemoveAll(_ => true);
                await _db.SaveChangesAsync();
                return;
            }

            var players = _pack.Nodes.Where(x => x.HasTag(TagPlayer) && x.DeletedAt == null);
            foreach (var player in players)
            {
                var playerId = player.Meta.PropertiesDict["tm_player_id"];
                var clubs = await GetPlayerClubs(playerId);
                var countries = clubs
                    .Select(x => x.League?.CountryName ?? x.AddressLine3).Where(x => !string.IsNullOrEmpty(x))
                    .Distinct();
                if (countries.Count() == 1)
                {
                    _quingoImportService.CreateLink(player, node, LinkPlayerOther, _pack);
                }
            }

            await _db.SaveChangesAsync();
        }

        async Task Create10Clubs()
        {
            _logger.LogInformation("Creating 10 clubs other");
            var node = _quingoImportService.CreateNode("10+ Clubs", TagOther, _pack);

            var players = _pack.Nodes.Where(x => x.HasTag(TagPlayer) && x.DeletedAt == null);
            foreach (var player in players)
            {
                var playerId = player.Meta.PropertiesDict["tm_player_id"];
                var clubs = await GetPlayerClubs(playerId);
                if (clubs.Count() >= 10)
                {
                    _quingoImportService.CreateLink(player, node, LinkPlayerOther, _pack);
                }
            }

            await _db.SaveChangesAsync();
        }

        async Task CreateStatsOther()
        {
            _logger.LogInformation("Creating stats other");
            var assNode = _quingoImportService.CreateNode("100+ Assists", TagOther, _pack);
            var goalNode = _quingoImportService.CreateNode("300+ Goals", TagOther, _pack);

            var players = _pack.Nodes.Where(x => x.HasTag(TagPlayer) && x.DeletedAt == null);
            foreach (var player in players)
            {
                var playerId = player.Meta.PropertiesDict["tm_player_id"];
                var stats = await _transfermarktService.GetStats(playerId);

                var assCount = stats.Stats?.Where(x => !string.IsNullOrEmpty(x?.Assists))
                    .Select(x => int.TryParse(x.Assists, out var i) ? i : 0).Sum() ?? 0;
                var goalCount = stats.Stats?.Where(x => !string.IsNullOrEmpty(x?.Goals))
                    .Select(x => int.TryParse(x.Goals, out var i) ? i : 0).Sum() ?? 0;

                if (assCount >= 100)
                {
                    _quingoImportService.CreateLink(player, assNode, LinkPlayerOther, _pack);
                }

                if (goalCount >= 300)
                {
                    _quingoImportService.CreateLink(player, goalNode, LinkPlayerOther, _pack);
                }
            }

            await _db.SaveChangesAsync();
        }

        async Task CreateProfileOther()
        {
            _logger.LogInformation("Creating profile other");
            var goalNode = _quingoImportService.CreateNode("Goalkeeper", TagOther, _pack);
            var under23Node = _quingoImportService.CreateNode("Under 23", TagOther, _pack);
            var dualCitizenshipNode = _quingoImportService.CreateNode("Dual Citizenship", TagOther, _pack);

            var players = _pack.Nodes.Where(x => x.HasTag(TagPlayer) && x.DeletedAt == null).ToList();
            var playerIds = players.Select(x => x.Meta.PropertiesDict["tm_player_id"]).ToList();
            foreach (var player in players)
            {
                var playerId = player.Meta.PropertiesDict["tm_player_id"];
                var profile = await _transfermarktService.GetPlayerProfile(playerId);

                if (profile.Position?.Main?.Equals("Goalkeeper", StringComparison.InvariantCultureIgnoreCase) == true)
                {
                    _quingoImportService.CreateLink(player, goalNode, LinkPlayerOther, _pack);
                }

                if (!string.IsNullOrEmpty(profile.Age) && int.TryParse(profile.Age, out var age) && age <= 23)
                {
                    _quingoImportService.CreateLink(player, under23Node, LinkPlayerOther, _pack);
                }
                else
                {
                    _quingoImportService.DeleteLink(player, under23Node, LinkPlayerOther);
                }

                if (profile.Citizenship?.Count > 1)
                {
                    _quingoImportService.CreateLink(player, dualCitizenshipNode, LinkPlayerOther, _pack);
                }
            }

            await _db.SaveChangesAsync();
        }

        async Task Create50Mil(bool useExcelData = true)
        {
            _logger.LogInformation("Creating 50Mil other");
            var node = _quingoImportService.CreateNode("50+ Millions", TagOther, _pack);
            var players = _pack.Nodes.Where(x => x.HasTag(TagPlayer) && x.DeletedAt == null);
            foreach (var player in players)
            {
                var playerId = player.Meta.PropertiesDict["tm_player_id"];
                bool create;
                if (useExcelData)
                {
                    create = _excelData.PlayerMarketValues.FirstOrDefault(x => x.PlayerId == playerId) != null;
                }
                else
                {
                    var profile = await _transfermarktService.GetPlayerProfile(playerId);
                    var mvStr = profile.MarketValue;
                    if (string.IsNullOrEmpty(mvStr)) continue;
                    var mv = ParseMarketValue(mvStr);
                    create = mv >= 50_000_000;
                }
                
                if (create)
                {
                    _quingoImportService.CreateLink(player, node, LinkPlayerOther, _pack, true);
                }
                else
                {
                    _quingoImportService.DeleteLink(player, node, LinkPlayerOther);
                }
            }

            await _db.SaveChangesAsync();
        }

        async Task CreatePlayerUrlLists()
        {
            _logger.LogInformation("Creating player url lists other");
            var trebleWinners = _quingoImportService.CreateNode("Treble winner", TagTrophy, _pack);
            var goalscorers = _quingoImportService.CreateNode("Top-5 Top Goalscorer", TagOther, _pack);
            var dynasty = _quingoImportService.CreateNode("Dynasty", TagOther, _pack);
            var medalists = _quingoImportService.CreateNode("WC&EURO Medalist", TagOther, _pack);
            var hCaps = _quingoImportService.CreateNode("100+ Caps", TagOther, _pack);
            var hGamesCl = _quingoImportService.CreateNode("100+ Games in the CL", TagOther, _pack);

            var players = _pack.Nodes.Where(x => x.HasTag(TagPlayer) && x.DeletedAt == null).ToList();
            foreach (var player in players)
            {
                var playerId = player.Meta.PropertiesDict["tm_player_id"];

                if (_excelData.TrebleWinners.FirstOrDefault(x => x.PlayerId == playerId) != null)
                {
                    _quingoImportService.CreateLink(player, trebleWinners, LinkPlayerTrophy, _pack, true);
                }

                if (_excelData.Top5LeaguesGoalscorers.FirstOrDefault(x => x.PlayerId == playerId) != null)
                {
                    _quingoImportService.CreateLink(player, goalscorers, LinkPlayerOther, _pack);
                }

                if (_excelData.Dynasty.FirstOrDefault(x => x.PlayerId == playerId) != null)
                {
                    _quingoImportService.CreateLink(player, dynasty, LinkPlayerOther, _pack);
                }

                if (_excelData.Medalists.FirstOrDefault(x => x.PlayerId == playerId) != null)
                {
                    _quingoImportService.CreateLink(player, medalists, LinkPlayerOther, _pack);
                }

                if (_excelData.HundredCaps.FirstOrDefault(x => x.LinkPlayerId == playerId) != null)
                {
                    _quingoImportService.CreateLink(player, hCaps, LinkPlayerOther, _pack);
                }

                if (_excelData.HundredGamesCl.FirstOrDefault(x => x.LinkPlayerId == playerId) != null)
                {
                    _quingoImportService.CreateLink(player, hGamesCl, LinkPlayerOther, _pack);
                }
            }

            await _db.SaveChangesAsync();
        }

        long ParseMarketValue(string input)
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
    }

    private async Task<IEnumerable<ClubProfileResponse>> GetPlayerClubs(string playerId)
    {
        var stats = await _transfermarktService.GetStats(playerId);
        var statClubIds = stats.Stats?.Where(x => !string.IsNullOrEmpty(x?.ClubID))
            .Select(x => x.ClubID).Distinct() ?? [];

        var transfers = await _transfermarktService.GetTransfers(playerId);

        var transferClubIds = (transfers.Transfers?.Select(x => x.From) ?? [])
            .Concat(transfers.Transfers?.Select(x => x.To) ?? [])
            .Where(x => x != null && !string.IsNullOrEmpty(x.ClubID) && !string.IsNullOrEmpty(x.ClubName)
                        && !ExcludeRegex.IsMatch(x.ClubName) && !ClubDuplicatesRegex.IsMatch(x.ClubName)
                        && !ClubRetiredRegex.IsMatch(x.ClubName))
            .Select(x => x.ClubID).Distinct();

        var clubIds = statClubIds.Concat(transferClubIds).Distinct().ToList();
        var clubs = new List<ClubProfileResponse>();
        foreach (var clubId in clubIds)
        {
            var club = await _transfermarktService.GetClub(clubId);
            if (club != null)
            {
                clubs.Add(club);
            }
        }


        var clubsExcl = clubs
            .Where(x => !string.IsNullOrEmpty(x.Name) && x.Url?.Contains("jugend") != true &&
                        !ExcludeRegex.IsMatch(x.Name)
                        && !ClubDuplicatesRegex.IsMatch(x.Name) && !ClubRetiredRegex.IsMatch(x.Name));
        return clubsExcl;
    }
}