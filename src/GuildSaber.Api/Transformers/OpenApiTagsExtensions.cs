using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GuildSaber.Api.Transformers;

public static class OpenApiTagsExtensions
{
    private static readonly Dictionary<string, string> _tagDescriptions = new();

    internal sealed class TagInfoDocumentTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(
            OpenApiDocument document, OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            document.Tags ??= new HashSet<OpenApiTag>();
            foreach (var tag in document.Tags)
            {
                if (tag.Name is null || !_tagDescriptions.TryGetValue(tag.Name, out var description))
                    continue;

                tag.Description = description;
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Adds a tag with the specified name and description to the OpenAPI document.
    /// </summary>
    /// <param name="builder">The <see cref="RouteGroupBuilder" /> to add the tag to.</param>
    /// <param name="name">The name of the tag.</param>
    /// <param name="description">The description of the tag.</param>
    /// <remarks>
    /// The <see cref="AddTagDescriptionSupport" /> method must be called on the <see cref="OpenApiOptions" /> to
    /// enable tag descriptions.
    /// </remarks>
    /// <returns>The modified <see cref="RouteGroupBuilder" /> with the tag added. (To allow chaining)</returns>
    public static RouteGroupBuilder WithTag(this RouteGroupBuilder builder, string name, string description)
    {
        builder.WithTags(name);
        _tagDescriptions[name] = description;
        return builder;
    }

    /// <summary>
    /// Adds support for tag descriptions in the OpenAPI document.
    /// </summary>
    /// <param name="options">The <see cref="OpenApiOptions" /> to add the tag description support to.</param>
    /// <returns>The same <see cref="OpenApiOptions" /> returned to allow chaining.</returns>
    public static OpenApiOptions AddTagDescriptionSupport(this OpenApiOptions options)
        => options.AddDocumentTransformer<TagInfoDocumentTransformer>();
}