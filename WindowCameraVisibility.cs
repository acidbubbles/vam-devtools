using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// VaM Utilities
/// By Acidbubbles
/// Allows hiding the window camera preview from being rendered
/// Source: https://github.com/acidbubbles/vam-utilities
/// </summary>
public class WindowCameraVisibility : MVRScript
{
    private List<GameObject> _planes;

    public override void Init()
    {
        try
        {
            if (containingAtom.type != "WindowCamera") throw new InvalidOperationException("This plugin can only be applied on a WindowCamera tom.");
            var cameraGroup = containingAtom.gameObject.transform.Find("reParentObject/object/rescaleObject/CameraGroup");
            if (cameraGroup == null) throw new NullReferenceException("Could not find the CameraGroup in the WindowCamera atom.");
            _planes = new List<GameObject>();
            foreach (Transform child in cameraGroup)
            {
                if (child == null) continue;
                if (child.name == "Plane")
                    _planes.Add(child.gameObject);
            }
        }
        catch (Exception exc)
        {
            _planes = null;
            SuperController.LogError(exc.ToString());
        }

        OnEnable();
    }

    public void OnEnable()
    {
        try
        {
            if (_planes == null) return;
            foreach (var plane in _planes)
                plane.SetActive(false);
        }
        catch (Exception exc)
        {
            SuperController.LogError(exc.ToString());
        }
    }

    public void OnDisable()
    {
        try
        {
            if (_planes == null) return;
            foreach (var plane in _planes)
                plane.SetActive(true);
        }
        catch (Exception exc)
        {
            SuperController.LogError(exc.ToString());
        }
    }

    public void OnDestroy()
    {
        OnDisable();
    }
}