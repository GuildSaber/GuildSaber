using GuildSaber.Api.Features.Guilds;
using GuildSaber.Api.Features.Guilds.Categories;
using GuildSaber.Api.Features.Guilds.Members;
using GuildSaber.Common.Result;
using GuildSaber.Common.StrongTypes;
using GuildSaber.CSharpClient;
using Microsoft.Extensions.Caching.Hybrid;

namespace GuildSaber.DiscordBot.Core.Extensions;

public static class HybridCacheExtensions
{
    public readonly record struct DiscordPlayerPermissionGroup(
        Dictionary<DiscordGuildId, MemberResponses.EPermission> DiscordGuildPermissions,
        bool IsManager
    );

    extension(HybridCache self)
    {
        /// <summary>
        /// Tag used to invalidate cache entries related to Discord Guild ID changes.
        /// </summary>
        public string DiscordGuildIdChangeTag => "DiscordGuildIdChange";

        public ValueTask<GuildId?> FindGuildIdFromDiscordGuildIdAsync(DiscordGuildId id, GuildSaberClient client)
            => self.GetOrCreateAsync($"GuildId_{id}", (id, client),
                async static (state, token) => await state.client.Guilds
                    .LookupGuildIdByDiscordGuildIdAsync(state.id, token)
                    .Unwrap(),
                new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromHours(5)
                }, [self.DiscordGuildIdChangeTag]);

        public ValueTask<DiscordGuildId?> FindDiscordGuildIdFromGuildId(GuildId id, GuildSaberClient client)
            => self.GetOrCreateAsync($"DiscordGuildId_{id}", (id, client),
                async static (state, token) => (await state.client.Guilds
                        .GetByIdAsync(state.id, token))
                    .Unwrap()?.DiscordInfo.MainDiscordGuildId,
                new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromHours(5)
                }, [self.DiscordGuildIdChangeTag]);

        public ValueTask<CategoryResponses.Category[]> GetGuildCategoriesAsync(GuildId id, GuildSaberClient client)
            => self.GetOrCreateAsync($"GuildCategories_{id}", (id, client),
                async static (state, token) => await state.client.Categories
                    .GetAllByGuildIdAsync(state.id, token)
                    .Unwrap(),
                new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromHours(5)
                });

        public ValueTask<CategoryResponses.Category?> GetCategoryByIdAsync(CategoryId id, GuildSaberClient client)
            => self.GetOrCreateAsync($"CategoryById_{id}", (id, client),
                async static (state, token) => await state.client.Categories
                    .GetByIdAsync(state.id, token)
                    .Unwrap(),
                new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromHours(5)
                });

        public ValueTask<GuildResponses.Guild?> GetGuildFromDiscordGuildIdAsync(
            DiscordGuildId id, GuildSaberClient client)
            => self.GetOrCreateAsync($"GuildFromDiscordGuildId_{id}", (id, client),
                async static (state, token) => await state.client.Guilds
                    .GetByDiscordIdAsync(state.id, token)
                    .Unwrap(),
                new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromHours(5)
                }, [self.DiscordGuildIdChangeTag]);

        public ValueTask<GuildResponses.GuildExtended?> GetGuildExtendedAsync(GuildId id, GuildSaberClient client)
            => self.GetOrCreateAsync($"GuildExtended_{id}", (id, client),
                async static (state, token) => await state.client.Guilds
                    .GetExtendedByIdAsync(state.id, token)
                    .Unwrap(),
                new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromHours(5)
                });

        /// <summary>
        /// Get the permissions of a user by their Discord User ID.
        /// </summary>
        /// <returns>A dictionary mapping Discord Guild IDs to the user's permissions in those guilds.</returns>
        public ValueTask<DiscordPlayerPermissionGroup> GetUserPermissionsOnDiscordGuildsAsync(
            DiscordId id, IServiceProvider services)
            => self.GetOrCreateAsync($"DiscordUserPermissions_{id}",
                (services, id, self),
                async static (state, token) =>
                {
                    var client = GuildSaberClient.GetAuthenticatedClient(state.id, state.services);
                    var playerExtended = await client.Players.GetExtendedAtMeAsync(token)
                        .Unwrap();

                    if (playerExtended is null)
                        return new DiscordPlayerPermissionGroup(DiscordGuildPermissions: [], IsManager: false);

                    var (members, isManager) = (playerExtended.Members, playerExtended.Player.IsManager);
                    var permissionsByGuild = new Dictionary<DiscordGuildId, MemberResponses.EPermission>();
                    foreach (var member in members)
                    {
                        var discordGuildId = await state.self.FindDiscordGuildIdFromGuildId(member.GuildId, client);
                        if (discordGuildId is null) continue;

                        permissionsByGuild.Add(
                            discordGuildId.Value,
                            member.Permissions
                        );
                    }

                    return new DiscordPlayerPermissionGroup
                    (
                        DiscordGuildPermissions: permissionsByGuild,
                        IsManager: isManager
                    );
                },
                new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(20)
                }, tags: [self.DiscordGuildIdChangeTag]);
    }
}