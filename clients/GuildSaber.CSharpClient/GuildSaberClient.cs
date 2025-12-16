using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.Services.ScoreSaber.Models.StrongTypes;
using GuildSaber.CSharpClient.Auth;
using GuildSaber.CSharpClient.Routes.Guilds;
using GuildSaber.CSharpClient.Routes.Guilds.Categories;
using GuildSaber.CSharpClient.Routes.Guilds.Members.ContextStats;
using GuildSaber.CSharpClient.Routes.Guilds.Members.LevelStats;
using GuildSaber.CSharpClient.Routes.Leaderboards;
using GuildSaber.CSharpClient.Routes.Players;
using GuildSaber.CSharpClient.Routes.RankedMaps;

namespace GuildSaber.CSharpClient;

public class GuildSaberClient : IDisposable
{
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
    public GuildSaberClient(Uri baseUri, GuildSaberAuthentication? authentication) : this(new HttpClient
    {
        BaseAddress = baseUri,
        Timeout = TimeSpan.FromSeconds(30)
    }, authentication) => _disposeHttpClient = true;
#endif

    public GuildClient Guilds => field ??= new GuildClient(HttpClient, _authenticationHeader, _jsonOptions);
    public PlayerClient Players => field ??= new PlayerClient(HttpClient, _authenticationHeader, _jsonOptions);

    public CategoryClient Categories
        => field ??= new CategoryClient(HttpClient, /*_authenticationHeader,*/ _jsonOptions);

    public LevelStatClient LevelStats
        => field ??= new LevelStatClient(HttpClient, _authenticationHeader, _jsonOptions);

    public ContextStatClient ContextStats
        => field ??= new ContextStatClient(HttpClient, _authenticationHeader, _jsonOptions);

    public LeaderboardClient Leaderboards
        => field ??= new LeaderboardClient(HttpClient, _jsonOptions);

    public RankedMapClient RankedMaps
        => field ??= new RankedMapClient(HttpClient, _jsonOptions);

    public void Dispose()
    {
        if (_disposeHttpClient)
            HttpClient.Dispose();
    }
}