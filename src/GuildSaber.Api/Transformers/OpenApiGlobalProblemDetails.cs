using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GuildSaber.Api.Transformers;

public static class OpenApiGlobalProblemDetails
{
    public class GlobalProblemDetailsTransformer : IOpenApiOperationTransformer, IOpenApiDocumentTransformer
    {
        public Task TransformAsync(
            OpenApiDocument document, OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            document.Components ??= new OpenApiComponents();
            document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();

            document.Components.Schemas.Remove("ProblemDetails");
            document.Components.Schemas.Add(new KeyValuePair<string, IOpenApiSchema>("ProblemDetails",
                new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                    Properties = new Dictionary<string, IOpenApiSchema>
                    {
                        ["type"] = new OpenApiSchema
                        {
                            Type = JsonSchemaType.String,
                            Format = "uri"
                        },
                        ["title"] = new OpenApiSchema
                        {
                            Type = JsonSchemaType.String
                        },
                        ["status"] = new OpenApiSchema
                        {
                            Type = JsonSchemaType.Integer,
                            Format = "int32"
                        },
                        ["detail"] = new OpenApiSchema
                        {
                            Type = JsonSchemaType.String | JsonSchemaType.Null
                        },
                        ["instance"] = new OpenApiSchema
                        {
                            Type = JsonSchemaType.String
                        },
                        ["traceId"] = new OpenApiSchema
                        {
                            Type = JsonSchemaType.String
                        }
                    }
                }));

            return Task.CompletedTask;
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

                //TODO: Figure why this doesn't work anymore.
                var isHttpValidationProblemDetails = problemDetailsContent?.Schema?.Properties?
                    .ContainsKey("errors") ?? false;

                var problemDetails = isHttpValidationProblemDetails switch
                {
                    true => TypedResults.ValidationProblem(new Dictionary<string, string[]>()).ProblemDetails,
                    false => TypedResults.Problem(statusCode: statusCode).ProblemDetails
                };

                var example = new JsonObject();

                if (!string.IsNullOrEmpty(problemDetails.Type))
                    example["type"] = JsonValue.Create(problemDetails.Type);

                if (!string.IsNullOrEmpty(problemDetails.Title))
                    example["title"] = JsonValue.Create(problemDetails.Title);

                example["status"] = JsonValue.Create(statusCode);

                // Force details (even if empty) to be present in the example if the response is a problem details response.
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
                    // Prevent overwriting the schema example if it already exists.
                    // Maybe from a lib or the user has defined it.
                    if (problemDetailsContent is null || problemDetailsContent.Schema?.Example is not null)
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
        .AddOperationTransformer<GlobalProblemDetailsTransformer>()
        .AddDocumentTransformer<GlobalProblemDetailsTransformer>();
}