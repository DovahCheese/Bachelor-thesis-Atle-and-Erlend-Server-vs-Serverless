namespace Shared.Models;

public record GameState(
    string GameId,
    int WordLength,
    int MaxGuesses,
    int GuessesUsed,
    bool IsOver,
    bool IsWon,
    string? Answer = null,
    IReadOnlyList<GuessResult>? PreviousGuesses = null
);
