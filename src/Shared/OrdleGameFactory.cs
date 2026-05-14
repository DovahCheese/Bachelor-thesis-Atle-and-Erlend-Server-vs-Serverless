using Shared.Interfaces;

namespace Shared;

/// <summary>
/// Creates <see cref="OrdleGame"/> instances.
/// Keeps WebApi / Functions controllers free of construction details.
/// </summary>
public class OrdleGameFactory(IWordRepository wordRepository)
{
    private readonly IWordRepository _wordRepository = wordRepository;

    /// <summary>Creates a game using today's word of the day.</summary>
    public async Task<OrdleGame> CreateDailyGameAsync(string gameId, int maxGuesses = 6)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        string answer = await _wordRepository.GetWordOfTheDayAsync(today);
        return new OrdleGame(gameId, answer, maxGuesses);
    }

    /// <summary>Creates a game using a randomly selected word.</summary>
    public async Task<OrdleGame> CreateRandomGameAsync(string gameId, int maxGuesses = 6)
    {
        string answer = await _wordRepository.GetRandomWordAsync();
        return new OrdleGame(gameId, answer, maxGuesses);
    }

    /// <summary>Creates a game for a specific date — useful for replays and testing.</summary>
    public async Task<OrdleGame> CreateGameForDateAsync(string gameId, DateOnly date, int maxGuesses = 6)
    {
        string answer = await _wordRepository.GetWordOfTheDayAsync(date);
        return new OrdleGame(gameId, answer, maxGuesses);
    }
}