using System;
using GuildSaber.Mod.Configurations;
using UnityEngine;
using Zenject;

namespace GuildSaber.Mod.Core.Time;

public class TimeController : MonoBehaviour
{
    /// <summary>
    /// Template 1 : Hours
    /// Template 2 : Minutes
    /// Template 3 : Seconds
    /// </summary>
    public event Action<int, int, int>? EventChange;

    [Inject] private readonly PluginConfig _config = null!;
    
    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    private DateTime _currentTime = new DateTime(2026, 4, 17, 10, 7, 37);

    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    public void Awake()
    {
        _currentTime = DateTime.Now;

        ref TimeConfig l_TimeData = ref _config.PlayerCard.TimeData; 
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

    long LastSessionTime = 0;

    float LastTime = 0;

    float LastSaveTime = 0;

    float TimeToRemove = 0;

    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    public void Update()
    {
        float l_Time = UnityEngine.Time.realtimeSinceStartup + LastSessionTime - TimeToRemove;
        if (l_Time - LastTime < 1) return;

        int l_Hours = (int)(l_Time / 3600);
        int l_Minutes = (int)(l_Time / 60) - (l_Hours * 60);
        int l_Seconds = (int)(l_Time - (l_Hours * 3600) - (l_Minutes * 60));

        EventChange?.Invoke(l_Hours, l_Minutes, l_Seconds);

        LastTime = l_Time;

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (((float)l_Seconds / 20) == (int)(l_Seconds / 20))
        {
            _config.PlayerCard.TimeData.PlayDurationSec +=
                ((int)UnityEngine.Time.realtimeSinceStartup - (int)LastSaveTime - (int)TimeToRemove);
            LastSaveTime = (int)UnityEngine.Time.realtimeSinceStartup - TimeToRemove;
        }
    }

    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    public void Reset()
    {
        TimeToRemove = UnityEngine.Time.realtimeSinceStartup + LastSessionTime;
        LastTime = 0;
    }
}