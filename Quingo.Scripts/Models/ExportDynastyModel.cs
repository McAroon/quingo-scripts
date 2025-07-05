namespace Quingo.Scripts.Models;

public class ExportDynastyModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }

    public string RelativeId { get; set; }
    
    public string RelativeName { get; set; }
    
    public string RelativeProfileType { get; set; }
    
    public string RelativeUrl { get; set; }
    
    public string ShouldAdd { get; set; }

    public bool ShouldAddBool => "Yes".Equals(ShouldAdd, StringComparison.InvariantCultureIgnoreCase);
}