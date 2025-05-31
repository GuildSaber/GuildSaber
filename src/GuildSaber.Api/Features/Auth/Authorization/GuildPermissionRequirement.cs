using GuildSaber.Database.Models.Server.Guilds.Members;
using Microsoft.AspNetCore.Authorization;

namespace GuildSaber.Api.Features.Auth.Authorization;

public record GuildPermissionRequirement(Member.EPermission RequiredPermission)
    : IAuthorizationRequirement;