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
        SuperController.singleton.onAtomUIDsChangedHandlers += OnAtomUIDsChanged;
    }

    public void OnDisable()
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
            _coroutine = null;
        }
        SuperController.singleton.onAtomUIDsChangedHandlers -= OnAtomUIDsChanged;
    }

    private void OnAtomUIDsChanged(List<string> atomUIDs)
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
