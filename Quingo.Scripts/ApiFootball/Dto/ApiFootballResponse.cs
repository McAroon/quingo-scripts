using System.Text.Json.Serialization;

namespace Quingo.Scripts.ApiFootball.Dto;

public class ApiFootballResponse<T>
{
    [JsonPropertyName("get")]
    public string Get { get; set; }

    [JsonPropertyName("parameters")]
    public object Parameters { get; set; }

    //[JsonPropertyName("errors")]
    //public List<object> Errors { get; set; }

    [JsonPropertyName("results")]
    public int Results { get; set; }

    [JsonPropertyName("paging")]
    public Paging Paging { get; set; }

    [JsonPropertyName("response")]
    public List<T> Response { get; set; }
}
