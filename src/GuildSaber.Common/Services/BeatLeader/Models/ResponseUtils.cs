namespace GuildSaber.Common.Services.BeatLeader.Models;

public record Metadata
{
    public required int ItemsPerPage { get; init; }
    public required int Page { get; init; }
    public required int Total { get; init; }
}

public record ResponseWithMetadata<T>
{
    public required Metadata Metadata { get; init; }
    public required T[] Data { get; init; }
}