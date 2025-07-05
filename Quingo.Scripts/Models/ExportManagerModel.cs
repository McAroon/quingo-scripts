namespace Quingo.Scripts.Models;

public class ExportManagerModel
{
    public string PlayerId { get; set; }
    public string TrainerId { get; set; }
    public string Name { get; set; }
    public string Position { get; set; }
    public string PlayerUrl { get; set; }
    public string TrainerUrl { get; set; }

    public string ShouldAdd { get; set; }

    public bool ShouldAddBool => "Yes".Equals(ShouldAdd, StringComparison.InvariantCultureIgnoreCase);
}