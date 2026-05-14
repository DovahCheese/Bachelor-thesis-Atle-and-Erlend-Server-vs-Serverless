namespace WebApi.Data;

/// <summary>
/// One row per completed game session for a logged-in user.
/// Used for per-user stats and leaderboards.
/// </summary>
public class GameStat
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public UserRecord User { get; set; } = null!;

    /// <summary>"daily" or "random"</summary>
    public string GameType { get; set; } = "";

    public bool Won { get; set; }
    public int GuessesUsed { get; set; }

    /// <summary>
    /// Actual elapsed seconds for wins.
    /// Fixed 180-second penalty for lost random games.
    /// </summary>
    public int TimeSeconds { get; set; }

    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;

    /// <summary>yyyyMMdd — only set for daily games, to prevent duplicate daily entries.</summary>
    public string? GameDate { get; set; }
}
