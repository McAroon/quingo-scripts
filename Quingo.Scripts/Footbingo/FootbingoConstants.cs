namespace Quingo.Scripts.Footbingo;

public static class FootbingoConstants
{
    public const FootbingoUpdateFlagsEnum ImportPlayersFlags = FootbingoUpdateFlagsEnum.UpdatePlayerTransfers |
                                                             FootbingoUpdateFlagsEnum.UpdatePlayerAchievements |
                                                             FootbingoUpdateFlagsEnum.UpdatePlayerStats;

    public const string PackName = "Footbingo";

    public const string TagPlayer = "player";
    public const string TagClub = "club";
    public const string TagCountry = "country";
    public const string TagTrophy = "trophy";
    public const string TagLeague = "league";
    public const string TagRegion = "region";
    public const string TagIgnore = "ignore";
    public const string TagOther = "other";
    public const string TagManager = "manager";
    public const string TagTeammate = "teammate";

    public const string LinkPlayerClub = "played in a club";
    public const string LinkPlayerCountry = "has citizenship";
    public const string LinkPlayerTrophy = "won a trophy";
    public const string LinkClubLeague = "league member";
    public const string LinkCountryRegion = "part of region";
    public const string LinkPlayerLeague = "played in a league";
    public const string LinkPlayerOther = "linked to";
    public const string LinkPlayerManager = "played under management";
    public const string LinkPlayerTeammate = "played with";

    // Excel sheet names
    public const string SheetPlayers = "Players";
    public const string SheetCountries = "Countries";
    public const string SheetOther = "Other";
    public const string SheetTeams = "Teams";
    public const string SheetLeagues = "Leagues";
    public const string SheetManagers = "Managers";
    public const string SheetTeammates = "Teammates";
    public const string SheetMarketValue50mPlus = "MarketValue50m+";
    public const string SheetMedalists = "Medalists";
    public const string SheetCoefficients = "Coefficients";
    public const string Sheet100Caps = "100Caps";
    public const string Sheet100GamesInCL = "100GamesInCL";
    public const string SheetTrebleWinners = "TrebleWinners";
    public const string SheetTop5LeaguesGoalscorers = "Top5LeaguesGoalscorers";
    public const string SheetDynasty = "Dynasty";

    public static IReadOnlyList<string> Tags { get; } =
    [
        TagPlayer, TagClub, TagCountry, TagTrophy, TagLeague, TagRegion, TagIgnore,
        TagOther, TagManager, TagTeammate
    ];

    public static IReadOnlyList<string> LinkTypes { get; } =
    [
        LinkPlayerClub, LinkPlayerCountry, LinkPlayerTrophy, LinkClubLeague,
        LinkCountryRegion, LinkPlayerLeague, LinkPlayerOther, LinkPlayerManager, LinkPlayerTeammate
    ];


    public static IReadOnlyList<string> TrophyWhitelist { get; } =
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
        // "Top goal scorer",
        "Winner Ballon d'Or",
        "Euro runner-up",
        "World Cup runner-up",
        "World Cup third place",
        "Italian cup winner",
        "Spanish cup winner",
        "German cup winner",
        "English FA Cup winner",
    ];

    public static IReadOnlyDictionary<string, string> TrophyMerge { get; } = new Dictionary<string, string>
    {
        ["European Champion Clubs' Cup winner"] = "Champions League winner",
        ["Uefa Cup winner"] = "Europa League winner",
        ["Euro runner-up"] = "WC/Euro medalist",
        ["World Cup runner-up"] = "WC/Euro medalist",
        ["World Cup third place"] = "WC/Euro medalist",
        ["Winner Ballon d'Or"] = "Ballon d'Or winner",
    };

    public static IReadOnlyList<string> ExcludePlayerClubLeagues { get; } = ["Championship"];
}