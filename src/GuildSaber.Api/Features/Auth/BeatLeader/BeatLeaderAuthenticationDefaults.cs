/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers
 * for more information concerning the license and the contributors participating to this project.
 */

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;

// ReSharper disable once CheckNamespace
namespace AspNet.Security.OAuth.BeatLeader;

/// <summary>
/// Default values used by the BeatLeader authentication middleware.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "ConvertToConstant.Global")]
public class BeatLeaderAuthenticationDefaults
{
    /// <summary>
    /// Default value for <see cref="Microsoft.AspNetCore.Authentication.AuthenticationScheme.Name" />.
    /// </summary>
    public const string AuthenticationScheme = "BeatLeader";

    /// <summary>
    /// Default value for <see cref="Microsoft.AspNetCore.Authentication.AuthenticationScheme.DisplayName" />.
    /// </summary>
    public static readonly string DisplayName = "BeatLeader";

    /// <summary>
    /// Default value for <see cref="AuthenticationSchemeOptions.ClaimsIssuer" />.
    /// </summary>
    public static readonly string Issuer = "BeatLeader";

    /// <summary>
    /// Default value for <see cref="RemoteAuthenticationOptions.CallbackPath" />.
    /// </summary>
    public static readonly string CallbackPath = "/signin-beatleader";

    /// <summary>
    /// Default value for <see cref="OAuthOptions.AuthorizationEndpoint" />.
    /// </summary>
    public static readonly string AuthorizationEndpoint = "https://api.beatleader.com/oauth2/authorize";

    /// <summary>
    /// Default value for <see cref="OAuthOptions.TokenEndpoint" />.
    /// </summary>
    public static readonly string TokenEndpoint = "https://api.beatleader.com/oauth2/token";

    /// <summary>
    /// Default value for <see cref="OAuthOptions.UserInformationEndpoint" />.
    /// </summary>
    public static readonly string UserInformationEndpoint = "https://api.beatleader.com/oauth2/identity";
}