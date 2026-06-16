using System.ComponentModel.DataAnnotations;

namespace GameStore.Api.dtos;

public record class CreateGameDto(
    [Required(ErrorMessage ="Name is required")][StringLength(50, ErrorMessage ="Name must be between 2 and 50 characters")]string Name,
    [Range(1,50, ErrorMessage ="Genre must be between 1 and 50")]int GenreId,
    [Range(1, 100, ErrorMessage ="Price must be between 1 and 100")]decimal Price,
    DateOnly ReleaseDate
);
