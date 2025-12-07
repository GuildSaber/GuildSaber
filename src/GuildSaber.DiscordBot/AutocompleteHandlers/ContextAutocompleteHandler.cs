using Discord;
using Discord.Interactions;
using GuildSaber.CSharpClient;
using GuildSaber.DiscordBot.Core.Extensions;
using Microsoft.Extensions.Caching.Hybrid;

namespace GuildSaber.DiscordBot.AutocompleteHandlers;

public class ContextAutocompleteHandler : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context, IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter, IServiceProvider services)
    {
        if (context.Guild is null)
            return AutocompletionResult.FromSuccess();

        var cache = services.GetRequiredService<HybridCache>();
        var client = GuildSaberClient.GetAuthenticatedClient(context.User.DiscordId, services);

        var guildId = await cache.FindGuildIdFromDiscordGuildIdAsync(context.Guild.DiscordId, client);
        if (guildId is null) return AutocompletionResult.FromSuccess();

        var guildExtended = await cache.GetGuildExtendedAsync(guildId.Value, client);
        if (guildExtended is null) return AutocompletionResult.FromSuccess();

        return AutocompletionResult.FromSuccess(guildExtended.Contexts
            .Select(c => new AutocompleteResult(c.Info.Name, c.Id))
            .Take(AutocompletionResult.MaxSuggestionCount)
        );
    }
}