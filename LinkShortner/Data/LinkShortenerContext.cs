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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShortenedUrl>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ShortCode).IsUnique();
                entity.Property(e => e.ShortCode).IsRequired().HasMaxLength(10);
                entity.Property(e => e.OriginalUrl).IsRequired();

            }
        );
    }
}