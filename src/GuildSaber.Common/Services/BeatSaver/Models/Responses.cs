using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;

namespace GuildSaber.Common.Services.BeatSaver.Models;

public class BeatMap
{
    public required BeatSaverKey Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required Uploader Uploader { get; set; }
    public required Metadata Metadata { get; set; }
    public required Stats Stats { get; set; }
    public required string Uploaded { get; set; }
    public required bool Automapper { get; set; }
    public required bool Ranked { get; set; }
    public required bool Qualified { get; set; }
    public required List<Version> Versions { get; set; }
}

public class Metadata
{
    public required float Bpm { get; set; }
    public required int Duration { get; set; }
    public required string SongName { get; set; }
    public required string SongSubName { get; set; }
    public required string LevelAuthorName { get; set; }
    public required string SongAuthorName { get; set; }
}

public class Stats
{
    public required int Plays { get; set; }
    public required int Downloads { get; set; }
    public required int Upvotes { get; set; }
    public required int Downvotes { get; set; }
    public required float Score { get; set; }
}

public class Uploader
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required string Avatar { get; set; }
}

public class Version
{
    public required SongHash Hash { get; set; }
    public required string State { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required int SageScore { get; set; }
    public required List<Diff> Diffs { get; set; }
    public required string DownloadURL { get; set; }
    public required string CoverURL { get; set; }
    public required string PreviewURL { get; set; }
}

public class Diff
{
    public required float Njs { get; set; }
    public required float Offset { get; set; }
    public required int Notes { get; set; }
    public required int Bombs { get; set; }
    public required int Obstacles { get; set; }
    public required float Nps { get; set; }
    public required float Length { get; set; }
    public required string Characteristic { get; set; }
    public required string Difficulty { get; set; }
    public required int Events { get; set; }
    public required bool Chroma { get; set; }
    public required bool Me { get; set; }
    public required bool Ne { get; set; }
    public required bool Cinema { get; set; }
    public required float Seconds { get; set; }
    public required ParitySummary ParitySummary { get; set; }
    public required int MaxScore { get; set; }
}

public class ParitySummary
{
    public required int Errors { get; set; }
    public required int Warns { get; set; }
    public required int Resets { get; set; }
}