using System;
using UnityEngine;

namespace GuildSaber.Mod.Features.Common.Timer;

public class Timer : MonoBehaviour
{
    public const int UpdateIntervalSec = 1;

    private float _addedTime;
    private float _lastTime;

    public void Reset() => (_addedTime, _lastTime) = (-Time.realtimeSinceStartup, 0f);

    public void Update()
    {
        var time = Time.realtimeSinceStartup + _addedTime;
        if (time - _lastTime < UpdateIntervalSec) return;

        var hours = (int)(time / 3600);
        var minutes = (int)(time / 60) - hours * 60;
        var seconds = (int)(time - hours * 3600 - minutes * 60);

        OnTimeUpdate?.Invoke(new TimeOnlyLite(hours, minutes, seconds));

        _lastTime = time;
    }

    public event Action<TimeOnlyLite>? OnTimeUpdate;

    public void SetAddedTime(TimeOnlyLite timeToAdd)
        => _addedTime = timeToAdd.Hours * 3600 + timeToAdd.Minutes * 60 + timeToAdd.Seconds;

    public readonly record struct TimeOnlyLite(int Hours, int Minutes, int Seconds);
}