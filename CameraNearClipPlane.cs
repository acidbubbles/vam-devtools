using UnityEngine;

public class CameraNearClipPlane : MVRScript
{
    private Camera _mainCamera;
    private JSONStorableFloat _nearClipPlaneJSON;
    private float _originalClipDistance;

    public override void Init()
    {
        _mainCamera = CameraTarget.centerTarget?.targetCamera;
        _originalClipDistance = _mainCamera.nearClipPlane;

        CreateTextField(new JSONStorableString("Warning", "Warning: If you increase the range, you could lose the menu and be forced to restart Virt-A-Mate.")).enabled = false;
        _nearClipPlaneJSON = new JSONStorableFloat("Near clip plane", 0.01f, val => SyncCameraClipping(val), 0.01f, 1.5f, false);
        RegisterFloat(_nearClipPlaneJSON);
        CreateSlider(_nearClipPlaneJSON);

        SyncCameraClipping(_nearClipPlaneJSON.val);
    }

    public void OnEnable()
    {
        SyncCameraClipping(_nearClipPlaneJSON.val);
    }

    public void OnDisable()
    {
        _mainCamera.nearClipPlane = _originalClipDistance;
    }

    private void SyncCameraClipping(float nearClipPlane)
    {
        if (nearClipPlane <= 0f)
        {
            SuperController.LogError("Cannot set the clip distance to 0 or less");
            return;
        }

        _mainCamera.nearClipPlane = nearClipPlane;
    }
}