using System.Runtime.CompilerServices;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Database.Utils;

public static class EFCoreMonadicOperationUtils
{
    /// <summary>
    /// Asynchronously adds an entity to the DbContext and saves the changes.
    /// </summary>
    /// <typeparam name="T">The type of the entity to be added.</typeparam>
    /// <typeparam name="U">The type of the value to be mapped from the entity.</typeparam>
    /// <param name="context">The DbContext to which the entity will be added.</param>
    /// <param name="entity">The entity to be added.</param>
    /// <param name="mapper">A function that maps a value from the created entity.</param>
    /// <param name="callerMemberName">
    /// The name of the calling method. This parameter is optional and is filled in by the
    /// compiler.
    /// </param>
    /// <param name="callerFilePath">
    /// The path of the file that contains the calling method. This parameter is optional and is
    /// filled in by the compiler.
    /// </param>
    /// <returns>
    /// A Result object that contains the mapped value from the added entity if the operation is successful.
    /// If an exception occurs during the operation, the function returns a Result object on failure state represented by an
    /// InsertError.
    /// </returns>
    public static async Task<Result<U, InsertError>> AddAndSaveAsync<T, U>(
        this DbContext context, T entity, Func<T, U> mapper,
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath] string callerFilePath = "") where T : class
    {
        try
        {
            context.Set<T>().Add(entity);
            await context.SaveChangesAsync();
            return Success<U, InsertError>(mapper(entity));
        }
        catch (Exception exception)
        {
            return Failure<U, InsertError>(
                // ReSharper disable once ExplicitCallerInfoArgument
                new InsertError(exception, $"{callerMemberName}.[{nameof(AddAndSaveAsync)}]", callerFilePath)
            );
        }
    }

    /// <summary>
    /// Asynchronously adds an entity to the DbContext and saves the changes.
    /// </summary>
    /// <typeparam name="T">The type of the entity to be added.</typeparam>
    /// <typeparam name="U">The type of the value to be mapped from the entity.</typeparam>
    /// <param name="context">The DbContext to which the entity will be added.</param>
    /// <param name="createEntity">A function that creates the entity to be added.</param>
    /// <param name="mapper">A function that maps a value from the created entity.</param>
    /// <param name="callerMemberName">
    /// The name of the calling method. This parameter is optional and is filled in by the
    /// compiler.
    /// </param>
    /// <param name="callerFilePath">
    /// The path of the file that contains the calling method. This parameter is optional and is
    /// filled in by the compiler.
    /// </param>
    /// <returns>
    /// A Result object that contains the mapped value from the added entity if the operation is successful.
    /// If an exception occurs during the operation, the function returns a Result object on failure state represented by an
    /// InsertError.
    /// </returns>
    public static async Task<Result<U, InsertError>> AddAndSaveAsync<T, U>(
        this DbContext context, Func<T> createEntity, Func<T, U> mapper,
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath] string callerFilePath = "") where T : class
    {
        try
        {
            var entity = createEntity();
            context.Set<T>().Add(entity);
            await context.SaveChangesAsync();
            return Success<U, InsertError>(mapper(entity));
        }
        catch (Exception exception)
        {
            return Failure<U, InsertError>(
                // ReSharper disable once ExplicitCallerInfoArgument
                new InsertError(exception, $"{callerMemberName}.[{nameof(AddAndSaveAsync)}]", callerFilePath)
            );
        }
    }

    /// <summary>
    /// Asynchronously performs a bulk insert operation on a DbContext.
    /// </summary>
    /// <typeparam name="T">The type of the entities to be inserted.</typeparam>
    /// <param name="context">The DbContext to which the entities will be added.</param>
    /// <param name="inputs">An IEnumerable of entities to be inserted.</param>
    /// <returns>
    /// A UnitResult object that represents the outcome of the operation. If the operation is successful, the UnitResult object
    /// indicates success.
    /// If an exception occurs during the operation, the function returns a UnitResult object on failure state represented by
    /// an InsertError.
    /// </returns>
    public static Task<UnitResult<InsertError>> BulkInsert<T>(this DbContext context, IEnumerable<T> inputs)
        where T : class
        => UnitResult.Success<InsertError>()
            .Tap(() => context.Set<T>().AddRange(inputs))
            .TapTry(() => context.SaveChangesAsync(),
                exception => new InsertError(exception)
            );

    /// <summary>
    /// Asynchronously performs a bulk insert operation on a DbContext.
    /// </summary>
    /// <typeparam name="T">The type of the entities to be inserted.</typeparam>
    /// <param name="context">The DbContext to which the entities will be added.</param>
    /// <param name="inputs">An IEnumerable of arrays of entities to be inserted.</param>
    /// <returns>
    /// A UnitResult object that represents the outcome of the operation. If the operation is successful, the UnitResult object
    /// indicates success.
    /// If an exception occurs during the operation, the function returns a UnitResult object on failure state represented by
    /// an InsertError.
    /// </returns>
    public static Task<UnitResult<InsertError>> BulkInsert<T>(
        this DbContext context, IEnumerable<T[]> inputs)
        where T : class
        => UnitResult.Success<InsertError>()
            .Tap(() => context.Set<T>().AddRange(inputs.SelectMany(x => x)))
            .TapTry(() => context.SaveChangesAsync(),
                exception => new InsertError(exception)
            );

    /// <summary>
    /// Asynchronously performs a bulk insert operation on a DbContext and returns the count of inserted elements.
    /// </summary>
    /// <typeparam name="T">The type of the entities to be inserted.</typeparam>
    /// <param name="context">The DbContext to which the entities will be added.</param>
    /// <param name="inputs">An IEnumerable of entities to be inserted.</param>
    /// <returns>
    /// A Result object that contains the count of inserted elements if the operation is successful.
    /// If an exception occurs during the operation, the function returns a Result object on failure state represented by
    /// an InsertError.
    /// </returns>
    public static Task<Result<int, InsertError>> BulkInsertAware<T>(
        this DbContext context, IEnumerable<T> inputs) where T : class
        => UnitResult.Success<InsertError>()
            .Tap(() => context.Set<T>().AddRange(inputs))
            .MapTry(() => context.SaveChangesAsync(),
                exception => new InsertError(exception)
            );

    /// <summary>
    /// Asynchronously performs a bulk insert operation on a DbContext and returns the count of inserted elements.
    /// </summary>
    /// <typeparam name="T">The type of the entities to be inserted.</typeparam>
    /// <param name="context">The DbContext to which the entities will be added.</param>
    /// <param name="inputs">An IEnumerable of arrays of entities to be inserted.</param>
    /// <returns>
    /// A Result object that contains the count of inserted elements if the operation is successful.
    /// If an exception occurs during the operation, the function returns a UnitResult object on failure state represented by
    /// an InsertError.
    /// </returns>
    public static Task<Result<int, InsertError>> BulkInsertAware<T>(
        this DbContext context, IEnumerable<T[]> inputs)
        where T : class
        => UnitResult.Success<InsertError>()
            .Tap(() => context.Set<T>().AddRange(inputs.SelectMany(x => x)))
            .MapTry(() => context.SaveChangesAsync(),
                exception => new InsertError(exception)
            );

    /// <summary>
    /// Asynchronously performs a bulk update operation on a DbContext.
    /// </summary>
    /// <typeparam name="T">The type of the entities to be inserted.</typeparam>
    /// <param name="context">The DbContext of which the entities will be updated.</param>
    /// <param name="inputs">An IEnumerable of entities to be updated.</param>
    /// <returns>
    /// A UnitResult object that represents the outcome of the operation. If the operation is successful, the UnitResult object
    /// indicates success.
    /// If an exception occurs during the operation, the function returns a UnitResult object on failure state represented by
    /// an UpdateError.
    /// </returns>
    public static Task<UnitResult<UpdateError>> BulkUpdate<T>(this DbContext context, IEnumerable<T> inputs)
        where T : class
        => UnitResult.Success<UpdateError>()
            .Tap(() => context.Set<T>().UpdateRange(inputs))
            .TapTry(() => context.SaveChangesAsync(),
                exception => new UpdateError(exception)
            );
}