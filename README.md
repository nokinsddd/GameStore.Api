# 🎮 GameStore.Api

> REST API for managing a catalog of games, categories, and users. An educational project demonstrating backend development skills with **.NET 10**.

[![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com)
[![SQLite](https://img.shields.io/badge/SQLite-3-003B57?style=flat&logo=sqlite)](https://www.sqlite.org)
[![EF Core](https://img.shields.io/badge/EF_Core-8-623194?style=flat&logo=dotnet)](https://learn.microsoft.com/ef/core)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

---

## 🚀 Quick Start

### Requirements
- .NET 10 SDK
- (Optional) Visual Studio 2022 / Rider / VS Code

### 🔹 Run via .NET CLI
```bash
# Restore dependencies
dotnet restore

# Apply migrations (for initial database setup)
dotnet ef database update

# Run the application
dotnet run --project GameStore.Api.csproj
