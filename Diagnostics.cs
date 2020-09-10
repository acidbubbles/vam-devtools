using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// VaM Utilities
/// By AcidBubbles
/// Control for sliding between values
/// Source: https://github.com/AcidBubbles/vam-utilities
/// </summary>
public class Diagnostics : MVRScript
{
    public override void Init()
    {
        CreateButton("Get Hierarchy").button.onClick.AddListener(() => SuperController.LogMessage(GetHierarchy(containingAtom.gameObject)));
        CreateButton("Print Tree").button.onClick.AddListener(() => PrintTree(containingAtom.gameObject, true));
        CreateButton("Dump Scene GameObjects").button.onClick.AddListener(() => DumpSceneGameObjects());
    }

    public static void DumpSceneGameObjects()
    {
        foreach (var o in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            PrintTree(o.gameObject, false, "UI");
        }
    }

    public static void PrintTree(GameObject o, bool showScripts, params string[] exclude)
    {
        PrintTree(0, o, showScripts, exclude, new HashSet<GameObject>());
    }

    public static void PrintTree(int indent, GameObject o, bool showScripts, string[] exclude, HashSet<GameObject> found)
    {
        if (found.Contains(o))
        {
            SuperController.LogMessage("|" + new String(' ', indent) + " [" + o.tag + "] " + o.name + " {RECURSIVE}");
            return;
        }
        if (o == null)
        {
            SuperController.LogMessage("|" + new String(' ', indent) + "{null}");
            return;
        }
        if (exclude.Any(x => o.gameObject.name.Contains(x)))
        {
            return;
        }
        found.Add(o);
        SuperController.LogMessage(
            "|" +
            new String(' ', indent) +
            " [" + o.tag + "] " +
            o.name +
            " -> " +
            (showScripts ? string.Join(", ", o.GetComponents<MonoBehaviour>().Select(b => b?.ToString() ?? "[null]").ToArray()) : "")
            );
        for (int i = 0; i < o.transform.childCount; i++)
        {
            var under = o.transform.GetChild(i).gameObject;
            PrintTree(indent + 4, under, showScripts, exclude, found);
        }
    }

    public static string GetHierarchy(GameObject o)
    {
        if (o == null)
            return "{null}";

        var items = new List<string>(new[] { o.name });
        GameObject parent = o;
        for (int i = 0; i < 100; i++)
        {
            parent = parent.transform.parent?.gameObject;
            if (parent == null || parent == o) break;
            items.Insert(0, parent.gameObject.name);
        }
        return string.Join(" -> ", items.ToArray());
    }


    public static void SimulateSave()
    {
        var j = new SimpleJSON.JSONArray();
        foreach (var atom in SuperController.singleton.GetAtoms())
        {
            if (!atom.name.Contains("Mirror") && !atom.name.Contains("Glass")) continue;

            try
            {
                // atom.GetJSON(true, true, true);
                foreach (var id in atom.GetStorableIDs())
                {
                    var stor = atom.GetStorableByID(id);
                    if (stor.gameObject == null) throw new NullReferenceException("123");
                    try
                    {
                        if (stor == null) throw new Exception("Case 1");
                        if (stor.enabled == false) throw new Exception("Case 2");
                        SuperController.LogMessage("Storage" + atom.name + "/" + stor.name + " (" + stor.storeId + ")");
                        string[] value = stor.GetAllFloatAndColorParamNames().ToArray();
                        SuperController.LogMessage(" -" + string.Join(", ", value));
                        // var x = stor.name;
                        // stor.GetJSON();
                    }
                    catch (Exception se)
                    {
                        SuperController.LogMessage("Error with " + atom.name + "/" + stor.name + ": " + se);
                    }
                }
                // atom.Store(j);
            }
            catch (Exception je)
            {
                SuperController.LogMessage("Error with " + GetHierarchy(atom.gameObject) + " " + atom + ": " + je);
            }
        }
    }

    internal static IEnumerable<GameObject> AllChildren(GameObject gameObject)
    {
        return gameObject.GetComponentsInChildren<MonoBehaviour>().GroupBy(b => b.gameObject).Select(x => x.Key).Where(o => o != gameObject);
    }

    internal static string GetInfo(GameObject o)
    {
        if (o == null)
            return "{null}";

        var behaviors = o.GetComponents<MonoBehaviour>();
        var behaviorNames = behaviors.Select(b => b.ToString()).ToArray();
        return o.name + ": " + string.Join(", ", behaviorNames);
    }

    internal static string GetList(IEnumerable<string> values)
    {
        return string.Join(", ", values.ToArray());
    }
}