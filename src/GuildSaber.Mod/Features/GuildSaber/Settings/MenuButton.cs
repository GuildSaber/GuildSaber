using BeatSaberMarkupLanguage.MenuButtons;
using Zenject;

namespace GuildSaber.Mod.Features.GuildSaber.Settings;

public class GuildSaberMenuButton : BeatSaberMarkupLanguage.MenuButtons.MenuButton
{
    public GuildSaberMenuButton([Inject] GuildSaberSettingsFlowCoordinator guildSaberSettingsFlowCoordinator) 
        : base("Guild Saber", null)
    {
        OnClick += guildSaberSettingsFlowCoordinator.Present;
        
        MenuButtons.Instance.RegisterButton(this);
    }
}