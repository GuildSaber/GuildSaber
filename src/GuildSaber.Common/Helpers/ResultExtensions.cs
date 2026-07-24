using CSharpFunctionalExtensions;

namespace GuildSaber.Common.Helpers;

public static class ResultExtensions
{
    /// <summary>
    /// Reduces a collection of Result objects into a single Result object.
    /// </summary>
    /// <typeparam name="T">The type of the success value in the Result objects.</typeparam>
    /// <typeparam name="E">The type of the error value in the Result objects.</typeparam>
    /// <param name="results">An IEnumerable of Result objects to be reduced.</param>
    /// <returns>
    /// A Result object that contains a collection of success values if all Result objects in the input collection are
    /// successful.
    /// If any Result object in the input collection is a failure, the function immediately returns a failure Result with the
    /// error of the first encountered failure.
    /// </returns>
    public static Result<IEnumerable<T>, E> Reduce<T, E>(this IEnumerable<Result<T, E>> results)
    {
        var temp = new List<T>();
        foreach (var y in results)
        {
            if (y.IsFailure) return Failure<IEnumerable<T>, E>(y.Error);
            temp.Add(y.Value);
        }

        return Success<IEnumerable<T>, E>(temp);
    }

    public static Task<Result<T>> TryAsync<T>(
        Func<Task<T>> func,
        CancellationToken token) => TryAsync(func, Configuration.DefaultTryErrorHandler, token);

    public static async Task<Result<T>> TryAsync<T>(
        Func<Task<T>> func,
        Func<Exception, string> errorHandler,
        CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            var value = await func().ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            return Success(value);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Failure<T>(errorHandler(exception));
        }
    }
}