using Shared.Interfaces;

namespace WebApi.Repositories;

public class FileWordRepository : IWordRepository
{
    private readonly IReadOnlyList<string> _words;

    public FileWordRepository(IWebHostEnvironment env)
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
    {
        var result = _words.Contains(word.ToUpperInvariant());
        return Task.FromResult(result);
    }
}