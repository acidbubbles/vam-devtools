using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VaM Utilities
/// By AcidBubbles
/// Navigate gameobjects
/// Source: https://github.com/AcidBubbles/vam-utilities
/// </summary>
public class GameObjectExplorer : MVRScript
{
    private readonly Dictionary<string, Func<GameObject>> _wellknown = new Dictionary<string, Func<GameObject>>();
    
    private const float DisplayInfoFrequency = 0.5f;
    private const float DisplayScriptsFrequency = 2f;
    private const bool SideLeft = false;
    private const bool SideRight = true;

    private JSONStorableStringChooser _siblingsJSON;
    private UIDynamicButton _parentUI;
    private JSONStorableStringChooser _childrenJSON;
    private JSONStorableStringChooser _wellKnownJSON;
    private JSONStorableString _currentHierarchyJSON;

    private JSONStorableString _currentInfoJSON;
    private JSONStorableString _currentScriptsJSON;
    private GameObject _currentGameObject;
    private float _nextCurrentInfo = 0f;
    private float _nextCurrentScript = 0f;

    public override void Init()
    {
        try
        {
            var sc = SuperController.singleton;
            _wellknown.Add($"{containingAtom.name} (current atom)", () => containingAtom.gameObject);
            _wellknown.Add($"{nameof(UITransform)} (current atom)", () => containingAtom.UITransform.gameObject);
            _wellknown.Add(nameof(sc.navigationRig), () => sc.navigationRig.gameObject);
            _wellknown.Add(nameof(sc.centerCameraTarget), () => sc.centerCameraTarget.gameObject);
            _wellknown.Add(nameof(sc.worldUI), () => sc.worldUI.gameObject);
            _wellknown.Add(nameof(sc.mainMenuUI), () => sc.mainMenuUI.gameObject);
            _wellknown.Add(nameof(sc.mainHUD), () => sc.mainHUD.gameObject);
            if (sc.OVRRig != null) _wellknown.Add(nameof(sc.OVRRig), () => sc.OVRRig.gameObject);
            if (sc.ViveRig != null) _wellknown.Add(nameof(sc.ViveRig), () => sc.ViveRig.gameObject);
            if (sc.MonitorRig != null) _wellknown.Add(nameof(sc.MonitorRig), () => sc.MonitorRig.gameObject);
            if (sc.OVRCenterCamera != null)
                _wellknown.Add(nameof(sc.OVRCenterCamera), () => sc.OVRCenterCamera.gameObject);
            if (sc.ViveCenterCamera != null)
                _wellknown.Add(nameof(sc.ViveCenterCamera), () => sc.ViveCenterCamera.gameObject);
            if (sc.MonitorCenterCamera != null)
                _wellknown.Add(nameof(sc.MonitorCenterCamera), () => sc.MonitorCenterCamera.gameObject);
            foreach (var camera in Camera.allCameras)
            {
                var c = camera;
                _wellknown.Add($"Camera: {c.name}", () => c.gameObject);
            }

            // Left

            _wellKnownJSON = new JSONStorableStringChooser("WellKnown",
                _wellknown.Select(kvp => kvp.Key).OrderBy(k => k).ToList(), "", "Well Known");
            _wellKnownJSON.setCallbackFunction = (string val) => Select(_wellknown[val]());
            var wellKnownUI = CreateFilterablePopup(_wellKnownJSON, SideLeft);
            wellKnownUI.popupPanelHeight = 700f;

            _siblingsJSON = new JSONStorableStringChooser("Selected", new List<string>(), null, "Selected");
            _siblingsJSON.popupOpenCallback += SyncSiblings;
            _siblingsJSON.setCallbackFunction += SelectSibling;
            var siblingsUI = CreateFilterablePopup(_siblingsJSON, SideLeft);
            siblingsUI.popupPanelHeight = 900f;

            _parentUI = CreateButton("Select Parent", SideLeft);
            _parentUI.button.onClick.AddListener(() => Select(_currentGameObject?.transform.parent?.gameObject));

            _childrenJSON = new JSONStorableStringChooser("Children", new List<string>(), null, "Select Children");
            _childrenJSON.popupOpenCallback += SyncChildren;
            _childrenJSON.setCallbackFunction += SelectChild;
            var childrenUI = CreateFilterablePopup(_childrenJSON, SideLeft);
            childrenUI.popupPanelHeight = 700f;

            _currentHierarchyJSON = new JSONStorableString("CurrentHierarchy", "");
            CreateTextField(_currentHierarchyJSON).height = 728f;

            // Right

            _currentInfoJSON = new JSONStorableString("CurrentGameObject", "");
            CreateTextField(_currentInfoJSON, SideRight);

            _currentScriptsJSON = new JSONStorableString("CurrentScripts", "");
            CreateTextField(_currentScriptsJSON, SideRight).height = 980f;

            Select(containingAtom.gameObject);

        }
        catch (Exception exc)
        {
            enabled = false;
            SuperController.LogError($"{nameof(GameObjectExplorer)}: Failed to initialize: {exc}");
        }
    }
    
    private void SelectSibling(string val)
    {
        var index = _siblingsJSON.choices.IndexOf(val);
        var child = _currentGameObject.transform.parent.GetChild(index);
        Select(child.gameObject);
    }

    private void SelectChild(string val)
    {
        var index = _childrenJSON.choices.IndexOf(val);
        var child = _currentGameObject.transform.GetChild(index);
        Select(child.gameObject);
    }

    private void Select(GameObject go)
    {
        _wellKnownJSON.valNoCallback = "Select to navigate...";
        
        if (go == null) return;
        
        _currentGameObject = go;
        _parentUI.label = $"Select parent: {(go.transform.parent != null ? go.transform.parent.name : "(none)")}";
        SyncSiblings();
        _siblingsJSON.valNoCallback = _currentGameObject.name;
        SyncChildren();
        _childrenJSON.valNoCallback = "Select to navigate...";
        UpdateCurrentDisplay();
        UpdateCurrentScripts();
        
        var sb = new StringBuilder();
        sb.AppendLine($"<b>{go.name}</b> <i>(\u2536 {(go.transform.parent != null ? go.transform.parent.childCount : 0)})</i>");
        sb.AppendLine($"<i>... {go.transform.childCount} children</i>");
        var parent = go.transform;
        while ((parent = parent.parent) != null)
        {
            sb.Insert(0, $"{parent.name} <i>(\u2536 {(go.transform.parent != null ? parent.childCount : 0)})</i>\n");
        }
        _currentHierarchyJSON.val = sb.ToString();
    }
    
    private void SyncSiblings()
    {
        if (_currentGameObject.transform.parent == null)
        {
            _siblingsJSON.choices = new List<string>(1) {_currentGameObject.name};
            return;
        }

        var current = _currentGameObject.transform.parent;
        var siblings = new List<string>(current.childCount);
        for (var i = 0; i < current.childCount; i++)
            siblings.Add($"{current.GetChild(i).name} (\u260B {current.GetChild(i).childCount})");

        _siblingsJSON.choices = siblings;
    }
    
    private void SyncChildren()
    {
        var current = _currentGameObject.transform;
        var children = new List<string>(current.childCount);
        for (var i = 0; i < current.childCount; i++)
            children.Add($"{current.GetChild(i).name} (\u260B {current.GetChild(i).childCount})");

        _childrenJSON.choices = children;
    }

    public void Update()
    {
        // Optimization: do not refresh if the UI is not visible
        if(!_parentUI.isActiveAndEnabled) return;

        if (Time.realtimeSinceStartup >= _nextCurrentInfo)
            UpdateCurrentDisplay();
        if (Time.realtimeSinceStartup >= _nextCurrentScript)
            UpdateCurrentScripts();
    }

    public void DevToolsGameObjectExplorerShow(GameObject go)
    {
        Select(go);
    }

    private void UpdateCurrentDisplay()
    {
        if (_currentGameObject == null)
        {
            _currentInfoJSON.val = "<b>None selected</b>";
            return;
        }

        _currentInfoJSON.val = $@"<b>{_currentGameObject.name}</b>
Position: {_currentGameObject.transform.position}
Local:     {_currentGameObject.transform.localPosition}
Rotation: {_currentGameObject.transform.rotation.eulerAngles}
Local:     {_currentGameObject.transform.localRotation.eulerAngles}
";

        _nextCurrentInfo = Time.realtimeSinceStartup + DisplayInfoFrequency;
    }
    
    private void UpdateCurrentScripts()
    {
        if (_currentGameObject == null)
        {
            _currentInfoJSON.val = "<b>None selected</b>";
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("<b>Components</b>");
        
        foreach (var script in _currentGameObject.GetComponents<Behaviour>())
        {
            sb.AppendLine();
            sb.AppendLine(script.enabled
                ? $@"<b>{script.GetType()}</b>"
                : $@"<i><b>{script.GetType()}</b> (disabled)</i>");
            {
                var atom = script as Atom;
                if (atom != null)
                {
                    sb.AppendLine($"- {nameof(atom.on)}: {atom.on}");
                    sb.AppendLine($"- storables: {atom.GetStorableIDs().Count}");
                    continue;
                }
            }
            {
                var fc = script as FreeControllerV3;
                if (fc != null)
                {
                    sb.AppendLine($"- {nameof(fc.controlMode)}: {fc.controlMode}");
                    continue;
                }
            }
            {
                var mo = script as MaterialOptions;
                if (mo != null)
                {
                    sb.AppendLine($"- {nameof(mo.color1DisplayName)}: {mo.color1DisplayName}");
                    continue;
                }
            }
            {
                var ddi = script as DAZDynamicItem;
                if (ddi != null)
                {
                    sb.AppendLine($"- {nameof(ddi.active)}: {ddi.active}");
                    sb.AppendLine($"- {nameof(ddi.displayName)}: {ddi.displayName}");
                    continue;
                }
            }
            {
                var cam = script as Camera;
                if (cam != null)
                {
                    sb.AppendLine($"- {nameof(cam.cameraType)}: {cam.cameraType}");
                    sb.AppendLine($"- {nameof(cam.pixelWidth)}: {cam.pixelWidth}");
                    sb.AppendLine($"- {nameof(cam.pixelHeight)}: {cam.pixelHeight}");
                    continue;
                }
            }
            {
                var uiTab = script as UITab;
                if (uiTab != null)
                {
                    sb.AppendLine($"- {nameof(uiTab.name)}: {uiTab.name}");
                    continue;
                }
            }

            {
                var layout = script as LayoutElement;
                if (layout != null)
                {
                    sb.AppendLine($"- {nameof(layout.minWidth)}: {layout.minWidth}");
                    sb.AppendLine($"- {nameof(layout.preferredWidth)}: {layout.preferredWidth}");
                    sb.AppendLine($"- {nameof(layout.flexibleWidth)}: {layout.flexibleWidth}");
                    sb.AppendLine($"- {nameof(layout.minHeight)}: {layout.minHeight}");
                    sb.AppendLine($"- {nameof(layout.preferredHeight)}: {layout.preferredHeight}");
                    sb.AppendLine($"- {nameof(layout.flexibleHeight)}: {layout.flexibleHeight}");
                    continue;
                }
            }

            {
                var text = script as Text;
                if (text != null)
                {
                    sb.AppendLine($"- {nameof(text.text)}: {text.text}");
                    sb.AppendLine($"- {nameof(text.font)}: {text.font?.name}");
                    sb.AppendLine($"- {nameof(text.fontSize)}: {text.fontSize}");
                    sb.AppendLine($"- {nameof(text.alignment)}: {text.alignment}");
                    continue;
                }
            }

            {
                var image = script as Image;
                if (image != null)
                {
                    sb.AppendLine($"- {nameof(image.color)}: {image.color}");
                    sb.AppendLine($"- {nameof(image.mainTexture)}: {image.mainTexture?.name}");
                    sb.AppendLine($"- {nameof(image.mainTexture)}: {image.mainTexture?.name}");
                    continue;
                }
            }
        }
        _currentScriptsJSON.val = sb.ToString();

        _nextCurrentScript = Time.realtimeSinceStartup + DisplayScriptsFrequency;
    }
}