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

        public Uri ToCdnUri => self switch
        {
            ApiEnv.Prod => Constants.ProdCdnBaseUrl,
            ApiEnv.Dev => Constants.DevCdnBaseUrl,
            _ => throw new ArgumentOutOfRangeException(nameof(self), self, null)
        };


        public Uri ToWebsiteUri => self switch
        {
            ApiEnv.Prod => Constants.ProdWebsiteBaseUrl,
            ApiEnv.Dev => Constants.DevWebsiteBaseUrl,
            _ => throw new ArgumentOutOfRangeException(nameof(self), self, null)
        };
    }
}