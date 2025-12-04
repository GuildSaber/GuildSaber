using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GuildSaber.Database.Contexts.DiscordBot;
using GuildSaber.Database.Models.DiscordBot;
using GuildSaber.DiscordBot.Core.Handlers;
using GuildSaber.DiscordBot.Core.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GuildSaber.DiscordBot.Core.Host;

public class DiscordBotHost(
    IOptions<DiscordBotOptions> options,
    IServiceProvider services,
    DiscordSocketClient client,
    InteractionService interactionService,
    InteractionHandler interactionHandler,
    ILogger<DiscordBotHost> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await interactionHandler.InitializeAsync();
        await client.SetGameAsync(options.Value.Status);
        //TODO: await using service scope?
        var dbContext = services.GetRequiredService<DiscordBotDbContext>();

        await EnsureManagerExistAsync(dbContext, options.Value);

        client.Log += msg =>
        {
            logger.LogInformation("[DiscordClient] {msg}", msg);
            return Task.CompletedTask;
        };

        interactionService.Log += msg =>
        {
            logger.LogInformation("[Discord InteractionService] {msg}", msg);
            return Task.CompletedTask;
        };

        client.Ready += () =>
        {
            logger.LogInformation("[DiscordClient] DiscordClient Ready.");
            interactionService.RegisterCommandsToGuildAsync(options.Value.GuildId);

            return Task.CompletedTask;
        };

        await client.LoginAsync(TokenType.Bot, options.Value.Token);
        await client.StartAsync();

        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => client.StopAsync();

    /// <summary>
    /// Setup the database and ensure the manager user exists.
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="options"></param>
    private static async Task EnsureManagerExistAsync(DiscordBotDbContext dbContext, DiscordBotOptions options)
    {
        if (!await dbContext.Users.AnyAsync(x => x.Id == options.ManagerId))
            dbContext.Users.Add(new User
            {
                Id = options.ManagerId,
                Permissions = User.EPermissions.Manager
            });

        await dbContext.SaveChangesAsync();
    }
}