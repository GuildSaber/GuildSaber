using CSharpFunctionalExtensions;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatLeader.Models;
using GuildSaber.Database.Models.Server.StrongTypes;

namespace GuildSaber.Api.Services.BeatLeader;

/// <summary>
/// Safe strongly typed wrapper for BeatLeader API.
/// </summary>
/// <param name="httpClient"></param>
public class BeatLeaderApi(HttpClient httpClient) : BeatLeaderApiBase(httpClient)
{
    /// <inheritdoc cref="BeatLeaderApiBase.GetPlayerScoresCompact" />
    public IAsyncEnumerable<Result<CompactScoreResponse[]?>> GetPlayerScoresCompact(
        BeatLeaderId playerId, PaginatedRequestOptions<ScoresSortBy> requestOptions)
        => base.GetPlayerScoresCompact(playerId, requestOptions);
}