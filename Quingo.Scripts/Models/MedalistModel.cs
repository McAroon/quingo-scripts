namespace Quingo.Scripts.Models;

public class MedalistModel
{
    public string Player { get; set; }
    public string Link { get; set; }
    public string CorrectedLink { get; set; }
    
    public string PlayerId => !string.IsNullOrEmpty(CorrectedLink) ? CorrectedLink?.Split("/").Last() 
        : !string.IsNullOrEmpty(Link) ? Link?.Split("/").Last() 
        : null;
}