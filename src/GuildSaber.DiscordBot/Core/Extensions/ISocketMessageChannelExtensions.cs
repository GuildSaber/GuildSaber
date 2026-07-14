using Discord.WebSocket;

namespace GuildSaber.DiscordBot.Core.Extensions;

public static class SocketInteractionExtensions
{
    extension(SocketInteraction self)
    {
        public async Task<Exception> FollowupAsyncReturnException(string text)
        {
            await self.FollowupAsync(text);
            return new InvalidOperationException(text);
        }

        public async Task<Exception> RespondAsyncReturnException(string text)
        {
            await self.RespondAsync(text);
            return new InvalidOperationException(text);
        }

        public async Task<T> FollowupAsyncReturning<T>(string text, T returnValue)
        {
            await self.FollowupAsync(text);
            return returnValue;
        }

        public async Task<T> RespondAsyncReturning<T>(string message, T returnValue)
        {
            await self.RespondAsync(message);
            return returnValue;
        }
    }
}