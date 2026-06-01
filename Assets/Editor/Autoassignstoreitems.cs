using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class AutoAssignStoreItems : EditorWindow
{
    private string soFolderPath = "Assets/Furniture Mega Pack/scriptableObjects";
    private string storeScenePath = "Assets/Scenes/Store/Store.unity";
    private Vector2 scroll;
    private List<string> log = new List<string>();

    [MenuItem("Tools/Auto-Assign Store Items")]
    public static void ShowWindow()
    {
        GetWindow<AutoAssignStoreItems>("Auto-Assign Store Items");
    }

    private void OnGUI()
    {
        GUILayout.Label("Auto-Assign Store Item Data", EditorStyles.boldLabel);
        soFolderPath = EditorGUILayout.TextField("SO Folder", soFolderPath);
        storeScenePath = EditorGUILayout.TextField("Store Scene Path", storeScenePath);

        EditorGUILayout.HelpBox(
            "Scans every StoreItem in the current open scene (including children) and assigns the correct FurnitureItem SO based on the GameObject name (e.g. 'Chair25' → SO_Chairs). Open your Store scene first, then run this.",
            MessageType.Info);

        if (GUILayout.Button("Run on Current Scene", GUILayout.Height(40)))
        {
            RunAssignment();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("— Auto-Assign Store Item Data —", EditorStyles.centeredGreyMiniLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var line in log)
        {
            bool isError = line.StartsWith("✗") || line.StartsWith("×");
            bool isWarning = line.StartsWith("⚠");
            var style = new GUIStyle(EditorStyles.label);
            style.wordWrap = true;
            style.normal.textColor = isError ? Color.red : isWarning ? Color.yellow : Color.white;
            GUILayout.Label(line, style);
        }
        EditorGUILayout.EndScrollView();
    }

    private void RunAssignment()
    {
        log.Clear();

        // Load all FurnitureItem SOs
        string[] guids = AssetDatabase.FindAssets("t:FurnitureItem", new[] { soFolderPath });
        var allSOs = new List<FurnitureItem>();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<FurnitureItem>(path);
            if (so != null) allSOs.Add(so);
        }
        log.Add($"Loaded {allSOs.Count} FurnitureItem SOs.");

        if (allSOs.Count == 0)
        {
            log.Add($"✗ No FurnitureItem SOs found in '{soFolderPath}'.");
            return;
        }

        // Find ALL StoreItem components in scene, including inside children
        var storeItems = GameObject.FindObjectsOfType<StoreItem>(true);
        log.Add($"Found {storeItems.Length} StoreItem components (including children).");

        if (storeItems.Length == 0)
        {
            log.Add("✗ No StoreItem components found in the current scene.");
            log.Add("  Make sure your Store scene is open in the editor.");
            log.Add("  Note: StoreItem is now searched on ALL children, not just root objects.");
            return;
        }

        int assigned = 0;
        int skipped = 0;
        int unmatched = 0;

        foreach (var storeItem in storeItems)
        {
            string objName = storeItem.gameObject.name.ToLower();

            // Try to find the best matching SO
            FurnitureItem bestMatch = null;
            int bestScore = 0;

            foreach (var so in allSOs)
            {
                int score = GetMatchScore(objName, so);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = so;
                }
            }

            if (bestMatch != null && bestScore > 0)
            {
                if (storeItem.itemData != bestMatch)
                {
                    Undo.RecordObject(storeItem, "Auto-Assign StoreItem Data");
                    storeItem.itemData = bestMatch;
                    EditorUtility.SetDirty(storeItem);
                    log.Add($"  ✓ {storeItem.gameObject.name} → {bestMatch.itemName} (score:{bestScore})");
                    assigned++;
                }
                else
                {
                    skipped++;
                }
            }
            else
            {
                log.Add($"  ⚠ No match for: {storeItem.gameObject.name}");
                unmatched++;
            }
        }

        AssetDatabase.SaveAssets();
        log.Add($"");
        log.Add($"Done. Assigned: {assigned}, Already correct: {skipped}, Unmatched: {unmatched}");
        log.Add($"Ctrl+S to save the scene.");
    }

    private int GetMatchScore(string objName, FurnitureItem so)
    {
        string itemName = so.itemName.ToLower();
        string assetName = so.name.ToLower(); // e.g. "so_chairs"

        // Keyword mapping: furniture type keywords → SO item names
        var keywords = new Dictionary<string, string[]>
        {
            { "chair",      new[] { "chairs" } },
            { "sofa",       new[] { "sofas" } },
            { "couch",      new[] { "sofas" } },
            { "bed",        new[] { "beds" } },
            { "table",      new[] { "tables" } },
            { "desk",       new[] { "tables" } },
            { "bathroom",   new[] { "bathroom" } },
            { "vanity",     new[] { "bathroom" } },
            { "closet",     new[] { "closets" } },
            { "wardrobe",   new[] { "closets" } },
            { "cushion",    new[] { "cushions" } },
            { "pillow",     new[] { "cushions" } },
            { "drawer",     new[] { "drawers" } },
            { "dresser",    new[] { "drawers" } },
        };

        // Check if the SO's item name appears in the object name (strongest signal)
        if (objName.Contains(itemName)) return 100;
        if (itemName.Contains(objName)) return 90;

        // Check keyword → SO mapping
        foreach (var kv in keywords)
        {
            if (objName.Contains(kv.Key))
            {
                foreach (var target in kv.Value)
                {
                    if (itemName == target || assetName.Contains(target))
                        return 80;
                }
            }
        }

        // Partial match on SO name
        string soCore = assetName.Replace("so_", "").Replace("so", "");
        if (objName.Contains(soCore) && soCore.Length > 2) return 50;

        return 0;
    }
}