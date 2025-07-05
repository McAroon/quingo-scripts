using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Quingo.Scripts.Transfermarkt.Dto;

public class Club
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("lastClubID")]
    public string LastClubId { get; set; }

    [JsonPropertyName("lastClubName")]
    public string LastClubName { get; set; }

    [JsonPropertyName("mostGamesFor")]
    public string MostGamesFor { get; set; }

    [JsonPropertyName("joined")]
    public string Joined { get; set; }

    [JsonPropertyName("contractExpires")]
    public string ContractExpires { get; set; }
}

public class SearchPlayerResult
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("position")]
    public string Position { get; set; }

    [JsonPropertyName("club")]
    public Club Club { get; set; }

    [JsonPropertyName("age")]
    public string Age { get; set; }

    [JsonPropertyName("nationalities")]
    public List<string> Nationalities { get; set; }

    [JsonPropertyName("marketValue")]
    public string MarketValue { get; set; }
}

public class SearchPlayerResponse
{
    [JsonPropertyName("query")]
    public string Query { get; set; }

    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; set; }

    [JsonPropertyName("lastPageNumber")]
    public int LastPageNumber { get; set; }

    [JsonPropertyName("results")]
    public List<SearchPlayerResult> Results { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
