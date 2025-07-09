using System.Text.RegularExpressions;

namespace Quingo.Scripts.Footbingo;

public static partial class FootbingoRegex
{
    public static Regex ExcludeRegex { get; } = CreateExcludeRegex();
    public static Regex ClubRetiredRegex { get; } = CreateClubRetiredRegex();
    public static Regex ClubDuplicatesRegex { get; } = CreateClubDuplicatesRegex();
    
    [GeneratedRegex("career break|without club|youth|unknown|u\\d+|under|academ|akadem|jgd|jugend|jong|^ban$",
        RegexOptions.IgnoreCase)]
    private static partial Regex CreateExcludeRegex();

    [GeneratedRegex("retired", RegexOptions.IgnoreCase)]
    private static partial Regex CreateClubRetiredRegex();

    [GeneratedRegex("^(.+) (B|C|D|II|III|IV|2|3|4)$", RegexOptions.IgnoreCase)]
    private static partial Regex CreateClubDuplicatesRegex();
}