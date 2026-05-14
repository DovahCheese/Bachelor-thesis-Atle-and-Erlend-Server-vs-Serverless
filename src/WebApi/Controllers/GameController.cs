using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.Interfaces;
using Shared.Models;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController(
    OrdleGameFactory factory,
    WordValidator validator,
    IGameStore store) : ControllerBase
{
    private readonly OrdleGameFactory _factory = factory;
    private readonly WordValidator _validator  = validator;
    private readonly IGameStore _store         = store;

    /// <summary>
    /// Returns today's daily game, creating it if it doesn't exist yet.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<GameState>> GetGame([FromQuery] Guid? userId = null)
    {
        var date   = GetDailyGameId();
        var gameId = userId.HasValue ? $"{date}-{userId:N}" : date;
        var game   = await _store.GetAsync(gameId);

        if (game is null)
        {
            game = await _factory.CreateDailyGameAsync(gameId);
            await _store.SaveAsync(game, userId);
        }

        return Ok(game.GetState());
    }

    /// <summary>
    /// Creates or resumes a random game. Pass userId to link the game to a user account.
    /// </summary>
    [HttpGet("random")]
    public async Task<ActionResult<GameState>> GetRandomGame(
        [FromQuery] string? gameId = null,
        [FromQuery] Guid? userId = null)
    {
        if (!string.IsNullOrWhiteSpace(gameId))
        {
            var existing = await _store.GetAsync(gameId);
            if (existing is not null)
                return Ok(existing.GetState());
        }

        var newGameId = $"random-{Guid.NewGuid():N}";
        var game      = await _factory.CreateRandomGameAsync(newGameId);
        await _store.SaveAsync(game, userId);

        return Ok(game.GetState());
    }


    /// <summary>
    /// Submits a guess for the game identified by GameId in the request body.
    /// Returns 400 if the guess is invalid, 404 if the game doesn't exist,
    /// 409 if the game is already over.
    /// </summary>
    [HttpPost("guess")]
    public async Task<ActionResult<GuessResult>> SubmitGuess([FromBody] GuessRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.GameId))
            return BadRequest("GameId is required.");

        var game = await _store.GetAsync(request.GameId);

        if (game is null)
            return NotFound("No active game found. Call GET /api/game first.");

        if (game.IsOver)
            return Conflict("This game is already over.");

        var validation = await _validator.ValidateGuessAsync(request.Guess, game.WordLength);
        if (!validation.IsValid)
            return BadRequest(validation.ErrorMessage);

        var result = game.SubmitGuess(request.Guess);
        await _store.SaveAsync(game);

        var response = game.IsOver ? result with { Answer = game.Answer } : result;
        return Ok(response);
    }

    /// <summary>
    /// Deletes a game session so it can be restarted fresh.
    /// </summary>
    [HttpDelete("{gameId}")]
    public async Task<IActionResult> DeleteGame(string gameId)
    {
        await _store.DeleteAsync(gameId);
        return NoContent();
    }

    private static string GetDailyGameId()
        => DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyyMMdd");
}

// GameId added — required so the backend knows which game the guess belongs to
public record GuessRequest(string GameId, string Guess);