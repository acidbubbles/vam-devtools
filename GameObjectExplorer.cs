using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// VaM Utilities
/// By AcidBubbles
/// Navigate gameobjects
/// Source: https://github.com/AcidBubbles/vam-utilities
/// </summary>
public class GameObjectExplorer : MVRScript
{
    private const float CurrentUpdateFrequency = 1f;
    private const bool SideLeft = false;
    private const bool SideRight = true;

    private JSONStorableString _currentJSON;

    private UIDynamicButton _parentUI;
    
    private JSONStorableStringChooser _childrenJSON;

    private GameObject _currentGameObject;
    private float _nextCurrentUpdate = 0f;

    public override void Init()
    {
        _parentUI = CreateButton("Parent", SideLeft);
        _parentUI.button.onClick.AddListener(() => Select(_currentGameObject?.transform?.gameObject));
        
        _childrenJSON = new JSONStorableStringChooser("Children", new List<string>(), null, "Children");
        _childrenJSON.popupOpenCallback += SyncChildren;
        _childrenJSON.setCallbackFunction += SelectChild;
        CreateFilterablePopup(_childrenJSON, SideLeft);

        _currentJSON = new JSONStorableString("Current GameObject", "");
        CreateTextField(_currentJSON, SideRight);

        Select(containingAtom.gameObject);
    }

    private void SelectChild(string val)
    {
        var index = _childrenJSON.choices.IndexOf(val);
        var child = _currentGameObject.transform.GetChild(index);
        Select(child.gameObject);
    }

    private void Select(GameObject go)
    {
        _currentGameObject = go;
        _parentUI.label = $"Parent: {go.transform.parent?.name ?? "(none)"}";
        SyncChildren();
        _childrenJSON.valNoCallback = "Children...";
        UpdateCurrentDisplay();
    }
    
    private void SyncChildren()
    {
        var children = new List<string>(_currentGameObject.transform.childCount);
        for (var i = 0; i < _currentGameObject.transform.childCount; i++)
            children.Add(_currentGameObject.transform.GetChild(i).name);

        _childrenJSON.choices = children;
    }

    public void Update()
    {
        if (Time.realtimeSinceStartup < _nextCurrentUpdate) return;
        UpdateCurrentDisplay();
    }

    private void UpdateCurrentDisplay()
    {
        if (_currentGameObject == null)
        {
            _currentJSON.val = "<b>None selected</b>";
            return;
        }

        _currentJSON.val = $@"<b>{_currentGameObject.name}</b>
Position: {_currentGameObject.transform.position}
Local:    {_currentGameObject.transform.localPosition}
Rotation: {_currentGameObject.transform.rotation.eulerAngles}
Local:    {_currentGameObject.transform.localRotation.eulerAngles}
";

        _nextCurrentUpdate = Time.realtimeSinceStartup + CurrentUpdateFrequency;
    }
}