using Shared;
using Shared.Models;

namespace Shared.Tests;

public class WordleGameTests
{
    // Helper: create a game with a known answer
    private static OrdleGame NewGame(string answer, int maxGuesses = 6)
        => new(gameId: "test", answer: answer, maxGuesses: maxGuesses);

    // Helper: submit a guess and return the letter results as a simple array
    private static LetterResult[] Score(OrdleGame game, string guess)
        => game.SubmitGuess(guess).Letters.Select(l => l.Result).ToArray();

    // -------------------------------------------------------------------------
    // Basic scoring
    // -------------------------------------------------------------------------

    [Fact]
    public void CorrectGuess_AllLettersMarkedCorrect()
    {
        var game = NewGame("CRANE");
        var results = Score(game, "CRANE");

        Assert.All(results, r => Assert.Equal(LetterResult.Correct, r));
    }

    [Fact]
    public void CompletelyWrongGuess_AllLettersMarkedAbsent()
    {
        var results = Score(NewGame("CRANE"), "TULIP");
        Assert.All(results, r => Assert.Equal(LetterResult.Absent, r));
    }

    [Fact]
    public void CorrectLetterWrongPosition_MarkedPresent()
    {
        // Answer: CRANE = C(0) R(1) A(2) N(3) E(4)
        // Guess:  NACRE = N(0) A(1) C(2) R(3) E(4)
        // N(0)=present (N in CRANE at 3), A(1)=present (A at 2), C(2)=present (C at 0),
        // R(3)=present (R in CRANE at 1, NOT at 3), E(4)=correct (same position)
        var game = NewGame("CRANE");
        var result = game.SubmitGuess("NACRE");

        Assert.Equal(LetterResult.Present, result.Letters[0].Result); // N
        Assert.Equal(LetterResult.Present, result.Letters[1].Result); // A
        Assert.Equal(LetterResult.Present, result.Letters[2].Result); // C
        Assert.Equal(LetterResult.Present, result.Letters[3].Result); // R — in CRANE at index 1, not 3
        Assert.Equal(LetterResult.Correct, result.Letters[4].Result); // E — same position
    }

    // -------------------------------------------------------------------------
    // Duplicate letter handling — the trickiest Wordle edge cases
    // -------------------------------------------------------------------------

    [Fact]
    public void DuplicateInGuess_OnlyOneMarkedPresent_WhenAnswerHasOneCopy()
    {
        // Answer: SPITE, Guess: SISSY
        // S(0): Correct  — matches answer[0]
        // I(1): Present  — I exists in SPITE at index 2
        // S(2): Absent   — answer's only S already consumed by index 0
        // S(3): Absent
        // Y(4): Absent
        var game = NewGame("SPITE");
        var result = game.SubmitGuess("SISSY");

        Assert.Equal(LetterResult.Correct, result.Letters[0].Result); // S
        Assert.Equal(LetterResult.Present, result.Letters[1].Result); // I
        Assert.Equal(LetterResult.Absent,  result.Letters[2].Result); // S
        Assert.Equal(LetterResult.Absent,  result.Letters[3].Result); // S
        Assert.Equal(LetterResult.Absent,  result.Letters[4].Result); // Y
    }

    [Fact]
    public void DuplicateInAnswer_BothInstancesCanBeFound()
    {
        // Answer: SPEED, Guess: EESSD
        // E(0): Present  — E exists in SPEED but not at index 0
        // E(1): Present  — second E in SPEED consumed
        // S(2): Correct  — S matches answer[2]... wait, SPEED = S(0)P(1)E(2)E(3)D(4)
        // Corrected mapping:
        // Answer: S P E E D
        // Guess:  E E S S D
        // E(0): Present  (E in answer at index 2/3, not index 0)
        // E(1): Present  (second E in answer consumed)
        // S(2): Present  (S in answer at index 0, not index 2)
        // S(3): Absent   (answer's only S already consumed)
        // D(4): Correct
        var game = NewGame("SPEED");
        var result = game.SubmitGuess("EESSD");

        Assert.Equal(LetterResult.Present, result.Letters[0].Result); // E
        Assert.Equal(LetterResult.Present, result.Letters[1].Result); // E
        Assert.Equal(LetterResult.Present, result.Letters[2].Result); // S
        Assert.Equal(LetterResult.Absent,  result.Letters[3].Result); // S
        Assert.Equal(LetterResult.Correct, result.Letters[4].Result); // D
    }

    // -------------------------------------------------------------------------
    // Game state transitions
    // -------------------------------------------------------------------------

    [Fact]
    public void CorrectGuess_SetsIsWonAndIsOver()
    {
        var game = NewGame("CRANE");
        var result = game.SubmitGuess("CRANE");

        Assert.True(result.IsCorrect);
        Assert.True(game.IsWon);
        Assert.True(game.IsOver);
    }

    [Fact]
    public void ExhaustingGuesses_SetsIsOverButNotIsWon()
    {
        var game = NewGame("CRANE", maxGuesses: 2);
        game.SubmitGuess("TULIP");
        game.SubmitGuess("SLOTH");

        Assert.True(game.IsOver);
        Assert.False(game.IsWon);
    }

    [Fact]
    public void GuessesUsed_IncrementsCorrectly()
    {
        var game = NewGame("CRANE");
        game.SubmitGuess("TULIP");
        game.SubmitGuess("SLOTH");

        Assert.Equal(2, game.GetState().GuessesUsed);
    }

    [Fact]
    public void SubmitGuessAfterGameOver_ThrowsInvalidOperationException()
    {
        var game = NewGame("CRANE");
        game.SubmitGuess("CRANE"); // wins the game

        Assert.Throws<InvalidOperationException>(() => game.SubmitGuess("SLOTH"));
    }

    // -------------------------------------------------------------------------
    // Input validation
    // -------------------------------------------------------------------------

    [Fact]
    public void WrongLengthGuess_ThrowsArgumentException()
    {
        var game = NewGame("CRANE");

        Assert.Throws<ArgumentException>(() => game.SubmitGuess("CAT"));
    }

    [Fact]
    public void GuessIsCaseInsensitive()
    {
        var game = NewGame("CRANE");
        var result = game.SubmitGuess("crane");

        Assert.True(result.IsCorrect);
    }

    // -------------------------------------------------------------------------
    // WordValidator (uses FakeWordRepository — no file I/O needed)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ValidateGuessAsync_ValidWord_ReturnsOk()
    {
        var validator = new WordValidator(new FakeWordRepository(["CRANE", "TULIP"]));
        var result = await validator.ValidateGuessAsync("CRANE", 5);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateGuessAsync_UnknownWord_ReturnsFail()
    {
        var validator = new WordValidator(new FakeWordRepository(["CRANE"]));
        var result = await validator.ValidateGuessAsync("ZZZZZ", 5);

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateGuessAsync_WrongLength_ReturnsFail()
    {
        var validator = new WordValidator(new FakeWordRepository(["CRANE"]));
        var result = await validator.ValidateGuessAsync("CAT", 5);

        Assert.False(result.IsValid);
    }
}

// ---------------------------------------------------------------------------
// Test double — replaces FileWordRepository so tests have no file I/O.
// Implements GetRandomWordAsync to satisfy the updated IWordRepository contract.
// ---------------------------------------------------------------------------

file class FakeWordRepository(IEnumerable<string> words) : Shared.Interfaces.IWordRepository
{
    private readonly HashSet<string> _words = words
        .Select(w => w.ToUpperInvariant())
        .ToHashSet();

    public Task<string> GetWordOfTheDayAsync(DateOnly date)
        => Task.FromResult(_words.First());

    // Returns the first word deterministically — random behaviour not needed in tests
    public Task<string> GetRandomWordAsync()
        => Task.FromResult(_words.First());

    public Task<bool> IsValidWordAsync(string word)
        => Task.FromResult(_words.Contains(word.ToUpperInvariant()));
}