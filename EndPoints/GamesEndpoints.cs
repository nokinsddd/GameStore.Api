using System;
using Microsoft.EntityFrameworkCore;
using GameStore.Api.Data;
using GameStore.Api.dtos;
using GameStore.Api.Models;
using GameStore.Api.DTOs;
using System.ComponentModel.DataAnnotations;
namespace GameStore.Api.EndPoints;

public static class GamesEndpoints
{
    const string GetGameEndpointName = "GetGame";
    public static void MapGamesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/games");
        //GET /games
        app.MapGet("/games", async (GameStoreContext db, [AsParameters] PaginationRequest pagination) =>
{
    
    var context = new ValidationContext(pagination);
    var results = new List<ValidationResult>();
    
    if (!Validator.TryValidateObject(pagination, context, results, true))
{
    var errors = results
        .Select(r => r.ErrorMessage)
        .OfType<string>()
        .ToArray();

    return Results.ValidationProblem(new Dictionary<string, string[]>
    {
        { "Pagination", errors }
    });
}

    
    var query = db.Games.Include(g => g.Genre);

    
    var totalCount = await query.CountAsync();
    var totalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize);

    
    var games = await query
        .OrderBy(g => g.Id)
        .Skip((pagination.PageNumber - 1) * pagination.PageSize)
        .Take(pagination.PageSize)
        .Select(game => new GameSummaryDto(
            game.Id,
            game.Name,
            game.Genre != null ? game.Genre.Name : "Не указан",
            game.Price,
            game.ReleaseDate
        ))
        .ToListAsync();

    
    return Results.Ok(new PagedResult<GameSummaryDto>(
        games, 
        totalCount, 
        pagination.PageNumber, 
        pagination.PageSize, 
        totalPages
    ));
});

        //GET /games/1
        group.MapGet("/{id}", async (int id, GameStoreContext dbContext) => 
        {
            var game = await dbContext.Games.FindAsync(id);

            return game is null ? Results.NotFound() : Results.Ok(
                new GameDetailsDto(
                    game.Id,
                    game.Name,
                    game.GenreId,
                    game.Price,
                    game.ReleaseDate
                )
            );
        })
            .WithName(GetGameEndpointName);

        //POST /games
        group.MapPost("/", async (CreateGameDto newGame, GameStoreContext dbContext) =>
        {
            Game game = new()
            {
                Name = newGame.Name,
                GenreId = newGame.GenreId,
                Price = newGame.Price,
                ReleaseDate = newGame.ReleaseDate
            };

            dbContext.Games.Add(game);
            await dbContext.SaveChangesAsync();

            GameDetailsDto gameDto = new(
                game.Id,
                game.Name,
                game.GenreId,
                game.Price,
                game.ReleaseDate
            );

            return Results.CreatedAtRoute(GetGameEndpointName, new {id = gameDto.Id}, gameDto);
        })
        .RequireAuthorization("UserOnly"); 
        

        //PUT /games/1
        group.MapPut("/{id}", async (
            int id,
            UpdateGameDto updatedGame,
            GameStoreContext dbContext) =>
        {
           var existingGame = await dbContext.Games.FindAsync(id);

           if (existingGame is null)
            {
                return Results.NotFound();
            }

            existingGame.Name = updatedGame.Name;
            existingGame.GenreId = updatedGame.GenreId;
            existingGame.Price = updatedGame.Price;
            existingGame.ReleaseDate = updatedGame.ReleaseDate;

            await dbContext.SaveChangesAsync();

           return Results.NoContent();
        })
        .RequireAuthorization("AdminOnly");

        //DELETE /games/1
        group.MapDelete("/{id}", async (int id, GameStoreContext dbContext) =>
        {
            await dbContext.Games
                            .Where(game => game.Id == id)
                            .ExecuteDeleteAsync();

            return Results.NoContent();
        })
        .RequireAuthorization("AdminOnly");
    }
}
