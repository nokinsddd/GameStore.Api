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
        app.MapGet("/games", async (GameStoreContext db, [AsParameters] GetGamesRequest request) =>
    {
    // 1. Валидация
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
    
        if (!Validator.TryValidateObject(request, context, results, true))
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

        // 2. Базовый запрос с Include
        var query = db.Games.Include(g => g.Genre).AsQueryable();

        // 3. Фильтрация по жанру
        if (request.GenreId.HasValue)
        {
            query = query.Where(g => g.GenreId == request.GenreId);
        }

        // 4. Фильтрация по цене
        if (request.MinPrice.HasValue)
        {
            query = query.Where(g => g.Price >= request.MinPrice);
        }
        if (request.MaxPrice.HasValue)
        {
            query = query.Where(g => g.Price <= request.MaxPrice);
        }

        // 5. Поиск по названию (case-insensitive)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(g => g.Name.Contains(request.Search));
        }

        // 6. Сортировка
        query = request.SortBy?.ToLower() switch
        {
            "price" => request.SortOrder?.ToLower() == "desc" 
                ? query.OrderByDescending(g => g.Price) 
                : query.OrderBy(g => g.Price),
            "name" => request.SortOrder?.ToLower() == "desc" 
                ? query.OrderByDescending(g => g.Name) 
                : query.OrderBy(g => g.Name),
            "releasedate" => request.SortOrder?.ToLower() == "desc" 
                ? query.OrderByDescending(g => g.ReleaseDate) 
                : query.OrderBy(g => g.ReleaseDate),
            _ => query.OrderBy(g => g.Id) // По умолчанию сортировка по ID
        };

        // 7. Считаем метаданные
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        // 8. Применяем пагинацию и проекцию
        var games = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(game => new GameSummaryDto(
                game.Id,
                game.Name,
                game.Genre != null ? game.Genre.Name : "Не указан",
                game.Price,
                game.ReleaseDate
            ))
            .ToListAsync();

        // 9. Возвращаем результат
        return Results.Ok(new PagedResult<GameSummaryDto>(
            games, 
            totalCount, 
            request.PageNumber, 
            request.PageSize, 
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
