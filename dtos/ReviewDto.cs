using System.ComponentModel.DataAnnotations;

namespace GameStore.Api.DTOs;

// DTO for creating a new review
public record CreateReviewRequest(
    [property: Range(1, 5, ErrorMessage = "rating must be between 1 and 5")] 
    int Rating,
    
    [property: Required(ErrorMessage = "comment is required")]
    [property: MaxLength(500, ErrorMessage = "comment must not exceed 500 characters")]
    [property: MinLength(10, ErrorMessage = "comment must contain at least 10 characters")]
    string Comment
);

// DTO for displaying a review
public record ReviewDto(
    int Id,
    int GameId,
    string Username,
    int Rating,
    string Comment,
    DateTime CreatedAt
);