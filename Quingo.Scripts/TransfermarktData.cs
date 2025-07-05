using Quingo.Scripts.Transfermarkt.Dto;

namespace Quingo.Scripts;

public class TransfermarktData
{
    public Dictionary<string, ClubProfileResponse> Clubs { get; set; } = [];
    
    public Dictionary<string, PlayerProfileResponse> Players { get; set; } = [];
    
    public Dictionary<string, PlayerTransfersResponse> PlayerTransfers { get; set; } = [];
    
    public Dictionary<string, PlayerAchievementsResponse> PlayerAchievements { get; set; } = [];
    
    public Dictionary<string, PlayerStatsResponse> PlayerStats { get; set; } = [];
}