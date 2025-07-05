using System.Text.Json.Serialization;

namespace Quingo.Scripts.Transfermarkt.Dto;

public class CompetitionClub
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class CompetitionClubsResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("seasonID")]
    public string SeasonID { get; set; }

    [JsonPropertyName("clubs")]
    public List<CompetitionClub> Clubs { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}


