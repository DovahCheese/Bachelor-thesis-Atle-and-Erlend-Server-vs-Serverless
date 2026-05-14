using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Interfaces;
using Shared.Models;
using WebApi.Data;

namespace WebApi.Stores;

/// <summary>
/// SQL Server-backed implementation of IGameStore using EF Core.
/// Swap InMemoryGameStore for this in Program.cs to enable persistence.
/// </summary>
public class SqlGameStore(AppDbContext db) : IGameStore
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<OrdleGame?> GetAsync(string gameId)
    {
        var record = await db.Games.FindAsync(gameId);
        if (record is null) return null;

        IEnumerable<GuessResult>? history = null;
        if (!string.IsNullOrEmpty(record.GuessHistoryJson))
            history = JsonSerializer.Deserialize<List<GuessResult>>(record.GuessHistoryJson, _jsonOptions);

        return new OrdleGame(
            record.GameId,
            record.Answer,
            record.MaxGuesses,
            record.GuessesUsed,
            record.IsOver,
            record.IsWon,
            history);
    }

    public async Task SaveAsync(OrdleGame game, Guid? userId = null)
    {
        var state = game.GetState();
        var historyJson = JsonSerializer.Serialize(state.PreviousGuesses, _jsonOptions);
        var existing = await db.Games.FindAsync(game.GameId);

        if (existing is null)
        {
            db.Games.Add(new GameRecord
            {
                GameId = game.GameId,
                UserId = userId,
                Answer = game.Answer,
                MaxGuesses = game.MaxGuesses,
                GuessesUsed = state.GuessesUsed,
                IsOver = game.IsOver,
                IsWon = game.IsWon,
                GuessHistoryJson = historyJson
            });
        }
        else
        {
            existing.GuessesUsed = state.GuessesUsed;
            existing.IsOver = game.IsOver;
            existing.IsWon = game.IsWon;
            existing.GuessHistoryJson = historyJson;
            // UserId is intentionally not updated after creation
        }

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(string gameId)
    {
        var record = await db.Games.FindAsync(gameId);
        if (record is not null)
        {
            db.Games.Remove(record);
            await db.SaveChangesAsync();
        }
    }
}
