using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuildSaber.Mod.Core;

internal class FastAnimator : MonoBehaviour
{
    private static FastAnimator? s_Instance;

    ////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////

    protected List<FloatAnimData> m_FloatAnimations = new();
    protected List<FloatAnimData> m_FloatAnimationsToEnd = new();
    protected List<Vector3AnimData> m_Vector3Animations = new();

    ////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////

    internal static FastAnimator Instance
    {
        get
        {
            if (s_Instance != null)
                return s_Instance;

            s_Instance = new GameObject("GuildSaberFastAnimator").AddComponent<FastAnimator>();
            DontDestroyOnLoad(s_Instance);
            return s_Instance;
        }
        set => s_Instance = value;
    }

    ////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////

    public void Update()
    {
        for (var l_I = 0; l_I < m_FloatAnimations.Count; l_I++)
        {
            var l_Item = m_FloatAnimations[l_I];
            ParseFloatAnimData(l_Item, l_Item.AddDeltaTime, UnityEngine.Time.realtimeSinceStartup, l_I);
        }

        if (!m_FloatAnimationsToEnd.Any()) return;

        foreach (var l_Item in m_FloatAnimationsToEnd) m_FloatAnimations.Remove(l_Item);

        m_FloatAnimationsToEnd.Clear();
    }

    ////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////

    public static void Animate(List<FloatAnimKey> p_Keys, Action<float> p_Callback, Action? p_OnFinished = null)
    {
        if (p_Keys.Count < 2) throw new Exception("Not enough keys to run an animation, 2 required");

        var l_NewAnimation = new FloatAnimData(p_Keys, p_Callback, p_OnFinished);
        Instance.m_FloatAnimations.Add(l_NewAnimation);
    }

    ////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////

    private void ParseFloatAnimData(FloatAnimData p_FloatAnimData, float p_StartTime, float p_DeltaTime,
                                    int p_IndexInList)
    {
        var l_Time = p_DeltaTime - p_StartTime;

        if (l_Time > p_FloatAnimData.LastKey.Time)
        {
            p_FloatAnimData.OnFinished?.Invoke();
            p_FloatAnimData.Callback.Invoke(p_FloatAnimData.LastKey.Value);

            m_FloatAnimationsToEnd.Add(p_FloatAnimData);

            return;
        }

        ////////////////////////////////////////////////

        if (p_FloatAnimData.NextKey.Time == 0 || l_Time > p_FloatAnimData.NextKey.Time)
        {
            var l_KeysCount = p_FloatAnimData.Keys.Count;
            var l_Keys = p_FloatAnimData.Keys;

            ////////////////////////////////////////////////

            for (var l_I = 0; l_I < l_KeysCount; l_I++)
            {
                if (!(l_Keys[l_I].Time > l_Time))
                    continue;

                p_FloatAnimData.ActualKey = p_FloatAnimData.NextKey;
                p_FloatAnimData.NextKey = l_Keys[l_I];

                m_FloatAnimations[p_IndexInList] = p_FloatAnimData;
                break;
            }
        }

        var l_ActualKey = p_FloatAnimData.ActualKey;
        var l_NextKey = p_FloatAnimData.NextKey;
        var l_KeyIntervalTime = l_Time - l_ActualKey.Time;
        var l_KeyIntervalDuration = l_NextKey.Time - l_ActualKey.Time;

        var l_Value = CalculateFloatValue(l_ActualKey.Value, l_NextKey.Value, l_ActualKey.Exponent, l_KeyIntervalTime,
            l_KeyIntervalDuration);

        p_FloatAnimData.Callback.Invoke(
            l_Value
        );
    }

    private float CalculateFloatValue(float p_Start, float p_End, float p_Exponent, float p_Time, float p_Duration)
        => p_Start + (float)Math.Pow(p_Time / p_Duration, p_Exponent) * (p_End - p_Start);

    internal enum EAnimType
    {
        Float,
        Vector
    }

    internal struct FloatAnimKey
    {
        public float Value;
        public float Exponent;
        public float Time;

        public FloatAnimKey(float p_Value, float p_Time, float p_Exponent = 1)
        {
            Value = p_Value;
            Time = p_Time;
            Exponent = p_Exponent;
        }
    }

    internal struct FloatAnimData
    {
        public List<FloatAnimKey> Keys;
        public Action<float> Callback;
        public Action? OnFinished;
        public FloatAnimKey NextKey;
        public FloatAnimKey ActualKey;
        public FloatAnimKey LastKey;
        public float AddDeltaTime;

        public FloatAnimData(List<FloatAnimKey> p_Keys, Action<float> p_Callback, Action? p_OnFinished)
        {
            Keys = p_Keys;
            Callback = p_Callback;
            OnFinished = p_OnFinished;
            NextKey = new FloatAnimKey(p_Keys[0].Value, 0);
            ActualKey = NextKey;
            AddDeltaTime = UnityEngine.Time.realtimeSinceStartup;
            LastKey = p_Keys.Any() ? p_Keys.Last() : default;
        }
    }

    internal struct Vector3AnimKey
    {
        public Vector3 Start;
        public Vector3 End;
        public Vector3 Exponents;
        public float Duration;

        public Vector3AnimKey(Vector3 p_Start, Vector3 p_End, float p_Duration)
        {
            Start = p_Start;
            End = p_End;
            Duration = p_Duration;
            Exponents = new Vector3(1, 1, 1);
        }

        public Vector3AnimKey(Vector3 p_Start, Vector3 p_End, float p_Duration, Vector3 p_Exponents)
        {
            Start = p_Start;
            End = p_End;
            Duration = p_Duration;
            Exponents = p_Exponents;
        }
    }

    internal struct Vector3AnimData
    {
        internal List<Vector3AnimKey> Keys;
        internal Action<float> Callback;
        internal Action? OnFinished;

        public Vector3AnimData(List<Vector3AnimKey> p_Keys, Action<float> p_Callback, Action? p_OnFinished)
        {
            Keys = p_Keys;
            Callback = p_Callback;
            OnFinished = p_OnFinished;
        }
    }
}