using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Common.Services.BeatSaver.Models;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using Version = GuildSaber.Common.Services.BeatSaver.Models.Version;

namespace GuildSaber.Common.Services.BeatSaver;

public class BeatSaverApi(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new BeatSaverKeyJsonConverter(), new SongHashJsonConverter() }
    };

    /// <summary>Selects BeatSaver's current version.</summary>
    /// <remarks>
    /// It's not using creation date or whatever because that's just not how BeatSaver does it (and selected version
    /// could change).
    /// </remarks>
    public static Func<BeatMap, Version> SelectLatestBeatMapVersion
        => static beatMap => beatMap.Versions[^1];

    public abstract record Error
    {
        public sealed record RateLimitExceeded(TimeSpan RetryAfter) : Error;
        public sealed record Other(string Message) : Error;
    }

    /// <summary>Selects the requested hash, returning null when it is absent without falling back to another version.</summary>
    public static Func<BeatMap, Version?> SelectBeatMapVersionByHash(SongHash hash)
        => beatMap => beatMap.Versions.FirstOrDefault(version
            => string.Equals(version.Hash, hash, StringComparison.OrdinalIgnoreCase));

    public Task<Result<BeatMap?, Error>> GetBeatMapAsync(BeatSaverKey key, CancellationToken token = default)
        => GetBeatMapAsync($"maps/id/{key}", TimeSpan.FromSeconds(2), token);

    public Task<Result<BeatMap?, Error>> GetBeatMapAsync(SongHash hash, CancellationToken token = default)
        => GetBeatMapAsync($"maps/hash/{hash}", TimeSpan.FromMinutes(1), token);

    private async Task<Result<BeatMap?, Error>> GetBeatMapAsync(
        string path, TimeSpan defaultRetryAfter, CancellationToken token)
        => await httpClient.GetAsync(path, token) switch
        {
            { StatusCode: HttpStatusCode.NotFound } => Success<BeatMap?, Error>(null),
            { StatusCode: (HttpStatusCode)429, Headers: var headers } => Failure<BeatMap?, Error>(
                new Error.RateLimitExceeded(headers.RetryAfter?.Delta ?? defaultRetryAfter)),
            { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                => Failure<BeatMap?, Error>(
                    new Error.Other($"Failed to retrieve beatmap at '{path}': {statusCode} {reasonPhrase}")),
            var response => await Try(() => response.Content.ReadFromJsonAsync<BeatMap>(_jsonOptions, token),
                Error (error) => new Error.Other(error.Message))
        };
}