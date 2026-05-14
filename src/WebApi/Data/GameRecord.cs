namespace WebApi.Data;

/// <summary>
/// EF Core entity — one row per daily game.
/// GameId is the date string "yyyyMMdd" used as the primary key.
/// </summary>
public class GameRecord
{
    public string GameId { get; set; } = "";
    public Guid? UserId { get; set; }
    public UserRecord? User { get; set; }
    public string Answer { get; set; } = "";
    public int MaxGuesses { get; set; }
    public int GuessesUsed { get; set; }
    public bool IsOver { get; set; }
    public bool IsWon { get; set; }
    public string? GuessHistoryJson { get; set; }
}
