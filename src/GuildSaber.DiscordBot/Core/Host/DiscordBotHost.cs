using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GuildSaber.DiscordBot.Core.Handlers;
using GuildSaber.DiscordBot.Core.Options;
using Microsoft.Extensions.Options;

namespace GuildSaber.DiscordBot.Core.Host;

public class DiscordBotHost(
    IOptions<DiscordBotOptions> options,
    DiscordSocketClient client,
    InteractionService interactionService,
    InteractionHandler interactionHandler,
    ILogger<DiscordBotHost> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await interactionHandler.InitializeAsync();
        await client.SetGameAsync(options.Value.Status);

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
}