namespace Shared.Interfaces;

public interface IWordRepository
{
    /// <summary>Returns the word of the day (same word for all players on a given date).</summary>
    Task<string> GetWordOfTheDayAsync(DateOnly date);

    /// <summary>Returns a random word from the full word list.</summary>
    Task<string> GetRandomWordAsync();

    /// <summary>Returns true if the word exists in the valid words list.</summary>
    Task<bool> IsValidWordAsync(string word);
}