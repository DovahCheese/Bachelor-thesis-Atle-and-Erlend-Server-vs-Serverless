using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Data;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(AppDbContext db, IImageStore imageStore) : ControllerBase
{
    // POST /api/users/register
    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> Register([FromBody] RegisterRequest request)
    {
        var normalised = request.Username.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Username == normalised))
            return Conflict("Username is already taken.");

        var user = new UserRecord
        {
            Id = Guid.NewGuid(),
            Username = normalised,
            Password = request.Password
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Ok(ToResponse(user));
    }

    // POST /api/users/login
    [HttpPost("login")]
    public async Task<ActionResult<UserResponse>> Login([FromBody] LoginRequest request)
    {
        var normalised = request.Username.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Username == normalised && u.Password == request.Password);

        if (user is null)
            return Unauthorized("Invalid username or password.");

        return Ok(ToResponse(user));
    }

    // GET /api/users/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetUser(Guid id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        return Ok(ToResponse(user));
    }

    // PUT /api/users/{id}/username
    [HttpPut("{id:guid}/username")]
    public async Task<ActionResult<UserResponse>> ChangeUsername(Guid id, [FromBody] ChangeUsernameRequest request)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        if (user.Password != request.CurrentPassword) return Unauthorized("Wrong password.");

        var normalised = request.NewUsername.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Username == normalised && u.Id != id))
            return Conflict("Username is already taken.");

        user.Username = normalised;
        await db.SaveChangesAsync();
        return Ok(ToResponse(user));
    }

    // PUT /api/users/{id}/password
    [HttpPut("{id:guid}/password")]
    public async Task<ActionResult<UserResponse>> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        if (user.Password != request.CurrentPassword) return Unauthorized("Wrong password.");

        user.Password = request.NewPassword;
        await db.SaveChangesAsync();
        return Ok(ToResponse(user));
    }

    // DELETE /api/users/{id}?password=xxx
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id, [FromQuery] string password)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        if (user.Password != password) return Unauthorized("Wrong password.");

        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/users/{id}/picture
    // 5 MB limit — large enough for thesis upload benchmarks, practical for a profile picture
    private const long MaxPictureBytes = 5 * 1024 * 1024;

    [HttpPost("{id:guid}/picture")]
    public async Task<ActionResult<UserResponse>> UploadPicture(Guid id, IFormFile file)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();

        if (file.Length > MaxPictureBytes)
            return BadRequest("File size must not exceed 5 MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        if (!allowed.Contains(ext))
            return BadRequest("Only image files are allowed (.jpg, .png, .gif, .webp).");

        user.ProfilePictureUrl = await imageStore.SaveAsync(id, file);
        await db.SaveChangesAsync();

        return Ok(ToResponse(user));
    }

    // POST /api/users/{id}/stats
    [HttpPost("{id:guid}/stats")]
    public async Task<IActionResult> RecordStat(Guid id, [FromBody] RecordStatRequest request)
    {
        if (!await db.Users.AnyAsync(u => u.Id == id)) return NotFound();

        var stat = new GameStat
        {
            UserId    = id,
            GameType  = request.GameType,
            Won       = request.Won,
            GuessesUsed = request.GuessesUsed,
            TimeSeconds = request.TimeSeconds,
            GameDate  = request.GameDate,
        };

        db.GameStats.Add(stat);
        await db.SaveChangesAsync();
        return Ok();
    }

    // GET /api/users/{id}/stats
    [HttpGet("{id:guid}/stats")]
    public async Task<ActionResult<UserStatsResponse>> GetStats(Guid id)
    {
        if (!await db.Users.AnyAsync(u => u.Id == id)) return NotFound();

        var stats = await db.GameStats.Where(s => s.UserId == id).ToListAsync();

        var daily  = stats.Where(s => s.GameType == "daily").ToList();
        var random = stats.Where(s => s.GameType == "random").ToList();

        var dailyWon = daily.Where(s => s.Won).ToList();
        var dist = new Dictionary<int, int>();
        for (int i = 1; i <= 6; i++)
            dist[i] = dailyWon.Count(s => s.GuessesUsed == i);

        var randomSolved = random.Count(s => s.Won);

        return Ok(new UserStatsResponse(
            Daily: new DailyStats(
                GamesPlayed:      daily.Count,
                GamesWon:         dailyWon.Count,
                AverageGuesses:   dailyWon.Count > 0 ? Math.Round(dailyWon.Average(s => s.GuessesUsed), 1) : 0,
                AverageTimeSeconds: daily.Count > 0 ? Math.Round(daily.Average(s => s.TimeSeconds), 0) : 0,
                GuessDistribution: dist
            ),
            Random: new RandomStats(
                GamesPlayed:        random.Count,
                WordsSolved:        randomSolved,
                AverageTimeSeconds: random.Count > 0 ? Math.Round(random.Average(s => s.TimeSeconds), 0) : 0
            )
        ));
    }

    private static UserResponse ToResponse(UserRecord u) =>
        new(u.Id, u.Username, u.ProfilePictureUrl);
}

public record RegisterRequest(string Username, string Password);
public record LoginRequest(string Username, string Password);
public record ChangeUsernameRequest(string CurrentPassword, string NewUsername);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record RecordStatRequest(string GameType, bool Won, int GuessesUsed, int TimeSeconds, string? GameDate);
public record DailyStats(int GamesPlayed, int GamesWon, double AverageGuesses, double AverageTimeSeconds, Dictionary<int, int> GuessDistribution);
public record RandomStats(int GamesPlayed, int WordsSolved, double AverageTimeSeconds);
public record UserStatsResponse(DailyStats Daily, RandomStats Random);
public record UserResponse(Guid Id, string Username, string? ProfilePictureUrl);
