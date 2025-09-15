namespace GuildSaber.Database.Models.Server.RankedMaps;

public record RankedMapRating(
    float Pass,
    float Acc,
    float Tech,
    float PredictedAcc
);