using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Shared;
using Shared.Interfaces;
using Shared.Models;

namespace AzureFunctions.Functions;

public class GameFunctions(
    OrdleGameFactory factory,
    WordValidator validator,
    IGameStore store)
{
    /// <summary>GET /api/game — returns today's daily game, creating it if needed.</summary>
    [Function("GetGame")]
    public async Task<IActionResult> GetGame(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "game")] HttpRequest req)
    {
        Guid? userId = ParseGuid(req.Query["userId"]);
        var date   = GetDailyGameId();
        var gameId = userId.HasValue ? $"{date}-{userId:N}" : date;
        var game   = await store.GetAsync(gameId);

        if (game is null)
        {
            game = await factory.CreateDailyGameAsync(gameId);
            await store.SaveAsync(game, userId);
        }

        return new OkObjectResult(game.GetState());
    }

    /// <summary>GET /api/game/random — creates or resumes a random game.</summary>
    [Function("GetRandomGame")]
    public async Task<IActionResult> GetRandomGame(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "game/random")] HttpRequest req)
    {
        var gameIdParam = req.Query["gameId"].ToString();
        Guid? userId = ParseGuid(req.Query["userId"]);

        if (!string.IsNullOrWhiteSpace(gameIdParam))
        {
            var existing = await store.GetAsync(gameIdParam);
            if (existing is not null)
                return new OkObjectResult(existing.GetState());
        }

        var newGameId = $"random-{Guid.NewGuid():N}";
        var game      = await factory.CreateRandomGameAsync(newGameId);
        await store.SaveAsync(game, userId);

        return new OkObjectResult(game.GetState());
    }

    /// <summary>POST /api/game/guess — submits a guess.</summary>
    [Function("SubmitGuess")]
    public async Task<IActionResult> SubmitGuess(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "game/guess")] HttpRequest req)
    {
        GuessRequest? request;
        try { request = await req.ReadFromJsonAsync<GuessRequest>(); }
        catch { return new BadRequestObjectResult("Invalid JSON body."); }

        if (request is null || string.IsNullOrWhiteSpace(request.GameId))
            return new BadRequestObjectResult("GameId is required.");

        var game = await store.GetAsync(request.GameId);
        if (game is null)
            return new NotFoundObjectResult("No active game found. Call GET /api/game first.");

        if (game.IsOver)
            return new ConflictObjectResult("This game is already over.");

        var validation = await validator.ValidateGuessAsync(request.Guess, game.WordLength);
        if (!validation.IsValid)
            return new BadRequestObjectResult(validation.ErrorMessage);

        var result = game.SubmitGuess(request.Guess);
        await store.SaveAsync(game);

        var response = game.IsOver ? result with { Answer = game.Answer } : result;
        return new OkObjectResult(response);
    }

    /// <summary>DELETE /api/game/{gameId} — deletes a game so it can restart.</summary>
    [Function("DeleteGame")]
    public async Task<IActionResult> DeleteGame(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "game/{gameId}")] HttpRequest req,
        string gameId)
    {
        await store.DeleteAsync(gameId);
        return new NoContentResult();
    }

    private static string GetDailyGameId()
        => DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyyMMdd");

    private static Guid? ParseGuid(Microsoft.Extensions.Primitives.StringValues value)
        => Guid.TryParse(value.ToString(), out var g) ? g : null;
}

public record GuessRequest(string GameId, string Guess);
