using System;
using System.Linq;
using CP_SDK.UI.DefaultFactories;
using CP_SDK.XUI;
using UnityEngine;

namespace GuildSaber.Mod.Features.Common.UI.Components;

public class GSLoadingIndicator : IXUIElement
{
    protected LoadingControl? Element = null;

    private readonly Logger _logger = null!;
    private readonly LoadingControl _template = null!;
    
    public GSLoadingIndicator(LoadingControl loadingControlTemplate, Logger logger) : base("GuildSaberLoadingIndicator")
    {
        _template = loadingControlTemplate;
        _logger = logger;
    }

    public override void BuildUI(Transform p_Parent)
    {
        try
        {
            var loadingControl = _template;
            var instantiated = GameObject.Instantiate(loadingControl.gameObject, p_Parent);
            instantiated.name = m_InitialName;
            Element = instantiated.GetComponent<LoadingControl>();
            Element.ShowLoading();
        }
        catch (Exception ex)
        {
            _logger.Error("[GuildSaber][GSLoadingIndicator/BuildUI] Error while building GSLoadingIndicator");
            _logger.Error(ex);
        }
    }

    public override RectTransform? RTransform => Element?._refreshContainer.GetComponent<RectTransform>();
}