namespace GuildSaber.Common.Helpers;

public static class TaskExtensions
{
    public static async Task<(T, U)> WhenAll<T, U>(this (Task<T>, Task<U>) tasks)
    {
        await Task.WhenAll(tasks.Item1, tasks.Item2);
        return (tasks.Item1.Result, tasks.Item2.Result);
    }

    public static async Task<(T, U, K)> WhenAll<T, U, K>(this (Task<T>, Task<U>, Task<K>) tasks)
    {
        await Task.WhenAll(tasks.Item1, tasks.Item2, tasks.Item3);
        return (tasks.Item1.Result, tasks.Item2.Result, tasks.Item3.Result);
    }

    public static async Task<(T, U, K, V)> WhenAll<T, U, K, V>(this (Task<T>, Task<U>, Task<K>, Task<V>) tasks)
    {
        await Task.WhenAll(tasks.Item1, tasks.Item2, tasks.Item3, tasks.Item4);
        return (tasks.Item1.Result, tasks.Item2.Result, tasks.Item3.Result, tasks.Item4.Result);
    }

    public static async Task<(T, U, K, V, W)> WhenAll<T, U, K, V, W>(
        this (Task<T>, Task<U>, Task<K>, Task<V>, Task<W>) tasks)
    {
        await Task.WhenAll(tasks.Item1, tasks.Item2, tasks.Item3, tasks.Item4, tasks.Item5);
        return (tasks.Item1.Result, tasks.Item2.Result, tasks.Item3.Result, tasks.Item4.Result, tasks.Item5.Result);
    }
}