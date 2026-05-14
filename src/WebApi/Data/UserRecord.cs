namespace WebApi.Data;

/// <summary>
/// EF Core entity — one row per registered user.
/// Password is stored in plaintext (thesis project, no auth required).
/// </summary>
public class UserRecord
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string? ProfilePictureUrl { get; set; }
}
