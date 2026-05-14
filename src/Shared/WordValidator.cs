using Shared.Interfaces;

namespace Shared;

/// <summary>
/// Validates guesses before they are passed to <see cref="OrdleGame"/>.
/// Returns a typed result so controllers can return precise error responses.
/// </summary>
public class WordValidator(IWordRepository wordRepository)
{
    private readonly IWordRepository _wordRepository = wordRepository;

    public async Task<ValidationResult> ValidateGuessAsync(string guess, int expectedLength)
    {
        if (string.IsNullOrWhiteSpace(guess))
            return ValidationResult.Fail("Guess must not be empty.");

        if (guess.Length != expectedLength)
            return ValidationResult.Fail($"Guess must be {expectedLength} letters.");

        if (!guess.All(char.IsLetter))
            return ValidationResult.Fail("Guess must contain only letters.");

        bool isKnown = await _wordRepository.IsValidWordAsync(guess);
        if (!isKnown)
            return ValidationResult.Fail("Not a valid word.");

        return ValidationResult.Ok();
    }
}

public record ValidationResult(bool IsValid, string? ErrorMessage)
{
    public static ValidationResult Ok() => new(true, null);
    public static ValidationResult Fail(string message) => new(false, message);
}
