namespace Quingo.Scripts.Models;

public class ManagerPlayerModel
{
    public string Manager { get; set; }
    public string Player { get; set; }
    public string Url { get; set; }
    
    public string PlayerId => Url.Split('/').Last();
}