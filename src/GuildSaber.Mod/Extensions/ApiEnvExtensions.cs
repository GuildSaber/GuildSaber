using System;
using GuildSaber.Mod.Configurations;

namespace GuildSaber.Mod.Extensions;

public static class ApiEnvExtensions
{
    extension(ApiEnv self)
    {
        public Uri ToApiUri => self switch
        {
            ApiEnv.Prod => Constants.ProdApiBaseUrl,
            ApiEnv.Dev => Constants.DevApiBaseUrl,
            _ => throw new ArgumentOutOfRangeException(nameof(self), self, null)
        };
    }
}