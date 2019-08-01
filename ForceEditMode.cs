/// <summary>
/// VaM Utilities
/// By Acidbubbles
/// Automatically switches to Edit mode when initialized
/// Source: https://github.com/acidbubbles/vam-utilities
/// </summary>
public class ForceEditMode : MVRScript
{
    public override void Init()
    {
        SuperController.singleton.gameMode = SuperController.GameMode.Edit;
    }
}