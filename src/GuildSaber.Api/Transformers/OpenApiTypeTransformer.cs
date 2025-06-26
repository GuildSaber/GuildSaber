using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace GuildSaber.Api.Transformers;

/// <summary>
/// Provides functionality to customize type representations in OpenAPI schemas.
/// Allows mapping .NET types to specific OpenAPI schema definitions.
/// </summary>
/// <remarks>
/// This class is useful for:
/// <list type="bullet">
///     <item>Controlling how primitive types are represented in OpenAPI specifications</item>
///     <item>Ensuring consistent schema formats for specific types</item>
///     <item>Fixing type representation issues in generated OpenAPI documents</item>
/// </list>
/// </remarks>
/// <example>
/// Register in your Program.cs or Startup.cs:
/// <code>
/// OpenApiTypeTransformer.MapType{decimal}(new OpenApiSchema { Type = "number", Format = "decimal" });
/// builder.Services.AddOpenApi(options => options.AddSchemaTransformer(OpenApiTypeTransformer.TransformAsync));
/// </code>
/// </example>
public class OpenApiTypeTransformer
{
    private static readonly Dictionary<Type, OpenApiSchema> _transforms = new();

    /// <summary>
    /// Maps a .NET type to a specific OpenAPI schema representation.
    /// </summary>
    /// <typeparam name="T">The .NET type to map</typeparam>
    /// <param name="schema">The OpenAPI schema definition to use for this type</param>
    /// <remarks>
    /// Subsequent calls with the same type will override previous mappings.
    /// </remarks>
    public static void MapType<T>(OpenApiSchema schema) => _transforms[typeof(T)] = schema;

    /// <summary>
    /// Transforms an OpenAPI schema based on registered type mappings.
    /// </summary>
    /// <param name="schema">The schema being generated</param>
    /// <param name="context">The schema transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that completes when the transformation is done</returns>
    /// <remarks>
    /// This method is designed to be registered with the OpenAPI pipeline using
    /// <c>options.AddSchemaTransformer(OpenApiTypeTransformer.TransformAsync)</c>
    /// </remarks>
    public static Task TransformAsync(
        OpenApiSchema schema, OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (!_transforms.TryGetValue(context.JsonTypeInfo.Type, out var transformedSchema))
            return Task.CompletedTask;

        schema.Type = transformedSchema.Type;
        schema.Format = transformedSchema.Format;

        return Task.CompletedTask;
    }
}