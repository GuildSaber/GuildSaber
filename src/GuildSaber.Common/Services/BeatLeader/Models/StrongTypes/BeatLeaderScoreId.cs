using System.Diagnostics.CodeAnalysis;
using CSharpFunctionalExtensions;

namespace GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;

public readonly record struct BeatLeaderScoreId : IComparable<BeatLeaderScoreId>
{
    private readonly uint _value;

    private BeatLeaderScoreId(uint value)
        => _value = value;

    public int CompareTo(BeatLeaderScoreId other) => _value.CompareTo(other._value);

    public static Result<BeatLeaderScoreId> TryCreate(uint? value)
        => value is null
            ? Failure<BeatLeaderScoreId>("BeatLeaderScoreId must not be null")
            : Success(new BeatLeaderScoreId(value.Value));

    public static Result<BeatLeaderScoreId> TryParse(string? value)
        => uint.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<BeatLeaderScoreId>("BeatLeaderScoreId must be a number.");

    public static implicit operator uint(BeatLeaderScoreId id)
        => id._value;

    [return: NotNullIfNotNull(nameof(value))]
    public static BeatLeaderScoreId? CreateUnsafe(uint? value)
        => value is null ? null : new BeatLeaderScoreId(value.Value);

    public override string ToString()
        => _value.ToString();
}