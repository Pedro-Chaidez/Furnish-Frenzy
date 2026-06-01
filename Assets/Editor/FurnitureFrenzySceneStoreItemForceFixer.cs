// FurnitureFrenzySceneStoreItemForceFixer.cs
// Put this file in: Assets/Editor/FurnitureFrenzySceneStoreItemForceFixer.cs
// Menu: Tools > Furniture Frenzy > FORCE Fix StoreItems
//
// This is a clean-slate scene fixer for Level2 / Tuesday / Wednesday / Thursday / Friday.
// It does NOT require prefabVariants to already be perfect. It categorizes scene objects
// by their object names / prefab source names / folder path names and then adds the same
// runtime StoreItem setup that working Level1 furniture has.

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class FurnitureFrenzySceneStoreItemForceFixer : EditorWindow
{
    private string furnitureItemSearchFolder = ""; // blank = entire project

    private string scenePaths =
        "Assets/Scenes/Levell2.unity,\n" +
        "Assets/Scenes/Tuesday.unity,\n" +
        "Assets/Scenes/Wednesday.unity,\n" +
        "Assets/Scenes/Thursday.unity,\n" +
        "Assets/Scenes/Friday.unity";

    private bool includeInactive = true;
    private bool setInteractableLayer = true;
    private bool addColliderIfMissing = true;
    private bool fitColliderToRenderers = true;
    private bool addRigidbodyIfMissing = true;
    private bool removeStoreItemFromChildren = true;
    private bool markStaticOff = true;

    private Vector2 scroll;
    private readonly List<string> log = new List<string>();

    private const int MATCH_THRESHOLD = 70;

    [MenuItem("Tools/Furniture Frenzy/FORCE Fix StoreItems")]
    public static void Open()
    {
        GetWindow<FurnitureFrenzySceneStoreItemForceFixer>("FORCE StoreItems");
    }

    private void OnGUI()
    {
        GUILayout.Label("Furniture Frenzy FORCE StoreItem Fixer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Use this when objects get Rigidbody/Collider but still do not show Store Item. " +
            "It force-adds the runtime StoreItem component and assigns itemData by name matching.",
            MessageType.Info);

        furnitureItemSearchFolder = EditorGUILayout.TextField("FurnitureItem Folder", furnitureItemSearchFolder);

        EditorGUILayout.LabelField("Batch Scene Paths", EditorStyles.boldLabel);
        scenePaths = EditorGUILayout.TextArea(scenePaths, GUILayout.Height(64));

        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);
        setInteractableLayer = EditorGUILayout.Toggle("Set Interactable Layer", setInteractableLayer);
        addColliderIfMissing = EditorGUILayout.Toggle("Add Collider If Missing", addColliderIfMissing);
        fitColliderToRenderers = EditorGUILayout.Toggle("Fit Collider To Renderers", fitColliderToRenderers);
        addRigidbodyIfMissing = EditorGUILayout.Toggle("Add Rigidbody If Missing", addRigidbodyIfMissing);
        removeStoreItemFromChildren = EditorGUILayout.Toggle("Remove Nested StoreItems", removeStoreItemFromChildren);
        markStaticOff = EditorGUILayout.Toggle("Turn Off Static On Store Items", markStaticOff);

        EditorGUILayout.Space();
        if (GUILayout.Button("Fix SELECTED Object(s) Only", GUILayout.Height(34)))
            FixSelectedObjects();
        if (GUILayout.Button("Fix CURRENT Open Scene", GUILayout.Height(34)))
            FixOpenScene(true);
        if (GUILayout.Button("Fix LISTED Scenes", GUILayout.Height(34)))
            FixListedScenes();
        if (GUILayout.Button("Clear Log"))
            log.Clear();

        EditorGUILayout.Space();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (string line in log)
        {
            Color old = GUI.color;
            if (line.StartsWith("OK") || line.StartsWith("DONE")) GUI.color = Color.green;
            else if (line.StartsWith("WARN") || line.StartsWith("SKIP")) GUI.color = Color.yellow;
            else if (line.StartsWith("ERROR")) GUI.color = Color.red;
            else if (line.StartsWith("--")) GUI.color = Color.cyan;
            EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
            GUI.color = old;
        }
        EditorGUILayout.EndScrollView();
    }

    private void FixSelectedObjects()
    {
        log.Clear();
        List<FurnitureItem> items = LoadFurnitureItems();
        if (items.Count == 0)
        {
            log.Add("ERROR No FurnitureItem ScriptableObjects found. Make sure SO_Beds, SO_Chairs, etc. exist.");
            return;
        }

        int fixedCount = 0;
        foreach (GameObject selected in Selection.gameObjects)
        {
            if (selected == null) continue;
            GameObject target = ResolveFurnitureTarget(selected);
            if (target == null) target = selected;

            MatchResult match = MatchGameObjectToFurnitureItem(target, items);
            if (!match.Valid)
            {
                log.Add($"SKIP {target.name}: no FurnitureItem match. Names checked include object/prefab/folder names.");
                continue;
            }

            if (ApplyStoreSetup(target, match)) fixedCount++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        log.Add($"DONE Selected fix complete. Fixed {fixedCount} object(s).");
    }

    private void FixListedScenes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        log.Clear();
        foreach (string scenePath in ParseList(scenePaths))
        {
            if (!File.Exists(scenePath))
            {
                log.Add($"ERROR Scene not found: {scenePath}");
                continue;
            }

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            FixOpenScene(false);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        log.Add("DONE Batch scene fix complete.");
    }

    private void FixOpenScene(bool clearLog)
    {
        if (clearLog) log.Clear();

        List<FurnitureItem> items = LoadFurnitureItems();
        if (items.Count == 0)
        {
            log.Add("ERROR No FurnitureItem ScriptableObjects found. Make sure SO_Beds, SO_Chairs, etc. exist.");
            return;
        }

        log.Add($"-- Fixing scene: {SceneManager.GetActiveScene().name} --");
        log.Add($"-- Loaded {items.Count} FurnitureItem ScriptableObject(s). --");

        List<GameObject> objects = GetSceneObjects();
        HashSet<GameObject> processed = new HashSet<GameObject>();
        int fixedCount = 0;
        int skippedNoMatch = 0;

        foreach (GameObject obj in objects)
        {
            if (obj == null) continue;
            if (ShouldIgnore(obj)) continue;
            if (!HasRenderer(obj)) continue;

            GameObject target = ResolveFurnitureTarget(obj);
            if (target == null || ShouldIgnore(target) || !HasRenderer(target)) continue;
            if (processed.Contains(target) || HasProcessedAncestor(target.transform, processed)) continue;

            MatchResult match = MatchGameObjectToFurnitureItem(target, items);
            if (!match.Valid || match.Score < MATCH_THRESHOLD)
            {
                skippedNoMatch++;
                continue;
            }

            // Avoid category parent containers like "Chairs" when they contain many actual chairs.
            if (LooksLikeContainer(target, items)) continue;

            processed.Add(target);
            if (ApplyStoreSetup(target, match)) fixedCount++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        log.Add($"DONE Scene '{SceneManager.GetActiveScene().name}': fixed {fixedCount}, skipped no match {skippedNoMatch}.");
    }

    private bool ApplyStoreSetup(GameObject target, MatchResult match)
    {
        if (target == null || match.Item == null) return false;

        if (markStaticOff && target.isStatic)
        {
            Undo.RecordObject(target, "Turn off static for StoreItem");
            target.isStatic = false;
        }

        StoreItem storeItem = target.GetComponent<StoreItem>();
        if (storeItem == null)
        {
            storeItem = Undo.AddComponent<StoreItem>(target);
            if (storeItem == null)
            {
                log.Add($"ERROR Could not add StoreItem to '{target.name}'. StoreItem.cs must be outside Assets/Editor and there must be only one StoreItem class.");
                return false;
            }
        }

        Undo.RecordObject(storeItem, "Assign StoreItem data");
        storeItem.itemData = match.Item;
        storeItem.promptMessage = $"Press E to add {match.Item.itemName} to cart";
        EditorUtility.SetDirty(storeItem);

        if (removeStoreItemFromChildren)
        {
            StoreItem[] nested = target.GetComponentsInChildren<StoreItem>(true);
            foreach (StoreItem childStoreItem in nested)
            {
                if (childStoreItem != null && childStoreItem.gameObject != target)
                    Undo.DestroyObjectImmediate(childStoreItem);
            }
        }

        if (addColliderIfMissing && target.GetComponent<Collider>() == null)
        {
            BoxCollider box = Undo.AddComponent<BoxCollider>(target);
            if (fitColliderToRenderers) FitBoxCollider(target, box);
        }
        else if (fitColliderToRenderers)
        {
            BoxCollider box = target.GetComponent<BoxCollider>();
            if (box != null) FitBoxCollider(target, box);
        }

        if (addRigidbodyIfMissing && target.GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = Undo.AddComponent<Rigidbody>(target);
            SetupRigidbody(rb);
        }
        else
        {
            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb != null) SetupRigidbody(rb);
        }

        if (setInteractableLayer)
        {
            int layer = LayerMask.NameToLayer("Interactable");
            if (layer < 0)
            {
                log.Add("WARN Layer 'Interactable' does not exist. Create it in Project Settings > Tags and Layers.");
            }
            else
            {
                SetLayerOnRelevantObjects(target, layer);
            }
        }

        EditorUtility.SetDirty(target);
        log.Add($"OK {target.name} -> {match.Item.itemName} ({match.Score}, {match.Reason})");
        return true;
    }

    private List<FurnitureItem> LoadFurnitureItems()
    {
        string[] folders = string.IsNullOrWhiteSpace(furnitureItemSearchFolder) ? null : new[] { furnitureItemSearchFolder.Trim() };
        string[] guids = folders == null ? AssetDatabase.FindAssets("t:FurnitureItem") : AssetDatabase.FindAssets("t:FurnitureItem", folders);

        List<FurnitureItem> result = new List<FurnitureItem>();
        foreach (string guid in guids)
        {
            FurnitureItem item = AssetDatabase.LoadAssetAtPath<FurnitureItem>(AssetDatabase.GUIDToAssetPath(guid));
            if (item != null) result.Add(item);
        }
        return result;
    }

    private List<GameObject> GetSceneObjects()
    {
        List<GameObject> result = new List<GameObject>();
        foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (obj == null) continue;
            if (!obj.scene.IsValid()) continue;
            if (EditorUtility.IsPersistent(obj)) continue;
            if (!includeInactive && !obj.activeInHierarchy) continue;
            result.Add(obj);
        }

        result.Sort((a, b) => GetDepth(a.transform).CompareTo(GetDepth(b.transform)));
        return result;
    }

    private MatchResult MatchGameObjectToFurnitureItem(GameObject obj, List<FurnitureItem> items)
    {
        List<string> names = new List<string>();
        names.Add(obj.name);

        GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(obj);
        if (source != null)
        {
            string sourcePath = AssetDatabase.GetAssetPath(source);
            names.Add(source.name);
            names.AddRange(GetPathSegments(sourcePath));
        }

        GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(obj);
        if (root != null)
        {
            names.Add(root.name);
            GameObject rootSource = PrefabUtility.GetCorrespondingObjectFromSource(root);
            if (rootSource != null)
            {
                string rootSourcePath = AssetDatabase.GetAssetPath(rootSource);
                names.Add(rootSource.name);
                names.AddRange(GetPathSegments(rootSourcePath));
            }
        }

        Transform parent = obj.transform.parent;
        int hops = 0;
        while (parent != null && hops < 3)
        {
            names.Add(parent.name);
            parent = parent.parent;
            hops++;
        }

        FurnitureItem bestItem = null;
        int bestScore = 0;
        string bestName = "";

        foreach (string raw in names)
        {
            string clean = Clean(raw);
            if (string.IsNullOrEmpty(clean)) continue;

            foreach (FurnitureItem item in items)
            {
                int score = ScoreAgainstItem(clean, item);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestItem = item;
                    bestName = raw;
                }
            }
        }

        if (bestItem == null || bestScore <= 0) return MatchResult.None;
        return new MatchResult(bestItem, bestScore, bestName);
    }

    private static int ScoreAgainstItem(string cleanName, FurnitureItem item)
    {
        if (item == null) return 0;
        string itemName = Clean(item.itemName);
        string assetName = Clean(item.name);

        if (cleanName == itemName || cleanName == assetName) return 120;
        if (itemName.Length >= 4 && cleanName.Contains(itemName)) return 110;
        if (assetName.Length >= 4 && cleanName.Contains(assetName)) return 105;
        if (cleanName.Length >= 4 && itemName.Contains(cleanName)) return 80;

        int best = 0;
        foreach (KeyValuePair<string, string[]> group in Groups)
        {
            if (!ItemIsInGroup(itemName, assetName, group.Key, group.Value)) continue;

            foreach (string rawSynonym in group.Value)
            {
                string synonym = Clean(rawSynonym);
                if (synonym.Length < 3) continue;

                if (cleanName == synonym) best = Mathf.Max(best, 110);
                else if (synonym.Length >= 4 && cleanName.StartsWith(synonym)) best = Mathf.Max(best, 95);
                else if (synonym.Length >= 4 && cleanName.EndsWith(synonym)) best = Mathf.Max(best, 90);
                else if (synonym.Length >= 5 && cleanName.Contains(synonym)) best = Mathf.Max(best, 82);
            }
        }
        return best;
    }

    private static bool ItemIsInGroup(string itemName, string assetName, string key, string[] synonyms)
    {
        string cleanKey = Clean(key);
        if (itemName.Contains(cleanKey) || assetName.Contains(cleanKey)) return true;
        foreach (string rawSynonym in synonyms)
        {
            string synonym = Clean(rawSynonym);
            if (synonym.Length >= 4 && (itemName.Contains(synonym) || assetName.Contains(synonym))) return true;
        }
        return false;
    }

    private static readonly Dictionary<string, string[]> Groups = new Dictionary<string, string[]>
    {
        { "bed", new[] { "bed", "beds", "bunkbed", "mattress" } },
        { "chair", new[] { "chair", "chairs", "armchair", "stool", "seat" } },
        { "sofa", new[] { "sofa", "sofas", "couch", "couches", "loveseat" } },
        { "table", new[] { "table", "tables", "desk", "desks", "coffeetable", "coffee table", "sidetable", "side table", "endtable", "end table", "bedsidetable", "bedside table", "launchtable", "worktable", "nightstand" } },
        { "closet", new[] { "closet", "closets", "wardrobe", "wardrobes", "cabinet", "cabinets", "clotheshanger", "clothes hanger", "hanger" } },
        { "drawer", new[] { "drawer", "drawers", "dresser", "dressers", "chestdrawer", "chest of drawers" } },
        { "kitchen", new[] { "kitchen", "stove", "gasstove", "oven", "fridge", "refrigerator", "minifridge", "microwave", "sink", "faucet", "plate", "cup", "jar", "teapot", "kettle", "toaster", "iron", "ironingboard", "utensil", "cuttingboard" } },
        { "bathroom", new[] { "bathroom", "toilet", "bathtub", "bath", "shower", "vanity", "washbasin", "washstand", "soap", "soapdispenser", "towel", "tissue", "mixer" } },
        { "cushion", new[] { "cushion", "cushions", "cushioins", "pillow", "pillows" } },
        { "shelf", new[] { "shelf", "shelves", "rack", "racks", "shoerack", "storagebox", "storage box", "storage", "container", "containers", "box", "boxes" } },
        { "light", new[] { "light", "lights", "lamp", "lamps", "ceilinglight", "ceiling light", "roomlight", "room light", "desklight", "desk light", "mirrorlight", "cornerlight", "bedsidelight", "bedside light" } },
        { "mirror", new[] { "mirror", "mirrors" } },
        { "plant", new[] { "plant", "plants", "vase", "vases", "liana", "flower", "flowers", "smallplant" } },
    };

    private static string Clean(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        string s = raw;
        s = Regex.Replace(s, @"\s*\(\d+\)\s*$", "");       // Sofa42 (1) -> Sofa42
        s = Regex.Replace(s, @"^\([^)]{1,16}\)\s*", "");    // (Prb)Chair1 -> Chair1
        s = Regex.Replace(s, @"^(SO_|so_|PRB_|Prb_|HIP_)", "", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\d+$", "");                    // Chair07 -> Chair
        s = s.Replace("_", "").Replace("-", "").Replace(" ", "");
        return s.ToLowerInvariant();
    }

    private static List<string> ParseList(string text)
    {
        List<string> result = new List<string>();
        foreach (string raw in text.Split(',', '\n', '\r'))
        {
            string trimmed = raw.Trim();
            if (!string.IsNullOrEmpty(trimmed)) result.Add(trimmed);
        }
        return result;
    }

    private static IEnumerable<string> GetPathSegments(string path)
    {
        List<string> result = new List<string>();
        if (string.IsNullOrEmpty(path)) return result;
        string normalized = path.Replace('\\', '/');
        foreach (string part in normalized.Split('/'))
        {
            string segment = Path.GetFileNameWithoutExtension(part);
            if (!string.IsNullOrEmpty(segment)) result.Add(segment);
        }
        return result;
    }

    private static GameObject ResolveFurnitureTarget(GameObject obj)
    {
        GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(obj);
        if (root != null && root.scene == obj.scene && HasRenderer(root)) return root;
        return obj;
    }

    private static bool HasRenderer(GameObject obj)
    {
        return obj != null && obj.GetComponentInChildren<Renderer>(true) != null;
    }

    private static bool HasProcessedAncestor(Transform t, HashSet<GameObject> processed)
    {
        Transform current = t.parent;
        while (current != null)
        {
            if (processed.Contains(current.gameObject)) return true;
            current = current.parent;
        }
        return false;
    }

    private static bool LooksLikeContainer(GameObject obj, List<FurnitureItem> items)
    {
        if (obj.transform.childCount < 2) return false;

        string clean = Clean(obj.name);
        bool genericName = clean == "chair" || clean == "chairs" || clean == "bed" || clean == "beds" || clean == "sofa" || clean == "sofas" || clean == "table" || clean == "tables" || clean == "cushion" || clean == "cushions";
        if (!genericName) return false;

        int rendererChildren = 0;
        foreach (Transform child in obj.transform)
        {
            if (HasRenderer(child.gameObject)) rendererChildren++;
            if (rendererChildren >= 2) return true;
        }
        return false;
    }

    private static bool ShouldIgnore(GameObject obj)
    {
        if (obj == null) return true;
        if (obj.GetComponentInParent<Canvas>(true) != null) return true;
        if (obj.GetComponentInParent<UnityEngine.EventSystems.EventSystem>(true) != null) return true;
        if (obj.GetComponentInParent<Camera>(true) != null) return true;
        if (obj.GetComponentInParent<Light>(true) != null) return true;
        if (obj.GetComponentInParent<ShoppingCart3D>(true) != null) return true;

        string clean = Clean(obj.name);
        return clean.Contains("player") ||
               clean.Contains("manager") ||
               clean.Contains("eventsystem") ||
               clean.Contains("canvas") ||
               clean.Contains("camera") ||
               clean.Contains("directionallight") ||
               clean.Contains("shoppingcart") ||
               clean.Contains("cartzone") ||
               clean.Contains("sodazone") ||
               clean == "store" ||
               clean == "exit" ||
               clean == "enter" ||
               clean == "floor" ||
               clean == "ceiling" ||
               clean.Contains("wall") ||
               clean.StartsWith("cube");
    }

    private static void FitBoxCollider(GameObject obj, BoxCollider box)
    {
        if (obj == null || box == null) return;
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
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        EditorUtility.SetDirty(rb);
    }

    private static void SetLayerOnRelevantObjects(GameObject obj, int layer)
    {
        foreach (Transform child in obj.GetComponentsInChildren<Transform>(true))
        {
            if (child == null) continue;
            bool relevant = child == obj.transform || child.GetComponent<Renderer>() != null || child.GetComponent<Collider>() != null;
            if (!relevant) continue;
            if (child.gameObject.layer == layer) continue;
            Undo.RecordObject(child.gameObject, "Set Interactable layer");
            child.gameObject.layer = layer;
            EditorUtility.SetDirty(child.gameObject);
        }
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
