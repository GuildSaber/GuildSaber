namespace GuildSaber.Api.Features.Guilds.Categories;

public static class CategoryResponses
{
    public readonly record struct Category(
        ulong Id,
        ulong GuildId,
        CategoryInfo Info
    );

    public readonly record struct CategoryInfo(
        string Name,
        string Description
    );
}