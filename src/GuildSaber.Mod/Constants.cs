using System;

namespace GuildSaber.Mod;

public static class Constants
{
    public static readonly Uri ProdApiBaseUrl = new("https://api.guildsaber.com/");
    public static readonly Uri DevApiBaseUrl = new("https://api-dev.guildsaber.com/");

    public static readonly Uri ProdCdnBaseUrl = new("https://cdn.guildsaber.com/");
    public static readonly Uri DevCdnBaseUrl = new("https://cdn-dev.guildsaber.com/");
}