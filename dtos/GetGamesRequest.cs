using System.ComponentModel.DataAnnotations;

namespace GameStore.Api.DTOs;

public record GetGamesRequest(
    [property: Range(1, 10000)] int PageNumber = 1,
    [property: Range(1, 100)] int PageSize = 10,
    string? SortBy = null,
    string? SortOrder = "asc",
    int? GenreId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Search = null
);