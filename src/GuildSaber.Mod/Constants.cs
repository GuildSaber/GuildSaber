using System;

namespace GuildSaber.Mod;

public static class Constants
{
    public const string CardFloatingPanelId = "GuildSaber.Mod.PlayerCard.FloatingPanel";
    public const string MenuButtonId = "GuildSaber.Mod.MenuButton";
    public const string LoadingControlTemplateId = "GuildSaber.Mod.LoadingControlTemplate";

    public static readonly Uri ProdApiBaseUrl = new("https://api.guildsaber.com/");
    public static readonly Uri DevApiBaseUrl = new("https://api-dev.guildsaber.com/");

    public static readonly Uri ProdCdnBaseUrl = new("https://cdn.guildsaber.com/");
    public static readonly Uri DevCdnBaseUrl = new("https://cdn-dev.guildsaber.com/");

    public static readonly Uri ProdWebsiteBaseUrl = new("https://guildsaber.com/");
    public static readonly Uri DevWebsiteBaseUrl = new("https://dev.guildsaber.com/");
}