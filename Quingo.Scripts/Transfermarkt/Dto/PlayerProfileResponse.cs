using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Quingo.Scripts.Transfermarkt.Dto;

public class Agent
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}

public class Position
{
    [JsonPropertyName("main")]
    public string Main { get; set; }

    [JsonPropertyName("other")]
    public List<string> Other { get; set; }
}

public class RelatedPlayer
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("profileType")]
    public string ProfileType { get; set; }
}

public class TrainerProfile
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("position")]
    public string Position { get; set; }
}

public class PlayerProfileResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("nameInHomeCountry")]
    public string NameInHomeCountry { get; set; }

    [JsonPropertyName("fullName")]
    public string FullName { get; set; }

    [JsonPropertyName("imageURL")]
    public string ImageURL { get; set; }

    [JsonPropertyName("dateOfBirth")]
    public string DateOfBirth { get; set; }

    [JsonPropertyName("age")]
    public string Age { get; set; }

    [JsonPropertyName("height")]
    public string Height { get; set; }

    [JsonPropertyName("citizenship")]
    public List<string> Citizenship { get; set; }

    [JsonPropertyName("isRetired")]
    public bool IsRetired { get; set; }

    [JsonPropertyName("position")]
    public Position Position { get; set; }

    [JsonPropertyName("foot")]
    public string Foot { get; set; }

    [JsonPropertyName("shirtNumber")]
    public string ShirtNumber { get; set; }

    [JsonPropertyName("club")]
    public Club Club { get; set; }

    [JsonPropertyName("marketValue")]
    public string MarketValue { get; set; }

    [JsonPropertyName("agent")]
    public Agent Agent { get; set; }

    [JsonPropertyName("outfitter")]
    public string Outfitter { get; set; }

    [JsonPropertyName("socialMedia")]
    public List<string> SocialMedia { get; set; }
    
    [JsonPropertyName("trainerProfile")]
    public TrainerProfile TrainerProfile { get; set; }

    [JsonPropertyName("relatives")]
    public List<RelatedPlayer> Relatives { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

