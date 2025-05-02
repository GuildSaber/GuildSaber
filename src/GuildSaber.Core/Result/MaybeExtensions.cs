using CSharpFunctionalExtensions;

namespace GuildSaber.Core.Result;

public static class MaybeExtensions
{
    public static async Task Match<T>(this Task<Maybe<T>> self, Action<T> some, Action none)
        => (await self.ConfigureAwait(false)).Match(some, none);

    public static async Task<K> Match<T, K>(this Task<Maybe<T>> self, Func<T, K> some, Func<K> none)
        => (await self.ConfigureAwait(false)).Match(some, none);
}