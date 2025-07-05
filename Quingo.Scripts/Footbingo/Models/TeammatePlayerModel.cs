namespace Quingo.Scripts.Footbingo.Models;

public class TeammatePlayerModel
{
    public string Teammate { get; set; }
    public string Player { get; set; }
    public string Url { get; set; }
    
    public string PlayerId => Url.Split('/').Last().Trim();
}