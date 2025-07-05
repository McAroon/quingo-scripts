using Quingo.Scripts.Transfermarkt.Dto;

namespace Quingo.Scripts.Transfermarkt;

public interface ITransfermarktService
{
    Task<ClubProfileResponse> GetClub(string clubId);
    Task<PlayerStatsResponse> GetStats(string playerId);
    Task<PlayerTransfersResponse> GetTransfers(string playerId);
    Task<PlayerProfileResponse> GetPlayerProfile(string playerId);
    Task<PlayerAchievementsResponse> GetPlayerAchievements(string playerId);

    Task ClearCache();
}