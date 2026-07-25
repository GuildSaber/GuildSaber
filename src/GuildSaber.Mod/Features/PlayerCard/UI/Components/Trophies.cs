using CP_SDK.UI.Components;
using CP_SDK.XUI;
using GuildSaber.Common.Extra;
using UnityEngine;

namespace GuildSaber.Mod.Features.PlayerCard.UI.Components;

internal sealed class PlayerCardTrophies : XUIHLayout
{
    private readonly PlayerCardResources _resources;
    private Trophy _diamond = null!;
    private Trophy _gold = null!;
    private Trophy _plastic = null!;
    private Trophy _ruby = null!;
    private Trophy _silver = null!;

    private PlayerCardTrophies(PlayerCardResources resources) : base("PlayerCardTrophies")
    {
        _resources = resources;
        SetPadding(1, 0, 0, 0);
        SetSpacing(2);
        OnReady(Build);
    }

    private sealed class Trophy : XUIHLayout
    {
        private XUIText _value = null!;

        public Trophy(Texture2D texture) : base("PlayerCardTrophy")
        {
            SetWidth(15.6f);
            SetPadding(0);
            OnReady(layout =>
            {
                XUIImage.Make(Sprite.Create(
                        texture,
                        new Rect(Vector2.zero, new Vector2(texture.width, texture.height)),
                        Vector2.zero))
                    .SetWidth(3.6f)
                    .SetHeight(3.6f)
                    .BuildUI(layout.transform);

                XUIText.Make(string.Empty)
                    .Bind(ref _value)
                    .SetFontSize(3.6f)
                    .BuildUI(layout.transform);

                layout.HOrVLayoutGroup.childForceExpandHeight = false;
                layout.HOrVLayoutGroup.childForceExpandWidth = false;
                layout.HOrVLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
            });
        }

        public void Render(int value) => _value.SetText(value.ToString("0"));
    }

    public static PlayerCardTrophies Make(PlayerCardResources resources) => new(resources);

    public void Render(TrophiesData data)
    {
        _plastic.Render(data.Plastic);
        _silver.Render(data.Silver);
        _gold.Render(data.Gold);
        _diamond.Render(data.Diamond);
        _ruby.Render(data.Ruby);
    }

    private void Build(CHOrVLayout layout)
    {
        layout.LElement.flexibleHeight = 0;
        layout.HOrVLayoutGroup.childForceExpandHeight = false;
        layout.HOrVLayoutGroup.childForceExpandWidth = false;
        layout.HOrVLayoutGroup.childAlignment = TextAnchor.MiddleCenter;

        _plastic = MakeTrophy(_resources.PlasticTrophyTexture);
        _silver = MakeTrophy(_resources.SilverTrophyTexture);
        _gold = MakeTrophy(_resources.GoldTrophyTexture);
        _diamond = MakeTrophy(_resources.DiamondTrophyTexture);
        _ruby = MakeTrophy(_resources.RubyTrophyTexture);

        _plastic.BuildUI(layout.transform);
        _silver.BuildUI(layout.transform);
        _gold.BuildUI(layout.transform);
        _diamond.BuildUI(layout.transform);
        _ruby.BuildUI(layout.transform);
    }

    private Trophy MakeTrophy(Texture2D texture) => new(texture);
}