using Newtonsoft.Json;
using Quingo.Scripts.Transfermarkt.Dto;

namespace Quingo.Scripts.Transfermarkt;

public class TransfermarktFileService : IDisposable, ITransfermarktService
{
    private readonly FileService _fileService;
    private readonly TransfermarktClient _api;
    private readonly TransfermarktData _data;

    public TransfermarktFileService(FileService fileService, TransfermarktClient api)
    {
        _fileService = fileService;
        _api = api;
        _data = LoadDataJson();
    }

    public async Task<ClubProfileResponse> GetClub(string clubId)
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

    public async Task<PlayerStatsResponse> GetStats(string playerId)
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

    public async Task<PlayerTransfersResponse> GetTransfers(string playerId)
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

    public async Task<PlayerProfileResponse> GetPlayerProfile(string playerId)
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

    public async Task<PlayerAchievementsResponse> GetPlayerAchievements(string playerId)
    {
        if (_data.PlayerAchievements.TryGetValue(playerId, out var tmAchievements))
        {
            return tmAchievements;
        }

        tmAchievements = await _api.GetPlayerAchievements(playerId);
        if (tmAchievements == null)
        {
            throw new Exception($"Achievements not found: {playerId}");
        }

        _data.PlayerAchievements[playerId] = tmAchievements;

        return tmAchievements;
    }

    public Task ClearCache()
    {
        throw new NotImplementedException();
    }

    private TransfermarktData LoadDataJson()
    {
        var json = _fileService.ReadTextFile("TmData.json");
        return JsonConvert.DeserializeObject<TransfermarktData>(json);
    }

    private void SaveDataJson()
    {
        var json = JsonConvert.SerializeObject(_data);
        _fileService.SaveTextFile("TmData.json", json);
    }

    public void Dispose()
    {
        SaveDataJson();
    }
}