using System;
using GuildSaber.Mod.Features.Common.UI.Components;
using GuildSaber.Mod.Resources;
using TMPro;
using Zenject;

namespace GuildSaber.Mod.Features.Common.UI;

/// <summary>
/// Factory for creating UI components with the correct font and styling.
/// </summary>
/// <param name="font"></param>
public class UIFactory(
    [Inject(Id = nameof(ResourceMap.TekoMedium))] TMP_FontAsset font,
    [Inject(Id = Constants.LoadingControlTemplateId)] LoadingControl loadingControlTemplate, 
    [Inject] Logger logger)
{
    public GSText Text(string text) => new(text, font);
    public GSDropdown Dropdown() => new(font);
    public GSSecondaryButton SecondaryButton(string text, Action? onClick = null) => new(text, font, onClick);
    public GSLoadingIndicator LoadingIndicator() => new GSLoadingIndicator(loadingControlTemplate, logger);
}