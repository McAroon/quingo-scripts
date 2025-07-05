namespace Quingo.Scripts.Footbingo.Models;

public class TmPlayerModel
{
    public string NameRu { get; set; }
    public string Name { get; set; }
    public string FullName { get; set; }
    public string NameInHomeCountry { get; set; }
    public string Link { get; set; }
    public string LinkPlayerId => Link?.Split("/").Last().Trim();
}
