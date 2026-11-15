namespace GuildSaber.Api.Features.Guilds.Categories.Http;

public static class CategoryRequests
{
    /// <summary>Create a category shared by the guild's contexts.</summary>
    /// <param name="Name">Display name, between 2 and 50 characters after trimming.</param>
    /// <param name="Description">Description, between 1 and 256 characters after trimming.</param>
    public readonly record struct CreateCategory(
        string Name,
        string Description
    );

    /// <summary>Update a category's name and description.</summary>
    /// <param name="Name">Display name, between 2 and 50 characters after trimming.</param>
    /// <param name="Description">Description, between 1 and 256 characters after trimming.</param>
    public readonly record struct UpdateCategory(
        string Name,
        string Description
    );
}