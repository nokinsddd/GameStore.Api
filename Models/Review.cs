using System.ComponentModel.DataAnnotations;

namespace GameStore.Api.Models;

public class Review
{
    public int Id { get; set; }
    
    public int GameId { get; set; }
    public Game Game { get; set; } = null!;
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    [Range(1, 5)]
    public int Rating { get; set; }
    
    [MaxLength(500)]
    public string Comment { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}