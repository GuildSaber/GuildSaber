using Discord;
using Discord.Interactions;
using GuildSaber.CSharpClient;
using GuildSaber.DiscordBot.Core.Extensions;
using Microsoft.Extensions.Caching.Hybrid;

namespace GuildSaber.DiscordBot.AutocompleteHandlers;

public class CategoryAutocompleteHandler : AutocompleteHandler
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

        var categories = await cache.GetGuildCategoriesAsync(guildId.Value, client);
        return AutocompletionResult.FromSuccess(categories
            .Select(c => new AutocompleteResult(c.Info.Name, c.Id))
            .Take(AutocompletionResult.MaxSuggestionCount)
        );
    }
}