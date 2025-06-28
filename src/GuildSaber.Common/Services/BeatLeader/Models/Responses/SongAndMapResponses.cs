using System.Diagnostics.CodeAnalysis;

namespace GuildSaber.Common.Services.BeatLeader.Models.Responses;

public class CompactSongResponse
{
    public required string Id { get; init; }
    public required string Hash { get; init; }
    public required string Name { get; init; }
    public required string? SubName { get; init; }
    public required string Author { get; init; }
    public required string Mapper { get; init; }
    public required int MapperId { get; init; }
    public required string? CollaboratorIds { get; init; }
    public required string CoverImage { get; init; }
    public required double Bpm { get; init; }
    public required double Duration { get; init; }
    public required string? FullCoverImage { get; init; }
}

public class DifficultyDescription
{
    public required int Id { get; init; }
    public required int Value { get; init; }
    public required int Mode { get; init; }
    public required string DifficultyName { get; init; }
    public required string ModeName { get; init; }
    public required DifficultyStatus Status { get; init; }
    public required ModifiersMap? ModifierValues { get; init; }
    public required ModifiersRating? ModifiersRating { get; init; }

    public required MaxScoreGraph? MaxScoreGraph { get; init; }
    public required int NominatedTime { get; init; }
    public required int QualifiedTime { get; init; }
    public required int RankedTime { get; init; }

    public required string Hash { get; init; } = "";
    public required string? SongId { get; init; }

    public required int SpeedTags { get; init; }
    public required int StyleTags { get; init; }
    public required int FeatureTags { get; init; }

    public required float? Stars { get; init; }
    public required float? PredictedAcc { get; init; }
    public required float? PassRating { get; init; }
    public required float? AccRating { get; init; }
    public required float? TechRating { get; init; }
    public required int Type { get; init; }

    public required float Njs { get; init; }
    public required float Nps { get; init; }
    public required int Notes { get; init; }
    public required int Chains { get; init; }
    public required int Sliders { get; init; }
    public required int Bombs { get; init; }
    public required int Walls { get; init; }
    public required int MaxScore { get; init; }
    public required double Duration { get; init; }

    public required Requirements Requirements { get; init; }
}

public class MaxScoreGraph
{
    public required int Id { get; init; }
    public required byte[] Graph { get; init; }

    public int GetScoreForPercent(float percent)
    {
        if (Graph.Length == 0) return 0;
        var place = (int)(percent / 100f * Graph.Length);
        if (place >= Graph.Length) place = Graph.Length - 1;
        return Graph[place] * 1000;
    }
}

[Flags]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum Requirements
{
    Ignore = -1,
    None = 0,
    Chroma = 1 << 1,
    Noodles = 1 << 2,
    MappingExtensions = 1 << 3,
    Cinema = 1 << 4,
    V3 = 1 << 5,
    OptionalProperties = 1 << 6,
    VNJS = 1 << 7,
    Vivify = 1 << 8,
    V3Pepega = 1 << 9,
    GroupLighting = 1 << 10
}

public class Song
{
    public required string Id { get; init; }
    public required string Hash { get; init; }
    public required string Name { get; init; }
    public required string? SubName { get; init; }
    public required string Author { get; init; }
    public required string Mapper { get; init; }
    public required int MapperId { get; init; }
    public required string? CollaboratorIds { get; init; }
    public required string CoverImage { get; init; }
    public required string? FullCoverImage { get; init; }
    public required string DownloadUrl { get; init; }
    public required double Bpm { get; init; }
    public required double Duration { get; init; }
    public required string? Tags { get; init; }
    public required SongCreator MapCreator { get; init; }
    public required int UploadTime { get; init; }
    public required ICollection<DifficultyDescription> Difficulties { get; init; }
}

public enum SongCreator
{
    Human = 0,
    GenericBot = 1,
    BeatSage = 2,
    TopMapper = 3
}

public class SongResponse
{
    public required string Id { get; init; }
    public required string Hash { get; init; }
    public required string Name { get; init; }
    public required string? SubName { get; init; }
    public required string Author { get; init; }
    public required string Mapper { get; init; }
    public required int MapperId { get; init; }
    public required string CoverImage { get; init; }
    public required string? FullCoverImage { get; init; }
    public required string DownloadUrl { get; init; }
    public required double Bpm { get; init; }
    public required double Duration { get; init; }
    public required int UploadTime { get; init; }
    public required ICollection<DifficultyDescription> Difficulties { get; init; }
}

public class DifficultyResponse
{
    public required int Id { get; init; }
    public required int Value { get; init; }
    public required int Mode { get; init; }
    public required string DifficultyName { get; init; }
    public required string ModeName { get; init; }
    public required DifficultyStatus Status { get; init; }
    public required ModifiersMap? ModifierValues { get; init; }
    public required ModifiersRating? ModifiersRating { get; init; }
    public required int NominatedTime { get; init; }
    public required int QualifiedTime { get; init; }
    public required int RankedTime { get; init; }

    public required int SpeedTags { get; init; }
    public required int StyleTags { get; init; }
    public required int FeatureTags { get; init; }

    public required float? Stars { get; init; }
    public required float? PredictedAcc { get; init; }
    public required float? PassRating { get; init; }
    public required float? AccRating { get; init; }
    public required float? TechRating { get; init; }
    public required int Type { get; init; }

    public required float Njs { get; init; }
    public required float Nps { get; init; }
    public required int Notes { get; init; }
    public required int Bombs { get; init; }
    public required int Walls { get; init; }
    public required int MaxScore { get; init; }
    public required double Duration { get; init; }

    public required Requirements Requirements { get; init; }
}

public class MapDiffResponse : DifficultyResponse
{
    public required string LeaderboardId { get; init; }
    public required int Plays { get; init; }
    public required int LastScoreTime { get; init; }
    public required int Attempts { get; init; }
    public required int PositiveVotes { get; init; }
    public required int StarVotes { get; init; }
    public required int NegativeVotes { get; init; }
    public required ScoreResponseWithAcc? MyScore { get; init; }
    public required bool Applicable { get; init; }
}

public class MapperResponse
{
    public required int? Id { get; init; }
    public required string? PlayerId { get; init; }
    public required string Name { get; init; }
    public required string Avatar { get; init; }
}

public class MapInfoResponse
{
    public required string Id { get; init; }
    public required ICollection<MapDiffResponse> Difficulties { get; init; }
    public required string Hash { get; init; }
    public required string Name { get; init; }
    public required string? SubName { get; init; }
    public required string Author { get; init; }
    public required string Mapper { get; init; }
    public required ICollection<MapperResponse>? Mappers { get; init; }
    public required int MapperId { get; init; }
    public required string? CollaboratorIds { get; init; }
    public required string CoverImage { get; init; }
    public required string? FullCoverImage { get; init; }
    public required string DownloadUrl { get; init; }
    public required double Bpm { get; init; }
    public required double Duration { get; init; }
    public required string? Tags { get; init; }
    public required int UploadTime { get; init; }
}

public class MapInfoResponseWithUpvotes : MapInfoResponse
{
    public required int Upvotes { get; init; }
}

public class RankedMap
{
    public required string Name { get; init; }
    public required string SongId { get; init; }
    public required string Cover { get; init; }
    public required float? Stars { get; init; }
}

public class RankedMapperResponse
{
    public required int PlayersCount { get; init; }
    public required float TotalPp { get; init; }
    public required ICollection<RankedMap> Maps { get; init; }
    public required int TotalMapCount { get; init; }
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "IdentifierTypo")]
public enum DifficultyStatus
{
    unranked = 0,
    nominated = 1,
    qualified = 2,
    ranked = 3,
    unrankable = 4,
    outdated = 5,
    inevent = 6,
    OST = 7
}

public class ModifiersMap
{
    public required int ModifierId { get; init; }

    public required float DA { get; init; }
    public required float FS { get; init; } = 0.20f;
    public required float SF { get; init; } = 0.36f;
    public required float SS { get; init; } = -0.3f;
    public required float GN { get; init; } = 0.04f;
    public required float NA { get; init; } = -0.3f;
    public required float NB { get; init; } = -0.2f;
    public required float NF { get; init; } = -0.5f;
    public required float NO { get; init; } = -0.2f;
    public required float PM { get; init; }
    public required float SC { get; init; }
    public required float SA { get; init; }
    public required float OP { get; init; } = -0.5f;

    public static ModifiersMap RankedMap() => new()
    {
        ModifierId = 0,
        DA = 0.0f,
        FS = 0.20f * 2,
        SF = 0.36f * 2,
        SS = -0.3f,
        GN = 0.00f,
        NA = -0.3f,
        NB = -0.2f,
        NF = -1.0f,
        NO = -0.2f,
        PM = 0.0f,
        SC = 0.0f,
        SA = 0.0f,
        OP = -0.5f
    };


    public static ModifiersMap ReBeatMap() => new()
    {
        ModifierId = 0,
        FS = 0.07f,
        SF = 0.15f,
        SS = -0.5f,
        PM = 0.12f,
        DA = 0.0f,
        GN = 0.0f,
        NA = -0.7f,
        NB = -0.4f,
        NO = -0.4f,
        SC = 0.0f,
        SA = 0.0f,
        NF = -0.5f,
        OP = -0.5f
    };
}

public class ModifiersRating
{
    public required int Id { get; init; }

    public required float SSPredictedAcc { get; init; }
    public required float SSPassRating { get; init; }
    public required float SSAccRating { get; init; }
    public required float SSTechRating { get; init; }
    public required float SSStars { get; init; }

    public required float FSPredictedAcc { get; init; }
    public required float FSPassRating { get; init; }
    public required float FSAccRating { get; init; }
    public required float FSTechRating { get; init; }
    public required float FSStars { get; init; }
    public required float SFPredictedAcc { get; init; }
    public required float SFPassRating { get; init; }
    public required float SFAccRating { get; init; }
    public required float SFTechRating { get; init; }
    public required float SFStars { get; init; }

    public required float BFSPredictedAcc { get; init; }
    public required float BFSPassRating { get; init; }
    public required float BFSAccRating { get; init; }
    public required float BFSTechRating { get; init; }
    public required float BFSStars { get; init; }
    public required float BSFPredictedAcc { get; init; }
    public required float BSFPassRating { get; init; }
    public required float BSFAccRating { get; init; }
    public required float BSFTechRating { get; init; }
    public required float BSFStars { get; init; }
}