using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
    private int _lastCount = 0;
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

        _lastCount = SuperController.singleton.GetAtoms().Count;
        _ready = true;

        if (_autoSelectJSON.val)
        {
            foreach (var atom in SuperController.singleton.GetAtoms())
            {
                if (atom.type != "Person") continue;
                SuperController.singleton.SelectController(atom.mainController);
                break;
            }
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
        var previousCount = _lastCount;
        _lastCount = SuperController.singleton.GetAtoms().Count;
        if (!_ready) return;
        var sctrl = SuperController.singleton;
        if (sctrl.isLoading) return;
        if(_lastCount <= previousCount) return;
        var sortAtomUIDs = sctrl.sortAtomUIDs;
        sctrl.sortAtomUIDs = false;
        var atoms = sctrl.GetAtomUIDs();
        sctrl.sortAtomUIDs = sortAtomUIDs;
        if (atoms.Count == 0) return;
        var atom = sctrl.GetAtomByUid(atoms[atoms.Count - 1]);
        if (_disableCollisionJSON.val)
            atom.collisionEnabledJSON.val = false;
        if (_autoSelectJSON.val)
            sctrl.SelectController(atom.mainController);
    }
}