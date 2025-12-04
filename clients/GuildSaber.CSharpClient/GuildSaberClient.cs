using System.Text.Json;
using System.Text.Json.Serialization;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.Services.ScoreSaber.Models.StrongTypes;
using GuildSaber.CSharpClient.Routes.Guilds;
using GuildSaber.CSharpClient.Routes.Players;

namespace GuildSaber.CSharpClient;

public class GuildSaberClient : IDisposable
{
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
    private readonly HttpClient _httpClient;

    public GuildSaberClient(HttpClient httpClient)
    {
        if (httpClient.BaseAddress is null)
            throw new ArgumentNullException(nameof(httpClient.BaseAddress), "HttpClient must have a BaseAddress set.");
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.Timeout = TimeSpan.FromSeconds(30);

        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "GuildSaber.CSharpClient/1.0");
    }

#if NETCOREAPP2_1_OR_GREATER
    public GuildSaberClient(Uri baseUri) :
        this(new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = int.MaxValue
        })
        {
            BaseAddress = baseUri,
            Timeout = TimeSpan.FromSeconds(30)
        }) => _disposeHttpClient = true;
#else
    public GuildSaberClient(Uri baseUri) : this(new HttpClient
    {
        BaseAddress = baseUri,
        Timeout = TimeSpan.FromSeconds(30)
    }) => _disposeHttpClient = true;
#endif

    public GuildClient Guilds => field ??= new GuildClient(_httpClient, _jsonOptions);
    public PlayerClient Players => field ??= new PlayerClient(_httpClient, _jsonOptions);

    public void Dispose()
    {
        if (_disposeHttpClient)
            _httpClient.Dispose();
    }
}