using System;
using BeatSaberMarkupLanguage;
using HMUI;
using UnityEngine;

namespace GuildSaber.Mod.Features.Common.UI;

/// <summary>
/// A simple ple-implemented FlowCoordinator that provides fixes for some common ui issues with BS+.
/// </summary>
public abstract class SimpleFlowCoordinator : FlowCoordinator
{
    private FlowCoordinator? _lastFlowCoordinator;

    protected abstract string Title { get; }

    protected virtual bool ShowBackButton => true;

    public bool IsPresent { get; private set; }

    public void Awake() => OnCreation();

    protected virtual void OnCreation() { }

    protected abstract ViewController? GetMainViewController();

    protected virtual ViewController? GetLeftViewController() => null;
    protected virtual ViewController? GetRightViewController() => null;
    protected virtual ViewController? GetBottomViewController() => null;

    protected override void DidActivate(
        bool firstActivation,
        bool addedToHierarchy,
        bool screenSystemEnabling)
    {
        if (!firstActivation)
            return;
        SetTitle(Title);

        showBackButton = ShowBackButton;
        ProvideInitialViewControllers(
            GetMainViewController(),
            GetLeftViewController(),
            GetRightViewController(),
            GetBottomViewController());
    }

    protected override void BackButtonWasPressed(ViewController topView)
    {
        base.BackButtonWasPressed(topView);
        Dismiss();
    }

    public void Present()
    {
        //BUG: The "MainScreen" workaround doesn't work and opening a flow coordinator while bsplus menu is open soft lock the game.
        if (IsPresent || GameObject.Find("MainScreen") == null) return;

        _lastFlowCoordinator = BeatSaberUI.MainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf();
        if (!_lastFlowCoordinator) return;

        _lastFlowCoordinator.PresentFlowCoordinator(this, () => IsPresent = true);
        OnShow();
    }

    public void Dismiss() => Dismiss(null);

    public void Dismiss(Action? finishedCallback)
    {
        if (_lastFlowCoordinator == null)
            return;

        _lastFlowCoordinator.DismissFlowCoordinator(this, () =>
        {
            IsPresent = false;
            finishedCallback?.Invoke();
        });
        _lastFlowCoordinator = null;
        OnHide();
    }

    protected virtual void OnShow() { }

    protected virtual void OnHide() { }
}