namespace GuildSaber.Api.Features.Guilds.Categories;

public static class CategoryResponses
{
    public readonly record struct Category(
        int Id,
        int GuildId,
        CategoryInfo Info
    );

    public readonly record struct CategoryInfo(
        string Name,
        string Description
    );
}