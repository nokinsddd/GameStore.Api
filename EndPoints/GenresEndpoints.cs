using Microsoft.EntityFrameworkCore;
using GameStore.Api.Data;
using GameStore.Api.dtos;
using GameStore.Api.Models;

namespace GameStore.Api.EndPoints;

public static class GenresEndpoints
{
    public static void MapGenresEndpoints (this WebApplication app)
    {
        var group = app.MapGroup("/genres");

        //GET /genres
        group.MapGet("/", async (GameStoreContext dbContext) =>
            await dbContext.Genres
                            .Select(genre => new GenreDto(genre.Id, genre.Name))
                            .AsNoTracking()
                            .ToListAsync()
            );
    }
}
