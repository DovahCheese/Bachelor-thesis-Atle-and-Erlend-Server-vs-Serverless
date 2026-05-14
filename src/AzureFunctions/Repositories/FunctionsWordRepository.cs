using Microsoft.Extensions.Hosting;
using Shared.Interfaces;

namespace AzureFunctions.Repositories;

/// <summary>
/// Word repository for the Azure Functions host. Uses IHostEnvironment instead of
/// IWebHostEnvironment (not available in isolated worker), otherwise identical to
/// WebApi's FileWordRepository.
/// </summary>
public class FunctionsWordRepository : IWordRepository
{
    private readonly IReadOnlyList<string> _words;

    public FunctionsWordRepository(IHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "Data", "words.txt");
        _words = File.ReadAllLines(path)
            .Select(w => w.Trim().ToUpperInvariant())
            .Where(w => w.Length == 5 && w.All(char.IsLetter))
            .ToList()
            .AsReadOnly();
    }

    public Task<string> GetWordOfTheDayAsync(DateOnly date)
    {
        var index = date.DayNumber % _words.Count;
        return Task.FromResult(_words[index]);
    }

    public Task<string> GetRandomWordAsync()
    {
        var index = Random.Shared.Next(_words.Count);
        return Task.FromResult(_words[index]);
    }

    public Task<bool> IsValidWordAsync(string word)
        => Task.FromResult(_words.Contains(word.ToUpperInvariant()));
}
