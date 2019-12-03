using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VaM Utilities
/// By Acidbubbles
/// Control for sliding between values
/// Source: https://github.com/acidbubbles/vam-utilities
/// </summary>
public class UISlider : MVRScript
{
    private Atom _atom;
    private JSONStorableFloat _valueJSON;
    private Transform _slider;

    public override void Init()
    {
        // Note: Yeah, that's horrible, but containingAtom is null
        var container = gameObject?.transform?.parent?.parent?.parent?.parent?.parent?.gameObject;
        if (container == null)
        {
            SuperController.LogError($"UISlider could not find the parent gameObject");
            return;
        }
        var atom = container.GetComponent<Atom>();
        if (atom == null)
        {
            SuperController.LogError($"UISlider could not find the parent atom in {container.name}");
            return;
        }
        if (atom.type != "SimpleSign")
        {
            SuperController.LogError("UISlider can only be applied on SimpleSign");
            return;
        }
        _atom = atom;

        _valueJSON = new JSONStorableFloat("Scale", 0f, 0f, 10f, false);
        RegisterFloat(_valueJSON);

        OnEnable();
    }

    private void InitControls()
    {
    }

    public void OnEnable()
    {
        if (_slider != null || _atom == null) return;

        try
        {
            var canvas = _atom.GetComponentInChildren<Canvas>();
            if (canvas == null) throw new NullReferenceException("Could not find a canvas to attach to");

            _slider = Instantiate(manager.configurableSliderPrefab.transform);
            if (_slider == null) throw new NullReferenceException("Could not instantiate configurableSliderPrefab");
            _slider.SetParent(canvas.transform, false);
            _slider.gameObject.SetActive(true);

            var ui = _slider.GetComponent<UIDynamicSlider>();
            if (ui == null) throw new NullReferenceException("Could not find a UIDynamicSlider component");
            ui.Configure(_valueJSON.name, _valueJSON.min, _valueJSON.max, _valueJSON.val, _valueJSON.constrained, "F2", true, !_valueJSON.constrained);
            _valueJSON.slider = ui.slider;
        }
        catch (Exception exc)
        {
            SuperController.LogError("UISlider Enable: " + exc);
        }
    }

    public void OnDisable()
    {
        if (_slider == null) return;

        try
        {
            _valueJSON.slider = null;
            Destroy(_slider.gameObject);
        }
        catch (Exception exc)
        {
            SuperController.LogError("UISlider Disable: " + exc);
        }
    }

    public void OnDestroy()
    {
        OnDisable();
    }
}