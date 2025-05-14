namespace GuildSaber.Common.Helpers;

public static class FunctionalUtils
{
    /// <summary>
    /// Repeatedly executes the provided function until the result satisfies the given predicate.
    /// </summary>
    /// <typeparam name="T">The type of the result produced by the function.</typeparam>
    /// <param name="func">The function to be executed repeatedly.</param>
    /// <param name="predicate">The predicate that the result must satisfy to stop execution.</param>
    public static T RunUntil<T>(Func<T> func, Predicate<T> predicate)
    {
        T result;
        do
        {
            result = func();
        } while (!predicate(result));

        return result;
    }

    /// <summary>
    /// Repeatedly executes the provided asynchronous function until the result satisfies the given predicate.
    /// </summary>
    /// <typeparam name="T">The type of the result produced by the function.</typeparam>
    /// <param name="func">The asynchronous function to be executed repeatedly.</param>
    /// <param name="predicate">The predicate that the result must satisfy to stop execution.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async Task<T> RunUntilAsync<T>(Func<Task<T>> func, Predicate<T> predicate)
    {
        T result;
        do
        {
            result = await func();
        } while (!predicate(result));

        return result;
    }

    /// <summary>
    /// Repeatedly executes the provided asynchronous function until the result satisfies the given predicate.
    /// </summary>
    /// <typeparam name="T">The type of the result produced by the function.</typeparam>
    /// <param name="func">A task that returns an asynchronous function to be executed repeatedly.</param>
    /// <param name="predicate">The predicate that the result must satisfy to stop execution.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async Task<T> RunUntilAsync<T>(Func<Task<Task<T>>> func, Predicate<T> predicate)
    {
        T result;
        do
        {
            result = await await func();
        } while (!predicate(result));

        return result;
    }
}