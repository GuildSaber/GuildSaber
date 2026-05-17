using TMPro;
using UnityEngine;

namespace GuildSaber.Mod.Features.PlayerCard;

public record PlayerCardResources(
    TMP_FontAsset Font,
    Sprite BorderSprite,
    Material BorderMaterial,
    Texture2D DownArrowTexture,
    Texture2D GsWhiteLogoTexture,
    Texture2D PlasticTrophyTexture,
    Texture2D SilverTrophyTexture,
    Texture2D GoldTrophyTexture,
    Texture2D DiamondTrophyTexture,
    Texture2D RubyTrophyTexture
);