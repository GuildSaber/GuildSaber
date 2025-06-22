using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Database.Extensions;

[SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
public static class EFCoreSavingExtensions
{
    /// <summary>
    /// Asynchronously adds an entity to the DbContext and saves the changes, then returns a mapped value from the entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="U">The mapped return type.</typeparam>
    /// <param name="context">The DbContext.</param>
    /// <param name="entity">The entity to add.</param>
    /// <param name="mapper">A function to map the entity to the return value.</param>
    /// <returns>The mapped value from the added entity.</returns>
    public static async Task<U> AddAndSaveAsync<T, U>(this DbContext context, T entity, Func<T, U> mapper)
        where T : class
    {
        context.Set<T>().Add(entity);
        await context.SaveChangesAsync();
        return mapper(entity);
    }

    /// <summary>
    /// Asynchronously creates, adds, and saves an entity to the DbContext, then returns a mapped value from the entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="U">The mapped return type.</typeparam>
    /// <param name="context">The DbContext.</param>
    /// <param name="createEntity">A function to create the entity.</param>
    /// <param name="mapper">A function to map the entity to the return value.</param>
    /// <returns>The mapped value from the added entity.</returns>
    public static async Task<U> AddAndSaveAsync<T, U>(this DbContext context, Func<T> createEntity, Func<T, U> mapper)
        where T : class
    {
        var entity = createEntity();
        context.Set<T>().Add(entity);
        await context.SaveChangesAsync();
        return mapper(entity);
    }

    /// <summary>
    /// Asynchronously adds an entity to the DbContext and saves the changes.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="context">The DbContext.</param>
    /// <param name="entity">The entity to add.</param>
    public static async Task<T> AddAndSaveAsync<T>(this DbContext context, T entity) where T : class
    {
        context.Set<T>().Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Asynchronously creates, adds, and saves an entity to the DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="context">The DbContext.</param>
    /// <param name="createEntity">A function to create the entity.</param>
    public static async Task<T> AddAndSaveAsync<T>(this DbContext context, Func<T> createEntity) where T : class
    {
        var entity = createEntity();
        context.Set<T>().Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Asynchronously performs a bulk insert operation on a DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="context">The DbContext.</param>
    /// <param name="inputs">The entities to insert.</param>
    public static async Task BulkInsert<T>(this DbContext context, IEnumerable<T> inputs) where T : class
    {
        context.Set<T>().AddRange(inputs);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Asynchronously performs a bulk insert operation on a DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="context">The DbContext.</param>
    /// <param name="inputs">The arrays of entities to insert.</param>
    public static async Task BulkInsert<T>(this DbContext context, IEnumerable<T[]> inputs) where T : class
    {
        context.Set<T>().AddRange(inputs.SelectMany(x => x));
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Asynchronously performs a bulk insert operation on a DbContext and returns the count of inserted elements.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="context">The DbContext.</param>
    /// <param name="inputs">The entities to insert.</param>
    /// <returns>The number of inserted entities.</returns>
    public static async Task<int> BulkInsertAware<T>(this DbContext context, IEnumerable<T> inputs) where T : class
    {
        context.Set<T>().AddRange(inputs);
        return await context.SaveChangesAsync();
    }

    /// <summary>
    /// Asynchronously performs a bulk insert operation on a DbContext and returns the count of inserted elements.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="context">The DbContext.</param>
    /// <param name="inputs">The arrays of entities to insert.</param>
    /// <returns>The number of inserted entities.</returns>
    public static async Task<int> BulkInsertAware<T>(this DbContext context, IEnumerable<T[]> inputs) where T : class
    {
        context.Set<T>().AddRange(inputs.SelectMany(x => x));
        return await context.SaveChangesAsync();
    }

    /// <summary>
    /// Asynchronously performs a bulk update operation on a DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="context">The DbContext.</param>
    /// <param name="inputs">The entities to update.</param>
    public static async Task BulkUpdate<T>(this DbContext context, IEnumerable<T> inputs) where T : class
    {
        context.Set<T>().UpdateRange(inputs);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Asynchronously updates an entity in the DbContext and saves the changes, then returns a mapped value from the entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="U">The mapped return type.</typeparam>
    /// <param name="context">The DbContext.</param>
    /// <param name="entity">The entity to update.</param>
    /// <param name="mapper">A function to map the entity to the return value.</param>
    /// <returns>The mapped value from the updated entity.</returns>
    public static async Task<U> UpdateAndSaveAsync<T, U>(this DbContext context, T entity, Func<T, U> mapper)
        where T : class
    {
        context.Set<T>().Update(entity);
        await context.SaveChangesAsync();
        return mapper(entity);
    }

    /// <summary>
    /// Asynchronously creates, updates, and saves an entity in the DbContext, then returns a mapped value from the entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="U">The mapped return type.</typeparam>
    /// <param name="context">The DbContext.</param>
    /// <param name="createEntity">A function to create the entity.</param>
    /// <param name="mapper">A function to map the entity to the return value.</param>
    /// <returns>The mapped value from the updated entity.</returns>
    public static async Task<U> UpdateAndSaveAsync<T, U>(
        this DbContext context, Func<T> createEntity,
        Func<T, U> mapper)
        where T : class
    {
        var entity = createEntity();
        context.Set<T>().Update(entity);
        await context.SaveChangesAsync();
        return mapper(entity);
    }

    /// <summary>
    /// Asynchronously updates an entity in the DbContext and saves the changes.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="context">The DbContext.</param>
    /// <param name="entity">The entity to update.</param>
    public static async Task<T> UpdateAndSaveAsync<T>(this DbContext context, T entity) where T : class
    {
        context.Set<T>().Update(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Asynchronously creates, updates, and saves an entity in the DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="context">The DbContext.</param>
    /// <param name="createEntity">A function to create the entity.</param>
    public static async Task<T> UpdateAndSaveAsync<T>(this DbContext context, Func<T> createEntity)
        where T : class
    {
        var entity = createEntity();
        context.Set<T>().Update(entity);
        await context.SaveChangesAsync();
        return entity;
    }
}