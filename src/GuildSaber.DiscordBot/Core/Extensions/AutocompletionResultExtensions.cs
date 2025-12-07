using Discord.Interactions;

namespace GuildSaber.DiscordBot.Core.Extensions;

public static class AutocompletionResultExtensions
{
    extension(AutocompletionResult)
    {
        /// <summary>
        /// The maximum number of suggestions that can be returned in an autocompletion response due to API limits.
        /// </summary>
        public static int MaxSuggestionCount => 25;
    }
}