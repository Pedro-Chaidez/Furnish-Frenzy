// AutoAssignFurniturePrefabs.cs
// Place in Assets/Editor/
// Run via Tools > Auto-Assign Furniture Prefabs
// Scans your prefab folders and assigns them to matching FurnitureItem ScriptableObjects.
 
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
 
public class AutoAssignFurniturePrefabs : EditorWindow
{
    private string prefabRootFolder = "Assets/Furniture Mega Pack/Prefabs";
    private string scriptableObjectFolder = "Assets/Furniture Mega Pack/scriptableObjects";
    private Vector2 scroll;
    private List<string> log = new List<string>();
 
    [MenuItem("Tools/Auto-Assign Furniture Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<AutoAssignFurniturePrefabs>("Auto-Assign Prefabs");
    }
 
    void OnGUI()
    {
        GUILayout.Label("Auto-Assign Furniture Prefabs", EditorStyles.boldLabel);
        EditorGUILayout.Space();
 
        prefabRootFolder = EditorGUILayout.TextField("Prefab Root Folder", prefabRootFolder);
        scriptableObjectFolder = EditorGUILayout.TextField("ScriptableObject Folder", scriptableObjectFolder);
 
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "This will scan each subfolder in your Prefab Root and assign all prefabs " +
            "to the FurnitureItem whose itemName matches the folder name. " +
            "Existing assignments will be replaced.", MessageType.Info);
 
        EditorGUILayout.Space();
 
        if (GUILayout.Button("Run Auto-Assign", GUILayout.Height(40)))
            RunAutoAssign();
 
        EditorGUILayout.Space();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var line in log)
        {
            Color prev = GUI.color;
            if (line.StartsWith("✓")) GUI.color = Color.green;
            else if (line.StartsWith("✗") || line.Contains("WARNING")) GUI.color = Color.red;
            else if (line.StartsWith("──")) GUI.color = Color.cyan;
            EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
            GUI.color = prev;
        }
        EditorGUILayout.EndScrollView();
    }
 
    void RunAutoAssign()
    {
        log.Clear();
        log.Add("── Starting Auto-Assign ─────────────────────");
 
        // Load all FurnitureItem SOs
        string[] soGuids = AssetDatabase.FindAssets("t:FurnitureItem", new[] { scriptableObjectFolder });
        if (soGuids.Length == 0)
        {
            log.Add($"✗ No FurnitureItem assets found in '{scriptableObjectFolder}'");
            return;
        }
 
        var allSOs = new List<FurnitureItem>();
        foreach (var guid in soGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var fi = AssetDatabase.LoadAssetAtPath<FurnitureItem>(path);
            if (fi != null) allSOs.Add(fi);
        }
 
        log.Add($"Found {allSOs.Count} FurnitureItem(s).");
 
        // Get all subfolders in the prefab root
        if (!AssetDatabase.IsValidFolder(prefabRootFolder))
        {
            log.Add($"✗ Prefab root folder not found: '{prefabRootFolder}'");
            return;
        }
 
        string[] subFolders = AssetDatabase.GetSubFolders(prefabRootFolder);
        if (subFolders.Length == 0)
        {
            log.Add($"✗ No subfolders found in '{prefabRootFolder}'");
            return;
        }
 
        int totalAssigned = 0;
 
        foreach (string folder in subFolders)
        {
            string folderName = Path.GetFileName(folder);
            log.Add($"\n── Folder: {folderName} ───────────────────");
 
            // Find matching FurnitureItem — try exact match first, then partial
            FurnitureItem match = allSOs.Find(so =>
                so.itemName.Equals(folderName, System.StringComparison.OrdinalIgnoreCase));
 
            // If no exact match, try if folder name contains the itemName or vice versa
            if (match == null)
            {
                match = allSOs.Find(so =>
                    folderName.Contains(so.itemName, System.StringComparison.OrdinalIgnoreCase) ||
                    so.itemName.Contains(folderName, System.StringComparison.OrdinalIgnoreCase));
            }
 
            if (match == null)
            {
                log.Add($"  ✗ No FurnitureItem matches folder '{folderName}' — skipping.");
                log.Add($"    Available itemNames: {string.Join(", ", allSOs.ConvertAll(s => s.itemName))}");
                continue;
            }
 
            log.Add($"  Matched to SO: '{match.itemName}' (asset: {AssetDatabase.GetAssetPath(match)})");
 
            // Find all prefabs in this folder
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            if (prefabGuids.Length == 0)
            {
                log.Add($"  ✗ WARNING: No prefabs found in '{folder}'");
                continue;
            }
 
            var prefabs = new List<GameObject>();
            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                    prefabs.Add(prefab);
            }
 
            // Sort prefabs alphabetically so they're in order (Chair01, Chair02...)
            prefabs.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
 
            // Assign to SO
            match.prefabVariants = prefabs.ToArray();
            EditorUtility.SetDirty(match);
 
            log.Add($"  ✓ Assigned {prefabs.Count} prefabs to '{match.itemName}'");
            totalAssigned += prefabs.Count;
        }
 
        // Save all changes
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
 
        log.Add($"\n── Done! Assigned {totalAssigned} prefabs total. ───────────────────");
        log.Add("Run Tools > Audit Furniture Names to verify.");
    }
}
#endif
 