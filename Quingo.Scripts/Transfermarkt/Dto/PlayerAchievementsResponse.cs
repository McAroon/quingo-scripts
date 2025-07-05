using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Quingo.Scripts.Transfermarkt.Dto;

public class Achievement
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("details")]
    public List<Detail> Details { get; set; }
}

public class Competition
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class Detail
{
    [JsonPropertyName("season")]
    public Season Season { get; set; }

    [JsonPropertyName("club")]
    public Club Club { get; set; }

    [JsonPropertyName("competition")]
    public Competition Competition { get; set; }
}

public class PlayerAchievementsResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("achievements")]
    public List<Achievement> Achievements { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

public class Season
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }
}


