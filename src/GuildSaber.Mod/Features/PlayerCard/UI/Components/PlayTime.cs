using System;
using System.Runtime.CompilerServices;
using GuildSaber.Mod.Features.GuildSaber;
using UnityEngine;
using Zenject;

namespace GuildSaber.Mod.Features.PlayerCard.UI.Components;

public class PlayerCardPlayTime(GuildSaberConfig config)
    : IInitializable, IDisposable, ITickable
{
    public const int UpdateIntervalSec = 1;

    private float _addedTime;
    private float _lastTime;
    public TimeOnlyLite Current => TimeOnlyLite.FromSeconds(_lastTime);

    public event Action<TimeOnlyLite>? OnTimeUpdate;

    public readonly record struct TimeOnlyLite(int Hours, int Minutes, int Seconds)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeOnlyLite FromSeconds(float seconds)
        {
            var hours = (int)(seconds / 3600);
            var minutes = (int)(seconds / 60) - hours * 60;
            seconds = seconds - hours * 3600 - minutes * 60;

            return new TimeOnlyLite(hours, minutes, (int)seconds);
        }
    }

    public void Initialize()
    {
        var currentTime = DateTime.Now;
        var savedTime = config.PlayerCard.TimerConfig;

        // Used for the Current property;
        _lastTime = Time.realtimeSinceStartup;

        if (savedTime.Day == currentTime.Day)
        {
            // We subtract _lastTime (that is RealTimeSinceStartup) because we might reinitialize the timer causing drift.
            SetAddedTime(TimeOnlyLite.FromSeconds(savedTime.PlayDurationSec - _lastTime));
            return;
        }

        savedTime.Day = currentTime.Day;
        savedTime.PlayDurationSec = 0;
    }

    public void Tick()
    {
        var time = Time.realtimeSinceStartup + _addedTime;
        if (time - _lastTime < UpdateIntervalSec) return;

        OnTimeUpdate?.Invoke(TimeOnlyLite.FromSeconds(time));
        _lastTime = time;
    }

    public void Dispose()
        => config.PlayerCard.TimerConfig.PlayDurationSec = (int)_lastTime;

    public void Reset() => (_addedTime, _lastTime) = (-Time.realtimeSinceStartup, 0f);

    public void SetAddedTime(TimeOnlyLite timeToAdd)
        => _addedTime = timeToAdd.Hours * 3600 + timeToAdd.Minutes * 60 + timeToAdd.Seconds;
}