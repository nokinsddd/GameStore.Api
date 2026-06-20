using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GameStore.Api.Data;
using GameStore.Api.DTOs;
using GameStore.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.EndPoints;

public static class ReviewsEndpoints
{
    public static void MapReviewsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/games/{gameId:int}/reviews");

        // get reviews for a game
        group.MapGet("/", async (int gameId, GameStoreContext db) =>
        {
            var reviews = await db.Reviews
                .Where(r => r.GameId == gameId)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewDto(
                    r.Id,
                    r.GameId,
                    r.User.Username,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt
                ))
                .ToListAsync();

            return Results.Ok(reviews);
        });

        // add review endpoint (only for authenticated users)
        group.MapPost("/", async (
            int gameId, 
            [FromBody] CreateReviewRequest request, 
            GameStoreContext db, 
            HttpContext httpContext) =>
        {
            // 1. Автоматическая валидация DTO
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
                    { "Review", errors }
                });
            }

            // 2. check if the game exists
            var gameExists = await db.Games.AnyAsync(g => g.Id == gameId);
            if (!gameExists) return Results.NotFound("Игра не найдена");

            // 3. get id of the current user from JWT
            var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return Results.Unauthorized();
            }

            // 4. check if the user has already left a review for this game
            var existingReview = await db.Reviews.FirstOrDefaultAsync(r => r.GameId == gameId && r.UserId == userId);
            if (existingReview != null)
            {
                return Results.Conflict("Вы уже оставляли отзыв на эту игру");
            }

            // 5. create and save the new review
            var review = new Review
            {
                GameId = gameId,
                UserId = userId,
                Rating = request.Rating,
                Comment = request.Comment
            };

            db.Reviews.Add(review);
            await db.SaveChangesAsync();

            return Results.Created($"/games/{gameId}/reviews/{review.Id}", review);
        })
        .RequireAuthorization("UserOnly");

        // Update review endpoint (only for the user who created it)
    group.MapPut("/{reviewId:int}", async (
        int gameId, 
        int reviewId,
        [FromBody] CreateReviewRequest request, 
        GameStoreContext db, 
        HttpContext httpContext) =>
    {
        // 1. DTO validation
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
                { "Review", errors }
            });
        }

        // 2. get user ID from JWT
        var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out int userId))
        {
            return Results.Unauthorized();
        }

        // 3. search for the review in the database
        var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.GameId == gameId);
    
        if (review == null)
        {   
            return Results.NotFound("Отзыв не найден");
        }

        // 4. check if the review belongs to the current user
        if (review.UserId != userId)
        {
            return Results.Forbid(); // 403 Forbidden - нельзя менять чужие отзывы
        }

        // 5. update the review
        review.Rating = request.Rating;
        review.Comment = request.Comment;
    
        await db.SaveChangesAsync();

        return Results.Ok(new ReviewDto(
            review.Id,
            review.GameId,
            httpContext.User.FindFirstValue(ClaimTypes.Name) ?? "Unknown",
            review.Rating,
            review.Comment,
            review.CreatedAt
        ));
    })
    .RequireAuthorization("UserOnly");


    // delete review endpoint (only for the user who created it)
    group.MapDelete("/{reviewId:int}", async (
        int gameId, 
        int reviewId,
        GameStoreContext db, 
        HttpContext httpContext) =>
    {
        // 1. get id of the current user from JWT
        var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out int userId))
        {
            return Results.Unauthorized();
        }

        // 2. search for the review in the database
        var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.GameId == gameId);
    
        if (review == null)
        {
            return Results.NotFound("Отзыв не найден");
        }

        // 3. check if the review belongs to the current user
        if (review.UserId != userId)
        {
            return Results.Forbid(); // 403 Forbidden
        }

        // delete the review
        db.Reviews.Remove(review);
        await db.SaveChangesAsync();

        return Results.NoContent(); // 204 No Content
    })
    .RequireAuthorization("UserOnly");
    
    }
}