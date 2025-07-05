using System.Text.Json.Serialization;

namespace Quingo.Scripts.Transfermarkt.Dto;

public class PlayerStatsResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("stats")]
    public List<Stat> Stats { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

public class Stat
{
    [JsonPropertyName("competitionID")]
    public string CompetitionID { get; set; }

    [JsonPropertyName("clubID")]
    public string ClubID { get; set; }

    [JsonPropertyName("seasonID")]
    public string SeasonID { get; set; }

    [JsonPropertyName("competitionName")]
    public string CompetitionName { get; set; }

    [JsonPropertyName("appearances")]
    public string Appearances { get; set; }

    [JsonPropertyName("goals")]
    public string Goals { get; set; }

    [JsonPropertyName("yellowCards")]
    public string YellowCards { get; set; }

    [JsonPropertyName("minutesPlayed")]
    public string MinutesPlayed { get; set; }

    [JsonPropertyName("assists")]
    public string Assists { get; set; }

    [JsonPropertyName("redCards")]
    public string RedCards { get; set; }

    [JsonPropertyName("secondYellowCards")]
    public string SecondYellowCards { get; set; }
}

