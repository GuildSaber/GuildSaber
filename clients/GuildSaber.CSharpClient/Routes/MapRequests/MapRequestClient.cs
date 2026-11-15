using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Shared;
using ThreadResponse = GuildSaber.Api.Features.MapRequests.Http.MapRequestResponses.Thread;
using static GuildSaber.Api.Features.MapRequests.Http.MapProposalRequests;
using static GuildSaber.Api.Features.MapRequests.Http.MapRequestResponses;

namespace GuildSaber.CSharpClient.Routes.MapRequests;

/// <summary>
/// Client for interacting with ranking request endpoints.
/// All changes use the authenticated player. Accepted snapshots cannot be modified.
/// </summary>
public sealed class MapRequestClient(
    HttpClient httpClient,
    AuthenticationHeaderValue? authenticationHeader,
    JsonSerializerOptions jsonOptions)
{
    private static string ThreadUrl(ContextId contextId, MapRequestThreadId threadId)
        => $"contexts/{contextId}/map-requests/{threadId}";

    private static string ProposalUrl(ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId)
        => $"{ThreadUrl(contextId, threadId)}/proposals/{proposalId}";

    /// <summary>Looks up BeatSaver metadata and supported difficulties.</summary>
    public Task<Result<SongLookup, Error>> LookupSongAsync(
        ContextId contextId, SongReference song, CancellationToken token = default)
        => SendAsync<SongLookup>(HttpMethod.Post, $"contexts/{contextId}/map-requests/lookup",
            JsonContent.Create(song, options: jsonOptions), token);

    /// <summary>Finds visible threads using an exact song difficulty, including your drafts and published history.</summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="requestFilters">Exact song version, characteristic and difficulty to match.</param>
    /// <param name="requestOptions">Pagination, sorting and ordering settings.</param>
    /// <param name="token">Cancellation token.</param>
    public Task<Result<PagedList<ThreadMatch>, Error>> GetMatchesAsync(
        ContextId contextId, MatchFilters requestFilters, PaginatedRequestOptions<EMapThreadSorter> requestOptions,
        CancellationToken token = default)
        => SendAsync<PagedList<ThreadMatch>>(HttpMethod.Get,
            $"contexts/{contextId}/map-requests/matches?hash={requestFilters.Hash}" +
            $"&characteristic={Uri.EscapeDataString(requestFilters.Characteristic)}&difficulty={requestFilters.Difficulty}" +
            $"&page={requestOptions.Page}&pageSize={requestOptions.PageSize}" +
            $"&sortBy={requestOptions.SortBy}&order={requestOptions.Order}", null, token);

    private static string GetQueueUrl(
        ContextId contextId,
        Filters requestFilters,
        PaginatedRequestOptions<EMapRequestSorter> requestOptions)
        => $"contexts/{contextId}/map-requests?page={requestOptions.Page}&pageSize={requestOptions.PageSize}" +
           $"&order={requestOptions.Order}&sortBy={requestOptions.SortBy}" +
           $"{(requestFilters.Search is null ? "" : $"&search={Uri.EscapeDataString(requestFilters.Search)}")}" +
           $"{(requestFilters.IsRevision is null ? "" : $"&isRevision={requestFilters.IsRevision}")}" +
           $"{(requestFilters.IsDraft is null ? "" : $"&isDraft={requestFilters.IsDraft}")}" +
           $"{(requestFilters.IsBoostedByMe is null ? "" : $"&isBoostedByMe={requestFilters.IsBoostedByMe}")}";

    private static string GetMineUrl(
        ContextId contextId,
        Filters requestFilters,
        PaginatedRequestOptions<EMapProposalSorter> requestOptions)
        => $"contexts/{contextId}/map-requests/proposals/@me?page={requestOptions.Page}&pageSize={requestOptions.PageSize}" +
           $"&order={requestOptions.Order}&sortBy={requestOptions.SortBy}" +
           $"{(requestFilters.Search is null ? "" : $"&search={Uri.EscapeDataString(requestFilters.Search)}")}" +
           $"{(requestFilters.IsRevision is null ? "" : $"&isRevision={requestFilters.IsRevision}")}" +
           $"{(requestFilters.IsDraft is null ? "" : $"&isDraft={requestFilters.IsDraft}")}" +
           $"{(requestFilters.IsBoostedByMe is null ? "" : $"&isBoostedByMe={requestFilters.IsBoostedByMe}")}";

    /// <summary>
    /// Gets a paginated ranking queue of thread previews with submitted proposal metadata and vote counts.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="requestFilters">Filters to apply to the ranking queue.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    public Task<Result<PagedList<ThreadPreview>, Error>> GetQueueAsync(
        ContextId contextId,
        Filters requestFilters,
        PaginatedRequestOptions<EMapRequestSorter> requestOptions,
        CancellationToken token = default)
        => SendAsync<PagedList<ThreadPreview>>(HttpMethod.Get, GetQueueUrl(contextId, requestFilters, requestOptions),
            null, token);

    /// <summary>
    /// Asynchronously retrieves ranking queue pages with automatic pagination.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="requestFilters">Filters to apply to the ranking queue.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <remarks>Enumeration stops at MaxPage, an empty page, or an error. Queue positions may change between pages.</remarks>
    public async IAsyncEnumerable<Result<ThreadPreview[], Error>> GetQueueAsyncEnumerable(
        ContextId contextId,
        Filters requestFilters,
        PaginatedRequestOptions<EMapRequestSorter> requestOptions,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        while (requestOptions.Page <= requestOptions.MaxPage)
        {
            var result = (await GetQueueAsync(contextId, requestFilters, requestOptions, token).ConfigureAwait(false))
                .Map(page => page.Data);
            yield return result;

            if (result is { IsFailure: true } or { Value: [] } || requestOptions.Page == requestOptions.MaxPage)
                yield break;

            requestOptions.Page++;
        }
    }

    public Task<Result<ThreadResponse?, Error>> GetThreadAsync(
        ContextId contextId, MapRequestThreadId threadId, CancellationToken token = default)
        => GetOptionalAsync<ThreadResponse>(ThreadUrl(contextId, threadId), token);

    public Task<Result<Proposal?, Error>> GetProposalAsync(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, CancellationToken token = default)
        => GetOptionalAsync<Proposal>(ProposalUrl(contextId, threadId, proposalId), token);

    /// <summary>
    /// Gets your proposals in a context, including private drafts and published history.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="requestFilters">Filters to apply to your proposals.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    public Task<Result<PagedList<Proposal>, Error>> GetMineAsync(
        ContextId contextId,
        Filters requestFilters,
        PaginatedRequestOptions<EMapProposalSorter> requestOptions,
        CancellationToken token = default)
        => SendAsync<PagedList<Proposal>>(HttpMethod.Get, GetMineUrl(contextId, requestFilters, requestOptions),
            null, token);

    /// <summary>
    /// Asynchronously retrieves your proposal pages with automatic pagination.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="requestFilters">Filters to apply to your proposals.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <remarks>Enumeration stops at MaxPage, an empty page, or an error.</remarks>
    public async IAsyncEnumerable<Result<Proposal[], Error>> GetMineAsyncEnumerable(
        ContextId contextId,
        Filters requestFilters,
        PaginatedRequestOptions<EMapProposalSorter> requestOptions,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        while (requestOptions.Page <= requestOptions.MaxPage)
        {
            var result = (await GetMineAsync(contextId, requestFilters, requestOptions, token).ConfigureAwait(false))
                .Map(page => page.Data);
            yield return result;

            if (result is { IsFailure: true } or { Value: [] } || requestOptions.Page == requestOptions.MaxPage)
                yield break;

            requestOptions.Page++;
        }
    }

    /// <summary>Creates a thread and a private Rank draft. Missing ratings are resolved on submission.</summary>
    public Task<Result<Proposal, Error>> CreateAsync(
        ContextId contextId, CreateDraft request, CancellationToken token = default)
        => SendAsync<Proposal>(HttpMethod.Post, $"contexts/{contextId}/map-requests",
            JsonContent.Create(request, options: jsonOptions), token);

    /// <summary>Saves a Rank or Unrank draft in an existing thread. Unrank captures the current accepted ranking.</summary>
    public Task<Result<Proposal, Error>> CreateProposalAsync(
        ContextId contextId, MapRequestThreadId threadId, CreateDraft request, CancellationToken token = default)
        => SendAsync<Proposal>(HttpMethod.Post, $"{ThreadUrl(contextId, threadId)}/proposals",
            JsonContent.Create(request, options: jsonOptions), token);

    /// <summary>Saves a replacement private draft and deletes the original only when the transaction succeeds.</summary>
    /// <remarks>Returns a new proposal ID. Does not require another free draft slot.</remarks>
    public Task<Result<Proposal, Error>> ReplaceDraftAsync(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, CreateDraft request,
        CancellationToken token = default)
        => SendAsync<Proposal>(HttpMethod.Post, $"{ProposalUrl(contextId, threadId, proposalId)}/replace",
            JsonContent.Create(request, options: jsonOptions), token);

    /// <summary>Publishes a draft. Only set force after reviewing a duplicate_maps error's Duplicates.</summary>
    public Task<Result<Proposal, Error>> SubmitAsync(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, bool force = false,
        CancellationToken token = default)
        => SendAsync<Proposal>(HttpMethod.Post, $"{ProposalUrl(contextId, threadId, proposalId)}/submit?force={force}",
            null, token);

    public Task<Result<Proposal, Error>> CloseAsync(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, string? reason = null,
        CancellationToken token = default)
        => SendAsync<Proposal>(HttpMethod.Post, $"{ProposalUrl(contextId, threadId, proposalId)}/close",
            JsonContent.Create(new Close(reason), options: jsonOptions), token);

    public Task<UnitResult<Error>> DeleteDraftAsync(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, CancellationToken token = default)
        => SendEmptyAsync(HttpMethod.Delete, ProposalUrl(contextId, threadId, proposalId), null, token);

    /// <summary>Accepts a submitted change and returns its Ranked or Unranked result.</summary>
    public Task<Result<Acceptance, Error>> AcceptAsync(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, CancellationToken token = default)
        => SendAsync<Acceptance>(HttpMethod.Post, $"{ProposalUrl(contextId, threadId, proposalId)}/accept", null,
            token);

    public Task<Result<Proposal, Error>> DenyAsync(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, string? reason = null,
        CancellationToken token = default)
        => SendAsync<Proposal>(HttpMethod.Post, $"{ProposalUrl(contextId, threadId, proposalId)}/deny",
            JsonContent.Create(new Close(reason), options: jsonOptions), token);

    public Task<UnitResult<Error>> VoteCommunityAsync(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, CommunityVote vote,
        CancellationToken token = default)
        => SendEmptyAsync(HttpMethod.Put, $"{ProposalUrl(contextId, threadId, proposalId)}/votes/community/@me",
            JsonContent.Create(vote, options: jsonOptions), token);

    public Task<UnitResult<Error>> VoteRankingTeamAsync(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, RankingTeamVote vote,
        CancellationToken token = default)
        => SendEmptyAsync(HttpMethod.Put, $"{ProposalUrl(contextId, threadId, proposalId)}/votes/ranking-team/@me",
            JsonContent.Create(vote, options: jsonOptions), token);

    public Task<UnitResult<Error>> RemoveCommunityVoteAsync(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, CancellationToken token = default)
        => SendEmptyAsync(HttpMethod.Delete, $"{ProposalUrl(contextId, threadId, proposalId)}/votes/community/@me",
            null, token);

    public Task<UnitResult<Error>> RemoveRankingTeamVoteAsync(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, CancellationToken token = default)
        => SendEmptyAsync(HttpMethod.Delete, $"{ProposalUrl(contextId, threadId, proposalId)}/votes/ranking-team/@me",
            null, token);

    public Task<Result<BoostAllowance, Error>> GetBoostsAsync(CancellationToken token = default)
        => SendAsync<BoostAllowance>(HttpMethod.Get, "players/@me/map-request-boosts", null, token);

    public Task<Result<Allowance, Error>> GetAllowanceAsync(ContextId contextId, CancellationToken token = default)
        => SendAsync<Allowance>(HttpMethod.Get, $"contexts/{contextId}/map-requests/allowance/@me", null, token);

    public Task<UnitResult<Error>> BoostAsync(
        ContextId contextId, MapRequestThreadId threadId, CancellationToken token = default)
        => SendEmptyAsync(HttpMethod.Put, $"{ThreadUrl(contextId, threadId)}/boosts/@me", null, token);

    public Task<UnitResult<Error>> MoveBoostAsync(
        ContextId targetContextId, MapRequestThreadId sourceThreadId, MapRequestThreadId targetThreadId,
        CancellationToken token = default)
        => SendEmptyAsync(HttpMethod.Post, $"{ThreadUrl(targetContextId, targetThreadId)}/boosts/@me/move",
            JsonContent.Create(new MoveBoost(sourceThreadId), options: jsonOptions), token);

    public Task<UnitResult<Error>> RemoveBoostAsync(
        ContextId contextId, MapRequestThreadId threadId, CancellationToken token = default)
        => SendEmptyAsync(HttpMethod.Delete, $"{ThreadUrl(contextId, threadId)}/boosts/@me", null, token);

    private async Task<Result<T?, Error>> GetOptionalAsync<T>(string url, CancellationToken token) where T : class
    {
        var result = await SendAsync<T>(HttpMethod.Get, url, null, token).ConfigureAwait(false);
        return result.IsSuccess
            ? Success<T?, Error>(result.Value)
            : result.Error.Status == 404
                ? Success<T?, Error>(null)
                : Failure<T?, Error>(result.Error);
    }

    private async Task<Result<T, Error>> SendAsync<T>(
        HttpMethod method, string url, HttpContent? content, CancellationToken token)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Content = content;
        request.Headers.Authorization = authenticationHeader;
        using var response = await httpClient.SendAsync(request, token).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return Failure<T, Error>(await ReadErrorAsync(response, token).ConfigureAwait(false));
        try
        {
            var value = await response.Content.ReadFromJsonAsync<T>(jsonOptions, token).ConfigureAwait(false);
            return value is null
                ? Failure<T, Error>(new Error((int)response.StatusCode, "Empty response",
                    "The API returned no response body."))
                : Success<T, Error>(value);
        }
        catch (JsonException exception)
        {
            return Failure<T, Error>(new Error((int)response.StatusCode, "Invalid response", exception.Message));
        }
    }

    private async Task<UnitResult<Error>> SendEmptyAsync(
        HttpMethod method, string url, HttpContent? content, CancellationToken token)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Content = content;
        request.Headers.Authorization = authenticationHeader;
        using var response = await httpClient.SendAsync(request, token).ConfigureAwait(false);
        return response.IsSuccessStatusCode
            ? UnitResult.Success<Error>()
            : Failure(await ReadErrorAsync(response, token).ConfigureAwait(false));
    }

    private async Task<Error> ReadErrorAsync(HttpResponseMessage response, CancellationToken token)
    {
        try
        {
            if (await response.Content.ReadFromJsonAsync<Error>(jsonOptions, token).ConfigureAwait(false) is { } error)
                return error with { Status = (int)response.StatusCode };
        }
        catch (JsonException)
        {
            /* A proxy or authentication middleware may return a non-JSON error. */
        }

        return new Error((int)response.StatusCode, response.ReasonPhrase, "The API request failed.");
    }
}