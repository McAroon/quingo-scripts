namespace Quingo.Scripts.Models;

public class NodeDifficultyModel
{
    public string Tag { get; set; }
    public string Name { get; set; }
    public string Difficulty { get; set; }
    public string Score { get; set; }

    public int DifficultyInt => int.Parse(Difficulty);
    public int ScoreInt => int.Parse(Score);
}