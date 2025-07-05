using Quingo.Scripts.Transfermarkt.Dto;
using StackExchange.Redis;

namespace Quingo.Scripts.Transfermarkt;

public class TransfermarktRedisService : ITransfermarktService
{
    private readonly TransfermarktClient _api;
    private readonly RedisCacheService _redis;

    public TransfermarktRedisService(TransfermarktClient api, RedisCacheService redis)
    {
        _api = api;
        _redis = redis;
    }

    private static string RedisKey(string type, string id) => $"tm:{type}:{id}";

    public async Task<ClubProfileResponse> GetClub(string clubId)
    {
        var key = RedisKey("club", clubId);
        var tmClub = await _redis.Get<ClubProfileResponse>(key);
        if (tmClub != null) return tmClub;

        tmClub = await _api.GetClubProfile(clubId);
        if (tmClub == null)
        {
            return null;
        }

        await _redis.Set(key, tmClub);

        return tmClub;
    }

    public async Task<PlayerStatsResponse> GetStats(string playerId)
    {
        var key = RedisKey("stats", playerId);
        var tmStats = await _redis.Get<PlayerStatsResponse>(key);
        if (tmStats != null) return tmStats;

        tmStats = await _api.GetPlayerStats(playerId);
        if (tmStats == null)
        {
            throw new Exception($"Player stats not found: {playerId}");
        }

        await _redis.Set(key, tmStats);

        return tmStats;
    }

    public async Task<PlayerTransfersResponse> GetTransfers(string playerId)
    {
        var key = RedisKey("transfers", playerId);
        var tmTransfers = await _redis.Get<PlayerTransfersResponse>(key);
        if (tmTransfers != null) return tmTransfers;

        tmTransfers = await _api.GetPlayerTransfers(playerId);
        if (tmTransfers == null)
        {
            throw new Exception($"Player transfers not found: {playerId}");
        }

        await _redis.Set(key, tmTransfers);

        return tmTransfers;
    }

    public async Task<PlayerProfileResponse> GetPlayerProfile(string playerId)
    {
        var key = RedisKey("player", playerId);
        var tmPlayer = await _redis.Get<PlayerProfileResponse>(key);
        if (tmPlayer != null) return tmPlayer;

        tmPlayer = await _api.GetPlayerProfile(playerId);
        if (tmPlayer == null)
        {
            throw new Exception($"Player not found: {playerId}");
        }

        await _redis.Set(key, tmPlayer);

        return tmPlayer;
    }

    public async Task<PlayerAchievementsResponse> GetPlayerAchievements(string playerId)
    {
        var key = RedisKey("achievements", playerId);
        var tmAchievements = await _redis.Get<PlayerAchievementsResponse>(key);
        if (tmAchievements != null) return tmAchievements;

        tmAchievements = await _api.GetPlayerAchievements(playerId);
        if (tmAchievements == null)
        {
            throw new Exception($"Achievements not found: {playerId}");
        }

        await _redis.Set(key, tmAchievements);

        return tmAchievements;
    }

    public async Task ClearCache()
    {
        await _redis.RemoveAll();
    }
}