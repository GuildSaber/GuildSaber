namespace GuildSaber.Api.Features.Hardware.Http;

public static class HardwareResponses
{
    public record Hmd(HmdId Id, string Name);
    public record Controller(ControllerId Id, string Name);
}