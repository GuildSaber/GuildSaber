using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using GuildSaber.Api.Features.RankedMaps;
using GuildSaber.Common.Helpers;
using GuildSaber.Mod.Features.RankedMap;
using GuildSaber.Mod.Helpers;
using HMUI;
using UnityEngine;
using Zenject;

namespace GuildSaber.Mod.Features.MenuTweaks.PlayButtonRequirements;

/// <summary>
/// Makes the play button red when the selected map is ranked and the player's selected modifiers don't meet the ranked
/// map's requirements.
/// </summary>
[SuppressMessage("ReSharper", "AccessToStaticMemberViaDerivedType")]
public sealed class PlayButtonRequirements(
    RankedMapManager rankedMapManager,
    StandardLevelDetailView standardLevelDetailView,
    GameplayModifiersPanelController gameplayModifiersPanelController
) : IInitializable, IDisposable
{
    private readonly NoTransitionsButton _actionButton = (NoTransitionsButton)standardLevelDetailView.actionButton;
    private ImageView[] _actionButtonBaseImageViews = null!;

    private ImageView[] _actionButtonRedImageViews = null!;
    private RankedMapResponses.RankedMap? _rankedMap;

    public void Dispose()
    {
        rankedMapManager.OnMapSelected -= OnMapSelected;
        gameplayModifiersPanelController.didChangeGameplayModifiersEvent -= OnModifiersChanged;
        //_actionButton.selectionStateDidChangeEvent -= OnActionButtonStateChange;
    }

    public void Initialize()
    {
        _actionButtonBaseImageViews = _actionButton.GetComponentsInChildren<ImageView>()
            // We exclude "Outline" on purpose so it can still be shown on hover normally.
            .Where(x => x.name is "BG" or "Border")
            .ToArray();

        _actionButtonRedImageViews = new ImageView[_actionButtonBaseImageViews.Length];
        for (var i = 0; i < _actionButtonRedImageViews.Length; i++)
        {
            var baseImageView = _actionButtonBaseImageViews[i];

            var newGameObject = GameObject.Instantiate(baseImageView.gameObject, baseImageView.transform.parent, false);
            var newImageView = newGameObject.GetComponent<ImageView>();

            newGameObject.name = baseImageView.gameObject.name + "_red";

            newImageView.color = Color.white;
            newImageView.color0 = Color.red;
            newImageView.color1 = new Color(1.0f, 0.3f, 0.0f);

            newImageView.gameObject.SetActive(false);
            newImageView.gameObject.transform.SetSiblingIndex(baseImageView.gameObject.transform.GetSiblingIndex() + 1);

            _actionButtonRedImageViews[i] = newImageView;
        }

        rankedMapManager.OnMapSelected += OnMapSelected;
        gameplayModifiersPanelController.didChangeGameplayModifiersEvent += OnModifiersChanged;
        //_actionButton.selectionStateDidChangeEvent += OnActionButtonStateChange;
    }

    private void OnMapSelected(RankedMapEventData eventData)
    {
        _rankedMap = eventData.RankedMapWithScores?.RankedMap;
        UpdateEligibility(gameplayModifiersPanelController.gameplayModifiers);
    }

    private void OnModifiersChanged() => UpdateEligibility(gameplayModifiersPanelController.gameplayModifiers);

    private void UpdateEligibility(GameplayModifiers? gameplayModifiers)
    {
        var modifiers = gameplayModifiers?.ToEModifier() ?? RankedMapRequests.EModifiers.None;

        // Interactable check is used because SongCore or over mods disables the PlayButton.
        var isInteractable = _actionButton.IsInteractable();
        var isEligible = _rankedMap is null
                         || !modifiers.HasAnyFlag(_rankedMap.Requirements.ProhibitedModifiers)
                         && modifiers.HasFlag(_rankedMap.Requirements.MandatoryModifiers);

        foreach (var imageView in _actionButtonBaseImageViews)
            imageView.gameObject.SetActive(isEligible && isInteractable);

        foreach (var imageView in _actionButtonRedImageViews)
            imageView.gameObject.SetActive(!isEligible && isInteractable);
    }
}