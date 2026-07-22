using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using GuildSaber.CSharpClient.Auth;
using GuildSaber.CSharpClient.Routes.Guilds;
using GuildSaber.CSharpClient.Routes.Guilds.Categories;
using GuildSaber.CSharpClient.Routes.Guilds.Levels;
using GuildSaber.CSharpClient.Routes.Guilds.Levels.Playlists;
using GuildSaber.CSharpClient.Routes.Guilds.Members.ContextStats;
using GuildSaber.CSharpClient.Routes.Guilds.Members.LevelStats;
using GuildSaber.CSharpClient.Routes.Leaderboards;
using GuildSaber.CSharpClient.Routes.Players;
using GuildSaber.CSharpClient.Routes.RankedMaps;
using GuildSaber.CSharpClient.Routes.RankedScores;
using GuildSaber.CSharpClient.Routes.Scores;

namespace GuildSaber.CSharpClient;

/// <summary>
/// Main client for interacting with the GuildSaber API.
/// Provides access to all API endpoints through specialized sub-clients.
/// </summary>
public class GuildSaberClient : IDisposable
{
    private const string RequestVerificationHeaderName = "X-GuildSaber-Request";
    private const string RequestVerificationHeaderValue = "1";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly AuthenticationHeaderValue? _authenticationHeader;
    private readonly Uri _cdnBaseUri;

    private readonly bool _disposeHttpClient;

    /// <summary>
    /// The underlying HTTP client used for all API requests.
    /// </summary>
    public readonly HttpClient HttpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="GuildSaberClient" /> class using an existing HTTP client.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use. Must have a BaseAddress set.</param>
    /// <param name="cdnBaseUri">The base URI for the CDN. This is used for constructing URLs to access media assets.</param>
    /// <param name="authentication">Optional authentication credentials.</param>
    /// <remarks>
    /// Session authentication is carried by cookies. The supplied client must reuse a cookie-enabled handler for
    /// authentication responses and subsequent API requests to share the same cookie container.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when httpClient is null or BaseAddress is not set.</exception>
    public GuildSaberClient(HttpClient httpClient, Uri cdnBaseUri, GuildSaberAuthentication? authentication)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        if (HttpClient.BaseAddress is null)
            throw new ArgumentNullException(nameof(httpClient.BaseAddress), "HttpClient must have a BaseAddress set.");
        HttpClient.Timeout = TimeSpan.FromSeconds(30);

        if (!HttpClient.DefaultRequestHeaders.Contains("User-Agent"))
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "GuildSaber.CSharpClient/1.0");
        if (!HttpClient.DefaultRequestHeaders.Contains(RequestVerificationHeaderName))
            HttpClient.DefaultRequestHeaders.Add(RequestVerificationHeaderName, RequestVerificationHeaderValue);

        _authenticationHeader = authentication?.ToAuthenticationHeader();
        _cdnBaseUri = cdnBaseUri;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GuildSaberClient" /> class with a base URI.
    /// Creates an internal cookie-enabled HTTP client.
    /// </summary>
    /// <param name="baseUri">The base URI for the GuildSaber API.</param>
    /// <param name="cdnBaseUri">The base URI for the CDN. This is used for constructing URLs to access media assets.</param>
    /// <param name="authentication">Optional authentication credentials.</param>
    public GuildSaberClient(Uri baseUri, Uri cdnBaseUri, GuildSaberAuthentication? authentication) :
        this(baseUri, cdnBaseUri, new CookieContainer(), authentication) { }

    /// <summary>
    /// Initializes a client with a caller-owned cookie container.
    /// Reuse the container to retain a session established by an authentication request made with this client.
    /// </summary>
    /// <param name="baseUri">The base URI for the GuildSaber API.</param>
    /// <param name="cdnBaseUri">The base URI for the CDN.</param>
    /// <param name="cookieContainer">The cookie container used for authentication and subsequent requests.</param>
    /// <param name="authentication">Optional Basic API-key credentials.</param>
    public GuildSaberClient(
        Uri baseUri, Uri cdnBaseUri, CookieContainer cookieContainer, GuildSaberAuthentication? authentication) :
        this(CreateHttpClient(baseUri, cookieContainer), cdnBaseUri, authentication) => _disposeHttpClient = true;

    /// <summary>
    /// Gets the guild client for interacting with guild endpoints.
    /// </summary>
    public GuildClient Guilds => field
        ??= new GuildClient(HttpClient, _cdnBaseUri, _authenticationHeader, _jsonOptions);

    /// <summary>
    /// Gets the player client for interacting with player endpoints.
    /// </summary>
    public PlayerClient Players => field ??= new PlayerClient(HttpClient, _authenticationHeader, _jsonOptions);

    /// <summary>
    /// Gets the category client for interacting with category endpoints.
    /// </summary>
    public CategoryClient Categories
        => field ??= new CategoryClient(HttpClient, _cdnBaseUri, _authenticationHeader, _jsonOptions);

    /// <summary>
    /// Gets the level stat client for interacting with member level stat endpoints.
    /// </summary>
    public LevelStatClient LevelStats
        => field ??= new LevelStatClient(HttpClient, _authenticationHeader, _jsonOptions);

    /// <summary>
    /// Gets the context stat client for interacting with member context stat endpoints.
    /// </summary>
    public ContextStatClient ContextStats
        => field ??= new ContextStatClient(HttpClient, _authenticationHeader, _jsonOptions);

    /// <summary>
    /// Gets the leaderboard client for interacting with leaderboard endpoints.
    /// </summary>
    public LeaderboardClient Leaderboards
        => field ??= new LeaderboardClient(HttpClient, _jsonOptions);

    /// <summary>
    /// Gets the ranked map client for interacting with ranked map endpoints.
    /// </summary>
    public RankedMapClient RankedMaps
        => field ??= new RankedMapClient(HttpClient, _authenticationHeader, _jsonOptions);

    /// <summary>
    /// Gets the ranked score client for interacting with ranked score endpoints.
    /// </summary>
    public RankedScoreClient RankedScores
        => field ??= new RankedScoreClient(HttpClient, _authenticationHeader, _jsonOptions);

    /// <summary>
    /// Gets the score client for interacting with score endpoints.
    /// </summary>
    public ScoreClient Scores
        => field ??= new ScoreClient(HttpClient, _jsonOptions);

    /// <summary>
    /// Gets the playlist client for interacting with playlist endpoints.
    /// </summary>
    public PlaylistClient Playlists
        => field ??= new PlaylistClient(HttpClient, _jsonOptions);

    /// <summary>
    /// Gets the level client for interacting with level endpoints.
    /// </summary>
    public LevelClient Levels => field ??= new LevelClient(HttpClient, _cdnBaseUri, _jsonOptions);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposeHttpClient)
            HttpClient.Dispose();
    }

    private static HttpClient CreateHttpClient(Uri baseUri, CookieContainer cookieContainer)
    {
        if (cookieContainer is null)
            throw new ArgumentNullException(nameof(cookieContainer));

#if NETCOREAPP2_1_OR_GREATER
        var handler = new SocketsHttpHandler
        {
            UseCookies = true,
            CookieContainer = cookieContainer,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = int.MaxValue
        };
#else
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = cookieContainer
        };
#endif

        return new HttpClient(handler)
        {
            BaseAddress = baseUri,
            Timeout = TimeSpan.FromSeconds(30)
        };
    }
}