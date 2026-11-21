using CSharpFunctionalExtensions;
using GuildSaber.Api.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using SkiaSharp;

namespace GuildSaber.Api.Extensions;

internal static class ImageUploadExtensions
{
    private static ReadOnlySpan<byte> PngEndChunk => [0, 0, 0, 0, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82];
    private static ReadOnlySpan<byte> JpegEndMarker => [0xFF, 0xD9];

    public static RouteHandlerBuilder WithImageUpload(this RouteHandlerBuilder builder)
        => builder.WithFormOptions(valueCountLimit: 1, multipartBodyLengthLimit: ImageUploadLimits.MaxBytes)
            // SessionCookieRequestVerificationMiddleware verifies requests authenticated by a session cookie.
            .DisableAntiforgery()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status502BadGateway);

    public static async Task<Results<NoContent, ProblemHttpResult>> WithValidatedImageAsync<TState>(
        this IFormFile file, SKEncodedImageFormat format, int maxDimension, (int Width, int Height) aspectRatio,
        TState state,
        Func<TState, Stream, CancellationToken, Task<UnitResult<string>>> upload, CancellationToken token)
    {
        var formatName = format == SKEncodedImageFormat.Png ? "PNG" : "JPEG";
        if (file.Length is 0 or > ImageUploadLimits.MaxBytes)
            return TypedResults.Problem($"Upload a {formatName} image up to 2 MiB.",
                statusCode: StatusCodes.Status400BadRequest);

        await using var image = file.OpenReadStream();
        if (!IsValidImage(image, format, maxDimension, aspectRatio))
            return TypedResults.Problem(
                $"Upload a valid static {formatName} with a {aspectRatio.Width}:{aspectRatio.Height} aspect ratio "
                + $"and up to {maxDimension} pixels per side.",
                statusCode: StatusCodes.Status400BadRequest);

        image.Position = 0;
        return (await upload(state, image, token)).TryGetError(out var error)
            ? TypedResults.Problem(error, statusCode: StatusCodes.Status502BadGateway)
            : TypedResults.NoContent();
    }

    private static bool IsValidImage(
        Stream image, SKEncodedImageFormat format, int maxDimension, (int Width, int Height) aspectRatio)
    {
        var endMarker = format == SKEncodedImageFormat.Png ? PngEndChunk : JpegEndMarker;
        if (image.Length < endMarker.Length) return false;

        image.Position = image.Length - endMarker.Length;
        Span<byte> end = stackalloc byte[endMarker.Length];
        image.ReadExactly(end);
        if (!end.SequenceEqual(endMarker)) return false;

        image.Position = 0;
        using var codec = SKCodec.Create(new SKManagedStream(image, disposeManagedStream: false));
        if (codec is null || codec.EncodedFormat != format
            || codec.Info is not { Width: > 0, Height: > 0 } info
            || info.Width > maxDimension || info.Height > maxDimension
            || codec.FrameCount > 1)
            return false;

        var (width, height) = codec.EncodedOrigin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom
            ? (info.Height, info.Width) : (info.Width, info.Height);
        if ((long)width * aspectRatio.Height != (long)height * aspectRatio.Width) return false;

        using var bitmap = new SKBitmap(info);
        return codec.GetPixels(info, bitmap.GetPixels()) == SKCodecResult.Success;
    }
}
