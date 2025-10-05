namespace GuildSaber.Common.Services.BeatLeader.Models;

public class Metadata
{
    public required int ItemsPerPage { get; init; }
    public required int Page { get; init; }
    public required int Total { get; init; }
}

public class ResponseWithMetadata<T>
{
    public required Metadata Metadata { get; init; }
    public required T[] Data { get; init; }
}