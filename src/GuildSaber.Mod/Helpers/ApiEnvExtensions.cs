using System;
using GuildSaber.Mod.Features.GuildSaber;

namespace GuildSaber.Mod.Helpers;

public static class ApiEnvExtensions
{
    extension(ApiEnv self)
    {
        public Uri ToApiUri() => self switch
        {
            ApiEnv.Prod => Constants.ProdApiBaseUrl,
            ApiEnv.Dev => Constants.DevApiBaseUrl,
            ApiEnv.Local => Constants.LocalApiBaseUrl,
            _ => throw new ArgumentOutOfRangeException(nameof(self), self, null)
        };

        public Uri ToCdnUri() => self switch
        {
            ApiEnv.Prod => Constants.ProdCdnBaseUrl,
            ApiEnv.Dev => Constants.DevCdnBaseUrl,
            ApiEnv.Local => Constants.LocalCdnBaseUrl,
            _ => throw new ArgumentOutOfRangeException(nameof(self), self, null)
        };


        public Uri ToWebsiteUri() => self switch
        {
            ApiEnv.Prod => Constants.ProdWebsiteBaseUrl,
            ApiEnv.Dev => Constants.DevWebsiteBaseUrl,
            ApiEnv.Local => Constants.LocalWebsiteBaseUrl,
            _ => throw new ArgumentOutOfRangeException(nameof(self), self, null)
        };
    }
}