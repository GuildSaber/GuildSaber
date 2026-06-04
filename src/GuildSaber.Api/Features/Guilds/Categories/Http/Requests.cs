namespace GuildSaber.Api.Features.Guilds.Categories.Http;

public static class CategoryRequests
{
    public readonly record struct CreateCategory(
        string Name,
        string Description
    );

    public readonly record struct UpdateCategory(
        string Name,
        string Description
    );
}