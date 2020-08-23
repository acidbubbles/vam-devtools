using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// VaM Dev Tools
/// By AcidBubbles
/// Automatically switches to Edit mode when initialized
/// Source: https://github.com/acidbubbles/vam-devtools
/// </summary>
public class ForceEditMode : MVRScript
{
    private Coroutine _coroutine;

    public void OnEnable()
    {
        _coroutine = StartCoroutine(EnableEditModeCoroutine());
#if (VAM_GT_1_20)
        SuperController.singleton.onSceneLoadedHandlers += OnSceneLoaded;
#else
        SuperController.singleton.onAtomUIDsChangedHandlers += OnAtomUIDsChanged;
#endif
    }

    public void OnDisable()
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
            _coroutine = null;
        }
#if (VAM_GT_1_20)
        SuperController.singleton.onSceneLoadedHandlers -= OnSceneLoaded;
#else
        SuperController.singleton.onAtomUIDsChangedHandlers -= OnAtomUIDsChanged;
#endif
    }

#if (VAM_GT_1_20)
    private void OnSceneLoaded()
#else
    private void OnAtomUIDsChanged(List<string> atomUIDs)
#endif
    {
        if (!enabled) return;
        if (_coroutine == null)
        {
            _coroutine = StartCoroutine(EnableEditModeCoroutine());
        }
    }

    private IEnumerator EnableEditModeCoroutine()
    {
        while (SuperController.singleton.isLoading)
            yield return 0;

        while (SuperController.singleton.freezeAnimation)
            yield return 0;

        yield return 0;

        SuperController.singleton.gameMode = SuperController.GameMode.Edit;
        _coroutine = null;
    }
}
