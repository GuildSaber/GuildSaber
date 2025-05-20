namespace GuildSaber.Common.Services.BeatLeader.Models;

public class Metadata
{
    public required int ItemsPerPage { get; set; }
    public required int Page { get; set; }
    public required int Total { get; set; }
}

public class ResponseWithMetadata<T>
{
    public required Metadata Metadata { get; set; }
    public required T[] Data { get; set; }
}