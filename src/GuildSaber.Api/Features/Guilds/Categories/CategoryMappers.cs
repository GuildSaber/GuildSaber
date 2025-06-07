using System.Linq.Expressions;
using GuildSaber.Database.Models.Server.Guilds.Categories;

namespace GuildSaber.Api.Features.Guilds.Categories;

public static class CategoryMappers
{
    public static Expression<Func<Category, CategoryResponses.Category>> MapCategoryExpression
        => self => new CategoryResponses.Category
        {
            Id = self.Id,
            GuildId = self.GuildId,
            Info = new CategoryResponses.CategoryInfo
            {
                Name = self.Info.Name,
                Description = self.Info.Description
            }
        };

    public static CategoryResponses.Category Map(this Category self) => new()
    {
        Id = self.Id,
        GuildId = self.GuildId,
        Info = new CategoryResponses.CategoryInfo
        {
            Name = self.Info.Name,
            Description = self.Info.Description
        }
    };
}