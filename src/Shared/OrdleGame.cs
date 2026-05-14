using Shared.Models;

namespace Shared;

/// <summary>
/// Encapsulates a single Ordle game session.
/// Instantiate once per game; call SubmitGuess for each attempt.
/// </summary>
public class OrdleGame
{
    private readonly string _answer;
    private int _guessesUsed;
    private readonly List<GuessResult> _guessHistory = [];

    public string GameId { get; }
    public int WordLength => _answer.Length;
    public int MaxGuesses { get; }
    public bool IsOver { get; private set; }
    public bool IsWon { get; private set; }
    public string Answer => _answer;

    public OrdleGame(string gameId, string answer, int maxGuesses = 6)
    {
        if (string.IsNullOrWhiteSpace(answer))
            throw new ArgumentException("Answer must not be empty.", nameof(answer));
        if (maxGuesses < 1)
            throw new ArgumentOutOfRangeException(nameof(maxGuesses), "Must allow at least one guess.");

        GameId = gameId;
        _answer = answer.ToUpperInvariant();
        MaxGuesses = maxGuesses;
    }

    /// <summary>Restores a game instance from persisted state (e.g. loaded from the database).</summary>
    public OrdleGame(string gameId, string answer, int maxGuesses, int guessesUsed, bool isOver, bool isWon, IEnumerable<GuessResult>? guessHistory = null)
    {
        GameId = gameId;
        _answer = answer.ToUpperInvariant();
        MaxGuesses = maxGuesses;
        _guessesUsed = guessesUsed;
        IsOver = isOver;
        IsWon = isWon;
        if (guessHistory != null)
            _guessHistory.AddRange(guessHistory);
    }

    /// <summary>
    /// Evaluates a guess and returns per-letter results.
    /// Throws <see cref="InvalidOperationException"/> if the game is already over.
    /// Throws <see cref="ArgumentException"/> if the guess length doesn't match the word length.
    /// </summary>
    public GuessResult SubmitGuess(string guess)
    {
        if (IsOver)
            throw new InvalidOperationException("The game is already over.");

        guess = guess.ToUpperInvariant();

        if (guess.Length != WordLength)
            throw new ArgumentException(
                $"Guess must be {WordLength} letters, but was {guess.Length}.", nameof(guess));

        var letters = ScoreGuess(guess);
        _guessesUsed++;

        bool isCorrect = letters.All(l => l.Result == LetterResult.Correct);
        IsWon = isCorrect;
        IsOver = isCorrect || _guessesUsed >= MaxGuesses;

        var result = new GuessResult(letters, isCorrect, _guessesUsed);
        _guessHistory.Add(result);
        return result;
    }

    /// <summary>Returns a snapshot of the current game state.</summary>
    public GameState GetState() =>
        new(GameId, WordLength, MaxGuesses, _guessesUsed, IsOver, IsWon, IsOver ? _answer : null, _guessHistory.AsReadOnly());

    // Two-pass scoring matching standard Ordle rules:
    //   Pass 1 — mark exact matches (Correct).
    //   Pass 2 — mark letters present elsewhere, but only consume unmatched answer letters.
    private IReadOnlyList<LetterGuess> ScoreGuess(string guess)
    {
        var results = Enumerable.Repeat(LetterResult.Absent, WordLength).ToArray();
        var answerLetterPool = new Dictionary<char, int>();

        // Pass 1: exact matches
        for (int i = 0; i < WordLength; i++)
        {
            if (guess[i] == _answer[i])
                results[i] = LetterResult.Correct;
            else
                answerLetterPool[_answer[i]] = answerLetterPool.GetValueOrDefault(_answer[i]) + 1;
        }

        // Pass 2: present / absent
        for (int i = 0; i < WordLength; i++)
        {
            if (results[i] == LetterResult.Correct)
                continue;

            if (answerLetterPool.GetValueOrDefault(guess[i]) > 0)
            {
                results[i] = LetterResult.Present;
                answerLetterPool[guess[i]]--;
            }
            else
            {
                results[i] = LetterResult.Absent;
            }
        }

        return Enumerable.Range(0, WordLength)
            .Select(i => new LetterGuess(guess[i], results[i]))
            .ToList()
            .AsReadOnly();
    }
}
