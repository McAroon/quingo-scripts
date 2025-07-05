using Quingo.Scripts.Excel;

namespace Quingo.Scripts.Footbingo.Models;

public class ManagerPlayerModel
{
    public string Manager { get; set; }
    public object Player { get; set; }
    
    [ExcelExtractUrl("Player")]
    public string Url { get; set; }
    
    public string PlayerId => Url.Split('/').Last().Trim();
}