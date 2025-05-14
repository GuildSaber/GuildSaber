using GuildSaber.Api.Features.Guilds._guildId_.Members;
using GuildSaber.Api.Hangfire;
using GuildSaber.Api.Hangfire.Configuration;
using GuildSaber.Database;
using GuildSaber.Database.Contexts.Server;
using Hangfire;
using Hangfire.Redis.StackExchange;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptionsWithValidateOnStart<RetryPolicyOptions>()
    .Bind(builder.Configuration.GetSection(RetryPolicyOptions.RetryPolicyOptionsSectionsKey)).ValidateDataAnnotations();

var connectionMultiplexer = ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("cache")!);

builder.AddServiceDefaults();
builder.Services.AddHangfire((serviceCollection, option) =>
{
    option.UseSimpleAssemblyNameTypeSerializer();
    option.UseRecommendedSerializerSettings();
    option.UseRedisStorage(connectionMultiplexer);
    option.UseActivator(new HangfireActivator(serviceCollection));
    option.UseFilter(new AutomaticRetryAttribute
    {
        Attempts = builder.Configuration.GetSection(RetryPolicyOptions.RetryPolicyOptionsSectionsKey)
            .Get<RetryPolicyOptions>()!.MaxRetryAttempts
    });
}).AddHangfireServer();

builder.AddMySqlDbContext<ServerDbContext>(connectionName: Constants.ServerDbConnectionStringKey);
builder.Services.AddOpenApi();

var app = builder.Build();
app.MapDefaultEndpoints()
    .MapOpenApi();

app.MapHangfireDashboard(new DashboardOptions
{
    Authorization = [new HangFireDashboardAuthorizationFilter()]
});

app.MapScalarApiReference(options =>
{
    options.WithTitle("GuildSaber's Api")
        .WithTheme(ScalarTheme.Purple)
        .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch);
});

app.MapGet("/guilds/{guildId}/members/{playerId}", ([FromServices] ServerDbContext db, [FromServices] ILogger log) =>
    new Get(db, log).GetMember);

app.Run();