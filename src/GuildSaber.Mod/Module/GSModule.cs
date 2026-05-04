using CP_SDK;
using CP_SDK.UI;
using GuildSaber.Mod.Core.UI.Settings;

namespace GuildSaber.Mod.Module;

public class GSModule : ModuleBase<GSModule>
{
    protected override void OnEnable() { }

    protected override void OnDisable() { }
    public override EIModuleBaseType Type => EIModuleBaseType.Integrated;
    public override string Name => "Guild Saber";
    public override string Description => "Guild Saber settings!";
    public override bool UseChatFeatures => false;
    public override bool IsEnabled { get => field; set => field = true; }
    public override EIModuleBaseActivationType ActivationType => EIModuleBaseActivationType.Never;

    public static GuildSaberSettingsView? SettingsView = null;
    
    protected override (IViewController?, IViewController?, IViewController?) GetSettingsViewControllersImplementation() 
        => (SettingsView, null, null);
    
    
}