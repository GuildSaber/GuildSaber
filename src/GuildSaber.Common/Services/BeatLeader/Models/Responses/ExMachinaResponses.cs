using System.Text.Json.Serialization;

namespace GuildSaber.Common.Services.BeatLeader.Models.Responses;

public class ExMachinaResponse
{
    public required RatingResult FS { get; set; }
    public required RatingResult SFS { get; set; }
    public required RatingResult BFS { get; set; }
    public required RatingResult BSF { get; set; }
    public required RatingResult SS { get; set; }
    public required RatingResult None { get; set; }
}

public class RatingResult
{
    [JsonPropertyName("predicted_acc")]
    public float PredictedAcc { get; set; } = 0;

    [JsonPropertyName("acc_rating")]
    public float AccRating { get; set; } = 0;

    [JsonPropertyName("lack_map_calculation")]
    public LackMapCalculation LackMapCalculation { get; set; } = new();

    [JsonPropertyName("pointlist")]
    public List<CurvePoint> PointList { get; set; } = new();
}

public class LackMapCalculation
{
    [JsonPropertyName("avg_pattern_rating")]
    public float PatternRating { get; set; } = 0;

    [JsonPropertyName("balanced_pass_diff")]
    public float PassRating { get; set; } = 0;

    [JsonPropertyName("linear_rating")]
    public float LinearRating { get; set; } = 0;

    [JsonPropertyName("balanced_tech")]
    public float TechRating { get; set; } = 0;

    [JsonPropertyName("low_note_nerf")]
    public float LowNoteNerf { get; set; } = 0;
}

public class CurvePoint
{
    public double X { get; set; } = 0;
    public double Y { get; set; } = 0;
}