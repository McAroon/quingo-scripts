using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Quingo.Scripts.Transfermarkt.Dto;

public class ClubProfileResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("officialName")]
    public string OfficialName { get; set; }

    [JsonPropertyName("image")]
    public string Image { get; set; }

    [JsonPropertyName("addressLine1")]
    public string AddressLine1 { get; set; }

    [JsonPropertyName("addressLine2")]
    public string AddressLine2 { get; set; }

    [JsonPropertyName("addressLine3")]
    public string AddressLine3 { get; set; }

    [JsonPropertyName("tel")]
    public string Tel { get; set; }

    [JsonPropertyName("fax")]
    public string Fax { get; set; }

    [JsonPropertyName("website")]
    public string Website { get; set; }

    [JsonPropertyName("foundedOn")]
    public string FoundedOn { get; set; }

    [JsonPropertyName("colors")]
    public List<string> Colors { get; set; }

    [JsonPropertyName("stadiumName")]
    public string StadiumName { get; set; }

    [JsonPropertyName("stadiumSeats")]
    public string StadiumSeats { get; set; }

    [JsonPropertyName("currentTransferRecord")]
    public string CurrentTransferRecord { get; set; }

    [JsonPropertyName("currentMarketValue")]
    public string CurrentMarketValue { get; set; }

    [JsonPropertyName("squad")]
    public Squad Squad { get; set; }

    [JsonPropertyName("league")]
    public League League { get; set; }

    [JsonPropertyName("historicalCrests")]
    public List<string> HistoricalCrests { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

public class League
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("countryID")]
    public string CountryID { get; set; }

    [JsonPropertyName("countryName")]
    public string CountryName { get; set; }

    [JsonPropertyName("tier")]
    public string Tier { get; set; }
}

public class Squad
{
    [JsonPropertyName("size")]
    public string Size { get; set; }

    [JsonPropertyName("averageAge")]
    public string AverageAge { get; set; }

    [JsonPropertyName("foreigners")]
    public string Foreigners { get; set; }

    [JsonPropertyName("nationalTeamPlayers")]
    public string NationalTeamPlayers { get; set; }
}

