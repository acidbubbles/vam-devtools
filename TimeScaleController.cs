using UnityEngine;

/// <summary>
/// VaM Utilities
/// By Acidbubbles
/// Allows triggers that affect time scale
/// Source: https://github.com/acidbubbles/vam-utilities
/// </summary>
public class TimeScaleController : MVRScript
{
    private TimeControl _timeControl;

    public override void Init()
    {
        _timeControl = GameObject.FindObjectOfType<TimeControl>();
        var timeScaleJSON = new JSONStorableFloat("Time Scale", 1f, val => _timeControl.currentScale = val, 0.1f, 1f);
        RegisterFloat(timeScaleJSON);
        CreateSlider(timeScaleJSON).label = "Time Scale (Set Only)";
        _timeControl.currentScale = timeScaleJSON.val;
    }
}