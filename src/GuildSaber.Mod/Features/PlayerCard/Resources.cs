using TMPro;
using UnityEngine;

namespace GuildSaber.Mod.Features.PlayerCard;

public record PlayerCardResources(
    TMP_FontAsset Font,
    Sprite BorderSprite,
    Material BorderMaterial,
    Texture2D DownArrowTexture,
    Texture2D GsWhiteLogoTexture
);