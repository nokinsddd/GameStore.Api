using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GameStore.Api.Data;
using GameStore.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace GameStore.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth");

        // Registration endpoint
        group.MapPost("/register", async ([FromBody] RegisterRequest request, GameStoreContext db) =>
        {
            if (await db.Users.AnyAsync(u => u.Username == request.Username))
                return Results.Conflict("User already exists");

            var user = new User
            {
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = "User"
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return Results.Ok(new { Message = "User registered successfully" });
        });

        // Login endpoint
        group.MapPost("/login", async ([FromBody] LoginRequest request, GameStoreContext db, IConfiguration config) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Results.Unauthorized();

            // generate JWT token
            var jwtSettings = config.GetSection("JwtSettings");
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
            var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpireMinutes"]!)),
                signingCredentials: credentials
            );

            return Results.Ok(new { Token = new JwtSecurityTokenHandler().WriteToken(token) });
        });

        // Admin-only endpoint to promote a user to admin
        group.MapPost("/make-admin", async ([FromBody] MakeAdminRequest request, GameStoreContext db) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
    
            if (user == null)
            return Results.NotFound("User not found");

            user.Role = "Admin";
            await db.SaveChangesAsync();

            return Results.Ok(new { Message = $"User {user.Username} is now an administrator" });
    });
    }
}

// DTOs for requests
public record RegisterRequest(string Username, string Password);
public record LoginRequest(string Username, string Password);
public record MakeAdminRequest(string Username);


