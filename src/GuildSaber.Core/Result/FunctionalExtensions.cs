using CSharpFunctionalExtensions;
using GuildSaber.Core.Helpers;
using static CSharpFunctionalExtensions.Result;

namespace GuildSaber.Core.Result;

/// <summary>
/// Provides a set of extension methods to enhance the functionality of the Result class from the
/// CSharpFunctionalExtensions library.
/// These extensions are designed to address specific needs that are not covered by the original library, making it more
/// feature-rich and adaptable to various scenarios.
/// </summary>
public static class FunctionalExtensions
{
    /// <summary>
    /// Returns a wrapped task which either provides a failed result when the task throws, or a success result containing a
    /// Maybe from the task result.
    /// The Maybe represents the task result if it's not null or default, otherwise it's an empty Maybe (None).
    /// This method works with both reference types and value types (structs).
    /// </summary>
    /// <param name="task">The task that may return a null or default value.</param>
    /// <typeparam name="T">The type of the result value of the task.</typeparam>
    /// <returns>A wrapped Task which either fails when the task throws, or returns a Maybe from the task success result.</returns>
    /// <remarks>If it's a value type, it's default value translates to None</remarks>
    public static Task<Result<Maybe<T>, Exception>> TryMaybe<T>(this Task<T?> task)
        => Try(async () => await task, exception => exception)
            .Map(value => Check<T>.IsNullOrDefault(value!) ? Maybe<T>.None : Maybe<T>.From(value!));

    /// <summary>
    /// Returns a wrapped task which either provides a failed result when the task throws, or a success result containing a
    /// Maybe from the first element of the task result array.
    /// The Maybe represents the first element of the task result array if it exists, otherwise it's an empty Maybe (None).
    /// This method works with both reference types and value types (structs).
    /// </summary>
    /// <param name="task">The task that may return an array of values.</param>
    /// <typeparam name="T">The type of the elements in the task result array.</typeparam>
    /// <returns>
    /// A wrapped Task which either fails when the task throws, or returns a Maybe from the first element of the task
    /// success result array.
    /// </returns>
    public static Task<Result<Maybe<T>, Exception>> TryFirstMaybe<T>(this Task<T[]> task)
        => Try(async () => await task, exception => exception)
            .Map(value => value.Length is not 0 ? Maybe<T>.From(value[0]) : Maybe<T>.None);

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

        return temp;
    }

    /// <summary>
    /// Asynchronously maps and reduces an array of Result items into a Result of items.
    /// </summary>
    /// <typeparam name="T">The type of the items in the input array.</typeparam>
    /// <typeparam name="R">The type of the success value in the Result object.</typeparam>
    /// <typeparam name="E">The type of the error value in the Result object.</typeparam>
    /// <param name="items">An array of items to be mapped and reduced.</param>
    /// <param name="mapper">A function that maps each item to a Task that returns a Result object.</param>
    /// <returns>
    /// A Task that returns a Result object. If all tasks complete successfully, the Result object indicates success and
    /// contains an array of the success values.
    /// If any task fails, the function immediately returns a failure Result with the error of the first encountered failure.
    /// </returns>
    public static async Task<Result<IReadOnlyCollection<R>, E>> MapReduce<T, R, E>(
        this IReadOnlyList<T> items, Func<T, Task<Result<R, E>>> mapper)
    {
        var count = items.Count;
        var temp = new R[count];

        for (var i = 0; i < count; i++)
        {
            var item = items[i];
            var result = await mapper(item);
            if (result.IsFailure) return Failure<IReadOnlyCollection<R>, E>(result.Error);

            temp[i] = result.Value;
        }

        return Success<IReadOnlyCollection<R>, E>(temp);
    }

    /// <summary>
    /// Asynchronously maps and reduces a collection of items into a tuple containing the results and errors.
    /// </summary>
    /// <typeparam name="T">The type of the items in the input collection.</typeparam>
    /// <typeparam name="R">The type of the success value in the Result object.</typeparam>
    /// <typeparam name="E">The type of the error value in the Result object.</typeparam>
    /// <param name="items">A collection of items to be mapped and reduced.</param>
    /// <param name="mapper">A function that maps each item to a Task that returns a Result object.</param>
    /// <returns>
    /// A Task that returns a tuple. The first element of the tuple is a collection of the success values.
    /// The second element of the tuple is a collection of the error values.
    /// </returns>
    public static async Task<(IReadOnlyCollection<R> items, IReadOnlyCollection<E> errors)> MapReduceToTuple<T, R, E>(
        this IReadOnlyList<T> items, Func<T, Task<Result<R, E>>> mapper)
    {
        var count = items.Count;
        var temp = new List<R>(count);
        var errors = new List<E>();

        for (var i = 0; i < count; i++)
        {
            var item = items[i];
            var result = await mapper(item);
            if (result.IsFailure)
            {
                errors.Add(result.Error);
                continue;
            }

#pragma warning disable CFE0001 // Failure case handled above (Analyser bug)
            temp.Add(result.Value);
#pragma warning restore CFE0001
        }

        return (temp, errors);
    }

    /// <summary>
    /// Executes a function within a disposable scope. Then, if the function is successful, it executes an onSuccess action,
    /// otherwise it executes an onError action.
    /// </summary>
    /// <typeparam name="T">The type of the success value in the Result object.</typeparam>
    /// <typeparam name="E">The type of the error value in the Result object.</typeparam>
    /// <typeparam name="TScoped">The type of the disposable scope.</typeparam>
    /// <typeparam name="TResult">The type of the success value in the returned Result object.</typeparam>
    /// <typeparam name="TError">The type of the error value in the returned Result object.</typeparam>
    /// <param name="self">The Result object to be processed.</param>
    /// <param name="scopedFunc">A function that creates the disposable scope.</param>
    /// <param name="f">A function that processes the Result object providing the disposable scope .</param>
    /// <param name="onSuccess">An action to be executed if the function is successful.</param>
    /// <param name="onError">An action to be executed if the function fails.</param>
    /// <returns>A Result object that represents the outcome of the function.</returns>
    public static Result<TResult, TError> WithTapScope<T, E, TScoped, TResult, TError>(
        this Result<T, E> self, Func<TScoped> scopedFunc, Func<Result<T, E>, TScoped, Result<TResult, TError>> f,
        Action<TScoped> onSuccess, Action<TScoped> onError)
        where TScoped : IDisposable
    {
        using var scoped = scopedFunc();
        var result = f(self, scoped);

        if (result.IsSuccess) onSuccess(scoped);
        else onError(scoped);

        return result;
    }

    /// <summary>
    /// Asynchronously executes a function within a disposable scope. Then, if the function is successful, it executes an
    /// onSuccess
    /// action,
    /// otherwise it executes an onError action. The function returns a UnitResult, which represents an operation that does not
    /// produce a meaningful value.
    /// </summary>
    /// <typeparam name="T">The type of the success value in the Result object.</typeparam>
    /// <typeparam name="E">The type of the error value in the Result object.</typeparam>
    /// <typeparam name="TScoped">The type of the disposable scope.</typeparam>
    /// <typeparam name="TError">The type of the error value in the returned UnitResult object.</typeparam>
    /// <param name="self">The Task that returns a Result object to be processed.</param>
    /// <param name="scopedFunc">A function that creates the disposable scope asynchronously.</param>
    /// <param name="f">
    /// A function that takes a Result object and the disposable scope and returns a Task that returns a Task that returns a
    /// UnitResult object.
    /// </param>
    /// <param name="onSuccess">An action to be executed if the function is successful.</param>
    /// <param name="onError">An action to be executed if the function fails.</param>
    /// <returns>A Task that returns a UnitResult object that represents the outcome of the function.</returns>
    /// <remarks>
    /// If the scope crashes, the crash will be silent. Use another overload if you want to map its exception.
    /// </remarks>
    public static async Task<UnitResult<TError>> WithTapScope<T, E, TScoped, TError>(
        this Task<Result<T, E>> self, Func<Task<TScoped>> scopedFunc,
        Func<Result<T, E>, TScoped, Task<UnitResult<TError>>> f,
        Action<TScoped> onSuccess, Action<TScoped> onError)
        where TScoped : IDisposable
    {
        var callerResult = await self;

        using var scoped = await scopedFunc();
        var result = await f(callerResult, scoped);

        if (result.IsSuccess) onSuccess(scoped);
        else onError(scoped);

        return result;
    }

    /// <summary>
    /// Asynchronously executes a function within a disposable scope. If the function is successful afterward, it executes an
    /// onSuccess
    /// action, otherwise it executes an onError action. If an exception occurs during the execution of the scope, it executes
    /// a mapScopeCrashed function.
    /// The function returns a UnitResult, which represents an operation that does not produce a meaningful value.
    /// </summary>
    /// <typeparam name="T">The type of the success value in the Result object.</typeparam>
    /// <typeparam name="E">The type of the error value in the Result object.</typeparam>
    /// <typeparam name="TScoped">The type of the disposable scope.</typeparam>
    /// <typeparam name="TError">The type of the error value in the returned UnitResult object.</typeparam>
    /// <param name="self">The Task that returns a Result object to be processed.</param>
    /// <param name="scopedFunc">A function that creates the disposable scope asynchronously.</param>
    /// <param name="f">
    /// A function that takes a Result object and the disposable scope and returns a Task that returns a UnitResult object.
    /// </param>
    /// <param name="onSuccess">An action to be executed if the function is successful.</param>
    /// <param name="onError">An action to be executed if the function fails.</param>
    /// <param name="mapException">
    /// A function to be executed if an exception occurs during the execution. It
    /// takes the exception as a parameter and returns a TError object. It also catches scope execution exceptions.
    /// </param>
    /// <returns>A Task that returns a UnitResult object that represents the outcome of the function.</returns>
    public static async Task<UnitResult<TError>> WithTapScope<T, E, TScoped, TError>(
        this Task<Result<T, E>> self, Func<Task<TScoped>> scopedFunc,
        Func<Result<T, E>, TScoped, Task<UnitResult<TError>>> f,
        Action<TScoped> onSuccess, Action<TScoped> onError,
        Func<Exception, TError> mapException)
        where TScoped : IDisposable
    {
        try
        {
            var callerResult = await self;

            using var scoped = await scopedFunc();
            var result = await f(callerResult, scoped);

            if (result.IsSuccess) onSuccess(scoped);
            else onError(scoped);

            return result;
        }
        catch (Exception e)
        {
            return mapException(e);
        }
    }

    /// <summary>
    /// Executes a function within a disposable scope. If the function is successful afterward, it executes an
    /// onSuccess action, otherwise it executes an onError action. If an exception occurs during the execution of the scope, it
    /// executes
    /// a mapScopeCrashed function. The function returns a UnitResult, which represents an operation that does not produce a
    /// meaningful value.
    /// </summary>
    /// <typeparam name="T">The type of the success value in the Result object.</typeparam>
    /// <typeparam name="E">The type of the error value in the Result object.</typeparam>
    /// <typeparam name="TScoped">The type of the disposable scope.</typeparam>
    /// <typeparam name="TError">The type of the error value in the returned UnitResult object.</typeparam>
    /// <param name="self">The Result object to be processed.</param>
    /// <param name="scopedFunc">A function that creates the disposable scope asynchronously.</param>
    /// <param name="f">
    /// A function that processes the Result object and the disposable scope and returns a Task that returns a UnitResult
    /// object.
    /// </param>
    /// <param name="onSuccess">An action to be executed if the function is successful.</param>
    /// <param name="onError">An action to be executed if the function fails.</param>
    /// <param name="mapException">
    /// A function to be executed if an exception occurs during the execution of the scope. It
    /// takes the exception as a parameter and returns a TError object. It also catches scope execution exceptions.
    /// </param>
    /// <returns>A Task that returns a UnitResult object that represents the outcome of the function.</returns>
    public static async Task<UnitResult<TError>> WithTapScope<T, E, TScoped, TError>(
        this Result<T, E> self, Func<Task<TScoped>> scopedFunc,
        Func<Result<T, E>, TScoped, Task<UnitResult<TError>>> f,
        Action<TScoped> onSuccess, Action<TScoped> onError,
        Func<Exception, TError> mapException)
        where TScoped : IDisposable
    {
        try
        {
            using var scoped = await scopedFunc();
            var result = await f(self, scoped);

            if (result.IsSuccess) onSuccess(scoped);
            else onError(scoped);

            return result;
        }
        catch (Exception e)
        {
            return mapException(e);
        }
    }


    /// <summary>
    /// Asynchronously executes a function within a disposable scope. If the function is successful afterward, it executes an
    /// onSuccess action, otherwise it executes an onError action. If an exception occurs during the execution of the scope, it
    /// executes
    /// a mapScopeCrashed function. The function returns a Result object, which either contains a success value or an error
    /// value.
    /// </summary>
    /// <typeparam name="T">The type of the success value in the Result object.</typeparam>
    /// <typeparam name="E">The type of the error value in the Result object.</typeparam>
    /// <typeparam name="TScoped">The type of the disposable scope.</typeparam>
    /// <typeparam name="TError">The type of the error value in the returned UnitResult object.</typeparam>
    /// <typeparam name="TResult">The type of the success value in the returned Result object.</typeparam>
    /// <param name="self">The Task that returns a Result object to be processed.</param>
    /// <param name="scopedFunc">A function that creates the disposable scope asynchronously.</param>
    /// <param name="f">
    /// A function that takes a Result object and the disposable scope, and returns a Task that returns a Result object.
    /// </param>
    /// <param name="onSuccess">An action to be executed if the function is successful.</param>
    /// <param name="onError">An action to be executed if the function fails.</param>
    /// <param name="mapException">
    /// A function to be executed if an exception occurs during the execution of the scope. It
    /// takes the exception as a parameter and returns a TError object. It also catches scope execution exceptions.
    /// </param>
    /// <returns>A Task that returns a Result object that represents the outcome of the function.</returns>
    public static async Task<Result<TResult, TError>> WithTapScope<T, TResult, E, TScoped, TError>(
        this Task<Result<T, E>> self, Func<Task<TScoped>> scopedFunc,
        Func<Result<T, E>, TScoped, Task<Result<TResult, TError>>> f,
        Action<TScoped> onSuccess, Action<TScoped> onError,
        Func<Exception, TError> mapException)
        where TScoped : IDisposable
    {
        try
        {
            var callerResult = await self;

            using var scoped = await scopedFunc();
            var result = await f(callerResult, scoped);

            if (result.IsSuccess) onSuccess(scoped);
            else onError(scoped);

            return result;
        }
        catch (Exception e)
        {
            return mapException(e);
        }
    }


    /// <summary>
    /// Executes a function within a disposable scope. If the function is successful afterward, it executes an
    /// onSuccess action, otherwise it executes an onError action. If an exception occurs during the execution of the scope, it
    /// executes
    /// a mapScopeCrashed function. The function returns a Result object, which either contains a success value or an error
    /// value.
    /// </summary>
    /// <typeparam name="T">The type of the success value in the Result object.</typeparam>
    /// <typeparam name="E">The type of the error value in the Result object.</typeparam>
    /// <typeparam name="TScoped">The type of the disposable scope.</typeparam>
    /// <typeparam name="TError">The type of the error value in the returned UnitResult object.</typeparam>
    /// <typeparam name="TResult">The type of the success value in the returned Result object.</typeparam>
    /// <param name="self">The Result object to be processed.</param>
    /// <param name="scopedFunc">A function that creates the disposable scope asynchronously.</param>
    /// <param name="f">
    /// A function that takes a Result object and the disposable scope, and returns a Task that returns a Result object.
    /// </param>
    /// <param name="onSuccess">An action to be executed if the function is successful.</param>
    /// <param name="onError">An action to be executed if the function fails.</param>
    /// <param name="mapException">
    /// A function to be executed if an exception occurs during the execution of the scope. It
    /// takes the exception as a parameter and returns a TError object. It also catches scope execution exceptions.
    /// </param>
    /// <returns>A Task that returns a Result object that represents the outcome of the function.</returns>
    public static async Task<Result<TResult, TError>> WithTapScope<T, TResult, E, TScoped, TError>(
        this Result<T, E> self, Func<Task<TScoped>> scopedFunc,
        Func<Result<T, E>, TScoped, Task<Result<TResult, TError>>> f,
        Action<TScoped> onSuccess, Action<TScoped> onError,
        Func<Exception, TError> mapException)
        where TScoped : IDisposable
    {
        try
        {
            using var scoped = await scopedFunc();
            var result = await f(self, scoped);

            if (result.IsSuccess) onSuccess(scoped);
            else onError(scoped);

            return result;
        }
        catch (Exception e)
        {
            return mapException(e);
        }
    }
}