namespace GuildSaber.Api.Endpoints.Internal;

public interface IEndPoints
{
    public static abstract void MapEndpoints(IEndpointRouteBuilder endpoints);
    public static virtual void AddServices(IServiceCollection services, IConfiguration configuration) { }
}