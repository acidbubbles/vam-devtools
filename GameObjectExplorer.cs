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
            _wellknown.Add("SceneAtoms (root)", () => SuperController.singleton.transform.parent.gameObject);
            if (UITransform != null) _wellknown.Add($"{nameof(UITransform)} (current plugin)", () => UITransform.gameObject);
            if (containingAtom != null) _wellknown.Add($"{containingAtom.name} (current atom)", () => containingAtom.gameObject);
            if (containingAtom != null) _wellknown.Add($"{nameof(UITransform)} (current atom)", () => containingAtom.UITransform.gameObject);
            _wellknown.Add($"{nameof(GameObjectExplorer)} (current plugin)", () => gameObject);
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

            _wellKnownJSON = new JSONStorableStringChooser("WellKnown", _wellknown.Select(kvp => kvp.Key).OrderBy(k => k).ToList(), "", "Well Known");
            _wellKnownJSON.setCallbackFunction = (string val) => Select(_wellknown[val]());
            var wellKnownUI = CreateFilterablePopup(_wellKnownJSON, SideLeft);
            wellKnownUI.popupPanelHeight = 700f;

            _siblingsJSON = new JSONStorableStringChooser("Selected", new List<string>(), null, "Selected");
            _siblingsJSON.popupOpenCallback += SyncSiblings;
            _siblingsJSON.setCallbackFunction += SelectSibling;
            var siblingsUI = CreateFilterablePopup(_siblingsJSON, SideLeft);
            siblingsUI.popupPanelHeight = 900f;

            _parentUI = CreateButton("Select Parent", SideLeft);
            _parentUI.button.onClick.AddListener(() => Select(_currentGameObject == null ? null : _currentGameObject.transform.parent?.gameObject));

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

            // Keybindings

            SuperController.singleton.BroadcastMessage("OnActionsProviderAvailable", this, SendMessageOptions.DontRequireReceiver);
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
        
        if (go == null) {
            Select(SuperController.singleton.transform.parent.gameObject);
            return;
        }
        
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
        if (_currentGameObject == null)
        {
            _siblingsJSON.choices = new List<string>();
            return;
        }

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
        if(_currentGameObject == null)
        {
            _childrenJSON.choices = new List<string>();
            return;
        }

        var current = _currentGameObject.transform;
        var children = new List<string>(current.childCount);
        for (var i = 0; i < current.childCount; i++)
            children.Add($"{current.GetChild(i).name} (\u260B {current.GetChild(i).childCount})");

        _childrenJSON.choices = children;
    }

    public void Update()
    {
        // Optimization: do not refresh if the UI is not visible
        if (ReferenceEquals(_parentUI, null)) return;
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
Active: {(_currentGameObject.activeInHierarchy ? "Yes" : (_currentGameObject.activeSelf ? "No (Parent)" : "No (Self)"))}
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
        foreach (var component in _currentGameObject.GetComponents<Component>())
        {
            if(component is Transform && !(component is RectTransform))
                continue;

            if (sb.Length > 0)
                sb.AppendLine();

            {
                var script = component as Behaviour;
                sb.AppendLine(script == null || script.enabled
                    ? $@"<b>{component.GetType()}</b>"
                    : $@"<i><b>{component.GetType()}</b> (disabled)</i>");
            }

            {
                var atom = component as Atom;
                if (atom != null)
                {
                    sb.AppendLine($"- {nameof(atom.on)}: {atom.on}");
                    sb.AppendLine($"- storables: {atom.GetStorableIDs().Count}");
                    continue;
                }
            }

            {
                var rectTransform = component as RectTransform;
                if(rectTransform != null)
                {
                    sb.AppendLine($"- {nameof(rectTransform.anchorMin)}: {rectTransform.anchorMin}");
                    sb.AppendLine($"- {nameof(rectTransform.anchorMax)}: {rectTransform.anchorMax}");
                    sb.AppendLine($"- {nameof(rectTransform.anchoredPosition)}: {rectTransform.anchoredPosition}");
                    sb.AppendLine($"- {nameof(rectTransform.offsetMin)}: {rectTransform.offsetMin}");
                    sb.AppendLine($"- {nameof(rectTransform.offsetMax)}: {rectTransform.offsetMax}");
                    sb.AppendLine($"- {nameof(rectTransform.pivot)}: {rectTransform.pivot}");
                    sb.AppendLine($"- {nameof(rectTransform.sizeDelta)}: {rectTransform.sizeDelta}");
                    sb.AppendLine($"- {nameof(rectTransform.rect)}: {rectTransform.rect}");
                    continue;
                }
            }

            {
                var canvas = component as Canvas;
                if (canvas != null)
                {
                    sb.AppendLine($"- {nameof(canvas.worldCamera)}: {canvas.worldCamera}");
                    sb.AppendLine($"- {nameof(canvas.renderMode)}: {canvas.renderMode}");
                    continue;
                }
            }

            {
                var fc = component as FreeControllerV3;
                if (fc != null)
                {
                    sb.AppendLine($"- {nameof(fc.controlMode)}: {fc.controlMode}");
                    continue;
                }
            }

            {
                var mo = component as MaterialOptions;
                if (mo != null)
                {
                    sb.AppendLine($"- {nameof(mo.color1DisplayName)}: {mo.color1DisplayName}");
                    continue;
                }
            }

            {
                var ddi = component as DAZDynamicItem;
                if (ddi != null)
                {
                    sb.AppendLine($"- {nameof(ddi.active)}: {ddi.active}");
                    sb.AppendLine($"- {nameof(ddi.displayName)}: {ddi.displayName}");
                    continue;
                }
            }

            {
                var cam = component as Camera;
                if (cam != null)
                {
                    sb.AppendLine($"- {nameof(cam.cameraType)}: {cam.cameraType}");
                    sb.AppendLine($"- {nameof(cam.pixelWidth)}: {cam.pixelWidth}");
                    sb.AppendLine($"- {nameof(cam.pixelHeight)}: {cam.pixelHeight}");
                    continue;
                }
            }

            {
                var uiTab = component as UITab;
                if (uiTab != null)
                {
                    sb.AppendLine($"- {nameof(uiTab.name)}: {uiTab.name}");
                    continue;
                }
            }

            {
                var layout = component as ILayoutElement;
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
                var text = component as Text;
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
                var fitter = component as ContentSizeFitter;
                if (fitter != null)
                {
                    sb.AppendLine($"- {nameof(fitter.horizontalFit)}: {fitter.horizontalFit}");
                    sb.AppendLine($"- {nameof(fitter.verticalFit)}: {fitter.verticalFit}");
                    continue;
                }
            }

            {
                var image = component as Image;
                if (image != null)
                {
                    sb.AppendLine($"- {nameof(image.color)}: {image.color}");
                    sb.AppendLine($"- {nameof(image.mainTexture)}: {image.mainTexture?.name}");
                    sb.AppendLine($"- {nameof(image.mainTexture)}: {image.mainTexture?.name}");
                    continue;
                }
            }

            {
                var ovrManager = component as OVRManager;
                if (ovrManager != null)
                {
                    sb.AppendLine($"- {nameof(ovrManager.usePositionTracking)}: {ovrManager.usePositionTracking}");
                    sb.AppendLine($"- {nameof(ovrManager.useRotationTracking)}: {ovrManager.useRotationTracking}");
                    sb.AppendLine($"- {nameof(ovrManager.trackingOriginType)}: {ovrManager.trackingOriginType}");
                    sb.AppendLine($"- {nameof(ovrManager.useIPDInPositionTracking)}: {ovrManager.useIPDInPositionTracking}");
                    continue;
                }
            }

            {
                var ovrRig = component as OVRCameraRig;
                if (ovrRig != null)
                {
                    sb.AppendLine($"- {nameof(ovrRig.usePerEyeCameras)}: {ovrRig.usePerEyeCameras}");
                    sb.AppendLine($"- {nameof(ovrRig.useFixedUpdateForTracking)}: {ovrRig.useFixedUpdateForTracking}");
                    continue;
                }
            }


            {
                var rigidbodyAttributes = component as RigidbodyAttributes;
                if (rigidbodyAttributes != null)
                {
                    sb.AppendLine($"- {nameof(rigidbodyAttributes.useOverrideIterations)}: {rigidbodyAttributes.useOverrideIterations}");
                    sb.AppendLine($"- {nameof(rigidbodyAttributes.useOverrideTensor)}: {rigidbodyAttributes.useOverrideTensor}");
                    continue;
                }
            }

            {
                var forceReceiver = component as ForceReceiver;
                if (forceReceiver != null)
                {
                    sb.AppendLine($"- {nameof(forceReceiver.containingAtom)}: {forceReceiver.containingAtom}");
                    continue;
                }
            }

            {
                var joint = component as ConfigurableJoint;
                if (joint != null)
                {
                    sb.AppendLine($"- {nameof(joint.connectedBody)}: {joint.connectedBody}");
                    sb.AppendLine($"- {nameof(joint.autoConfigureConnectedAnchor)}: {joint.autoConfigureConnectedAnchor}");
                    sb.AppendLine($"- {nameof(joint.rotationDriveMode)}: {joint.rotationDriveMode}");
                    sb.AppendLine($"- {nameof(joint.projectionMode)}: {joint.projectionMode}");
                    sb.AppendLine($"- {nameof(joint.angularXDrive)}: {joint.angularXDrive}");
                    sb.AppendLine($"- {nameof(joint.angularXLimitSpring)}: {joint.angularXLimitSpring}");
                    sb.AppendLine($"- {nameof(joint.angularXMotion)}: {joint.angularXMotion}");
                    sb.AppendLine($"- {nameof(joint.lowAngularXLimit)}: {joint.lowAngularXLimit}");
                    sb.AppendLine($"- {nameof(joint.highAngularXLimit)}: {joint.highAngularXLimit}");
                    sb.AppendLine($"- {nameof(joint.xDrive)}: {joint.xDrive}");
                    sb.AppendLine($"- {nameof(joint.xMotion)}: {joint.xMotion}");
                    sb.AppendLine($"- {nameof(joint.angularYMotion)}: {joint.angularYMotion}");
                    sb.AppendLine($"- {nameof(joint.angularYLimit)}: {joint.angularYLimit}");
                    sb.AppendLine($"- {nameof(joint.yDrive)}: {joint.yDrive}");
                    sb.AppendLine($"- {nameof(joint.yMotion)}: {joint.yMotion}");
                    sb.AppendLine($"- {nameof(joint.angularZMotion)}: {joint.angularZMotion}");
                    sb.AppendLine($"- {nameof(joint.angularZLimit)}: {joint.angularZLimit}");
                    sb.AppendLine($"- {nameof(joint.zDrive)}: {joint.zDrive}");
                    sb.AppendLine($"- {nameof(joint.zMotion)}: {joint.zMotion}");
                    sb.AppendLine($"- {nameof(joint.angularYZDrive)}: {joint.angularYZDrive}");
                    sb.AppendLine($"- {nameof(joint.angularYZLimitSpring)}: {joint.angularYZLimitSpring}");
                    sb.AppendLine($"- {nameof(joint.massScale)}: {joint.massScale}");
                    sb.AppendLine($"- {nameof(joint.connectedMassScale)}: {joint.connectedMassScale}");
                    sb.AppendLine($"- {nameof(joint.targetPosition)}: {joint.targetPosition}");
                    sb.AppendLine($"- {nameof(joint.targetRotation)}: {joint.targetRotation}");
                    sb.AppendLine($"- {nameof(joint.targetVelocity)}: {joint.targetVelocity}");
                    sb.AppendLine($"- {nameof(joint.targetAngularVelocity)}: {joint.targetAngularVelocity}");
                    sb.AppendLine($"- {nameof(joint.anchor)}: {joint.anchor}");
                    sb.AppendLine($"- {nameof(joint.connectedAnchor)}: {joint.connectedAnchor}");
                    sb.AppendLine($"- {nameof(joint.slerpDrive)}: {joint.slerpDrive}");
                    sb.AppendLine($"- {nameof(joint.enableCollision)}: {joint.enableCollision}");
                    sb.AppendLine($"- {nameof(joint.breakForce)}: {joint.breakForce}");
                    sb.AppendLine($"- {nameof(joint.breakTorque)}: {joint.breakTorque}");
                    sb.AppendLine($"- {nameof(joint.swapBodies)}: {joint.swapBodies}");
                }
            }

            {
                var jointReconnector = component as ConfigurableJointReconnector;
                if (jointReconnector != null)
                {
                    sb.AppendLine($"- {nameof(jointReconnector.controlRelativePositionAndRotation)}: {jointReconnector.controlRelativePositionAndRotation}");
                    sb.AppendLine($"- {nameof(jointReconnector.rigidBodyToConnect)}: {jointReconnector.rigidBodyToConnect}");
                    continue;
                }
            }

            {
                var rigidbody = component as Rigidbody;
                if (rigidbody != null)
                {
                    sb.AppendLine($"- {nameof(rigidbody.isKinematic)}: {rigidbody.isKinematic}");
                    sb.AppendLine($"- {nameof(rigidbody.detectCollisions)}: {rigidbody.detectCollisions}");
                    sb.AppendLine($"- {nameof(rigidbody.mass)}: {rigidbody.mass}");
                    sb.AppendLine($"- {nameof(rigidbody.useGravity)}: {rigidbody.useGravity}");
                    sb.AppendLine($"- {nameof(rigidbody.interpolation)}: {rigidbody.interpolation}");
                    continue;
                }
            }

            {
                var collider = component as Collider;
                if (collider != null)
                {
                    sb.AppendLine($"- {nameof(collider.attachedRigidbody)}: {collider.attachedRigidbody}");
                    sb.AppendLine($"- {nameof(collider.isTrigger)}: {collider.isTrigger}");
                    continue;
                }
            }

            {
                var filter = component as MeshFilter;
                if (filter != null)
                {
                    sb.AppendLine($"- {nameof(filter.mesh)}: {(filter.mesh != null ? filter.mesh.name : "none")}");
                    sb.AppendLine($"- {nameof(filter.sharedMesh)}: {(filter.sharedMesh != null ? filter.sharedMesh.name : "none")}");
                    continue;
                }
            }

            {
                var renderer = component as Renderer;
                if (renderer != null)
                {
                    sb.AppendLine($"- {nameof(renderer.material)}: {(renderer.material != null ? renderer.material.name : "none")}");
                    sb.AppendLine($"  - {nameof(Material.shader)}: {(renderer.material != null && renderer.material.shader != null ? renderer.material.shader.name : "none")}");
                    continue;
                }
            }

            {
                var renderer = component as MeshRenderer;
                if (renderer != null)
                {
                    sb.AppendLine($"- {nameof(renderer.subMeshStartIndex)}: {renderer.subMeshStartIndex}");
                    continue;
                }
            }

            {
                var bone = component as DAZBone;
                if (bone != null)
                {
                    sb.AppendLine($"- {nameof(bone.baseJointRotation)}: {bone.baseJointRotation}");
                    sb.AppendLine($"- {nameof(bone.appearanceLocked)}: {bone.appearanceLocked}");
                    sb.AppendLine($"- {nameof(bone.currentAngles)}: {bone.currentAngles}");
                    sb.AppendLine($"- {nameof(bone.disableMorph)}: {bone.disableMorph}");
                    sb.AppendLine($"- {nameof(bone.exclude)}: {bone.exclude}");
                    sb.AppendLine($"- {nameof(bone.importWorldOrientation)}: {bone.importWorldOrientation}");
                    sb.AppendLine($"- {nameof(bone.importWorldPosition)}: {bone.importWorldPosition}");
                    sb.AppendLine($"- {nameof(bone.inverseStartingLocalRotation)}: {bone.inverseStartingLocalRotation}");
                    sb.AppendLine($"- {nameof(bone.isRoot)}: {bone.isRoot}");
                    sb.AppendLine($"- {nameof(bone.jointDriveTargetRotationOrder)}: {bone.jointDriveTargetRotationOrder}");
                    sb.AppendLine($"- {nameof(bone.jointRotationDisabled)}: {bone.jointRotationDisabled}");
                    sb.AppendLine($"- {nameof(bone.maleWorldOrientation)}: {bone.maleWorldOrientation}");
                    sb.AppendLine($"- {nameof(bone.rotationOrder)}: {bone.rotationOrder}");
                    sb.AppendLine($"- {nameof(bone.morphedWorldOrientation)}: {bone.morphedWorldOrientation}");
                    sb.AppendLine($"- {nameof(bone.worldOrientation)}: {bone.worldOrientation}");
                    sb.AppendLine($"- {nameof(bone.maleWorldPosition)}: {bone.maleWorldPosition}");
                    sb.AppendLine($"- {nameof(bone.morphedWorldPosition)}: {bone.morphedWorldPosition}");
                    sb.AppendLine($"- {nameof(bone.rotationOrder)}: {bone.rotationOrder}");
                    sb.AppendLine($"- {nameof(bone.rotationOrder)}: {bone.rotationOrder}");
                    sb.AppendLine($"- {nameof(bone.rotationOrder)}: {bone.rotationOrder}");
                    sb.AppendLine($"- {nameof(bone.useCustomJointMap)}: {bone.useCustomJointMap}");
                    sb.AppendLine($"- {nameof(bone.useUnityEulerOrientation)}: {bone.useUnityEulerOrientation}");
                    sb.AppendLine($"- {nameof(bone.parentBone)}: {(bone.parentBone == null ? "null" : bone.parentBone.name)}");
                    continue;
                }
            }
        }

        _currentScriptsJSON.val = sb.Length == 0 ? "<i>No components in this gameobject</i>" : sb.ToString();

        _nextCurrentScript = Time.realtimeSinceStartup + DisplayScriptsFrequency;
    }

    public void OnBindingsListRequested(List<object> bindings)
    {
        bindings.Add(new[]
        {
                new KeyValuePair<string, string>("Namespace", "GameObjectExplorer")
            });
        bindings.Add(new JSONStorableAction("SelectColliderUnderCursor", SelectColliderUnderCursor));
    }

    private void SelectColliderUnderCursor()
    {
        var ray = Camera.main.ScreenPointToRay (Input.mousePosition);
        
        RaycastHit hit;
        if(Physics.Raycast (ray, out hit))
        {
            if (hit.collider != null) {
                Select(hit.collider.gameObject);
                return;
            }
        }

        Select(null);
    }

    public void OnDestroy()
    {
        SuperController.singleton.BroadcastMessage("OnActionsProviderDestroyed", this, SendMessageOptions.DontRequireReceiver);
    }
}