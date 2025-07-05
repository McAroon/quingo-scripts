namespace Quingo.Scripts.Footbingo.Models;

public class MedalistModel
{
    public string Player { get; set; }
    public string Link { get; set; }
    public string CorrectedLink { get; set; }

    public string PlayerId => !string.IsNullOrEmpty(CorrectedLink) && !CorrectedLink.Contains("N/A")
        ? CorrectedLink?.Split("/").Last().Trim()
        : !string.IsNullOrEmpty(Link)
            ? Link?.Split("/").Last().Trim()
            : null;
}