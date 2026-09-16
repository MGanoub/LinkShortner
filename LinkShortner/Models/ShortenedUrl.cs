using System.ComponentModel.DataAnnotations;
namespace LinkShortner.Models;

public class ShortenedUrl
{
    public  int Id { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string ShortCode { get; set; } = string.Empty;
    
    [Required]
    public string OriginalUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ExpiresAt { get; set; }

    public int ClickCount { get; set; } = 0;
    
    public int UserId { get; set; }
    
    public User? User { get; set; }
}