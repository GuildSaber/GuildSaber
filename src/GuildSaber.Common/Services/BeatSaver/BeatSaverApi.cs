using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Common.Services.BeatSaver.Models;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;

namespace GuildSaber.Common.Services.BeatSaver;

public class BeatSaverApi(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new BeatSaverKeyJsonConverter(), new SongHashJsonConverter() }
    };

    public abstract record Error
    {
        public sealed record RateLimitExceeded(TimeSpan RetryAfter) : Error;
        public sealed record Other(string Message) : Error;
    }

    public async Task<Result<BeatMap?, Error>> GetBeatMapAsync(BeatSaverKey key)
        => await httpClient.GetAsync($"maps/id/{key}") switch
        {
            { StatusCode: HttpStatusCode.NotFound } => Success<BeatMap?, Error>(null),
            { StatusCode: (HttpStatusCode)429, Headers: var headers } => Failure<BeatMap?, Error>(
                new Error.RateLimitExceeded(headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2))
            ),
            { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                => Failure<BeatMap?, Error>(
                    new Error.Other($"Failed to retrieve beatmap with key {key}: {statusCode} {reasonPhrase}")
                ),
            var response => await Try(() => response.Content.ReadFromJsonAsync<BeatMap>(_jsonOptions),
                Error (err) => new Error.Other(err.Message))
        };

    public async Task<Result<BeatMap?, Error>> GetBeatMapAsync(SongHash hash)
        => await httpClient.GetAsync($"maps/hash/{hash}") switch
        {
            { StatusCode: HttpStatusCode.NotFound } => Success<BeatMap?, Error>(null),
            { StatusCode: (HttpStatusCode)429, Headers: var headers } => Failure<BeatMap?, Error>(
                new Error.RateLimitExceeded(headers.RetryAfter?.Delta ?? TimeSpan.FromMinutes(1))
            ),
            { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                => Failure<BeatMap?, Error>(
                    new Error.Other($"Failed to retrieve beatmap with hash {hash}: {statusCode} {reasonPhrase}")
                ),
            var response => await Try(() => response.Content.ReadFromJsonAsync<BeatMap>(_jsonOptions),
                Error (err) => new Error.Other(err.Message))
        };
}