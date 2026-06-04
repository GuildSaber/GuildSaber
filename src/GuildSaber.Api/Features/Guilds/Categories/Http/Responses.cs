namespace GuildSaber.Api.Features.Guilds.Categories.Http;

public static class CategoryResponses
{
    public readonly record struct Category(
        CategoryId Id,
        GuildId GuildId,
        CategoryInfo Info
    );

    public readonly record struct CategoryInfo(
        string Name,
        string Description
    );
}