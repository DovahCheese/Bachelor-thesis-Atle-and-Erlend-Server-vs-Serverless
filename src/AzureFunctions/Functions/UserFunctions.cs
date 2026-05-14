using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using WebApi.Data;
using WebApi.Services;

namespace AzureFunctions.Functions;

public class UserFunctions(AppDbContext db, IImageStore imageStore)
{
    private const long MaxPictureBytes = 5 * 1024 * 1024;

    // POST /api/users/register
    [Function("Register")]
    public async Task<IActionResult> Register(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users/register")] HttpRequest req)
    {
        RegisterRequest? request;
        try { request = await req.ReadFromJsonAsync<RegisterRequest>(); }
        catch { return new BadRequestObjectResult("Invalid JSON body."); }

        if (request is null) return new BadRequestObjectResult("Request body required.");

        var normalised = request.Username.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Username == normalised))
            return new ConflictObjectResult("Username is already taken.");

        var user = new UserRecord
        {
            Id       = Guid.NewGuid(),
            Username = normalised,
            Password = request.Password
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return new OkObjectResult(ToResponse(user));
    }

    // POST /api/users/login
    [Function("Login")]
    public async Task<IActionResult> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users/login")] HttpRequest req)
    {
        LoginRequest? request;
        try { request = await req.ReadFromJsonAsync<LoginRequest>(); }
        catch { return new BadRequestObjectResult("Invalid JSON body."); }

        if (request is null) return new BadRequestObjectResult("Request body required.");

        var normalised = request.Username.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Username == normalised && u.Password == request.Password);

        if (user is null)
            return new UnauthorizedObjectResult("Invalid username or password.");

        return new OkObjectResult(ToResponse(user));
    }

    // GET /api/users/{id}
    [Function("GetUser")]
    public async Task<IActionResult> GetUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{id}")] HttpRequest req,
        Guid id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return new NotFoundResult();
        return new OkObjectResult(ToResponse(user));
    }

    // PUT /api/users/{id}/username
    [Function("ChangeUsername")]
    public async Task<IActionResult> ChangeUsername(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "users/{id}/username")] HttpRequest req,
        Guid id)
    {
        ChangeUsernameRequest? request;
        try { request = await req.ReadFromJsonAsync<ChangeUsernameRequest>(); }
        catch { return new BadRequestObjectResult("Invalid JSON body."); }

        if (request is null) return new BadRequestObjectResult("Request body required.");

        var user = await db.Users.FindAsync(id);
        if (user is null) return new NotFoundResult();
        if (user.Password != request.CurrentPassword)
            return new UnauthorizedObjectResult("Wrong password.");

        var normalised = request.NewUsername.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Username == normalised && u.Id != id))
            return new ConflictObjectResult("Username is already taken.");

        user.Username = normalised;
        await db.SaveChangesAsync();
        return new OkObjectResult(ToResponse(user));
    }

    // PUT /api/users/{id}/password
    [Function("ChangePassword")]
    public async Task<IActionResult> ChangePassword(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "users/{id}/password")] HttpRequest req,
        Guid id)
    {
        ChangePasswordRequest? request;
        try { request = await req.ReadFromJsonAsync<ChangePasswordRequest>(); }
        catch { return new BadRequestObjectResult("Invalid JSON body."); }

        if (request is null) return new BadRequestObjectResult("Request body required.");

        var user = await db.Users.FindAsync(id);
        if (user is null) return new NotFoundResult();
        if (user.Password != request.CurrentPassword)
            return new UnauthorizedObjectResult("Wrong password.");

        user.Password = request.NewPassword;
        await db.SaveChangesAsync();
        return new OkObjectResult(ToResponse(user));
    }

    // DELETE /api/users/{id}?password=xxx
    [Function("DeleteUser")]
    public async Task<IActionResult> DeleteUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "users/{id}")] HttpRequest req,
        Guid id)
    {
        var password = req.Query["password"].ToString();
        var user = await db.Users.FindAsync(id);
        if (user is null) return new NotFoundResult();
        if (user.Password != password) return new UnauthorizedObjectResult("Wrong password.");

        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return new NoContentResult();
    }

    // POST /api/users/{id}/picture
    [Function("UploadPicture")]
    public async Task<IActionResult> UploadPicture(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users/{id}/picture")] HttpRequest req,
        Guid id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return new NotFoundResult();

        var file = req.Form.Files.GetFile("file");
        if (file is null) return new BadRequestObjectResult("No file provided.");

        if (file.Length > MaxPictureBytes)
            return new BadRequestObjectResult("File size must not exceed 5 MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        if (!allowed.Contains(ext))
            return new BadRequestObjectResult("Only image files are allowed (.jpg, .png, .gif, .webp).");

        user.ProfilePictureUrl = await imageStore.SaveAsync(id, file);
        await db.SaveChangesAsync();

        return new OkObjectResult(ToResponse(user));
    }

    // POST /api/users/{id}/stats
    [Function("RecordStat")]
    public async Task<IActionResult> RecordStat(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users/{id}/stats")] HttpRequest req,
        Guid id)
    {
        if (!await db.Users.AnyAsync(u => u.Id == id)) return new NotFoundResult();

        RecordStatRequest? request;
        try { request = await req.ReadFromJsonAsync<RecordStatRequest>(); }
        catch { return new BadRequestObjectResult("Invalid JSON body."); }

        if (request is null) return new BadRequestObjectResult("Request body required.");

        var stat = new GameStat
        {
            UserId      = id,
            GameType    = request.GameType,
            Won         = request.Won,
            GuessesUsed = request.GuessesUsed,
            TimeSeconds = request.TimeSeconds,
            GameDate    = request.GameDate,
        };

        db.GameStats.Add(stat);
        await db.SaveChangesAsync();
        return new OkResult();
    }

    // GET /api/users/{id}/stats
    [Function("GetStats")]
    public async Task<IActionResult> GetStats(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{id}/stats")] HttpRequest req,
        Guid id)
    {
        if (!await db.Users.AnyAsync(u => u.Id == id)) return new NotFoundResult();

        var stats  = await db.GameStats.Where(s => s.UserId == id).ToListAsync();
        var daily  = stats.Where(s => s.GameType == "daily").ToList();
        var random = stats.Where(s => s.GameType == "random").ToList();

        var dailyWon = daily.Where(s => s.Won).ToList();
        var dist = new Dictionary<int, int>();
        for (int i = 1; i <= 6; i++)
            dist[i] = dailyWon.Count(s => s.GuessesUsed == i);

        var randomSolved = random.Count(s => s.Won);

        return new OkObjectResult(new UserStatsResponse(
            Daily: new DailyStats(
                GamesPlayed:        daily.Count,
                GamesWon:           dailyWon.Count,
                AverageGuesses:     dailyWon.Count > 0 ? Math.Round(dailyWon.Average(s => s.GuessesUsed), 1) : 0,
                AverageTimeSeconds: daily.Count > 0 ? Math.Round(daily.Average(s => s.TimeSeconds), 0) : 0,
                GuessDistribution:  dist
            ),
            Random: new RandomStats(
                GamesPlayed:        random.Count,
                WordsSolved:        randomSolved,
                AverageTimeSeconds: random.Count > 0 ? Math.Round(random.Average(s => s.TimeSeconds), 0) : 0
            )
        ));
    }

    private static UserResponse ToResponse(UserRecord u) => new(u.Id, u.Username, u.ProfilePictureUrl);
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
