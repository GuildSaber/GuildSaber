using GuildSaber.Api.Features.Guilds.Members.Pipelines;

namespace GuildSaber.Api.Features.Guilds.Members;

public static class GuildMembersInstaller
{
    public static IServiceCollection AddGuildMembersFeature(this IServiceCollection services) => services
        .AddScoped<MemberService>()
        .AddTransient<MemberPointStatsPipeline>()
        .AddTransient<MemberLevelStatsPipeline>()
        .AddTransient<MemberJoinPipeline>();
}