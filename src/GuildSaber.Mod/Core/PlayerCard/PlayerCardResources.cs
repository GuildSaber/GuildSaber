using TMPro;
using UnityEngine;

namespace GuildSaber.Mod.Core.PlayerCard;

public class PlayerCardResources : MonoBehaviour
{
    public TMP_FontAsset TekoFont = null!;
    public Sprite BorderSprite = null!;
    public Material BorderMaterial = null!;


    public Texture2D DownArrowTexture = null!;
    public Texture2D GsWhiteLogoTexture = null!;

    public void SetValues(Sprite borderSprite, Material borderMaterial, Texture2D downArrowTexture,
                          Texture2D gsWhiteLogoTexture, TMP_FontAsset tekoFont)
    {
        BorderSprite = borderSprite;
        BorderMaterial = borderMaterial;
        DownArrowTexture = downArrowTexture;
        GsWhiteLogoTexture = gsWhiteLogoTexture;
        TekoFont = tekoFont;
    }
}