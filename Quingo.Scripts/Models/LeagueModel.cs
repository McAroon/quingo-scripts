namespace Quingo.Scripts.Models;

public class LeagueModel
{
    public string LeagueName { get; set; }
    public string Url { get; set; }
    public string Url2 { get; set; }
    
    public string Id => Url?.Split('/').Last();
    public string Id2 => Url2?.Split('/').Last();
}