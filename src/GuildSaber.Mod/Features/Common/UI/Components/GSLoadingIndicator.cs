using System;
using System.Diagnostics.CodeAnalysis;
using CP_SDK.XUI;
using UnityEngine;

namespace GuildSaber.Mod.Features.Common.UI.Components;

public class GSLoadingIndicator(LoadingControl loadingControlTemplate, Logger logger)
    : IXUIElement("GuildSaberLoadingIndicator")
{
    protected LoadingControl? LoadingControl;

    public override RectTransform? RTransform => LoadingControl?._refreshContainer.GetComponent<RectTransform>();

    [SuppressMessage("ReSharper", "AccessToStaticMemberViaDerivedType")]
    public override void BuildUI(Transform parent)
    {
        try
        {
            var instantiated = GameObject.Instantiate(loadingControlTemplate.gameObject, parent);
            instantiated.name = m_InitialName;
            LoadingControl = instantiated.GetComponent<LoadingControl>();
            LoadingControl.ShowLoading();
        }
        catch (Exception ex)
        {
            logger.Error("Error while building GSLoadingIndicator");
            logger.Error(ex);
        }
    }
}