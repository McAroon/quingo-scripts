using F23.StringSimilarity;
using Google.Apis.CustomSearchAPI.v1;
using Google.Apis.Kgsearch.v1;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quingo.Scripts;

public class GoogleClient : IDisposable
{
    const string AppName = "Quingo";
    const string ApiKey = "AIzaSyAZgXBJsEO9i6FNstjean3RlnBKfRv5u5E";
    const string Cx = "51d75309da18b4c50";

    private readonly JaroWinkler _jaroWinkler = new();

    private readonly CustomSearchAPIService _svc;

    private readonly int _throttleMs = 1000;

    public GoogleClient()
    {
        _svc = new CustomSearchAPIService(new Google.Apis.Services.BaseClientService.Initializer
        {
            ApplicationName = AppName,
            ApiKey = ApiKey
        });
    }

    public async Task<string> FindCorrectName(string name)
    {
        var resource = new CseResource(_svc);
        var req = resource.List();
        req.Cx = Cx;
        req.Q =$"{name} site:transfermarkt.com";
        //req.SiteSearch = "transfermarkt.com";
        var res = await ExecuteWithThrottle(req.ExecuteAsync);
        var item = res.Items?.FirstOrDefault();
        if (item == null)
        {
            if (res.Spelling?.CorrectedQuery == null) return null;
            req.Q = res.Spelling.CorrectedQuery;
            res = await ExecuteWithThrottle(req.ExecuteAsync);
            item = res.Items?.FirstOrDefault();
            if (item == null) return null;
        }

        var title = item.Title;

        var qSplit = name.Split(' ').Select(x => x.Trim()).ToList();
        var resSplit = title.Split(' ').Select(x => x.Trim()).ToList();
        var qFixed = new List<string>();

        foreach (var q in qSplit)
        {
            foreach (var r in resSplit)
            {
                var comp = _jaroWinkler.Similarity(q, r);
                if (comp > 0.7)
                {
                    qFixed.Add(r);
                    break;
                }
            }
        }

        var result = string.Join(' ', qFixed);
        return result;
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

    private bool disposedValue;
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _svc.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~GoogleClient()
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
}
