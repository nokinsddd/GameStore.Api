using GameStore.Api.Data;
using GameStore.Api.EndPoints;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using GameStore.Api.Endpoints;
using GameStore.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddGameStoreDb();
builder.Services.AddOpenApi();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = secretKey
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User", "Admin"));
});

//redis cache 
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "GameStore_";
});

builder.Services.AddMemoryCache();

builder.Services.AddSingleton<ICacheService, CacheService>();

var app = builder.Build();

Console.WriteLine($">>>Environment: {app.Environment.EnvironmentName}<<<");

app.UseAuthentication();
app.UseAuthorization();

app.MapGamesEndpoints();
app.MapGenresEndpoints();
app.MapAuthEndpoints();
app.MapReviewsEndpoints();

//scalar docs and openapi endpoints
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    Console.WriteLine(">>>Mapped OpenAPI and Sca    lar docs endpoints<<< \n >>> URL - http://localhost:5000/scalar <<<");
}

app.MigrateDb();

app.Run();