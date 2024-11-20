using GuildSaber.Database.Contexts;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
    .AddMySqlDbContext<GSDbContext>("mysqldb", static settings => settings.ServerVersion = new MariaDbServerVersion(new Version(10, 6, 11)).ToString());
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

app.UseHttpsRedirection();
app.Run();