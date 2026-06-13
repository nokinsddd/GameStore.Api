using GameStore.Api.Data;
using GameStore.Api.EndPoints;
using Scalar.AspNetCore;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddValidation();
builder.AddGameStoreDb();
builder.Services.AddOpenApi();

var app = builder.Build();

Console.WriteLine($">>>Environment: {app.Environment.EnvironmentName}<<<");

app.MapGamesEndpoints();
app.MapGenresEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();              // Generates /openapi/v1.json
    app.MapScalarApiReference();   // Serves docs at /scalar
    Console.WriteLine(">>>Mapped OpenAPI and Scalar docs endpoints<<< \n >>> URL - http://localhost:5000/scalar <<<");
}


app.MigrateDb();

app.Run();
