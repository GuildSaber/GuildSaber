namespace GuildSaber.Mod.Features.Common.Timer;

/// <remarks>
/// TimerConfig is a class because we need to be able to modify single properties without replacing the whole struct.
/// </remarks>
public class TimerConfig
{
    public long PlayDurationSec { get; set; } = 0;
    public int Day { get; set; } = -1;
}