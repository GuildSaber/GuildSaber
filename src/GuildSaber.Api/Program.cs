using GuildSaber.Api.Endpoints.Internal;
using GuildSaber.Api.Hangfire;
using GuildSaber.Api.Hangfire.Configuration;
using GuildSaber.Database;
using GuildSaber.Database.Contexts.Server;
using Hangfire;
using Hangfire.Redis.StackExchange;
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
    }).AddHangfireServer()
    .AddProblemDetails();

builder.AddMySqlDbContext<ServerDbContext>(connectionName: Constants.ServerDbConnectionStringKey);
builder.Services.AddOpenApi();
builder.Services.AddEndpoints<Program>(builder.Configuration);

var app = builder.Build();

app.MapOpenApi();
app.MapDefaultEndpoints()
    .MapEndpoints<Program>();

app.MapHangfireDashboard(new DashboardOptions
{
    Authorization = [new HangFireDashboardAuthorizationFilter()]
});

app.MapScalarApiReference("/", options =>
{
    options.WithTitle("GuildSaber's Api")
        .WithTheme(ScalarTheme.Purple)
        .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch);
});

app.Run();