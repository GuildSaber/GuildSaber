using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuildSaber.CSharpClient.Models;

internal sealed record ProblemDetails(
    string? Type = null,
    string? Title = null,
    int? Status = null,
    string? Detail = null,
    string? Instance = null,
    Dictionary<string, string[]>? Errors = null)
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement> Extensions { get; init; } = [];

    public string? GetMessage()
        => Detail ?? (Errors is { Count: > 0 } errors
            ? string.Join("\n", errors.SelectMany(x => x.Value))
            : Title);
}