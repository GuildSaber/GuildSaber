using CP_SDK.UI;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.PlayerCard.UI;
using GuildSaber.Mod.Features.RankedMapStats;
using Zenject;

namespace GuildSaber.Mod.Features.GuildSaber.Settings;

public class GuildSaberSettingsFactory(
    [Inject] PlayerCardView cardView,
    [Inject] Config config,
    [Inject] GuildSaberManager guildSaberManager,
    [Inject] UIFactory uiFactory,
    [Inject] MapRankedStats mapRankedStats) : IFactory<GuildSaberSettingsView>
{
    public GuildSaberSettingsView Create()
    {
        var view = UISystem.CreateViewController<GuildSaberSettingsView>();
        view.Inject(cardView, config, guildSaberManager, uiFactory, mapRankedStats);
        Module.SettingsView = view;
        return view;
    }
}