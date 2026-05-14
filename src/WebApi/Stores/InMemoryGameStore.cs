using System.Collections.Concurrent;
using Shared;
using Shared.Interfaces;

namespace WebApi.Stores;

/// <summary>
/// Keeps active games in a process-level dictionary.
/// Fine for a single-instance App Service; swap for a distributed cache (Redis)
/// if you scale out to multiple instances.
/// </summary>
public class InMemoryGameStore : IGameStore
{
    private readonly ConcurrentDictionary<string, OrdleGame> _games = new();

    public Task<OrdleGame?> GetAsync(string gameId)
    {
        _games.TryGetValue(gameId, out var game);
        return Task.FromResult(game);
    }

    public Task SaveAsync(OrdleGame game, Guid? userId = null)
    {
        _games[game.GameId] = game;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string gameId)
    {
        _games.TryRemove(gameId, out _);
        return Task.CompletedTask;
    }
}