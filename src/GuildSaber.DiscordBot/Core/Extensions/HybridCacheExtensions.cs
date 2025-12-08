using GuildSaber.Api.Features.Guilds;
using GuildSaber.Api.Features.Guilds.Categories;
using GuildSaber.Api.Features.Guilds.Members;
using GuildSaber.Common.Result;
using GuildSaber.CSharpClient;
using GuildSaber.Database.Models.StrongTypes;
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
                async static (state, token) => (await state.client.Guilds
                    .GetByDiscordIdAsync(state.id, token)
                    .Unwrap())?.Id,
                new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromHours(5)
                }, [self.DiscordGuildIdChangeTag]);

        public ValueTask<DiscordGuildId?> FindDiscordGuildIdFromGuildId(GuildId id, GuildSaberClient client)
            => self.GetOrCreateAsync($"DiscordGuildId_{id}", (id, client),
                async static (state, token) =>
                {
                    var guildResult = await state.client.Guilds.GetByIdAsync(state.id, token)
                        .Unwrap();

                    if (guildResult?.DiscordInfo.MainDiscordGuildId is null)
                        return null;

                    if (!DiscordGuildId.TryParse(guildResult.DiscordInfo.MainDiscordGuildId)
                            .TryGetValue(out var discordGuildId, out var error))
                        throw new InvalidOperationException(
                            $"Guild {state.id} has an invalid Discord Guild ID: {guildResult.DiscordInfo.MainDiscordGuildId} (Error: {error})");

                    return discordGuildId as DiscordGuildId?;
                },
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

        public ValueTask<CategoryResponses.Category?> GetCategoryByIdAsync(int id, GuildSaberClient client)
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
                    var response = await client.Players.GetExtendedAtMeAsync(token)
                        .Unwrap();

                    if (response is not { Player.IsManager: var isManager, Members: var members })
                        return new DiscordPlayerPermissionGroup
                        (
                            DiscordGuildPermissions: new Dictionary<DiscordGuildId, MemberResponses.EPermission>(),
                            IsManager: false
                        );

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