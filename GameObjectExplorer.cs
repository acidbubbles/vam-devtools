using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// VaM Utilities
/// By AcidBubbles
/// Navigate gameobjects
/// Source: https://github.com/AcidBubbles/vam-utilities
/// </summary>
public class GameObjectExplorer : MVRScript
{
    private const float DisplayInfoFrequency = 1f;
    private const float DisplayScriptsFrequency = 10f;
    private const bool SideLeft = false;
    private const bool SideRight = true;

    private JSONStorableStringChooser _siblingsJSON;
    private JSONStorableStringChooser _childrenJSON;
    private UIDynamicButton _parentUI;

    private JSONStorableString _currentInfoJSON;
    private JSONStorableString _currentScriptsJSON;
    private GameObject _currentGameObject;
    private float _nextCurrentInfo = 0f;
    private float _nextCurrentScript = 0f;

    public override void Init()
    {
        _siblingsJSON = new JSONStorableStringChooser("Current", new List<string>(), null, "Select");
        _siblingsJSON.popupOpenCallback += SyncSiblings;
        _siblingsJSON.setCallbackFunction += SelectSibling;
        CreateFilterablePopup(_siblingsJSON, SideLeft);
        
        _parentUI = CreateButton("Select Parent", SideLeft);
        _parentUI.button.onClick.AddListener(() => Select(_currentGameObject?.transform.parent?.gameObject));
        
        _childrenJSON = new JSONStorableStringChooser("Children", new List<string>(), null, "Select Children");
        _childrenJSON.popupOpenCallback += SyncChildren;
        _childrenJSON.setCallbackFunction += SelectChild;
        CreateFilterablePopup(_childrenJSON, SideLeft);

        _currentInfoJSON = new JSONStorableString("Current GameObject", "");
        CreateTextField(_currentInfoJSON, SideRight);
        
        _currentScriptsJSON = new JSONStorableString("Current GameObject", "");
        CreateTextField(_currentScriptsJSON, SideRight).height = 980f;

        Select(containingAtom.gameObject);
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
        if (go == null) return;
        
        _currentGameObject = go;
        _parentUI.label = $"Select parent: {(go.transform.parent != null ? go.transform.parent.name : "(none)")}";
        SyncSiblings();
        _siblingsJSON.valNoCallback = _currentGameObject.name;
        SyncChildren();
        _childrenJSON.valNoCallback = "Select to navigate...";
        UpdateCurrentDisplay();
        UpdateCurrentScripts();
    }
    
    private void SyncSiblings()
    {
        if (_currentGameObject.transform.parent == null)
        {
            var single = new List<string>(1);
            single.Add(_currentGameObject.name);
            _siblingsJSON.choices = single;
            return;
        }

        var current = _currentGameObject.transform.parent;
        var siblings = new List<string>(current.childCount);
        for (var i = 0; i < current.childCount; i++)
            siblings.Add(current.GetChild(i).name);

        _siblingsJSON.choices = siblings;
    }
    
    private void SyncChildren()
    {
        var current = _currentGameObject.transform;
        var children = new List<string>(current.childCount);
        for (var i = 0; i < current.childCount; i++)
            children.Add(current.GetChild(i).name);

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
        foreach (var script in _currentGameObject.GetComponents<MonoBehaviour>())
        {
            sb.AppendLine($@"<b>{script.GetType()}</b>");
            {
                var atom = script as Atom;
                if (atom != null)
                {
                    sb.AppendLine($"On: {atom.on}");
                    sb.AppendLine($"Storables: {atom.GetStorableIDs().Count}");
                }
            }
            {
                var fc = script as FreeControllerV3;
                if (fc != null)
                {
                    sb.AppendLine($"Control Mode: {fc.controlMode}");
                }
            }
            sb.AppendLine();
        }
        _currentScriptsJSON.val = sb.ToString();

        _nextCurrentScript = Time.realtimeSinceStartup + DisplayScriptsFrequency;
    }
}