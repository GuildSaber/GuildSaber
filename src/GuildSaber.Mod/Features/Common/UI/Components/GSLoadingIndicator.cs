using CP_SDK.XUI;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace GuildSaber.Mod.Features.Common.UI.Components;

public class GSLoadingIndicator(LoadingControl loadingControlTemplate)
    : IXUIElement("GuildSaberLoadingIndicator")
{
    protected LoadingControl? LoadingControl;
    public override RectTransform? RTransform => LoadingControl?._refreshContainer.GetComponent<RectTransform>();

    public static GSLoadingIndicator Make()
        => new(StaticContext.Container.ResolveId<LoadingControl>(Constants.LoadingControlTemplateId));

    public override void BuildUI(Transform parent)
    {
        var instantiated = Object.Instantiate(loadingControlTemplate.gameObject, parent);
        instantiated.name = m_InitialName;
        LoadingControl = instantiated.GetComponent<LoadingControl>();
        LoadingControl.ShowLoading();
    }
}