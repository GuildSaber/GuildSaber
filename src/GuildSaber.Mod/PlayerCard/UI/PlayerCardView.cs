using CP_SDK_BS.UI;
using CP_SDK.XUI;
using HMUI;
using SiraUtil.Logging;
using UnityEngine.UI;
using Zenject;

namespace GuildSaber.Mod.PlayerCard.UI;

internal class PlayerCardView : ViewController<PlayerCardView>
{
    [Inject] private readonly SiraLog _logger = null!;
    [Inject] private readonly PlayerCardResources _resources = null!;

    protected override void OnViewCreation()
    {
        _logger.Info(
            $"Down arrow texture size: {_resources.DownArrowTexture.width}x{_resources.DownArrowTexture.height}");
        XUIVLayout.Make()
            .SetBackground(true)
            .OnReady(x =>
            {
                var imageView = x.gameObject.GetComponent<ImageView>();
                (imageView.sprite, imageView.material) = (_resources.BorderSprite, _resources.BorderMaterial);
            })
            .OnReady(x => x.CSizeFitter.verticalFit =
                x.CSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained)
            .BuildUI(transform);
    }
}