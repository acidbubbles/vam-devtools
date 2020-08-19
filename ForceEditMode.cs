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
    public override void Init()
    {
        SuperController.singleton.onSceneLoadedHandlers += EnableEditMode;
    }

    public void OnDestroy() {
        SuperController.singleton.onSceneLoadedHandlers -= EnableEditMode;
    }

    public void EnableEditMode()
    {
        SuperController.singleton.gameMode = SuperController.GameMode.Edit;
    }
}
