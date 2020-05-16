using UnityEngine;

/// <summary>
/// VaM Utilities
/// By AcidBubbles
/// Prints Unity logs to the VaM messages log
/// Source: https://github.com/AcidBubbles/vam-utilities
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
        var msg = $"{type} {condition} {stackTrace}";
        if (type == LogType.Error || type == LogType.Exception)
            SuperController.LogError(msg);
        else
            SuperController.LogMessage(msg);
    }
}