namespace GuildSaber.Api.Features.Songs;

public static class SongsInstaller
{
    public static IServiceCollection AddSongsFeature(this IServiceCollection services) => services
        .AddScoped<SongService>();
}