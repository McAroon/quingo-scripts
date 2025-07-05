using Quingo.Scripts.Excel;
using Quingo.Scripts.Footbingo.Models;

namespace Quingo.Scripts.Footbingo;

public class FootbingoExcelData : IExcelData
{
    [ExcelSheet(FootbingoConstants.SheetPlayers)]
    public List<TmPlayerModel> Players { get; set; } = [];
    
    [ExcelSheet(FootbingoConstants.SheetCountries)]
    public List<CountryModel> Countries { get; set; } = [];
    
    [ExcelSheet(FootbingoConstants.SheetOther)]
    public List<GroupOtherModel> GroupsOther { get; set; } = [];
    
    [ExcelSheet(FootbingoConstants.SheetTeams)]
    public List<TeamModel> Teams { get; set; } = [];
    
    [ExcelSheet(FootbingoConstants.SheetLeagues)]
    public List<LeagueModel> Leagues { get; set; } = [];
    
    [ExcelSheet(FootbingoConstants.SheetManagers)]
    public List<ManagerPlayerModel> Managers { get; set; } = [];

    [ExcelSheet(FootbingoConstants.SheetTeammates)]
    public List<TeammatePlayerModel> Teammates { get; set; } = [];

    [ExcelSheet(FootbingoConstants.SheetMarketValue50mPlus)]
    public List<PlayerMarketValueModel> PlayerMarketValues { get; set; } = [];
    
    [ExcelSheet(FootbingoConstants.SheetMedalists)]
    public List<MedalistModel> Medalists { get; set; } = [];
    
    [ExcelSheet(FootbingoConstants.SheetCoefficients)]
    public List<NodeDifficultyModel> NodeDifficulty { get; set; } = [];

    [ExcelSheet(FootbingoConstants.Sheet100Caps)]
    public List<TmPlayerModel> HundredCaps { get; set; } = [];
    
    [ExcelSheet(FootbingoConstants.Sheet100GamesInCL)]
    public List<TmPlayerModel> HundredGamesCl { get; set; } = [];
    
    [ExcelSheet(FootbingoConstants.SheetTrebleWinners)]
    public List<PlayerUrlModel> TrebleWinners { get; set; } = [];
    
    [ExcelSheet(FootbingoConstants.SheetTop5LeaguesGoalscorers)]
    public List<PlayerUrlModel> Top5LeaguesGoalscorers { get; set; } = [];
    
    [ExcelSheet(FootbingoConstants.SheetDynasty)]
    public List<PlayerUrlModel> Dynasty { get; set; } = [];
}