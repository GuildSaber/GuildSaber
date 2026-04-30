using System;
using BeatSaberMarkupLanguage;
using HMUI;

namespace GuildSaber.Mod.Core.UI.Common;

public abstract class CustomFlowCoordinator : FlowCoordinator
{
    private FlowCoordinator? _lastFlowCoordinator;

    protected abstract string Title { get; }

    protected virtual bool ShowBackButton { get; } = true;

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
        _lastFlowCoordinator = BeatSaberUI.MainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf();
        if (!_lastFlowCoordinator) return;

        _lastFlowCoordinator.PresentFlowCoordinator(this, () => IsPresent = true);
        OnShow();
    }

    public void Dismiss(Action? finishedCallback = null)
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