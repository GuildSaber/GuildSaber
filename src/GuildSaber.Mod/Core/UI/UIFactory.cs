using System;
using GuildSaber.Mod.Core.UI.Common;
using GuildSaber.Mod.Resources;
using TMPro;
using Zenject;

namespace GuildSaber.Mod.Core.UI;

public class UIFactory(
    [Inject(Id = nameof(ResourceMap.TekoMedium))] TMP_FontAsset font
)
{
    public GSText Text(string text) => new(text, font);

    public GSSecondaryButton SecondaryButton(string text, Action? onClick = null) => new(text, font, onClick);

    public GSSecondaryButton SecondaryButton(string text, int width, int height, Action? onClick = null)
        => new(text, width, height, font, onClick);
}