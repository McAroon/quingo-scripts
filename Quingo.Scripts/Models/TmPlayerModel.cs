namespace Quingo.Scripts.Models;

public class TmPlayerModel
{
    public string NameRu { get; set; }
    public string Name { get; set; }
    public string FullName { get; set; }
    public string NameInHomeCountry { get; set; }
    public string PlayerId => Link.Split("/").Last();
    public string Link { get; set; }
}
