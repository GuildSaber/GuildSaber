namespace GuildSaber.Api.Features.RankedMaps.MapVersions.Http;

public class MapVersionRequests
{
    public record AddMapVersion(
        BeatSaverKey BeatSaverKey,
        string Characteristic = "Standard",
        EDifficulty Difficulty = EDifficulty.ExpertPlus,
        string PlayMode = "Standard",
        byte Order = 0
    );
}