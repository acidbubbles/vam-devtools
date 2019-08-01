using UnityEngine;

/// <summary>
/// VaM Utilities
/// By Acidbubbles
/// Prints Unity logs to the VaM messages log
/// Source: https://github.com/acidbubbles/vam-utilities
/// </summary>
public class ForwardUnityLogsToSceneLogs : MVRScript
{
    private static bool _registeredToUnityLogs;

    public override void Init()
    {
        OnEnable();
    }

    public void OnEnable()
    {
        if (_registeredToUnityLogs) return;
        Application.logMessageReceived += DebugLog;
        _registeredToUnityLogs = true;
    }

    public void OnDisable()
    {
        if (!_registeredToUnityLogs) return;
        Application.logMessageReceived -= DebugLog;
        _registeredToUnityLogs = false;
    }

    public static void DebugLog(string condition, string stackTrace, LogType type)
    {
        if (condition == null || condition.StartsWith("Log ") || string.IsNullOrEmpty(stackTrace)) return;
        SuperController.LogMessage(type + " " + condition + " " + stackTrace);
    }
}