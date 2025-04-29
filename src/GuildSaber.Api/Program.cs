using GuildSaber.Api;
using GuildSaber.Database.Contexts;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddMySqlDbContext<GSDbContext>("guildsaber-db");
builder.Services.AddOpenApi();

var app = builder.Build();
app.MapDefaultEndpoints()
    .MapOpenApi();

app.MapScalarApiReference(options =>
{
    options.WithTitle("GuildSaber's Api")
        .WithTheme(ScalarTheme.Purple)
        .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch);
});
app.MapGet("/weatherforecast", () =>
    {
        string[] summaries =
            ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

app.Run();