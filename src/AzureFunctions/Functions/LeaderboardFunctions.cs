using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using WebApi.Data;

namespace AzureFunctions.Functions;

public class LeaderboardFunctions(AppDbContext db)
{
    // GET /api/leaderboard/daily
    // Ranked by average guesses (wins only), tiebreak by average time.
    [Function("GetLeaderboardDaily")]
    public async Task<IActionResult> GetDaily(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leaderboard/daily")] HttpRequest req)
    {
        var allUsers = await db.Users.ToListAsync();
        var stats    = await db.GameStats
            .Where(s => s.GameType == "daily")
            .ToListAsync();

        var entries = allUsers
            .Select(u =>
            {
                var wins = stats.Where(s => s.UserId == u.Id && s.Won).ToList();
                return new
                {
                    Username   = u.Username,
                    HasWins    = wins.Count > 0,
                    AvgGuesses = wins.Count > 0 ? wins.Average(s => (double)s.GuessesUsed) : double.MaxValue,
                    AvgTime    = wins.Count > 0 ? wins.Average(s => (double)s.TimeSeconds)  : double.MaxValue,
                    GamesWon   = wins.Count,
                };
            })
            .OrderBy(e => e.AvgGuesses)
            .ThenBy(e => e.AvgTime)
            .Select((e, i) => new LeaderboardEntry(
                Rank:           i + 1,
                Username:       e.Username,
                PrimaryValue:   e.HasWins ? Math.Round(e.AvgGuesses, 1) : null,
                SecondaryValue: e.HasWins ? Math.Round(e.AvgTime, 0)    : null,
                GamesCount:     e.GamesWon
            ))
            .ToList();

        return new OkObjectResult(entries);
    }

    // GET /api/leaderboard/random/solved
    // Ranked by solve rate descending, tiebreak by words solved.
    [Function("GetLeaderboardRandomSolved")]
    public async Task<IActionResult> GetRandomSolved(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leaderboard/random/solved")] HttpRequest req)
    {
        var allUsers = await db.Users.ToListAsync();
        var stats    = await db.GameStats
            .Where(s => s.GameType == "random")
            .ToListAsync();

        var entries = allUsers
            .Select(u =>
            {
                var games = stats.Where(s => s.UserId == u.Id).ToList();
                var wins  = games.Where(s => s.Won).ToList();
                var solveRate = games.Count > 0
                    ? Math.Round(wins.Count / (double)games.Count * 100, 1)
                    : (double?)null;
                return new
                {
                    Username    = u.Username,
                    WordsSolved = wins.Count,
                    SolveRate   = solveRate,
                    GamesPlayed = games.Count,
                };
            })
            .OrderByDescending(e => e.SolveRate ?? -1)
            .ThenByDescending(e => e.WordsSolved)
            .Select((e, i) => new LeaderboardEntry(
                Rank:           i + 1,
                Username:       e.Username,
                PrimaryValue:   e.SolveRate,
                SecondaryValue: e.WordsSolved,
                GamesCount:     e.GamesPlayed
            ))
            .ToList();

        return new OkObjectResult(entries);
    }
}

public record LeaderboardEntry(int Rank, string Username, double? PrimaryValue, double? SecondaryValue, int GamesCount);
