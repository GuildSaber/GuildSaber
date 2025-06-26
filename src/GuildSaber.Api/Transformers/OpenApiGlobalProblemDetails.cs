using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace GuildSaber.Api.Transformers;

public static class ProblemResponseTransformer
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

                var containsProblemDetails = response.Value.Content.ContainsKey("application/problem+json");
                if (response.Value.Content.Count != 0 && !containsProblemDetails)
                    continue;

                // Use the problem details class to generate an appropriate response
                var result = TypedResults.Problem(statusCode: statusCode);

                var example = new OpenApiObject();

                if (!string.IsNullOrEmpty(result.ProblemDetails.Type))
                    example["type"] = new OpenApiString(result.ProblemDetails.Type);

                if (!string.IsNullOrEmpty(result.ProblemDetails.Title))
                    example["title"] = new OpenApiString(result.ProblemDetails.Title);

                example["status"] = new OpenApiInteger(statusCode);

                // Force details (even if empty) to be present in the example if the response is a problem details response.
                if (!string.IsNullOrEmpty(result.ProblemDetails.Detail) || containsProblemDetails)
                    example["detail"] = new OpenApiString(result.ProblemDetails.Detail);

                example["traceId"] = new OpenApiString(
                    "00-0123456789abcdef0123456789abcdef-0123456789abcdef-00"
                );

                if (containsProblemDetails)
                {
                    var content = response.Value.Content["application/problem+json"];
                    // Prevent overwriting the schema example if it already exists.
                    // Maybe from a lib or the user has defined it.
                    if (content.Schema.Example is not null) continue;

                    content.Schema.Example = example;

                    continue;
                }

                operation.Responses[response.Key] = new OpenApiResponse
                {
                    Description = result.ProblemDetails.Title,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/problem+json"] = new()
                        {
                            Schema = new OpenApiSchema
                            {
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
    {
        options.AddOperationTransformer<GlobalProblemDetailsTransformer>();
        return options;
    }
}