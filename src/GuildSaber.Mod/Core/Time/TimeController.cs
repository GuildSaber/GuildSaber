using System;
using GuildSaber.Mod.Configurations;
using UnityEngine;
using Zenject;

namespace GuildSaber.Mod.Core.Time;

public class TimeController : MonoBehaviour
{
    [Inject] private readonly PluginConfig _config = null!;

    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    private DateTime _currentTime = new(2026, 4, 17, 10, 7, 37);

    private float _lastSaveTime;

    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    private long _lastSessionTime;

    private float _lastTime;

    private float _timeToRemove;

    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    public void Awake()
    {
        _currentTime = DateTime.Now;

        ref var timeData = ref _config.PlayerCard.TimeData;
        if (timeData.Day == -1)
        {
            timeData.Day = _currentTime.Day;
            timeData.PlayDurationSec = 0;
            return;
        }

        if (timeData.Day == _currentTime.Day)
        {
            _lastSessionTime = timeData.PlayDurationSec;
        }
        else
        {
            timeData.Day = _currentTime.Day;
            timeData.PlayDurationSec = 0;
        }

        //GameObject.DontDestroyOnLoad(this);
    }

    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    public void Reset()
    {
        _timeToRemove = UnityEngine.Time.realtimeSinceStartup + _lastSessionTime;
        _lastTime = 0;
    }

    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    public void Update()
    {
        var time = UnityEngine.Time.realtimeSinceStartup + _lastSessionTime - _timeToRemove;
        if (time - _lastTime < 1) return;

        var hours = (int)(time / 3600);
        var minutes = (int)(time / 60) - hours * 60;
        var seconds = (int)(time - hours * 3600 - minutes * 60);

        EventChange?.Invoke(hours, minutes, seconds);

        _lastTime = time;

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if ((float)seconds / 20 == seconds / 20)
        {
            _config.PlayerCard.TimeData.PlayDurationSec +=
                (int)UnityEngine.Time.realtimeSinceStartup - (int)_lastSaveTime - (int)_timeToRemove;
            _lastSaveTime = (int)UnityEngine.Time.realtimeSinceStartup - _timeToRemove;
        }
    }

    /// <summary>
    /// Template 1 : Hours
    /// Template 2 : Minutes
    /// Template 3 : Seconds
    /// </summary>
    public event Action<int, int, int>? EventChange;
}