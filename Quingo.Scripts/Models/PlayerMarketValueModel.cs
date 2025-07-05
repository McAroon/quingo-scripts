namespace Quingo.Scripts.Models;

public class PlayerMarketValueModel
{
    public string Player { get; set; }
    public string MarketValue { get; set; }
    public string Url { get; set; }
    
    public string PlayerId => Url.Split('/').Last();
}