using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Quingo.Scripts.Transfermarkt.Dto;

public class PlayerTransfersResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("transfers")]
    public List<Transfer> Transfers { get; set; }

    [JsonPropertyName("youthClubs")]
    public List<string> YouthClubs { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
public class TransferInfo
{
    [JsonPropertyName("id")]
    public string ClubID { get; set; }

    [JsonPropertyName("name")]
    public string ClubName { get; set; }
}

public class Transfer
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("clubFrom")]
    public TransferInfo From { get; set; }

    [JsonPropertyName("clubTo")]
    public TransferInfo To { get; set; }

    [JsonPropertyName("date")]
    public string Date { get; set; }

    [JsonPropertyName("upcoming")]
    public bool Upcoming { get; set; }

    [JsonPropertyName("season")]
    public string Season { get; set; }

    [JsonPropertyName("marketValue")]
    public string MarketValue { get; set; }

    [JsonPropertyName("fee")]
    public string Fee { get; set; }
}


