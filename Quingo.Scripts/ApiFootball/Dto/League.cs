using System.Text.Json.Serialization;

namespace Quingo.Scripts.ApiFootball.Dto;

// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);

public class GetLeaguesResponse
{
    [JsonPropertyName("league")]
    public LeagueInfo League { get; set; }

    [JsonPropertyName("country")]
    public Country Country { get; set; }

    [JsonPropertyName("seasons")]
    public List<Season> Seasons { get; set; }
}

public class Country
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; }

    [JsonPropertyName("flag")]
    public string Flag { get; set; }
}

public class Coverage
{
    [JsonPropertyName("fixtures")]
    public Fixtures Fixtures { get; set; }

    [JsonPropertyName("standings")]
    public bool Standings { get; set; }

    [JsonPropertyName("players")]
    public bool Players { get; set; }

    [JsonPropertyName("top_scorers")]
    public bool TopScorers { get; set; }

    [JsonPropertyName("top_assists")]
    public bool TopAssists { get; set; }

    [JsonPropertyName("top_cards")]
    public bool TopCards { get; set; }

    [JsonPropertyName("injuries")]
    public bool Injuries { get; set; }

    [JsonPropertyName("predictions")]
    public bool Predictions { get; set; }

    [JsonPropertyName("odds")]
    public bool Odds { get; set; }
}

public class Fixtures
{
    [JsonPropertyName("events")]
    public bool Events { get; set; }

    [JsonPropertyName("lineups")]
    public bool Lineups { get; set; }

    [JsonPropertyName("statistics_fixtures")]
    public bool StatisticsFixtures { get; set; }

    [JsonPropertyName("statistics_players")]
    public bool StatisticsPlayers { get; set; }
}

public class LeagueInfo
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("logo")]
    public string Logo { get; set; }
}

public class Paging
{
    [JsonPropertyName("current")]
    public int? Current { get; set; }

    [JsonPropertyName("total")]
    public int? Total { get; set; }
}

public class Season
{
    [JsonPropertyName("year")]
    public int? Year { get; set; }

    [JsonPropertyName("start")]
    public string Start { get; set; }

    [JsonPropertyName("end")]
    public string End { get; set; }

    [JsonPropertyName("current")]
    public bool Current { get; set; }

    [JsonPropertyName("coverage")]
    public Coverage Coverage { get; set; }
}


