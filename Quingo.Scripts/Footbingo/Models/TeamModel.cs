namespace Quingo.Scripts.Footbingo.Models;

public class TeamModel
{
    public string TeamName { get; set; }
    public string Url { get; set; }
    public string Id => Url?.Split('/').Last().Trim();
}