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

    private float LastSaveTime;

    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    private long LastSessionTime;

    private float LastTime;

    private float TimeToRemove;

    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    public void Awake()
    {
        _currentTime = DateTime.Now;

        ref var l_TimeData = ref _config.PlayerCard.TimeData;
        if (l_TimeData.Day == -1)
        {
            l_TimeData.Day = _currentTime.Day;
            l_TimeData.PlayDurationSec = 0;
            return;
        }

        if (l_TimeData.Day == _currentTime.Day)
        {
            LastSessionTime = l_TimeData.PlayDurationSec;
        }
        else
        {
            l_TimeData.Day = _currentTime.Day;
            l_TimeData.PlayDurationSec = 0;
        }
    }

    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    public void Reset()
    {
        TimeToRemove = UnityEngine.Time.realtimeSinceStartup + LastSessionTime;
        LastTime = 0;
    }

    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    public void Update()
    {
        var l_Time = UnityEngine.Time.realtimeSinceStartup + LastSessionTime - TimeToRemove;
        if (l_Time - LastTime < 1) return;

        var l_Hours = (int)(l_Time / 3600);
        var l_Minutes = (int)(l_Time / 60) - l_Hours * 60;
        var l_Seconds = (int)(l_Time - l_Hours * 3600 - l_Minutes * 60);

        EventChange?.Invoke(l_Hours, l_Minutes, l_Seconds);

        LastTime = l_Time;

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if ((float)l_Seconds / 20 == l_Seconds / 20)
        {
            _config.PlayerCard.TimeData.PlayDurationSec +=
                (int)UnityEngine.Time.realtimeSinceStartup - (int)LastSaveTime - (int)TimeToRemove;
            LastSaveTime = (int)UnityEngine.Time.realtimeSinceStartup - TimeToRemove;
        }
    }

    /// <summary>
    /// Template 1 : Hours
    /// Template 2 : Minutes
    /// Template 3 : Seconds
    /// </summary>
    public event Action<int, int, int>? EventChange;
}