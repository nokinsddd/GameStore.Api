using Microsoft.EntityFrameworkCore;
using GameStore.Api.Data;
using GameStore.Api.dtos;
using GameStore.Api.Models;
using GameStore.Api.Services;

namespace GameStore.Api.EndPoints;

public static class GenresEndpoints
{
    public static void MapGenresEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/genres");

        // GET /genres - cached
        group.MapGet("/", async (GameStoreContext dbContext, ICacheService cache) =>
        {
            try
            {
                // Try to get from cache first
                var genres = await cache.GetOrSetAsync(
                    "genres_all",  
                    async () =>
                    {
                        // If cache miss, fetch from database
                        return await dbContext.Genres
                            .Select(genre => new GenreDto(genre.Id, genre.Name))
                            .AsNoTracking()  // ← Сохраняем оптимизацию
                            .ToListAsync();
                    },
                    TimeSpan.FromMinutes(30) 
                );

                return Results.Ok(genres);
            }
            catch (Exception ex)
            {
                // If cache fails, fallback to database
                Console.WriteLine($"Cache error: {ex.Message}. Fallback to database.");
                
                var genres = await dbContext.Genres
                    .Select(genre => new GenreDto(genre.Id, genre.Name))
                    .AsNoTracking()
                    .ToListAsync();
                
                return Results.Ok(genres);
            }
        })
        .RequireAuthorization("UserOnly");  
    }
}