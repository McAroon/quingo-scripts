namespace Quingo.Scripts.Footbingo;

[Flags]
public enum FootbingoUpdateFlagsEnum
{
    None = 0,
    CreatePlayersOnly = 1,
    UpdatePlayer = 2,
    UpdatePlayerTransfers = 4,
    UpdatePlayerAchievements = 8,
    UpdatePlayerStats = 16,
}