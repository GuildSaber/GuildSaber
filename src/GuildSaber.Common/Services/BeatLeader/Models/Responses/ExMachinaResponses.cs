using System.Text.Json.Serialization;

namespace GuildSaber.Common.Services.BeatLeader.Models.Responses;

public record ExMachinaResponse(
    [property: JsonRequired] RatingResult FS,
    [property: JsonRequired] RatingResult SFS,
    [property: JsonRequired] RatingResult BFS,
    [property: JsonRequired] RatingResult BSF,
    [property: JsonRequired] RatingResult SS,
    [property: JsonRequired] RatingResult None
);

public record RatingResult(
    [property: JsonRequired, JsonPropertyName("predicted_acc")] float PredictedAcc,
    [property: JsonRequired, JsonPropertyName("acc_rating")] float AccRating,
    [property: JsonRequired, JsonPropertyName("lack_map_calculation")] LackMapCalculation LackMapCalculation,
    [property: JsonRequired] CurvePoint[] PointList,
    [property: JsonRequired] float Stars,
    [property: JsonRequired, JsonPropertyName("song_length")] float Length,
    [property: JsonRequired] float Njs
);

public record LackMapCalculation(
    [property: JsonRequired, JsonPropertyName("balanced_pass_diff")] float PassRating,
    [property: JsonRequired, JsonPropertyName("balanced_tech")] float TechRating,
    [property: JsonRequired, JsonPropertyName("multi_percentage")] float MultiPercentage,
    [property: JsonRequired, JsonPropertyName("linear_percentage")] float LinearPercentage,
    [property: JsonRequired, JsonPropertyName("slider_percentage")] float SliderPercentage,
    [property: JsonRequired] float HitDistance,
    [property: JsonRequired] float HardSwingRatio,
    [property: JsonRequired, JsonPropertyName("note_count")] int NoteCount,
    [property: JsonRequired, JsonPropertyName("note_span")] double NoteSpan,
    [property: JsonRequired] MapStatistics Statistics,
    [property: JsonPropertyName("avg_pattern_rating")] float PatternRating = 0,
    [property: JsonPropertyName("linear_rating")] float LinearRating = 0,
    [property: JsonPropertyName("low_note_nerf")] float LowNoteNerf = 0
);

public record MapStatistics(
    [property: JsonRequired] int Stacks,
    [property: JsonRequired] int Towers,
    [property: JsonRequired] int Sliders,
    [property: JsonRequired] int CurvedSliders,
    [property: JsonRequired] int Windows,
    [property: JsonRequired] int SlantedWindows,
    [property: JsonRequired] int Chains,
    [property: JsonRequired] int DodgeWalls,
    [property: JsonRequired] float DodgeWallDuration,
    [property: JsonRequired] int CrouchWalls,
    [property: JsonRequired] float CrouchWallDuration,
    [property: JsonRequired] int ParityErrors,
    [property: JsonRequired] int BombAvoidances,
    [property: JsonRequired] int LinearSwings,
    [property: JsonRequired] int Inlines
);

public record CurvePoint(
    [property: JsonRequired] double X,
    [property: JsonRequired] double Y
);