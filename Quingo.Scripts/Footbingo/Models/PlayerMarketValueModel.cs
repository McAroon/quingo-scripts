using Quingo.Scripts.Excel;

namespace Quingo.Scripts.Footbingo.Models;

public class PlayerMarketValueModel
{
    public object Player { get; set; }
    public object MarketValue { get; set; }
    
    [ExcelExtractUrl("Player")]
    public string Url { get; set; }
    
    public string PlayerId => Url.Split('/').Last().Trim();
}