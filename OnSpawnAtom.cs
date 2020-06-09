using System.Collections.Generic;

/// <summary>
/// VaM Dev Tools
/// By AcidBubbles
/// Automatically switches to Edit mode when initialized
/// Source: https://github.com/AcidBubbles/vam-devtools
/// </summary>
public class DisableCollisionOnSpawnAtom : MVRScript
{
    private JSONStorableBool _disableCollisionJSON;
    private JSONStorableBool _autoSelectJSON;

    public override void Init()
    {
        _disableCollisionJSON = new JSONStorableBool("Disable collisions on spawn", true);
        RegisterBool(_disableCollisionJSON);
        CreateToggle(_disableCollisionJSON);

        _autoSelectJSON = new JSONStorableBool("Auto select on spawn", true);
        RegisterBool(_autoSelectJSON);
        CreateToggle(_autoSelectJSON);
    }

    public void OnEnable()
    {
        SuperController.singleton.onAtomUIDsChangedHandlers += OnAtomUIDsChanged;
    }

    public void OnDisable()
    {
        SuperController.singleton.onAtomUIDsChangedHandlers -= OnAtomUIDsChanged;
    }

    private void OnAtomUIDsChanged(List<string> atomUIDs)
    {
        var sctrl = SuperController.singleton;
        var sortAtomUIDs = sctrl.sortAtomUIDs;
        sctrl.sortAtomUIDs = false;
        var atoms = sctrl.GetAtomUIDs();
        sctrl.sortAtomUIDs = sortAtomUIDs;
        var latest = atoms[atoms.Count - 1];
        var atom = sctrl.GetAtomByUid(latest);
        if (_disableCollisionJSON.val)
            atom.collisionEnabledJSON.val = false;
        if (_autoSelectJSON.val)
            sctrl.SelectController(atom.mainController);
    }
}