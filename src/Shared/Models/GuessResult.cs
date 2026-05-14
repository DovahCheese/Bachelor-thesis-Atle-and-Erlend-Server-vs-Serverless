namespace Shared.Models;

public enum LetterResult { Correct, Present, Absent }

public record LetterGuess(char Letter, LetterResult Result);

public record GuessResult(IReadOnlyList<LetterGuess> Letters, bool IsCorrect, int GuessesUsed, string? Answer = null);
