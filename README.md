# 🎮 GameStore.Api

> REST API for managing a catalog of games, categories, and users. An educational project demonstrating backend development skills with **.NET 10**.

[![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?style=flat&logo=postgresql)](https://www.postgresql.org)
[![EF Core](https://img.shields.io/badge/EF_Core-10-623194?style=flat&logo=dotnet)](https://learn.microsoft.com/ef/core)

---

## 🚀 Quick Start

### Requirements
- .NET 10 SDK
- PostgreSQL 16+ (database server)
- (Optional) Visual Studio 2022 / Rider / VS Code

### 🔹 Setup
1. Install PostgreSQL and create a database named `gamestore`
2. Update connection string in `appsettings.json` if needed (default: `localhost:5432`)

### 🔹 Run via .NET CLI
```bash
# Restore dependencies
dotnet restore

# Apply migrations (for initial database setup)
dotnet ef database update

# Run the application
dotnet run --project GameStore.Api.csproj
```

### 🔐 Configuration
- Connection string: Modify `appsettings.json` or set `ConnectionStrings__GameStore` environment variable
- Note: Default credentials (`postgres:postgres`) are for development only. Use strong credentials in production.

## 📝 API Endpoints

### Games
- `GET /games` - Get all games
- `GET /games/{id}` - Get game by ID
- `POST /games` - Create game
- `PUT /games/{id}` - Update game
- `DELETE /games/{id}` - Delete game

### Genres
- `GET /genres` - Get all genres
- `POST /genres` - Create genre

API documentation available at: `http://localhost:5000/scalar`
