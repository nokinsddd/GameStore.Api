using System;
using Microsoft.EntityFrameworkCore;
using GameStore.Api.Data;
using GameStore.Api.dtos;
using GameStore.Api.Models;
using GameStore.Api.DTOs;
using System.ComponentModel.DataAnnotations;
using GameStore.Api.Services; 

namespace GameStore.Api.EndPoints;

public static class GamesEndpoints
{
    const string GetGameEndpointName = "GetGame";
    
    public static void MapGamesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/games");

        // GET /games - cached with pagination, filtering, and sorting
        app.MapGet("/games", async (
            GameStoreContext db, 
            ICacheService cache,
            [AsParameters] GetGamesRequest request) =>
        {
            // 1. Validation of request parameters
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

            // 2. Make unique key based on request parameters for caching
            var cacheKey = $"games_page{request.PageNumber}_size{request.PageSize}_" +
                           $"sort{request.SortBy}_{request.SortOrder}_" +
                           $"genre{request.GenreId}_price{request.MinPrice}_{request.MaxPrice}_" +
                           $"search{request.Search}";

            try
            {
                var result = await cache.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        // pagination, filtering, and sorting logic
                        var query = db.Games.Include(g => g.Genre).AsQueryable();

                        if (request.GenreId.HasValue)
                            query = query.Where(g => g.GenreId == request.GenreId);

                        if (request.MinPrice.HasValue)
                            query = query.Where(g => g.Price >= request.MinPrice);

                        if (request.MaxPrice.HasValue)
                            query = query.Where(g => g.Price <= request.MaxPrice);

                        if (!string.IsNullOrWhiteSpace(request.Search))
                            query = query.Where(g => g.Name.Contains(request.Search));

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
                            _ => query.OrderBy(g => g.Id)
                        };

                        var totalCount = await query.CountAsync();
                        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

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

                        return new PagedResult<GameSummaryDto>(
                            games, totalCount, request.PageNumber, request.PageSize, totalPages
                        );
                    },
                    TimeSpan.FromMinutes(5)
                );

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                // Fallback if Redis cache fails, query the database directly
                Console.WriteLine($"Cache error: {ex.Message}. Fallback to database.");
                
                var query = db.Games.Include(g => g.Genre).AsQueryable();

                if (request.GenreId.HasValue)
                    query = query.Where(g => g.GenreId == request.GenreId);

                if (request.MinPrice.HasValue)
                    query = query.Where(g => g.Price >= request.MinPrice);

                if (request.MaxPrice.HasValue)
                    query = query.Where(g => g.Price <= request.MaxPrice);

                if (!string.IsNullOrWhiteSpace(request.Search))
                    query = query.Where(g => g.Name.Contains(request.Search));

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
                    _ => query.OrderBy(g => g.Id)
                };

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

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

                return Results.Ok(new PagedResult<GameSummaryDto>(
                    games, totalCount, request.PageNumber, request.PageSize, totalPages
                ));
            }
        });

        // GET /games/{id} - cached
        group.MapGet("/{id}", async (int id, GameStoreContext dbContext, ICacheService cache) =>
        {
            try
            {
                var game = await cache.GetOrSetAsync(
                    $"game_{id}",
                    async () =>
                    {
                        var g = await dbContext.Games.FindAsync(id);
                        if (g is null) return null;
                        
                        return new GameDetailsDto(
                            g.Id,
                            g.Name,
                            g.GenreId,
                            g.Price,
                            g.ReleaseDate
                        );
                    },
                    TimeSpan.FromMinutes(10)  // Кэш на 10 минут
                );

                return game is null ? Results.NotFound() : Results.Ok(game);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cache error: {ex.Message}. Fallback to database.");
                var g = await dbContext.Games.FindAsync(id);
                return g is null ? Results.NotFound() : Results.Ok(
                    new GameDetailsDto(g.Id, g.Name, g.GenreId, g.Price, g.ReleaseDate)
                );
            }
        })
        .WithName(GetGameEndpointName);

        // POST /games - With cache invalidation
        group.MapPost("/", async (CreateGameDto newGame, GameStoreContext dbContext, ICacheService cache) =>
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

            // Invalidate cache for all games and specific game
            await cache.RemoveAsync("games_*");
            
            GameDetailsDto gameDto = new(
                game.Id,
                game.Name,
                game.GenreId,
                game.Price,
                game.ReleaseDate
            );

            return Results.CreatedAtRoute(GetGameEndpointName, new { id = gameDto.Id }, gameDto);
        })
        .RequireAuthorization("UserOnly");

        // PUT /games/{id} - With cache invalidation
        group.MapPut("/{id}", async (
            int id,
            UpdateGameDto updatedGame,
            GameStoreContext dbContext,
            ICacheService cache) =>
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

            // Cache invalidation for the updated game and the list of games
            await cache.RemoveAsync($"game_{id}");
            await cache.RemoveAsync("games_*");

            return Results.NoContent();
        })
        .RequireAuthorization("AdminOnly");

        // DELETE /games/{id} - with cache invalidation
        group.MapDelete("/{id}", async (int id, GameStoreContext dbContext, ICacheService cache) =>
        {
            await dbContext.Games
                .Where(game => game.Id == id)
                .ExecuteDeleteAsync();

            // Cache invalidation for the deleted game and the list of games
            await cache.RemoveAsync($"game_{id}");
            await cache.RemoveAsync("games_*");

            return Results.NoContent();
        })
        .RequireAuthorization("AdminOnly");
    }
}