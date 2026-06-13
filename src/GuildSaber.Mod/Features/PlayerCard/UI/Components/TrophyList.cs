using CP_SDK.UI.Components;
using CP_SDK.XUI;
using GuildSaber.Common.Extra;
using GuildSaber.Mod.Features.Common.UI;

namespace GuildSaber.Mod.Features.PlayerCard.UI.Components;

public class TrophyList : XUIVLayout
{
    private readonly PlayerCardResources _playerCardResources;
    private readonly UIFactory _uiFactory;
    private Trophy _diamond = null!;

    //private Trophy _plastic = null!; 
    //private Trophy _silver = null!; 
    private Trophy _gold = null!;
    private Trophy _ruby = null!;

    protected TrophyList(PlayerCardResources playerCardResources, UIFactory uiFactory) : base("GuildSaberTrophyList")
    {
        _uiFactory = uiFactory;
        _playerCardResources = playerCardResources;
        OnReady(EventReady);
    }

    public static TrophyList Make(PlayerCardResources playerCardResources, UIFactory uiFactory)
        => new(playerCardResources, uiFactory);

    private void EventReady(CHOrVLayout x)
    {
        //x.CSizeFitter.verticalFit = x.CSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        //x.SetWidth(1);
        //x.SetHeight(10);
        //x.SetSpacing(1);

        //_plastic = Trophy.Make(_playerCardResources.PlasticTrophyTexture, _uiFactory);
        //_silver = Trophy.Make(_playerCardResources.SilverTrophyTexture, _uiFactory);
        _gold = Trophy.Make(_playerCardResources.GoldTrophyTexture, _uiFactory);
        _diamond = Trophy.Make(_playerCardResources.DiamondTrophyTexture, _uiFactory);
        _ruby = Trophy.Make(_playerCardResources.RubyTrophyTexture, _uiFactory);

        //_plastic.BuildUI(Element.VLayoutGroup.transform);
        //_silver.BuildUI(Element.VLayoutGroup.transform);
        _gold.BuildUI(Element.VLayoutGroup.transform);
        _diamond.BuildUI(Element.VLayoutGroup.transform);
        _ruby.BuildUI(Element.VLayoutGroup.transform);
    }

    public void Refresh(TrophiesData data)
    {
        //_plastic.Refresh(data.Plastic);
        //_silver.Refresh(data.Silver);
        _gold.Refresh(data.Gold);
        _diamond.Refresh(data.Diamond);
        _ruby.Refresh(data.Ruby);
    }

    public TrophyList Bind(ref TrophyList value) => value = this;
}