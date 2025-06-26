using Microsoft.AspNetCore.Authorization;

namespace GuildSaber.Api.Features.Auth.Authorization;

public record GuildPermissionRequirement(EPermission RequiredPermission)
    : IAuthorizationRequirement;