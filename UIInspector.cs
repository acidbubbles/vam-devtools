using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class UIInspector : MVRScript
{
    private JSONStorableString _resultJSON;

    public override void Init()
    {
        base.Init();

        _resultJSON = new JSONStorableString("Inspector", "Use containingAtom.BroadcastMessage(\"DevToolsInspectUI\", gameObject) to display the UI hierarchy");
    }

    public override void InitUI()
    {
        base.InitUI();
        if (UITransform == null) return;
        var scriptUI = UITransform.GetComponentInChildren<MVRScriptUI>();

        var resultUI = CreateTextField(_resultJSON, true);
        resultUI.height = 1200f;
        resultUI.UItext.horizontalOverflow = HorizontalWrapMode.Overflow;
        resultUI.transform.SetParent(scriptUI.fullWidthUIContent.transform, false);
    }

    public void DevToolsInspectUI(GameObject go)
    {
        var sb = new StringBuilder();

        PrintStructure(sb, 0, go.transform);

        _resultJSON.val = sb.ToString();
    }

    private static void PrintStructure(StringBuilder sb, int depth, Transform transform)
    {
        var go = transform.gameObject;
        var prefix = new string(' ', depth * 2);

        sb.AppendLine($"{prefix}+ GameObject: {go.name ?? "(unnamed)"}");

        var rect = go.GetComponent<RectTransform>();
        if (rect != null)
        {
            sb.AppendLine($"{prefix}  | Rect: {rect.rect}");
            sb.AppendLine($"{prefix}  | Anchors: min {rect.anchorMin}, max {rect.anchorMax}");
            sb.AppendLine($"{prefix}  | Anchored Position: {rect.anchoredPosition}");
            sb.AppendLine($"{prefix}  | Pivot: {rect.pivot}");
        }

        foreach (var component in go.GetComponents<MonoBehaviour>())
        {
            var layout = component as LayoutElement;
            if (layout != null)
            {
                sb.AppendLine($"{prefix}  | Layout: w {layout.minWidth}-{layout.preferredWidth}, h {layout.minHeight}-{layout.preferredHeight}");
                continue;
            }

            var contentSizeFitter = component as ContentSizeFitter;
            if (contentSizeFitter != null)
            {
                sb.AppendLine($"{prefix}  | ContentSizeFitter: h {contentSizeFitter.horizontalFit}, v {contentSizeFitter.verticalFit}");
                continue;
            }

            var vGroup = component as VerticalLayoutGroup;
            if (vGroup != null)
            {
                sb.AppendLine($"{prefix}  | VerticalLayoutGroup: cCtrlH {vGroup.childControlHeight}, cExpH {vGroup.childForceExpandHeight}");
                continue;
            }

            sb.AppendLine($"{prefix}  | {component}");

        }

        for (var i = 0; i < transform.childCount; i++)
        {
            PrintStructure(sb, depth + 1, transform.GetChild(i));
        }
    }
}