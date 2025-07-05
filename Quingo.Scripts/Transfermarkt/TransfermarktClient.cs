using System.Diagnostics;
using Quingo.Scripts.Transfermarkt.Dto;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Quingo.Scripts.Transfermarkt;

public class TransfermarktClient
{
    private readonly ScriptsSettings _settings;
    private readonly HttpClient _httpClient;

    private readonly JsonSerializerOptions _serializerOptions;

    public TransfermarktClient(IOptions<ScriptsSettings> settings)
    {
        _settings = settings.Value;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_settings.TransfermarktApiUrl),
        };
        _serializerOptions = CreateJsonOpts();
    }

    public async Task<SearchPlayerResponse> SearchPlayer(string name)
    {
        using var res = await SendWithRetry(Req, "search");
        if (!CheckResponse(res)) return null;
        var result = await res.Content.ReadFromJsonAsync<SearchPlayerResponse>(_serializerOptions);
        return result;

        HttpRequestMessage Req() => new(HttpMethod.Get, $"/players/search/{name}");
    }

    public async Task<PlayerProfileResponse> GetPlayerProfile(string playerId)
    {
        using var res = await SendWithRetry(Req, "profile");
        if (!CheckResponse(res)) return null;
        var result = await res.Content.ReadFromJsonAsync<PlayerProfileResponse>(_serializerOptions);
        return result;

        HttpRequestMessage Req() => new(HttpMethod.Get, $"/players/{playerId}/profile");
    }

    public async Task<PlayerTransfersResponse> GetPlayerTransfers(string playerId)
    {
        using var res = await SendWithRetry(Req, "transfers");
        if (!CheckResponse(res)) return null;
        var result = await res.Content.ReadFromJsonAsync<PlayerTransfersResponse>(_serializerOptions);
        return result;

        HttpRequestMessage Req() => new(HttpMethod.Get, $"/players/{playerId}/transfers");
    }

    public async Task<PlayerAchievementsResponse> GetPlayerAchievements(string playerId)
    {
        using var res = await SendWithRetry(Req, "achievements");
        if (!CheckResponse(res)) return null;
        var result = await res.Content.ReadFromJsonAsync<PlayerAchievementsResponse>(_serializerOptions);
        return result;

        HttpRequestMessage Req() => new(HttpMethod.Get, $"/players/{playerId}/achievements");
    }

    public async Task<ClubProfileResponse> GetClubProfile(string clubId)
    {
        using var res = await SendWithRetry(Req, "club");
        if (!CheckResponse(res)) return null;
        var result = await res.Content.ReadFromJsonAsync<ClubProfileResponse>(_serializerOptions);
        return result;

        HttpRequestMessage Req() => new(HttpMethod.Get, $"/clubs/{clubId}/profile");
    }

    public async Task<CompetitionClubsResponse> GetCompetition(string competitionId)
    {
        using var res = await SendWithRetry(Req, "competition");
        if (!CheckResponse(res)) return null;
        var result = await res.Content.ReadFromJsonAsync<CompetitionClubsResponse>(_serializerOptions);
        return result;

        HttpRequestMessage Req() => new(HttpMethod.Get, $"/competitions/{competitionId}/clubs");
    }

    public async Task<PlayerStatsResponse> GetPlayerStats(string playerId)
    {
        using var res = await SendWithRetry(Req, "stats");
        if (!CheckResponse(res)) return null;
        var result = await res.Content.ReadFromJsonAsync<PlayerStatsResponse>(_serializerOptions);
        return result;

        HttpRequestMessage Req() => new(HttpMethod.Get, $"/players/{playerId}/stats");
    }

    private bool CheckResponse(HttpResponseMessage res, bool throwOnError = false)
    {
        if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }

        if (res.IsSuccessStatusCode) return true;

        if (throwOnError)
        {
            throw new Exception($"Transfermarkt API error: {res.StatusCode}");
        }

        return false;
    }

    private JsonSerializerOptions CreateJsonOpts()
    {
        var opts = new JsonSerializerOptions();
        opts.Converters.Add(new AutoNumberToStringConverter());
        return opts;
    }

    private async Task<HttpResponseMessage> SendWithRetry(Func<HttpRequestMessage> reqFunc, string reqKey,
        int retryCount = 20)
    {
        var delay = 1000;
        var res = new HttpResponseMessage();
        for (var i = 0; i <= retryCount; i++)
        {
            try
            {
                var req = reqFunc();
                res = await ExecuteWithThrottle(() => _httpClient.SendAsync(req), reqKey);
                if (res.IsSuccessStatusCode || res.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return res;
                }
            }
            catch
            {
                if (i >= retryCount)
                {
                    throw;
                }
            }
            finally
            {
                if (res.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    delay = _settings.TransfermarktSleepOnThrottledMs;
                }
                else
                {
                    delay = delay >= 32000 ? 32000 : delay * 2;
                }
            }

            await Task.Delay(delay);
            
        }

        return res;
    }


    private readonly Dictionary<string, Stopwatch> _throttleStopwatches = new();

    private async Task<T> ExecuteWithThrottle<T>(Func<Task<T>> func, string reqKey)
    {
        var throttleMs = _settings.TransfermarktThrottleMs + new Random().Next(0, 2000);

        if (!_throttleStopwatches.TryGetValue(reqKey, out var stopwatch))
        {
            stopwatch = _throttleStopwatches[reqKey] = new Stopwatch();
        }

        if (stopwatch.IsRunning)
        {
            var elapsed = (int)stopwatch.ElapsedMilliseconds;
            if (elapsed < throttleMs)
            {
                await Task.Delay(throttleMs - elapsed);
            }
        }

        try
        {
            var result = await func();
            return result;
        }
        finally
        {
            stopwatch.Restart();
        }
    }

    #region dispose

    private bool _disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _httpClient.Dispose();
            }
            
            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}