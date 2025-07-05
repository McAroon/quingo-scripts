namespace Quingo.Scripts;

public class ScriptsSettings
{
    public string FootbingoFileDirectory { get; set; }
    public string FootbingoExcelFile { get; set; }

    public string TransfermarktApiUrl { get; set; }

    public int TransfermarktThrottleMs { get; set; }

    public int TransfermarktSleepOnThrottledMs { get; set; }
}