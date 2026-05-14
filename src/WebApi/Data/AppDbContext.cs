using Microsoft.EntityFrameworkCore;

namespace WebApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<GameRecord> Games => Set<GameRecord>();
    public DbSet<UserRecord> Users => Set<UserRecord>();
    public DbSet<GameStat> GameStats => Set<GameStat>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<GameRecord>().HasKey(g => g.GameId);
        builder.Entity<GameRecord>().Property(g => g.Answer).HasMaxLength(50);
        builder.Entity<GameRecord>().Property(g => g.GameId).HasMaxLength(50);
        builder.Entity<GameRecord>()
            .HasOne(g => g.User)
            .WithMany()
            .HasForeignKey(g => g.UserId)
            .IsRequired(false);

        builder.Entity<UserRecord>().HasKey(u => u.Id);
        builder.Entity<UserRecord>().Property(u => u.Username).HasMaxLength(50);
        builder.Entity<UserRecord>().HasIndex(u => u.Username).IsUnique();
        builder.Entity<UserRecord>().Property(u => u.Password).HasMaxLength(100);
        builder.Entity<UserRecord>().Property(u => u.ProfilePictureUrl).HasMaxLength(500);

        builder.Entity<GameStat>().HasKey(s => s.Id);
        builder.Entity<GameStat>().Property(s => s.GameType).HasMaxLength(10);
        builder.Entity<GameStat>().Property(s => s.GameDate).HasMaxLength(8);
        builder.Entity<GameStat>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
