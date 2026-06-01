// StoreItemCartInventoryConnector.cs
// Put this file in: Assets/Editor/StoreItemCartInventoryConnector.cs
// Menu: Tools > Furniture Frenzy > Connect StoreItems To CartInventory

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StoreItemCartInventoryConnector : EditorWindow
{
    private string scenePaths =
        "Assets/Scenes/Levell2.unity,\n" +
        "Assets/Scenes/Tuesday.unity,\n" +
        "Assets/Scenes/Wednesday.unity,\n" +
        "Assets/Scenes/Thursday.unity,\n" +
        "Assets/Scenes/Friday.unity";

    private bool includeInactive = true;
    private bool overwriteExistingReferences = true;
    private Vector2 scroll;
    private readonly List<string> log = new List<string>();

    [MenuItem("Tools/Furniture Frenzy/Connect StoreItems To CartInventory")]
    public static void ShowWindow()
    {
        GetWindow<StoreItemCartInventoryConnector>("Connect CartInventory");
    }

    private void OnGUI()
    {
        GUILayout.Label("Connect StoreItems To CartInventory", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This assigns the scene's CartInventorySystem / CartInventory component to every StoreItem.cartInventory field. " +
            "Run this after your StoreItem fixer has added StoreItem scripts to the furniture.",
            MessageType.Info);

        includeInactive = EditorGUILayout.Toggle("Include Inactive Objects", includeInactive);
        overwriteExistingReferences = EditorGUILayout.Toggle("Overwrite Existing References", overwriteExistingReferences);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Batch Scene Paths", EditorStyles.boldLabel);
        scenePaths = EditorGUILayout.TextArea(scenePaths, GUILayout.Height(72));

        EditorGUILayout.Space();
        if (GUILayout.Button("Connect SELECTED StoreItems Only", GUILayout.Height(32)))
            ConnectSelected();

        if (GUILayout.Button("Connect CURRENT Open Scene", GUILayout.Height(36)))
            ConnectCurrentScene(true);

        if (GUILayout.Button("Connect LISTED Scenes", GUILayout.Height(36)))
            ConnectListedScenes();

        if (GUILayout.Button("Clear Log"))
            log.Clear();

        EditorGUILayout.Space();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (string line in log)
        {
            Color old = GUI.color;
            if (line.StartsWith("OK")) GUI.color = Color.green;
            else if (line.StartsWith("WARN")) GUI.color = Color.yellow;
            else if (line.StartsWith("ERROR")) GUI.color = Color.red;
            else if (line.StartsWith("--")) GUI.color = Color.cyan;
            EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
            GUI.color = old;
        }
        EditorGUILayout.EndScrollView();
    }

    private void ConnectSelected()
    {
        log.Clear();

        CartInventory cartInventory = FindSceneCartInventory();
        if (cartInventory == null)
        {
            log.Add("ERROR No CartInventory component found in this scene. Select/open a scene with CartInventorySystem first.");
            return;
        }

        int changed = 0;
        int checkedCount = 0;

        foreach (GameObject selected in Selection.gameObjects)
        {
            if (selected == null) continue;

            StoreItem[] storeItems = selected.GetComponentsInChildren<StoreItem>(includeInactive);
            foreach (StoreItem storeItem in storeItems)
            {
                checkedCount++;
                if (ConnectOne(storeItem, cartInventory)) changed++;
            }
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        log.Add($"OK Selected connect complete. Checked {checkedCount} StoreItem(s), updated {changed}.");
    }

    private void ConnectListedScenes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        log.Clear();
        List<string> paths = ParseScenePaths(scenePaths);
        if (paths.Count == 0)
        {
            log.Add("ERROR No scene paths listed.");
            return;
        }

        foreach (string path in paths)
        {
            if (!File.Exists(path))
            {
                log.Add($"WARN Scene file not found, skipping: {path}");
                continue;
            }

            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            ConnectCurrentScene(false);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        log.Add("OK Batch connect complete.");
    }

    private void ConnectCurrentScene(bool clearLog)
    {
        if (clearLog) log.Clear();

        Scene scene = SceneManager.GetActiveScene();
        log.Add($"-- Scene: {scene.name} ({scene.path})");

        CartInventory cartInventory = FindSceneCartInventory();
        if (cartInventory == null)
        {
            log.Add("ERROR No CartInventory component found in this scene. Expected an object like CartInventorySystem with CartInventory script.");
            return;
        }

        StoreItem[] storeItems = FindSceneStoreItems();
        int changed = 0;
        int skipped = 0;

        foreach (StoreItem storeItem in storeItems)
        {
            if (storeItem == null) continue;

            if (!overwriteExistingReferences && storeItem.cartInventory != null)
            {
                skipped++;
                continue;
            }

            if (ConnectOne(storeItem, cartInventory)) changed++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();

        log.Add($"OK Found CartInventory on '{cartInventory.gameObject.name}'.");
        log.Add($"OK Checked {storeItems.Length} StoreItem(s). Updated {changed}. Skipped {skipped}.");
    }

    private bool ConnectOne(StoreItem storeItem, CartInventory cartInventory)
    {
        if (storeItem == null || cartInventory == null) return false;
        if (storeItem.cartInventory == cartInventory) return false;

        Undo.RecordObject(storeItem, "Connect StoreItem CartInventory");
        storeItem.cartInventory = cartInventory;
        EditorUtility.SetDirty(storeItem);

        log.Add($"OK {GetFullPath(storeItem.transform)} -> {cartInventory.gameObject.name}");
        return true;
    }

    private CartInventory FindSceneCartInventory()
    {
        CartInventory best = null;

        foreach (CartInventory ci in Resources.FindObjectsOfTypeAll<CartInventory>())
        {
            if (ci == null) continue;
            if (!ci.gameObject.scene.IsValid()) continue;
            if (EditorUtility.IsPersistent(ci)) continue;
            if (!includeInactive && !ci.gameObject.activeInHierarchy) continue;

            string cleanName = ci.gameObject.name.ToLowerInvariant().Replace(" ", "");

            // Prefer the object explicitly named like your scene object.
            if (cleanName.Contains("cartinventorysystem")) return ci;
            if (cleanName.Contains("cartinventory")) best = ci;
            else if (best == null) best = ci;
        }

        return best;
    }

    private StoreItem[] FindSceneStoreItems()
    {
        List<StoreItem> result = new List<StoreItem>();

        foreach (StoreItem item in Resources.FindObjectsOfTypeAll<StoreItem>())
        {
            if (item == null) continue;
            if (!item.gameObject.scene.IsValid()) continue;
            if (EditorUtility.IsPersistent(item)) continue;
            if (!includeInactive && !item.gameObject.activeInHierarchy) continue;
            result.Add(item);
        }

        return result.ToArray();
    }

    private static List<string> ParseScenePaths(string text)
    {
        List<string> paths = new List<string>();
        string normalized = text.Replace("\r", "").Replace("\n", ",");
        foreach (string raw in normalized.Split(','))
        {
            string trimmed = raw.Trim();
            if (!string.IsNullOrEmpty(trimmed)) paths.Add(trimmed);
        }
        return paths;
    }

    private static string GetFullPath(Transform transform)
    {
        if (transform == null) return "<null>";

        string path = transform.name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}
#endif
