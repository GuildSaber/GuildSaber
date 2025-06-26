using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace GuildSaber.Api.Transformers;

public static class OpenApiGlobalProblemDetails
{
    public class GlobalProblemDetailsTransformer : IOpenApiOperationTransformer
    {
        public Task TransformAsync(
            OpenApiOperation operation, OpenApiOperationTransformerContext context,
            CancellationToken cancellationToken)
        {
            foreach (var response in operation.Responses)
            {
                if (!int.TryParse(response.Key, out var statusCode)
                    || statusCode is < 400 or >= 600)
                    continue;

                var containsProblemDetails = response.Value.Content
                    .TryGetValue("application/problem+json", out var problemDetailsContent);

                if (response.Value.Content.Count != 0 && !containsProblemDetails)
                    continue;

                var isHttpValidationProblemDetails =
                    problemDetailsContent?.Schema.Properties.ContainsKey("errors") ?? false;

                string? type;
                string? title;
                string? detail;

                if (isHttpValidationProblemDetails)
                {
                    var validationProblem = TypedResults.ValidationProblem(new Dictionary<string, string[]>());

                    type = validationProblem.ProblemDetails.Type;
                    title = validationProblem.ProblemDetails.Title;
                    detail = validationProblem.ProblemDetails.Detail;
                }
                else
                {
                    var problemDetails = TypedResults.Problem(statusCode: statusCode);

                    type = problemDetails.ProblemDetails.Type;
                    title = problemDetails.ProblemDetails.Title;
                    detail = problemDetails.ProblemDetails.Detail;
                }

                var example = new OpenApiObject();

                if (!string.IsNullOrEmpty(type))
                    example["type"] = new OpenApiString(type);

                if (!string.IsNullOrEmpty(title))
                    example["title"] = new OpenApiString(title);

                example["status"] = new OpenApiInteger(statusCode);

                // Force details (even if empty) to be present in the example if the response is a problem details response.
                if (!string.IsNullOrEmpty(detail) || containsProblemDetails)
                    example["detail"] = new OpenApiString(detail);

                example["instance"] = new OpenApiString(
                    $"{context.Description.HttpMethod} {context.Description.RelativePath}"
                );

                example["traceId"] = new OpenApiString(
                    "00-0123456789abcdef0123456789abcdef-0123456789abcdef-00"
                );

                if (isHttpValidationProblemDetails)
                    example["errors"] = new OpenApiObject
                    {
                        ["propertyName"] = new OpenApiArray
                        {
                            new OpenApiString("Error message")
                        }
                    };

                if (containsProblemDetails)
                {
                    // Prevent overwriting the schema example if it already exists.
                    // Maybe from a lib or the user has defined it.
                    if (problemDetailsContent!.Schema.Example is not null) continue;

                    problemDetailsContent.Schema.Reference = new OpenApiReference
                    {
                        Type = ReferenceType.Schema,
                        Id = isHttpValidationProblemDetails ? "HttpValidationProblemDetails" : "ProblemDetails"
                    };

                    problemDetailsContent.Schema.Example = example;
                    continue;
                }

                operation.Responses[response.Key] = new OpenApiResponse
                {
                    Description = title,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/problem+json"] = new()
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Annotations = new Dictionary<string, object>
                                {
                                    ["x-schema-id"] = "ProblemDetails"
                                },
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.Schema,
                                    Id = "ProblemDetails"
                                }
                            },
                            Example = example
                        }
                    }
                };
            }

            return Task.CompletedTask;
        }
    }

    public static OpenApiOptions AddGlobalProblemDetails(this OpenApiOptions options)
        => options.AddOperationTransformer<GlobalProblemDetailsTransformer>();
}