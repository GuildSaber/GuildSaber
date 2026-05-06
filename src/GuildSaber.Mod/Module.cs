using CP_SDK;
using CP_SDK.UI;
using GuildSaber.Mod.Features.GuildSaber.Settings;

namespace GuildSaber.Mod;

public class Module : ModuleBase<Module>
{
    public static GuildSaberSettingsView? SettingsView = null;
    public override EIModuleBaseType Type => EIModuleBaseType.Integrated;

    public override string Name => "Guild Saber";
    public override string Description => "Guild Saber settings!";
    public override bool UseChatFeatures => false;

    // ReSharper disable once ValueParameterNotUsed
    public override bool IsEnabled { get; set => field = true; } = true;
    public override EIModuleBaseActivationType ActivationType => EIModuleBaseActivationType.Never;

    protected override void OnEnable() { }
    protected override void OnDisable() { }

    protected override (IViewController?, IViewController?, IViewController?) GetSettingsViewControllersImplementation()
        => (SettingsView, null, null);
}