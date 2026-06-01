// FurnitureNameAudit.cs
// Place this in an Editor folder: Assets/Editor/FurnitureNameAudit.cs
// Then go to Tools > Audit Furniture Names in the Unity menu bar.
// It prints every ScriptableObject's itemName and each variant's
// actual .name so you can spot mismatches.

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FurnitureNameAudit : EditorWindow
{
    private Vector2 scroll;
    private List<string> lines = new List<string>();

    [MenuItem("Tools/Audit Furniture Names")]
    public static void ShowWindow()
    {
        var win = GetWindow<FurnitureNameAudit>("Furniture Name Audit");
        win.RunAudit();
    }

    void RunAudit()
    {
        lines.Clear();

        // Find every FurnitureItem ScriptableObject in the project
        string[] guids = AssetDatabase.FindAssets("t:FurnitureItem");

        if (guids.Length == 0)
        {
            lines.Add("No FurnitureItem assets found! Make sure FurnitureItem.cs is a ScriptableObject.");
            return;
        }

        lines.Add($"Found {guids.Length} FurnitureItem(s):\n");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            FurnitureItem fi = AssetDatabase.LoadAssetAtPath<FurnitureItem>(path);
            if (fi == null) continue;

            lines.Add($"Asset: {path}");
            lines.Add($"  itemName field : \"{fi.itemName}\"");

            if (fi.prefabVariants == null || fi.prefabVariants.Length == 0)
            {
                lines.Add("  prefabVariants : EMPTY — nothing will spawn!");
            }
            else
            {
                lines.Add($"  prefabVariants ({fi.prefabVariants.Length}):");
                foreach (var v in fi.prefabVariants)
                {
                    if (v == null)
                        lines.Add("    [null entry]");
                    else
                        lines.Add($"    v.name = \"{v.name}\"  (asset: {AssetDatabase.GetAssetPath(v)})");
                }
            }
            lines.Add("");
        }

        // Also check current PlayerPrefs for pending items
        lines.Add("── Current PlayerPrefs ──────────────────");
        int cartCount = PlayerPrefs.GetInt("PendingCartCount", -1);
        int handCount = PlayerPrefs.GetInt("PendingHandCount", -1);
        lines.Add($"PendingCartCount = {cartCount}");
        lines.Add($"PendingHandCount = {handCount}");

        for (int i = 0; i < Mathf.Max(cartCount, 0); i++)
        {
            string n = PlayerPrefs.GetString($"Cart_{i}_Name",   "MISSING");
            string p = PlayerPrefs.GetString($"Cart_{i}_Prefab", "MISSING");
            lines.Add($"  Cart[{i}]: itemName='{n}'  prefabName='{p}'");

            // Check if this prefab name matches any variant
            CheckMatch(n, p, guids);
        }

        for (int i = 0; i < Mathf.Max(handCount, 0); i++)
        {
            string n = PlayerPrefs.GetString($"Hand_{i}_Name",   "MISSING");
            string p = PlayerPrefs.GetString($"Hand_{i}_Prefab", "MISSING");
            lines.Add($"  Hand[{i}]: itemName='{n}'  prefabName='{p}'");

            CheckMatch(n, p, guids);
        }
    }

    void CheckMatch(string itemName, string prefabName, string[] guids)
    {
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            FurnitureItem fi = AssetDatabase.LoadAssetAtPath<FurnitureItem>(path);
            if (fi == null || fi.itemName != itemName) continue;

            if (fi.prefabVariants == null) { lines.Add($"    ✗ '{itemName}' has null prefabVariants"); return; }

            foreach (var v in fi.prefabVariants)
            {
                if (v != null && v.name == prefabName)
                {
                    lines.Add($"    ✓ MATCH FOUND: '{prefabName}' in '{fi.itemName}'");
                    return;
                }
            }

            lines.Add($"    ✗ NO MATCH for prefab '{prefabName}' in '{itemName}'");
            lines.Add($"      Available variants:");
            foreach (var v in fi.prefabVariants)
                lines.Add($"        '{v?.name ?? "null"}'");
            return;
        }

        lines.Add($"    ✗ No FurnitureItem with itemName='{itemName}' found");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Re-run Audit"))
            RunAudit();

        if (GUILayout.Button("Copy to Clipboard"))
            GUIUtility.systemCopyBuffer = string.Join("\n", lines);

        EditorGUILayout.Space();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var line in lines)
        {
            Color prev = GUI.color;
            if (line.Contains("✓")) GUI.color = Color.green;
            else if (line.Contains("✗") || line.Contains("EMPTY") || line.Contains("MISSING")) GUI.color = Color.red;
            EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
            GUI.color = prev;
        }
        EditorGUILayout.EndScrollView();
    }
}
#endif