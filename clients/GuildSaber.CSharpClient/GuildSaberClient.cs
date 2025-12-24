using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.Services.ScoreSaber.Models.StrongTypes;
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

namespace GuildSaber.CSharpClient;

/// <summary>
/// Main client for interacting with the GuildSaber API.
/// Provides access to all API endpoints through specialized sub-clients.
/// </summary>
public class GuildSaberClient : IDisposable
{
    /// <summary>
    /// The underlying HTTP client used for all API requests.
    /// </summary>
    public readonly HttpClient HttpClient;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new SongHashJsonConverter(),
            new BeatSaverKeyJsonConverter(),
            new BeatLeaderIdJsonConverter(),
            new BeatLeaderScoreIdJsonConverter(),
            new BLLeaderboardIdJsonConverter(),
            new ScoreSaberIdJsonConverter(),
            new ScoreSaberScoreIdJsonConverter(),
            new SSLeaderboardIdJsonConverter(),
            new SSGameModeJsonConverter()
        }
    };

    private readonly bool _disposeHttpClient;
    private readonly AuthenticationHeaderValue? _authenticationHeader;

    /// <summary>
    /// Initializes a new instance of the <see cref="GuildSaberClient" /> class using an existing HTTP client.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use. Must have a BaseAddress set.</param>
    /// <param name="authentication">Optional authentication credentials.</param>
    /// <exception cref="ArgumentNullException">Thrown when httpClient is null or BaseAddress is not set.</exception>
    public GuildSaberClient(HttpClient httpClient, GuildSaberAuthentication? authentication)
    {
        if (httpClient.BaseAddress is null)
            throw new ArgumentNullException(nameof(httpClient.BaseAddress), "HttpClient must have a BaseAddress set.");
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        HttpClient.Timeout = TimeSpan.FromSeconds(30);

        if (!HttpClient.DefaultRequestHeaders.Contains("User-Agent"))
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "GuildSaber.CSharpClient/1.0");

        _authenticationHeader = authentication?.ToAuthenticationHeader();
    }

#if NETCOREAPP2_1_OR_GREATER
    /// <summary>
    /// Initializes a new instance of the <see cref="GuildSaberClient"/> class with a base URI.
    /// Creates an internal HTTP client with optimized connection pooling settings.
    /// </summary>
    /// <param name="baseUri">The base URI for the GuildSaber API.</param>
    /// <param name="authentication">Optional authentication credentials.</param>
    public GuildSaberClient(Uri baseUri, GuildSaberAuthentication? authentication) :
        this(new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = int.MaxValue
        })
        {
            BaseAddress = baseUri,
            Timeout = TimeSpan.FromSeconds(30)
        }, authentication) => _disposeHttpClient = true;
#else
    /// <summary>
    /// Initializes a new instance of the <see cref="GuildSaberClient" /> class with a base URI.
    /// Creates an internal HTTP client.
    /// </summary>
    /// <param name="baseUri">The base URI for the GuildSaber API.</param>
    /// <param name="authentication">Optional authentication credentials.</param>
    public GuildSaberClient(Uri baseUri, GuildSaberAuthentication? authentication) : this(new HttpClient
    {
        BaseAddress = baseUri,
        Timeout = TimeSpan.FromSeconds(30)
    }, authentication) => _disposeHttpClient = true;
#endif

    /// <summary>
    /// Gets the guild client for interacting with guild endpoints.
    /// </summary>
    public GuildClient Guilds => field ??= new GuildClient(HttpClient, _authenticationHeader, _jsonOptions);

    /// <summary>
    /// Gets the player client for interacting with player endpoints.
    /// </summary>
    public PlayerClient Players => field ??= new PlayerClient(HttpClient, _authenticationHeader, _jsonOptions);

    /// <summary>
    /// Gets the category client for interacting with category endpoints.
    /// </summary>
    public CategoryClient Categories
        => field ??= new CategoryClient(HttpClient, _authenticationHeader, _jsonOptions);

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
        => field ??= new RankedMapClient(HttpClient, _jsonOptions);

    /// <summary>
    /// Gets the playlist client for interacting with playlist endpoints.
    /// </summary>
    public PlaylistClient Playlists
        => field ??= new PlaylistClient(HttpClient, _jsonOptions);

    /// <summary>
    /// Gets the level client for interacting with level endpoints.
    /// </summary>
    public LevelClient Levels
        => field ??= new LevelClient(HttpClient, _jsonOptions);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposeHttpClient)
            HttpClient.Dispose();
    }
}