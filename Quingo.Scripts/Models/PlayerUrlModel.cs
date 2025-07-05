namespace Quingo.Scripts.Models;

public class PlayerUrlModel
{
    public string Player { get; set; }

    public string Url { get; set; }
    
    public string PlayerId => Url.Split('/').Last();
}