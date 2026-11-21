using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using CSharpFunctionalExtensions;

namespace GuildSaber.Api.Extensions;

internal static class S3Extensions
{
    public static async Task<UnitResult<string>> UploadAsync(
        this AmazonS3Client client, string bucketName, string key, Stream stream, string contentType,
        CancellationToken token)
    {
        HttpStatusCode statusCode;
        try
        {
            statusCode = (await client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                InputStream = stream,
                ContentType = contentType,
                AutoCloseStream = false,
                DisablePayloadSigning = true,
                DisableDefaultChecksumValidation = true
            }, token)).HttpStatusCode;
        }
        catch (AmazonS3Exception exception) when ((int)exception.StatusCode is >= 400 and < 600)
        {
            statusCode = exception.StatusCode;
        }
        catch (Exception exception) when (!token.IsCancellationRequested
                                          && exception is HttpRequestException or TimeoutException)
        {
            return UnitResult.Failure("Could not reach S3 while uploading the file. Try again.");
        }

        return statusCode == HttpStatusCode.OK
            ? UnitResult.Success<string>()
            : UnitResult.Failure($"S3 returned HTTP {(int)statusCode} while uploading the file.");
    }
}
