namespace GuildSaber.Api.Shared;

public static class ImageUploadLimits
{
    public const int MaxBytes = 2 * 1024 * 1024;
    public const int MaxGuildImageDimension = 2048;
    public const int MaxIconDimension = 1024;

    public static readonly (int Width, int Height) GuildBannerAspectRatio = (15, 2);
}
