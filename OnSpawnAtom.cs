using System.Collections;
using System.Collections.Generic;

/// <summary>
/// VaM Dev Tools
/// By AcidBubbles
/// Automatically switches to Edit mode when initialized
/// Source: https://github.com/acidbubbles/vam-devtools
/// </summary>
public class DisableCollisionOnSpawnAtom : MVRScript
{
    private JSONStorableBool _disableCollisionJSON;
    private JSONStorableBool _autoSelectJSON;
    private bool _ready = false;

    public override void Init()
    {
        _disableCollisionJSON = new JSONStorableBool("Disable collisions on spawn", true);
        RegisterBool(_disableCollisionJSON);
        CreateToggle(_disableCollisionJSON);

        _autoSelectJSON = new JSONStorableBool("Auto select on spawn", true);
        RegisterBool(_autoSelectJSON);
        CreateToggle(_autoSelectJSON);

        StartCoroutine(WaitForLoadingComplete());
    }

    private IEnumerator WaitForLoadingComplete()
    {
        while (SuperController.singleton.isLoading)
            yield return 0;

        while (SuperController.singleton.freezeAnimation)
            yield return 0;

        yield return 0;

        _ready = true;

        foreach (var atom in SuperController.singleton.GetAtoms())
        {
            if (atom.type != "Person") continue;
            SuperController.singleton.SelectController(atom.mainController);
            break;
        }
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
        if (!_ready) return;
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