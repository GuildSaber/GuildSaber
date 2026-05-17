using CP_SDK.UI.Components;
using CP_SDK.XUI;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.Common.UI.Components;
using UnityEngine;

namespace GuildSaber.Mod.Features.PlayerCard.UI.Components;

public class Trophy : XUIHLayout
{
    private readonly Texture2D _trophyTexture;
    private readonly UIFactory _uiFactory;

    private XUIImage _trophyImage = null!;
    private GSText _valueText = null!;

    protected Trophy(Texture2D trophyTexture, UIFactory uiFactory) : base("GuildSaberTrophy")
    {
        _trophyTexture = trophyTexture;
        _uiFactory = uiFactory;
        OnReady(EventReady);
    }

    public static Trophy Make(Texture2D trophyTexture, UIFactory uiFactory) => new(trophyTexture, uiFactory);

    private void EventReady(CHOrVLayout x)
    {
        XUIImage.Make(
                Sprite.Create(
                    _trophyTexture,
                    new Rect(Vector2.zero, new Vector2(_trophyTexture.width, _trophyTexture.height)),
                    Vector2.zero))
            .Bind(ref _trophyImage)
            .SetWidth(3)
            .SetHeight(3)
            .BuildUI(x.transform);

        _uiFactory.Text(string.Empty)
            .Bind(ref _valueText)
            .SetFontSize(4.0f)
            .BuildUI(x.transform);

        x.HOrVLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
        //x.CSizeFitter.horizontalFit = x.CSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        //x.SetWidth(3);
        //x.SetHeight(2);
    }

    public void Refresh(int value) => _valueText.SetText(value.ToString("0"));
}