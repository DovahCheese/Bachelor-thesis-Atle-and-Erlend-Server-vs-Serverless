using Shared.Models;

namespace Shared.Interfaces;

/// <summary>
/// Persists active OrdleGame instances across HTTP requests.
/// The default implementation is in-memory; swap for Redis/CosmosDB later.
/// </summary>
public interface IGameStore
{
    Task<OrdleGame?> GetAsync(string gameId);
    Task SaveAsync(OrdleGame game, Guid? userId = null);
    Task DeleteAsync(string gameId);
}