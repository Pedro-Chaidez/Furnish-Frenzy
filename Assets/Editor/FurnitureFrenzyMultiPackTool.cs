// FurnitureFrenzyMultiPackTool.cs
// Put this file in: Assets/Editor/FurnitureFrenzyMultiPackTool.cs
// Menu: Tools > Furniture Frenzy > Multi-Pack Store Setup
//
// Purpose:
// 1) Assign prefabs from different asset packs to your existing FurnitureItem ScriptableObjects.
// 2) Fix Level2 / Tuesday / Wednesday / Thursday / Friday so scene furniture has StoreItem,
//    itemData, collider, Rigidbody, and Interactable layer like Level1.

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class FurnitureFrenzyMultiPackTool : EditorWindow
{
    [Header("Folders")]
    private string furnitureItemSearchFolder = ""; // blank = entire project

    private string prefabRoots =
        "Assets/Furniture Mega Pack/Prefabs,\n" +
        "Assets/smallitems/HouseInteriorPack/Prefabs,\n" +
        "Assets/cutepackage1/Prefabs,\n" +
        "Assets/ithappy/Cute_Furniture_Free/Prefabs";

    private string scenePaths =
        "Assets/Scenes/Levell2.unity,\n" +
        "Assets/Scenes/Tuesday.unity,\n" +
        "Assets/Scenes/Wednesday.unity,\n" +
        "Assets/Scenes/Thursday.unity,\n" +
        "Assets/Scenes/Friday.unity";

    [Header("Options")]
    private bool appendPrefabVariants = true;
    private bool includeInactiveObjects = true;
    private bool addMissingStoreItems = true;
    private bool updateExistingStoreItems = true;
    private bool addMissingCollider = true;
    private bool addMissingRigidbody = true;
    private bool setInteractableLayer = true;
    private bool selectedRootsOnly = false;

    private Vector2 scroll;
    private readonly List<string> log = new List<string>();

    private const int SceneMatchThreshold = 70;

    [MenuItem("Tools/Furniture Frenzy/Multi-Pack Store Setup")]
    public static void ShowWindow()
    {
        GetWindow<FurnitureFrenzyMultiPackTool>("Multi-Pack Store Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Furniture Frenzy Multi-Pack Store Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Run step 1 first to fill FurnitureItem prefabVariants from all packs. " +
            "Then run step 2 or 3 to make the scene furniture behave like Level1 store items.",
            MessageType.Info);

        furnitureItemSearchFolder = EditorGUILayout.TextField("FurnitureItem Folder", furnitureItemSearchFolder);

        EditorGUILayout.LabelField("Prefab Roots, comma/newline separated", EditorStyles.boldLabel);
        prefabRoots = EditorGUILayout.TextArea(prefabRoots, GUILayout.Height(68));

        EditorGUILayout.LabelField("Scene Paths, comma/newline separated", EditorStyles.boldLabel);
        scenePaths = EditorGUILayout.TextArea(scenePaths, GUILayout.Height(68));

        appendPrefabVariants = EditorGUILayout.Toggle("Append Prefab Variants", appendPrefabVariants);
        includeInactiveObjects = EditorGUILayout.Toggle("Include Inactive Objects", includeInactiveObjects);
        selectedRootsOnly = EditorGUILayout.Toggle("Selected Roots Only", selectedRootsOnly);
        addMissingStoreItems = EditorGUILayout.Toggle("Add Missing StoreItems", addMissingStoreItems);
        updateExistingStoreItems = EditorGUILayout.Toggle("Update Existing StoreItems", updateExistingStoreItems);
        addMissingCollider = EditorGUILayout.Toggle("Add Missing Collider", addMissingCollider);
        addMissingRigidbody = EditorGUILayout.Toggle("Add Missing Rigidbody", addMissingRigidbody);
        setInteractableLayer = EditorGUILayout.Toggle("Set Interactable Layer", setInteractableLayer);

        EditorGUILayout.Space();

        if (GUILayout.Button("1. Assign FurnitureItem Prefab Variants From All Packs", GUILayout.Height(36)))
            AssignPrefabVariantsFromAllPacks();

        if (GUILayout.Button("2. Fix Current Open Scene", GUILayout.Height(36)))
            FixOpenScene(true);

        if (GUILayout.Button("3. Fix Listed Scenes", GUILayout.Height(36)))
            FixListedScenes();

        if (GUILayout.Button("Clear Log"))
            log.Clear();

        EditorGUILayout.Space();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (string line in log)
        {
            Color old = GUI.color;
            if (line.StartsWith("OK") || line.StartsWith("✓")) GUI.color = Color.green;
            else if (line.StartsWith("WARN") || line.StartsWith("⚠")) GUI.color = Color.yellow;
            else if (line.StartsWith("ERROR") || line.StartsWith("✗")) GUI.color = Color.red;
            else if (line.StartsWith("--")) GUI.color = Color.cyan;
            EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
            GUI.color = old;
        }
        EditorGUILayout.EndScrollView();
    }

    // ---------------------------------------------------------------------
    // STEP 1: Fill FurnitureItem.prefabVariants from all asset packs
    // ---------------------------------------------------------------------

    private void AssignPrefabVariantsFromAllPacks()
    {
        log.Clear();
        log.Add("-- STEP 1: Assigning prefab variants from all packs --");

        List<FurnitureItem> items = LoadFurnitureItems();
        if (items.Count == 0)
        {
            log.Add("ERROR No FurnitureItem ScriptableObjects found.");
            return;
        }

        Dictionary<FurnitureItem, List<GameObject>> itemToPrefabs = new Dictionary<FurnitureItem, List<GameObject>>();
        int totalPrefabAssetsScanned = 0;
        int unmatched = 0;

        foreach (string root in ParseList(prefabRoots))
        {
            if (!AssetDatabase.IsValidFolder(root))
            {
                log.Add($"WARN Prefab root not found, skipping: {root}");
                continue;
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { root });
            log.Add($"-- Scanning {root} ({guids.Length} prefabs) --");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                totalPrefabAssetsScanned++;
                MatchResult match = FindBestItemMatch(GetNamesForPrefab(prefab, path), items, "prefab");

                if (!match.Valid)
                {
                    unmatched++;
                    continue;
                }

                if (!itemToPrefabs.ContainsKey(match.Item))
                    itemToPrefabs[match.Item] = new List<GameObject>();

                itemToPrefabs[match.Item].Add(prefab);
            }
        }

        int assignedTotal = 0;
        foreach (KeyValuePair<FurnitureItem, List<GameObject>> kv in itemToPrefabs)
        {
            FurnitureItem item = kv.Key;
            List<GameObject> merged = new List<GameObject>();
            HashSet<string> seen = new HashSet<string>();

            if (appendPrefabVariants && item.prefabVariants != null)
            {
                foreach (GameObject existing in item.prefabVariants)
                {
                    if (existing == null) continue;
                    string p = AssetDatabase.GetAssetPath(existing);
                    if (seen.Add(p)) merged.Add(existing);
                }
            }

            foreach (GameObject prefab in kv.Value)
            {
                if (prefab == null) continue;
                string p = AssetDatabase.GetAssetPath(prefab);
                if (seen.Add(p)) merged.Add(prefab);
            }

            merged.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));

            SerializedObject so = new SerializedObject(item);
            SerializedProperty prop = so.FindProperty("prefabVariants");
            prop.arraySize = merged.Count;
            for (int i = 0; i < merged.Count; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = merged[i];
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(item);
            assignedTotal += merged.Count;
            log.Add($"OK {item.itemName} now has {merged.Count} prefab variant(s).");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        log.Add($"-- Done. Scanned {totalPrefabAssetsScanned} prefab assets. Assigned into {itemToPrefabs.Count} FurnitureItem SOs. Unmatched: {unmatched}. --");
    }

    // ---------------------------------------------------------------------
    // STEP 2/3: Add StoreItem setup to scene furniture
    // ---------------------------------------------------------------------

    private void FixListedScenes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        log.Clear();
        foreach (string scenePath in ParseList(scenePaths))
        {
            if (!File.Exists(scenePath))
            {
                log.Add($"ERROR Scene path not found: {scenePath}");
                continue;
            }

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            FixOpenScene(false);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        log.Add("-- Batch scene fix complete. --");
    }

    private void FixOpenScene(bool clearLog)
    {
        if (clearLog) log.Clear();

        List<FurnitureItem> items = LoadFurnitureItems();
        if (items.Count == 0)
        {
            log.Add("ERROR No FurnitureItem ScriptableObjects found.");
            return;
        }

        Dictionary<string, FurnitureItem> prefabPathToItem = BuildPrefabPathIndex(items);
        List<GameObject> sceneObjects = GetSceneObjects();

        int interactableLayer = LayerMask.NameToLayer("Interactable");
        if (setInteractableLayer && interactableLayer < 0)
            log.Add("WARN No layer named 'Interactable' exists. Create that layer or turn off Set Interactable Layer.");

        string sceneName = SceneManager.GetActiveScene().name;
        log.Add($"-- STEP 2: Fixing scene '{sceneName}' --");
        log.Add($"-- Loaded {items.Count} FurnitureItem SOs, indexed {prefabPathToItem.Count} prefab paths, scanning {sceneObjects.Count} scene objects. --");

        HashSet<GameObject> processed = new HashSet<GameObject>();
        int matched = 0, addedStoreItem = 0, updated = 0, colliderAdded = 0, rbAdded = 0, layerChanged = 0;

        foreach (GameObject obj in sceneObjects)
        {
            if (obj == null) continue;
            if (ShouldIgnore(obj)) continue;
            if (!HasVisual(obj)) continue;

            GameObject target = ResolveTarget(obj);
            if (target == null || ShouldIgnore(target) || !HasVisual(target)) continue;
            if (processed.Contains(target) || HasProcessedParent(target.transform, processed)) continue;

            MatchResult match = FindBestSceneMatch(target, items, prefabPathToItem);
            if (!match.Valid || match.Score < SceneMatchThreshold) continue;
            if (LooksLikeCategoryContainer(target, match.Item, items, prefabPathToItem)) continue;

            processed.Add(target);

            StoreItem storeItem = target.GetComponent<StoreItem>();
            if (storeItem == null && addMissingStoreItems)
            {
                storeItem = Undo.AddComponent<StoreItem>(target);
                if (storeItem == null)
                {
                    log.Add($"ERROR Could not add StoreItem to {target.name}. Make sure StoreItem.cs is NOT inside Assets/Editor and there is only one StoreItem class.");
                    continue;
                }
                addedStoreItem++;
            }

            if (storeItem == null) continue;

            bool changed = false;
            if (updateExistingStoreItems && storeItem.itemData != match.Item)
            {
                Undo.RecordObject(storeItem, "Assign StoreItem itemData");
                storeItem.itemData = match.Item;
                changed = true;
                updated++;
            }

            string wantedPrompt = $"Press E to add {match.Item.itemName} to cart";
            if (storeItem.promptMessage != wantedPrompt)
            {
                Undo.RecordObject(storeItem, "Assign StoreItem prompt");
                storeItem.promptMessage = wantedPrompt;
                changed = true;
            }

            if (addMissingCollider && target.GetComponent<Collider>() == null)
            {
                Undo.AddComponent<BoxCollider>(target);
                FitColliderToRenderers(target);
                colliderAdded++;
                changed = true;
            }

            if (addMissingRigidbody && target.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = Undo.AddComponent<Rigidbody>(target);
                SetupRigidbody(rb);
                rbAdded++;
                changed = true;
            }

            if (setInteractableLayer && interactableLayer >= 0)
            {
                int count = SetLayerOnTargetAndColliderChildren(target, interactableLayer);
                if (count > 0)
                {
                    layerChanged += count;
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(target);
                EditorUtility.SetDirty(storeItem);
            }

            matched++;
            log.Add($"OK {target.name} -> {match.Item.itemName} ({match.Score}, {match.Reason})");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        log.Add($"-- Done '{sceneName}'. Matched {matched}. Added StoreItem {addedStoreItem}. Updated {updated}. Colliders {colliderAdded}. Rigidbodies {rbAdded}. Layer changes {layerChanged}. --");
    }

    // ---------------------------------------------------------------------
    // Data loading / indexing
    // ---------------------------------------------------------------------

    private List<FurnitureItem> LoadFurnitureItems()
    {
        string[] folders = string.IsNullOrWhiteSpace(furnitureItemSearchFolder)
            ? null
            : new[] { furnitureItemSearchFolder.Trim() };

        string[] guids = folders == null
            ? AssetDatabase.FindAssets("t:FurnitureItem")
            : AssetDatabase.FindAssets("t:FurnitureItem", folders);

        List<FurnitureItem> items = new List<FurnitureItem>();
        foreach (string guid in guids)
        {
            FurnitureItem item = AssetDatabase.LoadAssetAtPath<FurnitureItem>(AssetDatabase.GUIDToAssetPath(guid));
            if (item != null) items.Add(item);
        }
        return items;
    }

    private Dictionary<string, FurnitureItem> BuildPrefabPathIndex(List<FurnitureItem> items)
    {
        Dictionary<string, FurnitureItem> map = new Dictionary<string, FurnitureItem>();

        foreach (FurnitureItem item in items)
        {
            if (item == null || item.prefabVariants == null) continue;
            foreach (GameObject prefab in item.prefabVariants)
            {
                if (prefab == null) continue;
                string path = AssetDatabase.GetAssetPath(prefab);
                if (!string.IsNullOrEmpty(path) && !map.ContainsKey(path))
                    map[path] = item;
            }
        }

        return map;
    }

    // ---------------------------------------------------------------------
    // Matching
    // ---------------------------------------------------------------------

    private MatchResult FindBestSceneMatch(GameObject obj, List<FurnitureItem> items, Dictionary<string, FurnitureItem> prefabPathToItem)
    {
        List<string> names = new List<string> { obj.name };

        GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(obj);
        if (source != null)
        {
            string sourcePath = AssetDatabase.GetAssetPath(source);
            if (!string.IsNullOrEmpty(sourcePath) && prefabPathToItem.TryGetValue(sourcePath, out FurnitureItem exactItem))
                return new MatchResult(exactItem, 125, "exact prefab source");

            names.Add(source.name);
            names.AddRange(GetPathSegments(sourcePath));
        }

        GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(obj);
        if (root != null)
        {
            GameObject rootSource = PrefabUtility.GetCorrespondingObjectFromSource(root);
            if (rootSource != null)
            {
                string rootSourcePath = AssetDatabase.GetAssetPath(rootSource);
                if (!string.IsNullOrEmpty(rootSourcePath) && prefabPathToItem.TryGetValue(rootSourcePath, out FurnitureItem rootItem))
                    return new MatchResult(rootItem, 120, "exact prefab root");

                names.Add(rootSource.name);
                names.AddRange(GetPathSegments(rootSourcePath));
            }
        }

        Transform parent = obj.transform.parent;
        int hops = 0;
        while (parent != null && hops < 2)
        {
            names.Add(parent.name);
            parent = parent.parent;
            hops++;
        }

        return FindBestItemMatch(names, items, "scene name");
    }

    private MatchResult FindBestItemMatch(IEnumerable<string> rawNames, List<FurnitureItem> items, string reason)
    {
        FurnitureItem best = null;
        int bestScore = 0;
        string bestRaw = "";
        bool tied = false;

        foreach (string raw in rawNames)
        {
            string clean = CleanName(raw);
            if (string.IsNullOrEmpty(clean)) continue;

            foreach (FurnitureItem item in items)
            {
                int score = ScoreItem(clean, item);
                if (score > bestScore)
                {
                    best = item;
                    bestScore = score;
                    bestRaw = raw;
                    tied = false;
                }
                else if (score == bestScore && score > 0 && item != best)
                {
                    tied = true;
                }
            }
        }

        if (best == null || tied) return MatchResult.None;
        return new MatchResult(best, bestScore, $"{reason}: {bestRaw}");
    }

    private static int ScoreItem(string cleanName, FurnitureItem item)
    {
        string itemName = CleanName(item.itemName);
        string assetName = CleanName(item.name);

        if (cleanName == itemName || cleanName == assetName) return 115;
        if (itemName.Length >= 4 && cleanName.Contains(itemName)) return 100;
        if (assetName.Length >= 4 && cleanName.Contains(assetName)) return 98;
        if (cleanName.Length >= 4 && itemName.Contains(cleanName)) return 80;

        int best = 0;
        foreach (KeyValuePair<string, string[]> group in KeywordGroups)
        {
            if (!ItemBelongsToGroup(itemName, assetName, group.Key, group.Value)) continue;

            foreach (string rawSynonym in group.Value)
            {
                string synonym = CleanName(rawSynonym);
                if (string.IsNullOrEmpty(synonym)) continue;

                if (cleanName == synonym) best = Mathf.Max(best, 105);
                else if (synonym.Length >= 4 && cleanName.StartsWith(synonym)) best = Mathf.Max(best, 92);
                else if (synonym.Length >= 4 && cleanName.EndsWith(synonym)) best = Mathf.Max(best, 88);
                else if (synonym.Length >= 5 && cleanName.Contains(synonym)) best = Mathf.Max(best, 82);
            }
        }

        return best;
    }

    private static bool ItemBelongsToGroup(string itemName, string assetName, string key, string[] synonyms)
    {
        string cleanKey = CleanName(key);
        if (itemName.Contains(cleanKey) || assetName.Contains(cleanKey)) return true;

        foreach (string raw in synonyms)
        {
            string syn = CleanName(raw);
            if (syn.Length >= 4 && (itemName.Contains(syn) || assetName.Contains(syn))) return true;
        }
        return false;
    }

    private static readonly Dictionary<string, string[]> KeywordGroups = new Dictionary<string, string[]>
    {
        { "chair", new[] { "chair", "chairs", "armchair" } },
        { "sofa", new[] { "sofa", "sofas", "couch", "couches" } },
        { "bed", new[] { "bed", "beds", "bunkbed" } },
        { "table", new[] { "table", "tables", "desk", "desks", "coffee table", "coffetable", "coffeetable", "launch table", "launchtable", "bedside table", "bedsidetable", "sidetable", "endtable", "worktable" } },
        { "closet", new[] { "closet", "closets", "wardrobe", "wardrobes", "clothes hanger", "clotheshanger", "hanger" } },
        { "drawer", new[] { "drawer", "drawers", "dresser", "dressers", "nightstand" } },
        { "bathroom", new[] { "bathroom", "bathrooms", "toilet", "toilets", "vanity", "washbasin", "washstand", "bath", "bathtub", "shower", "towel", "tissue", "mixer", "faucet", "soap", "soapdispenser" } },
        { "kitchen", new[] { "kitchen", "kitchens", "cabinet", "cabinets", "sink", "stove", "gasstove", "oven", "kitchenoven", "microwave", "fridge", "refrigerator", "minifridge", "iron", "ironingboard", "plate", "cup", "jar", "teapot", "kettle", "toaster", "utensils", "cuttingboard" } },
        { "cushion", new[] { "cushion", "cushions", "cushioins", "pillow", "pillows" } },
        { "shelf", new[] { "shelf", "shelves", "rack", "racks", "shoerack", "storagebox", "storage", "container", "containers", "box", "boxes" } },
        { "light", new[] { "light", "lights", "lamp", "lamps", "ceilinglight", "roomlight", "desklight", "mirrorlight", "cornerlight", "bedsidelight" } },
        { "mirror", new[] { "mirror", "mirrors" } },
        { "plant", new[] { "plant", "plants", "vase", "vases", "liana", "flower" } },
        { "decor", new[] { "painting", "paintings", "picture", "bottle", "vinylplayer", "keyboard", "pcmonitor", "pcmouse" } },
    };

    private static string CleanName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        string value = raw;
        value = Regex.Replace(value, @"\s*\(Clone\)\s*$", "", RegexOptions.IgnoreCase);
        value = Regex.Replace(value, @"\s*\(\d+\)\s*$", "");
        value = Regex.Replace(value, @"^\([^)]{1,20}\)\s*", ""); // strips (Prb)
        value = Regex.Replace(value, @"^(SO_|so_|Prb_|PRB_|HIP_)", "", RegexOptions.IgnoreCase);
        value = Regex.Replace(value, @"\d+$", "");
        value = value.Replace("_", "").Replace("-", "").Replace(" ", "");
        return value.ToLowerInvariant();
    }

    // ---------------------------------------------------------------------
    // Scene object helpers
    // ---------------------------------------------------------------------

    private List<GameObject> GetSceneObjects()
    {
        List<GameObject> result = new List<GameObject>();
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        HashSet<GameObject> selected = new HashSet<GameObject>(Selection.gameObjects);

        foreach (GameObject obj in all)
        {
            if (obj == null) continue;
            if (!obj.scene.IsValid()) continue;
            if (EditorUtility.IsPersistent(obj)) continue;
            if (!includeInactiveObjects && !obj.activeInHierarchy) continue;
            if (selectedRootsOnly && !IsUnderSelectedRoot(obj.transform, selected)) continue;
            result.Add(obj);
        }

        result.Sort((a, b) => GetDepth(a.transform).CompareTo(GetDepth(b.transform)));
        return result;
    }

    private static GameObject ResolveTarget(GameObject obj)
    {
        GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(obj);
        if (root != null && root.scene == obj.scene && HasVisual(root)) return root;
        return obj;
    }

    private static bool HasVisual(GameObject obj)
    {
        return obj.GetComponentInChildren<Renderer>(true) != null;
    }

    private static bool ShouldIgnore(GameObject obj)
    {
        if (obj == null) return true;
        if (obj.GetComponentInParent<Canvas>(true) != null) return true;
        if (obj.GetComponentInParent<UnityEngine.EventSystems.EventSystem>(true) != null) return true;
        if (obj.GetComponentInParent<Camera>(true) != null) return true;
        if (obj.GetComponentInParent<Light>(true) != null) return true;
        if (obj.GetComponentInParent<ShoppingCart3D>(true) != null) return true;

        string clean = CleanName(obj.name);
        return clean.Contains("player") ||
               clean.Contains("cart") ||
               clean.Contains("canvas") ||
               clean.Contains("camera") ||
               clean.Contains("eventsystem") ||
               clean.Contains("manager") ||
               clean.Contains("light") && obj.GetComponent<Light>() != null ||
               clean.Contains("floor") ||
               clean.Contains("wall") ||
               clean.Contains("ceiling") ||
               clean.Contains("terrain") ||
               clean.Contains("door") ||
               clean.Contains("spawn") ||
               clean.Contains("npc");
    }

    private bool LooksLikeCategoryContainer(GameObject obj, FurnitureItem item, List<FurnitureItem> items, Dictionary<string, FurnitureItem> prefabPathToItem)
    {
        string objectClean = CleanNamePreserveNumber(obj.name);
        string itemClean = CleanNamePreserveNumber(item.itemName);
        string assetClean = CleanNamePreserveNumber(item.name);

        bool genericName = objectClean == itemClean || objectClean == assetClean;
        if (!genericName) return false;

        int matchingChildren = 0;
        foreach (Transform child in obj.transform)
        {
            if (!HasVisual(child.gameObject) || ShouldIgnore(child.gameObject)) continue;
            MatchResult childMatch = FindBestSceneMatch(child.gameObject, items, prefabPathToItem);
            if (childMatch.Valid && childMatch.Score >= SceneMatchThreshold)
            {
                matchingChildren++;
                if (matchingChildren >= 2) return true;
            }
        }
        return false;
    }

    private static string CleanNamePreserveNumber(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        string value = raw;
        value = Regex.Replace(value, @"\s*\(\d+\)\s*$", "");
        value = Regex.Replace(value, @"^\([^)]{1,20}\)\s*", "");
        value = Regex.Replace(value, @"^(SO_|so_|Prb_|PRB_|HIP_)", "", RegexOptions.IgnoreCase);
        value = value.Replace("_", "").Replace("-", "").Replace(" ", "");
        return value.ToLowerInvariant();
    }

    private static bool HasProcessedParent(Transform t, HashSet<GameObject> processed)
    {
        Transform current = t.parent;
        while (current != null)
        {
            if (processed.Contains(current.gameObject)) return true;
            current = current.parent;
        }
        return false;
    }

    private static int SetLayerOnTargetAndColliderChildren(GameObject target, int layer)
    {
        int changed = 0;
        foreach (Transform child in target.GetComponentsInChildren<Transform>(true))
        {
            bool shouldSet = child == target.transform ||
                             child.GetComponent<Collider>() != null ||
                             child.GetComponent<Renderer>() != null;
            if (!shouldSet || child.gameObject.layer == layer) continue;

            Undo.RecordObject(child.gameObject, "Set Interactable Layer");
            child.gameObject.layer = layer;
            EditorUtility.SetDirty(child.gameObject);
            changed++;
        }
        return changed;
    }

    private static void FitColliderToRenderers(GameObject obj)
    {
        BoxCollider box = obj.GetComponent<BoxCollider>();
        if (box == null) return;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

        box.center = obj.transform.InverseTransformPoint(bounds.center);
        Vector3 localSize = obj.transform.InverseTransformVector(bounds.size);
        box.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        box.isTrigger = false;
        EditorUtility.SetDirty(box);
    }

    private static void SetupRigidbody(Rigidbody rb)
    {
        if (rb == null) return;
        rb.mass = 1f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.5f;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        EditorUtility.SetDirty(rb);
    }

    // ---------------------------------------------------------------------
    // Utility
    // ---------------------------------------------------------------------

    private static List<string> ParseList(string text)
    {
        List<string> result = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return result;

        string normalized = text.Replace("\n", ",").Replace("\r", ",");
        foreach (string raw in normalized.Split(','))
        {
            string trimmed = raw.Trim();
            if (!string.IsNullOrEmpty(trimmed)) result.Add(trimmed);
        }
        return result;
    }

    private static IEnumerable<string> GetNamesForPrefab(GameObject prefab, string path)
    {
        List<string> names = new List<string>();
        if (prefab != null) names.Add(prefab.name);
        names.AddRange(GetPathSegments(path));
        return names;
    }

    private static IEnumerable<string> GetPathSegments(string path)
    {
        List<string> names = new List<string>();
        if (string.IsNullOrEmpty(path)) return names;
        string normalized = path.Replace('\\', '/');
        foreach (string segment in normalized.Split('/'))
        {
            string noExt = Path.GetFileNameWithoutExtension(segment);
            if (!string.IsNullOrEmpty(noExt)) names.Add(noExt);
        }
        return names;
    }

    private static bool IsUnderSelectedRoot(Transform t, HashSet<GameObject> selected)
    {
        if (selected.Count == 0) return false;
        Transform current = t;
        while (current != null)
        {
            if (selected.Contains(current.gameObject)) return true;
            current = current.parent;
        }
        return false;
    }

    private static int GetDepth(Transform t)
    {
        int depth = 0;
        Transform current = t.parent;
        while (current != null)
        {
            depth++;
            current = current.parent;
        }
        return depth;
    }

    private readonly struct MatchResult
    {
        public static readonly MatchResult None = new MatchResult(null, 0, "");
        public readonly FurnitureItem Item;
        public readonly int Score;
        public readonly string Reason;
        public bool Valid => Item != null && Score > 0;

        public MatchResult(FurnitureItem item, int score, string reason)
        {
            Item = item;
            Score = score;
            Reason = reason;
        }
    }
}
#endif
