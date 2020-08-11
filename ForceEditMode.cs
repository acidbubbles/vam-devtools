using System.Collections;
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
        StartCoroutine(InitCoroutine());
    }

    private IEnumerator InitCoroutine()
    {
        while (SuperController.singleton.isLoading)
            yield return 0;

        while (SuperController.singleton.freezeAnimation)
            yield return 0;

        yield return 0;

        SuperController.singleton.gameMode = SuperController.GameMode.Edit;

        //after the first run, monitor for scene loads and force Edit mode again after each one
        StartCoroutine(WaitForNextLoadCoroutine());
    }

    private IEnumerator WaitForNextLoadCoroutine()
    {
        while (! SuperController.singleton.isLoading)
        {
            yield return new WaitForSeconds(1.0f);
            yield return 0;
        }

        yield return 0;

        StartCoroutine(InitCoroutine());
    }
}
