namespace Quingo.Scripts.Footbingo.Models;

public class PlayerModel
{
    public string Name { get; set; }

    public string Country { get; set; }

    public string League { get; set; }

    public string LeagueName { get; set; }

    public string PlayerFirstname { get; set; }

    public string PlayerLastname { get; set; }

    public int? LeagueId { get; set; }

    public int? PlayerId { get; set; }

    public bool Found { get; set; }
}
