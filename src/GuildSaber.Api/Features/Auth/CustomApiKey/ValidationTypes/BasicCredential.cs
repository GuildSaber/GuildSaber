using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace GuildSaber.Api.Features.Auth.CustomApiKey.ValidationTypes;

public readonly record struct BasicCredential
{
    public const int SanitizedPasswordPrefixLength = 8;

    public readonly string? Password;
    public readonly string User;

    private BasicCredential(string User, string Password) =>
        (this.User, this.Password) = (User, Password);

    public string SanitizedPassword
        => $"{Password?[..Math.Min(SanitizedPasswordPrefixLength, Password.Length)]}-****-****-****-************";

    public static bool TryParse(string? parameter, out BasicCredential credential)
    {
        if (string.IsNullOrWhiteSpace(parameter))
        {
            credential = default;
            return false;
        }

        try
        {
            var credentialParts = Encoding.UTF8.GetString(Convert.FromBase64String(parameter)).Split(':');
            if (credentialParts.Length != 2 || string.IsNullOrWhiteSpace(credentialParts[0]))
            {
                credential = default;
                return false;
            }

            credential = new BasicCredential(credentialParts[0], credentialParts[1]);
            return true;
        }
        catch
        {
            credential = default;
            return false;
        }
    }

    [ExcludeFromCodeCoverage]
    public override string ToString() => string.Empty;
}