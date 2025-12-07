using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using static GuildSaber.Api.Features.Guilds.Categories.CategoryResponses;

namespace GuildSaber.CSharpClient.Routes.Guilds.Categories;

public sealed class CategoryClient(
    HttpClient httpClient,
    //AuthenticationHeaderValue? authenticationHeader,
    JsonSerializerOptions jsonOptions)
{
    public async Task<Result<Category[]>> GetAllByGuildIdAsync(GuildId guildId, CancellationToken token)
        => await httpClient.GetAsync($"guilds/{guildId}/categories", token).ConfigureAwait(false) switch
        {
            { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                => Failure<Category[]>(
                    $"Failed to retrieve categories for guild with ID {guildId}: {(int)statusCode} ({reasonPhrase})"),
            var response => (await Try(() => response.Content.ReadFromJsonAsync<Category[]>(jsonOptions, token))
                .ConfigureAwait(false))!
        };
}