using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GuildSaber.Api.Transformers;

public static class OpenApiGlobalProblemDetails
{
    public class GlobalProblemDetailsTransformer : IOpenApiOperationTransformer, IOpenApiDocumentTransformer
    {
        public async Task TransformAsync(
            OpenApiDocument document, OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            document.Components ??= new OpenApiComponents();
            document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();

            var problemDetailsSchema = await context.GetOrCreateSchemaAsync(
                typeof(ProblemDetails),
                cancellationToken: cancellationToken
            );
            var httpValidationProblemDetailsSchema = await context.GetOrCreateSchemaAsync(
                typeof(HttpValidationProblemDetails),
                cancellationToken: cancellationToken
            );

            problemDetailsSchema.Properties ??= new Dictionary<string, IOpenApiSchema>();
            problemDetailsSchema.Properties["traceId"] = new OpenApiSchema
            {
                Type = JsonSchemaType.String
            };

            httpValidationProblemDetailsSchema.Properties ??= new Dictionary<string, IOpenApiSchema>();
            httpValidationProblemDetailsSchema.Properties["traceId"] = new OpenApiSchema
            {
                Type = JsonSchemaType.String
            };

            document.Components.Schemas["ProblemDetails"] = problemDetailsSchema;
            document.Components.Schemas["HttpValidationProblemDetails"] = httpValidationProblemDetailsSchema;
        }

        public Task TransformAsync(
            OpenApiOperation operation, OpenApiOperationTransformerContext context,
            CancellationToken cancellationToken)
        {
            if (operation.Responses is null)
                return Task.CompletedTask;

            foreach (var response in operation.Responses)
            {
                if (!int.TryParse(response.Key, out var statusCode) || statusCode is < 400 or >= 600)
                    continue;

                var payloads = response.Value.Content;
                var problemDetailsContent = null as OpenApiMediaType;
                var containsProblemDetails = payloads
                    ?.TryGetValue("application/problem+json", out problemDetailsContent) ?? false;

                if (payloads is null || payloads.Count != 0 && !containsProblemDetails)
                    continue;

                var isHttpValidationProblemDetails = problemDetailsContent
                    ?.Schema is OpenApiSchemaReference { Reference.Id: "HttpValidationProblemDetails" };

                var problemDetails = isHttpValidationProblemDetails
                    ? TypedResults.ValidationProblem(new Dictionary<string, string[]>()).ProblemDetails
                    : TypedResults.Problem(statusCode: statusCode).ProblemDetails;

                var example = new JsonObject();

                if (!string.IsNullOrEmpty(problemDetails.Type))
                    example["type"] = JsonValue.Create(problemDetails.Type);

                if (!string.IsNullOrEmpty(problemDetails.Title))
                    example["title"] = JsonValue.Create(problemDetails.Title);

                example["status"] = JsonValue.Create(statusCode);

                /* And also include the detail property for native ProblemDetails responses. */
                if (!string.IsNullOrEmpty(problemDetails.Detail) || containsProblemDetails)
                    example["detail"] = JsonValue.Create(problemDetails.Detail);

                example["instance"] = JsonValue.Create(
                    $"{context.Description.HttpMethod} {context.Description.RelativePath}"
                );

                example["traceId"] = JsonValue.Create(
                    "00-0123456789abcdef0123456789abcdef-0123456789abcdef-00"
                );

                if (isHttpValidationProblemDetails)
                    example["errors"] = new JsonObject
                    {
                        ["propertyName"] = new JsonArray(JsonValue.Create("Error message"))
                    };

                if (containsProblemDetails)
                {
                    /* Prevent overwriting the schema example if it already exists,
                     * maybe from a lib or if the user has already defined it. */
                    if (problemDetailsContent!.Schema?.Example is not null)
                        continue;

                    problemDetailsContent.Schema = new OpenApiSchemaReference(
                        isHttpValidationProblemDetails ? "HttpValidationProblemDetails" : "ProblemDetails",
                        context.Document
                    );
                    problemDetailsContent.Example = example;
                    continue;
                }

                operation.Responses[response.Key] = new OpenApiResponse
                {
                    Description = problemDetails.Title,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/problem+json"] = new()
                        {
                            Schema = new OpenApiSchemaReference("ProblemDetails", context.Document),
                            Example = example
                        }
                    }
                };
            }

            return Task.CompletedTask;
        }
    }

    public static OpenApiOptions AddGlobalProblemDetails(this OpenApiOptions options) => options
        .AddDocumentTransformer<GlobalProblemDetailsTransformer>()
        .AddOperationTransformer<GlobalProblemDetailsTransformer>();
}