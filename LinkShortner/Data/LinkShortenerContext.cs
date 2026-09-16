using LinkShortner.Models;

namespace LinkShortner.Data;
using Microsoft.EntityFrameworkCore;

public class LinkShortenerContext : DbContext
{
    public LinkShortenerContext(DbContextOptions<LinkShortenerContext> options)
        : base(options)
    {
    }
    
    public DbSet<ShortenedUrl> ShortenedUrls => Set<ShortenedUrl>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShortenedUrl>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ShortCode).IsUnique();
                entity.Property(e => e.ShortCode).IsRequired().HasMaxLength(10);
                entity.Property(e => e.OriginalUrl).IsRequired();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

            });
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.PasswordHash).IsRequired();
        });
    }
}