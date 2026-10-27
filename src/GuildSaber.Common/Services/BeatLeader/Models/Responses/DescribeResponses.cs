using System.Text.Json.Serialization;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;

namespace GuildSaber.Common.Services.BeatLeader.Models.Responses;

public record Hmd(
    [field: JsonRequired] BeatLeaderHmdId Value,
    [field: JsonRequired] string Key,
    [field: JsonRequired] string? Name
);

public record Controller(
    [field: JsonRequired] BeatLeaderControllerId Value,
    [field: JsonRequired] string Key,
    [field: JsonRequired] string? Name
);
