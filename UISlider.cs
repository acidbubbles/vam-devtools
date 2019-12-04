using System;
using System.Collections.Generic;
using System.Linq;
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
    private JSONStorableString _labelJSON;
    private JSONStorableFloat _valueJSON;
    private JSONStorableStringChooser _atomJSON;
    private JSONStorableStringChooser _storableJSON;
    private JSONStorableStringChooser _floatParamJSON;
    private Atom _atom;
    private Transform _sliderTransform;
    private JSONStorableFloat _targetFloatParam;
    private UIDynamicSlider _sliderUI;

    public override void Init()
    {
        _atom = GetAtom();

        try
        {
            _labelJSON = new JSONStorableString("Label", "My Slider", label =>
            {
                if (_sliderUI == null) return;
                _sliderUI.label = label;
            });
            RegisterString(_labelJSON);
            CreateTextInput(_labelJSON);

            _valueJSON = new JSONStorableFloat(_labelJSON.val, 0f, v => UpdateValue(v), 0f, 10f, false);
            RegisterFloat(_valueJSON);

            OnEnable();

            _atomJSON = new JSONStorableStringChooser("Target atom", SuperController.singleton.GetAtomUIDs(), "", "Target atom", uid => OnTargetAtomChanged(uid));
            _atomJSON.storeType = JSONStorableParam.StoreType.Physical;
            RegisterStringChooser(_atomJSON);
            var atomPopup = CreateScrollablePopup(_atomJSON, true);
            atomPopup.popupPanelHeight = 800f;
            SuperController.singleton.onAtomUIDsChangedHandlers += (uids) => OnAtomsChanged(uids);
            OnAtomsChanged(SuperController.singleton.GetAtomUIDs());

            var storables = new List<string>(new[] { "" });
            _storableJSON = new JSONStorableStringChooser("Target storable", storables, "", "Target storable", storable => OnTargetStorableChanged(storable));
            _storableJSON.storeType = JSONStorableParam.StoreType.Physical;
            RegisterStringChooser(_storableJSON);
            var storablePopup = CreateScrollablePopup(_storableJSON, true);
            storablePopup.popupPanelHeight = 800f;

            var floatParams = new List<string>(new[] { "" });
            _floatParamJSON = new JSONStorableStringChooser("Target param", storables, "", "Target param", floatParam => OnTargetFloatParamChanged(floatParam));
            _floatParamJSON.storeType = JSONStorableParam.StoreType.Physical;
            RegisterStringChooser(_floatParamJSON);
            var floatParamPopup = CreateScrollablePopup(_floatParamJSON, true);
            floatParamPopup.popupPanelHeight = 800f;
        }
        catch (Exception exc)
        {
            SuperController.LogError("UISlider Enable: " + exc);
        }
    }

    private Atom GetAtom()
    {
        // Note: Yeah, that's horrible, but containingAtom is null
        var container = gameObject?.transform?.parent?.parent?.parent?.parent?.parent?.gameObject;
        if (container == null)
            throw LogError(new NullReferenceException($"UISlider could not find the parent gameObject"));
        var atom = container.GetComponent<Atom>();
        if (atom == null)
            throw LogError(new NullReferenceException($"UISlider could not find the parent atom in {container.name}"));
        if (atom.type != "SimpleSign")
            throw LogError(new InvalidOperationException("UISlider can only be applied on SimpleSign"));
        return atom;
    }

    private void OnAtomsChanged(List<string> uids)
    {
        try
        {
            var atoms = new List<string>(uids);
            atoms.Insert(0, "");
            _atomJSON.choices = atoms;
        }
        catch (Exception exc)
        {
            SuperController.LogError("UISlider OnAtomsChanged: " + exc);
        }
    }

    private void OnTargetAtomChanged(string uid)
    {
        try
        {
            if (uid == "")
            {
                _storableJSON.choices = new List<string>(new[] { "" });
                _storableJSON.val = "";
                return;
            }

            var atom = SuperController.singleton.GetAtomByUid(uid);
            if (atom == null) throw LogError(new NullReferenceException($"Atom {uid} does not exist"));
            var storables = new List<string>(atom.GetStorableIDs());
            storables.Insert(0, "");
            _storableJSON.choices = storables;
            _storableJSON.val = storables.FirstOrDefault(s => s == _storableJSON.val) ?? "";
        }
        catch (Exception exc)
        {
            SuperController.LogError("UISlider OnTargetAtomChanged: " + exc);
        }
    }

    private void OnTargetStorableChanged(string sid)
    {
        try
        {
            if (sid == "")
            {
                _floatParamJSON.choices = new List<string>(new[] { "" });
                _floatParamJSON.val = "";
                return;
            }

            if (_atomJSON.val == "") return;

            var atom = SuperController.singleton.GetAtomByUid(_atomJSON.val);
            if (atom == null) throw LogError(new NullReferenceException($"Atom {_atomJSON.val} does not exist"));
            var storable = atom.GetStorableByID(sid);
            if (storable == null) throw LogError(new NullReferenceException($"Storable {sid} of atom {_atomJSON.val} does not exist"));
            var floatParams = new List<string>(storable.GetFloatParamNames());
            floatParams.Insert(0, "");
            _floatParamJSON.choices = floatParams;
            _floatParamJSON.val = floatParamNames.FirstOrDefault(s => s == _floatParamJSON.val) ?? "";
        }
        catch (Exception exc)
        {
            SuperController.LogError("UISlider OnTargetAtomChanged: " + exc);
        }
    }

    private void OnTargetFloatParamChanged(string floatParamName)
    {

        if (_atomJSON.val == "") return;
        if (_storableJSON.val == "") return;
        if (floatParamName == "") return;


        var atom = SuperController.singleton.GetAtomByUid(_atomJSON.val);
        if (atom == null) throw LogError(new NullReferenceException($"Atom {_atomJSON.val} does not exist"));
        var storable = atom.GetStorableByID(_storableJSON.val);
        if (storable == null) throw LogError(new NullReferenceException($"Storable {_storableJSON.val} of atom {_atomJSON.val} does not exist"));
        _targetFloatParam = storable.GetFloatJSONParam(floatParamName);
        if (_targetFloatParam == null) throw LogError(new NullReferenceException($"Float JSON param {floatParamName} of storable {_storableJSON.val} of atom {_atomJSON.val} does not exist"));

        _valueJSON.constrained = false;
        _valueJSON.defaultVal = _targetFloatParam.defaultVal;
        _valueJSON.val = _targetFloatParam.val;
        _valueJSON.min = _targetFloatParam.min;
        _valueJSON.max = _targetFloatParam.max;
        _valueJSON.constrained = _targetFloatParam.constrained;
        _sliderUI.Configure(_labelJSON.val, _valueJSON.min, _valueJSON.max, _valueJSON.defaultVal, _valueJSON.constrained, "F2", true, !_valueJSON.constrained);
    }

    private void UpdateValue(float v)
    {
        if (_targetFloatParam == null) return;
        _targetFloatParam.val = v;
    }

    public void OnEnable()
    {
        if (_sliderTransform != null || _atom == null) return;

        try
        {
            CreateUISliderInCanvas();
        }
        catch (Exception exc)
        {
            SuperController.LogError("UISlider Enable: " + exc);
        }
    }

    private void CreateUISliderInCanvas()
    {
        var canvas = _atom.GetComponentInChildren<Canvas>();
        if (canvas == null) throw new NullReferenceException("Could not find a canvas to attach to");

        _sliderTransform = Instantiate(manager.configurableSliderPrefab.transform);
        if (_sliderTransform == null) throw new NullReferenceException("Could not instantiate configurableSliderPrefab");
        _sliderTransform.SetParent(canvas.transform, false);
        _sliderTransform.gameObject.SetActive(true);

        _sliderUI = _sliderTransform.GetComponent<UIDynamicSlider>();
        if (_sliderUI == null) throw new NullReferenceException("Could not find a UIDynamicSlider component");
        _sliderUI.Configure(_labelJSON.val, _valueJSON.min, _valueJSON.max, _valueJSON.val, _valueJSON.constrained, "F2", true, !_valueJSON.constrained);
        _valueJSON.slider = _sliderUI.slider;

        _sliderTransform.Translate(Vector3.down * 0.3f, Space.Self);
        _sliderTransform.Translate(Vector3.right * 0.35f, Space.Self);
    }

    public void OnDisable()
    {
        if (_sliderTransform == null) return;

        try
        {
            _valueJSON.slider = null;
            Destroy(_sliderTransform.gameObject);
            _sliderTransform = null;
            _sliderUI = null;
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

    private void CreateTextInput(JSONStorableString jss, bool rightSide = false)
    {
        var textfield = CreateTextField(jss, rightSide);
        textfield.height = 1f;
        textfield.backgroundColor = Color.white;
        var input = textfield.gameObject.AddComponent<InputField>();
        input.textComponent = textfield.UItext;
        jss.inputField = input;
    }

    private Exception LogError(Exception exception)
    {
        SuperController.LogError($"UISlider: {exception}");
        return exception;
    }
}