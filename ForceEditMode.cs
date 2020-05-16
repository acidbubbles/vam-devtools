using System.Collections;

/// <summary>
/// VaM Dev Tools
/// By AcidBubbles
/// Automatically switches to Edit mode when initialized
/// Source: https://github.com/AcidBubbles/vam-devtools
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

        yield return 0;

        SuperController.singleton.gameMode = SuperController.GameMode.Edit;
    }
}