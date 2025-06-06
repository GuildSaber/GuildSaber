﻿using CSharpFunctionalExtensions;

namespace GuildSaber.Common.Result;

public static class RustExtensions
{
    public class ErrorException(string message) : Exception(message);

    /// <summary>
    /// Unwraps the Result object within a Task. If the Result is successful, it returns the value.
    /// If the Result is a failure, it throws an ErrorException with the error message.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <typeparam name="E">The type of the error. Must be a subtype of ErrorType.</typeparam>
    /// <param name="self">The Task containing the Result to unwrap.</param>
    /// <returns>The result value if the Result is successful.</returns>
    /// <exception cref="ErrorException">Thrown when the Result is a failure.</exception>
    public static async Task<T> Unwrap<T, E>(this Task<Result<T, E>> self)
    {
        var result = await self;
        return result.IsSuccess
            ? result.Value
            : throw new Exception(result.Error!.ToString()!);
    }

    public static async Task<T> Unwrap<T>(this Task<Result<T>> self)
    {
        var result = await self;
        return result.IsSuccess
            ? result.Value
            : throw new Exception(result.Error);
    }

    public static async Task Unwrap<E>(this Task<UnitResult<E>> self)
    {
        var result = await self;
        if (result.IsFailure) throw new Exception(result.Error!.ToString());
    }


    /// <summary>
    /// Unwraps the Result object. If the Result is successful, it returns the value.
    /// If the Result is a failure, it throws an ErrorException with the error message.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <typeparam name="E">The type of the error. Must be a subtype of ErrorType.</typeparam>
    /// <param name="self">The Result to unwrap.</param>
    /// <returns>The result value if the Result is successful.</returns>
    /// <exception cref="ErrorException">Thrown when the Result is a failure.</exception>
    public static T Unwrap<T, E>(this Result<T, E> self)
        => self.IsSuccess
            ? self.Value
            : throw new ErrorException(self.Error!.ToString()!);

    /// <summary>
    /// Unwraps the Result object. If the Result is successful, it returns the value.
    /// If the Result is a failure, it throws an ErrorException with the error message.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="self">The Result to unwrap.</param>
    /// <returns>The result value if the Result is successful.</returns>
    /// <exception cref="ErrorException">Thrown when the Result is a failure.</exception>
    public static T Unwrap<T>(this Result<T> self)
        => self.IsSuccess
            ? self.Value
            : throw new ErrorException(self.Error);

    /// <summary>
    /// Unwraps the Result object. If the Result is successful, it returns the value.
    /// If the Result is a failure, it throws an ErrorException with the error message.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <typeparam name="E">The type of the error. Must be a subtype of ErrorType.</typeparam>
    /// <param name="self">The Result to unwrap.</param>
    /// <returns>The result value if the Result is successful.</returns>
    /// <exception cref="ErrorException">Thrown when the Result is a failure.</exception>
    public static void Unwrap<T, E>(this UnitResult<E> self)
    {
        if (self.IsFailure) throw new ErrorException(self.Error!.ToString()!);
    }

    /// <summary>
    /// Asynchronously unwraps the Result object within a Task. If the Result is successful, it returns the value.
    /// If the Result is a failure, it returns a default value.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <typeparam name="E">The type of the error. Must be a subtype of ErrorType.</typeparam>
    /// <param name="self">The Task containing the Result to unwrap.</param>
    /// <param name="defaultValue">The default value to return if the Result is a failure.</param>
    /// <returns>The result value if the Result is successful, otherwise the default value.</returns>
    public static async Task<T> UnwrapOr<T, E>(this Task<Result<T, E>> self, T defaultValue)
    {
        var result = await self;
        return result.IsSuccess
            ? result.Value
            : defaultValue;
    }

    public static async Task<Result<T, E>> Try<T, E>(
        Task<T> func,
        Func<Exception, E> errorHandler)
    {
        try
        {
            return Success<T, E>(await func);
        }
        catch (Exception ex)
        {
            return Failure<T, E>(errorHandler(ex));
        }
    }

    public static async ValueTask<Result<T, E>> Try<T, E>(
        ValueTask<T> func,
        Func<Exception, E> errorHandler)
    {
        try
        {
            return Success<T, E>(await func);
        }
        catch (Exception ex)
        {
            return Failure<T, E>(errorHandler(ex));
        }
    }

    /// <summary>
    /// Asynchronously unwraps the Result object within a Task. If the Result is successful, it returns the value.
    /// If the Result is a failure, it returns a value generated by a provided function.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <typeparam name="E">The type of the error. Must be a subtype of ErrorType.</typeparam>
    /// <param name="self">The Task containing the Result to unwrap.</param>
    /// <param name="defaultValue">A function that takes the error as a parameter and returns a value of type T.</param>
    /// <returns>The result value if the Result is successful, otherwise a value generated by the provided function.</returns>
    public static async Task<T> UnwrapOrElse<T, E>(this Task<Result<T, E>> self, Func<E, T> defaultValue)
    {
        var result = await self;
        return result.IsSuccess
            ? result.Value
            : defaultValue(result.Error);
    }

    public static async ValueTask<T> UnwrapOrElse<T, E>(this ValueTask<Result<T, E>> self, Func<E, T> defaultValue)
    {
        var result = await self;
        return result.IsSuccess
            ? result.Value
            : defaultValue(result.Error);
    }

    /// <summary>
    /// Asynchronously unwraps the Result object within a Task. If the Result is successful, it returns the value.
    /// If the Result is a failure, it returns a value generated by a provided asynchronous function.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <typeparam name="E">The type of the error. Must be a subtype of ErrorType.</typeparam>
    /// <param name="self">The Task containing the Result to unwrap.</param>
    /// <param name="defaultValue">An asynchronous function that takes the error as a parameter and returns a Task of type T.</param>
    /// <returns>
    /// The result value if the Result is successful, otherwise a value generated by the provided asynchronous
    /// function.
    /// </returns>
    public static async Task<T> UnwrapOrElse<T, E>(this Task<Result<T, E>> self, Func<E, Task<T>> defaultValue)
    {
        var result = await self;
        return result.IsSuccess
            ? result.Value
            : await defaultValue(result.Error);
    }

    /// <summary>
    /// Throws an ErrorException with the error as a message.
    /// </summary>
    /// <typeparam name="E">The type of the error. Must be a subtype of ErrorType.</typeparam>
    /// <typeparam name="T"></typeparam>
    /// <param name="self">The error to throw.</param>
    /// <exception cref="ErrorException">Thrown with the error message.</exception>
    public static void Throw<T>(this T self) where T : IError<T>
        => throw new ErrorException(self.ToString()!);
}