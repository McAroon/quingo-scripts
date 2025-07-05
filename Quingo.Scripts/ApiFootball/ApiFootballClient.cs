
using Quingo.Scripts.ApiFootball.Dto;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;

namespace Quingo.Scripts.ApiFootball;

public class ApiFootballClient : IDisposable
{
    private readonly HttpClient _httpClient;

    private readonly JsonSerializerOptions _serializerOptions;

    private int _throttleMs = 210;

    public ApiFootballClient()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api-football-v1.p.rapidapi.com"),
        };
        _httpClient.DefaultRequestHeaders.Add("x-rapidapi-key", "d743f235f8mshfbbb315f10e658dp151eddjsnef9fcd60261b");
        _httpClient.DefaultRequestHeaders.Add("x-rapidapi-host", "api-football-v1.p.rapidapi.com");
        _serializerOptions = CreateJsonOpts();
    }

    public async Task<ApiFootballResponse<GetLeaguesResponse>> SearchLeagues(string search)
    {
        return await ExecuteWithThrottle(() => SearchLeaguesFunc(search));
    }

    private async Task<ApiFootballResponse<GetLeaguesResponse>> SearchLeaguesFunc(string search)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["search"] = search;
        var req = new HttpRequestMessage(HttpMethod.Get, $"/v3/leagues?{query}");
        
        using var res = await _httpClient.SendAsync(req);
        res.EnsureSuccessStatusCode();
        var result = await res.Content.ReadFromJsonAsync<ApiFootballResponse<GetLeaguesResponse>>(_serializerOptions);
        return result!;
    }

    public async Task<ApiFootballResponse<GetPlayersResponse>> SearchPlayers(int leagueId, string search)
    {
        return await ExecuteWithThrottle(() => SearchPlayersFunc(leagueId, search));
    }

    private async Task<ApiFootballResponse<GetPlayersResponse>> SearchPlayersFunc(int leagueId, string search)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["league"] = leagueId.ToString();
        query["search"] = search;
        var req = new HttpRequestMessage(HttpMethod.Get, $"/v3/players?{query}");

        using var res = await _httpClient.SendAsync(req);
        res.EnsureSuccessStatusCode();
        var result = await res.Content.ReadFromJsonAsync<ApiFootballResponse<GetPlayersResponse>>(_serializerOptions);
        return result!;
    }

    private JsonSerializerOptions CreateJsonOpts()
    {
        var opts = new JsonSerializerOptions();
        opts.Converters.Add(new AutoNumberToStringConverter());
        return opts;
    }

    private async Task<T> ExecuteWithThrottle<T>(Func<Task<T>> func)
    {
        var sw = new Stopwatch();

        sw.Start();
        var result = await func();
        sw.Stop();

        var elapsed = (int)sw.ElapsedMilliseconds;
        if (elapsed < _throttleMs)
        {
            await Task.Delay(_throttleMs - elapsed);
        }
        return result;
    }

    #region dispose
    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _httpClient.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~ApiFootballClient()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}
