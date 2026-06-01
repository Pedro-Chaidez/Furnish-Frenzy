// StoreItemDoctorForceFixer.cs
// Put this file in: Assets/Editor/StoreItemDoctorForceFixer.cs
// Menu: Tools > Furniture Frenzy > STOREITEM DOCTOR + Force Fix
//
// Why this exists:
// If you accidentally have another `public class StoreItem` inside Assets/Editor,
// normal editor tools may bind to the EDITOR version of StoreItem and Unity refuses
// to add it to furniture objects. This tool avoids that by finding the runtime
// StoreItem.cs outside Assets/Editor and adding THAT type by reflection.

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class StoreItemDoctorForceFixer : EditorWindow
{
    [Header("Batch Scenes")]
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
    private bool removeNestedStoreItems = true;
    private bool turnOffStaticOnStoreItems = true;

    private Vector2 scroll;
    private readonly List<string> log = new List<string>();

    private const int MinimumScore = 70;

    [MenuItem("Tools/Furniture Frenzy/STOREITEM DOCTOR + Force Fix")]
    public static void Open()
    {
        GetWindow<StoreItemDoctorForceFixer>("StoreItem Doctor");
    }

    private void OnGUI()
    {
        GUILayout.Label("StoreItem Doctor + Multi-Pack Force Fix", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Use Diagnose first. This tool force-adds the RUNTIME StoreItem script even if an accidental Editor-folder StoreItem duplicate exists.",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Batch Scene Paths", EditorStyles.boldLabel);
        scenePaths = EditorGUILayout.TextArea(scenePaths, GUILayout.Height(74));

        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);
        setInteractableLayer = EditorGUILayout.Toggle("Set Interactable Layer", setInteractableLayer);
        addColliderIfMissing = EditorGUILayout.Toggle("Add Collider If Missing", addColliderIfMissing);
        fitColliderToRenderers = EditorGUILayout.Toggle("Fit Collider To Renderers", fitColliderToRenderers);
        addRigidbodyIfMissing = EditorGUILayout.Toggle("Add Rigidbody If Missing", addRigidbodyIfMissing);
        removeNestedStoreItems = EditorGUILayout.Toggle("Remove Nested StoreItems", removeNestedStoreItems);
        turnOffStaticOnStoreItems = EditorGUILayout.Toggle("Turn Off Static On Store Items", turnOffStaticOnStoreItems);

        EditorGUILayout.Space();

        if (GUILayout.Button("1. Diagnose StoreItem Scripts", GUILayout.Height(34)))
            DiagnoseStoreItemScripts();

        if (GUILayout.Button("2. Fix SELECTED Object(s) Only", GUILayout.Height(34)))
            FixSelectedObjects();

        if (GUILayout.Button("3. Fix CURRENT Open Scene", GUILayout.Height(34)))
            FixCurrentOpenScene(true);

        if (GUILayout.Button("4. Fix LISTED Scenes", GUILayout.Height(34)))
            FixListedScenes();

        if (GUILayout.Button("Clear Log"))
            log.Clear();

        EditorGUILayout.Space();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (string line in log)
        {
            Color old = GUI.color;
            if (line.StartsWith("OK")) GUI.color = Color.green;
            else if (line.StartsWith("ERROR")) GUI.color = Color.red;
            else if (line.StartsWith("WARN")) GUI.color = Color.yellow;
            else if (line.StartsWith("--")) GUI.color = Color.cyan;
            else if (line.StartsWith("FOUND")) GUI.color = Color.white;
            EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
            GUI.color = old;
        }
        EditorGUILayout.EndScrollView();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Diagnose / runtime StoreItem type resolution
    // ─────────────────────────────────────────────────────────────────────

    private void DiagnoseStoreItemScripts()
    {
        log.Clear();
        Type runtimeType = ResolveRuntimeStoreItemType(true);
        if (runtimeType == null)
        {
            log.Add("ERROR No usable runtime StoreItem was found. You need exactly one StoreItem.cs outside Assets/Editor.");
        }
        else
        {
            log.Add("OK Runtime StoreItem chosen: " + runtimeType.FullName + " from assembly " + runtimeType.Assembly.GetName().Name);
        }

        List<FurnitureItem> items = LoadFurnitureItems();
        log.Add("-- FurnitureItem ScriptableObjects found: " + items.Count);
        foreach (FurnitureItem item in items)
            log.Add("FOUND FurnitureItem: " + AssetDatabase.GetAssetPath(item) + " | itemName='" + item.itemName + "'");
    }

    private Type ResolveRuntimeStoreItemType(bool print)
    {
        Type chosen = null;
        string chosenPath = "";
        int runtimeCount = 0;
        int editorCount = 0;

        string[] guids = AssetDatabase.FindAssets("t:MonoScript");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (script == null) continue;

            Type type = script.GetClass();
            if (type == null) continue;
            if (type.Name != "StoreItem") continue;
            if (!typeof(MonoBehaviour).IsAssignableFrom(type)) continue;

            bool isEditorPath = IsEditorPath(path);
            bool isEditorAssembly = type.Assembly.GetName().Name.ToLowerInvariant().Contains("editor");
            bool isEditor = isEditorPath || isEditorAssembly;

            if (print)
            {
                log.Add((isEditor ? "WARN" : "OK") + " StoreItem script found: " + path +
                        " | assembly=" + type.Assembly.GetName().Name +
                        " | " + (isEditor ? "EDITOR script - cannot be added to scene objects" : "RUNTIME script"));
            }

            if (isEditor)
            {
                editorCount++;
                continue;
            }

            runtimeCount++;
            if (chosen == null)
            {
                chosen = type;
                chosenPath = path;
            }
        }

        if (print)
        {
            if (editorCount > 0)
                log.Add("WARN You still have " + editorCount + " StoreItem class(es) in Editor scripts. Delete or rename those later. This Doctor tool will ignore them for now.");
            if (runtimeCount == 0)
                log.Add("ERROR No runtime StoreItem class found outside Assets/Editor.");
            else if (runtimeCount > 1)
                log.Add("WARN More than one runtime StoreItem was found. Keep only one if weird behavior continues.");
            if (chosen != null)
                log.Add("OK Using runtime StoreItem from: " + chosenPath);
        }

        return chosen;
    }

    private static bool IsEditorPath(string path)
    {
        string p = path.Replace('\\', '/').ToLowerInvariant();
        return p.Contains("/editor/") || p.EndsWith("/editor");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Fix buttons
    // ─────────────────────────────────────────────────────────────────────

    private void FixSelectedObjects()
    {
        log.Clear();
        Type storeItemType = ResolveRuntimeStoreItemType(true);
        if (storeItemType == null) return;

        List<FurnitureItem> furnitureItems = LoadFurnitureItems();
        if (furnitureItems.Count == 0)
        {
            log.Add("ERROR No FurnitureItem ScriptableObjects found.");
            return;
        }

        int fixedCount = 0;
        foreach (GameObject selected in Selection.gameObjects)
        {
            if (selected == null) continue;
            GameObject target = ResolveFurnitureTarget(selected);
            if (target == null) target = selected;

            MatchResult match = FindBestFurnitureItem(target, furnitureItems);
            if (!match.IsValid)
            {
                log.Add("WARN No FurnitureItem match for selected object: " + selected.name);
                continue;
            }

            if (FixOneTarget(target, match, storeItemType)) fixedCount++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        log.Add("OK Selected fix complete. Fixed " + fixedCount + " object(s).");
    }

    private void FixCurrentOpenScene(bool clearLog)
    {
        if (clearLog) log.Clear();

        Type storeItemType = ResolveRuntimeStoreItemType(true);
        if (storeItemType == null) return;

        List<FurnitureItem> furnitureItems = LoadFurnitureItems();
        if (furnitureItems.Count == 0)
        {
            log.Add("ERROR No FurnitureItem ScriptableObjects found.");
            return;
        }

        log.Add("-- Scene: " + SceneManager.GetActiveScene().name + " --");
        log.Add("-- Loaded " + furnitureItems.Count + " FurnitureItem ScriptableObject(s). --");

        List<GameObject> sceneObjects = GetSceneObjects();
        HashSet<GameObject> processed = new HashSet<GameObject>();
        int fixedCount = 0;
        int skippedNoMatch = 0;

        foreach (GameObject obj in sceneObjects)
        {
            if (obj == null) continue;
            if (ShouldIgnore(obj)) continue;
            if (!HasVisualOrCollider(obj)) continue;

            MatchResult match = FindBestFurnitureItem(obj, furnitureItems);
            if (!match.IsValid)
            {
                skippedNoMatch++;
                continue;
            }

            GameObject target = ResolveFurnitureTarget(obj);
            if (target == null || ShouldIgnore(target)) continue;
            if (processed.Contains(target)) continue;
            if (HasProcessedAncestor(target.transform, processed)) continue;

            processed.Add(target);
            if (FixOneTarget(target, match, storeItemType)) fixedCount++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        log.Add("OK Scene fix complete. Fixed " + fixedCount + " object(s). Skipped/no match: " + skippedNoMatch + ".");
    }

    private void FixListedScenes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        log.Clear();
        List<string> paths = ParseCommaList(scenePaths);
        if (paths.Count == 0)
        {
            log.Add("ERROR No scene paths listed.");
            return;
        }

        foreach (string path in paths)
        {
            if (!File.Exists(path))
            {
                log.Add("ERROR Scene not found: " + path);
                continue;
            }

            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            log.Add("");
            log.Add("-- Batch fixing: " + path + " --");
            FixCurrentOpenScene(false);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        log.Add("OK Batch scene fix finished.");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Core object fixing
    // ─────────────────────────────────────────────────────────────────────

    private bool FixOneTarget(GameObject target, MatchResult match, Type storeItemType)
    {
        if (target == null || match.Item == null) return false;

        Undo.RegisterFullObjectHierarchyUndo(target, "Fix StoreItem Furniture");

        Component storeItem = target.GetComponent(storeItemType);
        if (storeItem == null)
        {
            storeItem = Undo.AddComponent(target, storeItemType);
        }

        if (storeItem == null)
        {
            log.Add("ERROR Unity still refused to add the runtime StoreItem to '" + target.name + "'. Run Diagnose and delete/rename any StoreItem class inside Assets/Editor.");
            return false;
        }

        SerializedObject serializedStoreItem = new SerializedObject(storeItem);

        SerializedProperty itemDataProp = serializedStoreItem.FindProperty("itemData");
        if (itemDataProp != null)
            itemDataProp.objectReferenceValue = match.Item;
        else
            log.Add("WARN StoreItem on '" + target.name + "' has no itemData field/property.");

        SerializedProperty promptProp = serializedStoreItem.FindProperty("promptMessage");
        if (promptProp != null)
            promptProp.stringValue = "Press E to add " + match.Item.itemName + " to cart";

        // Leave cartInventory empty; your StoreItem already finds CartInventory.Instance at runtime.
        serializedStoreItem.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(storeItem);

        if (addRigidbodyIfMissing)
        {
            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb == null) rb = Undo.AddComponent<Rigidbody>(target);
            if (rb != null)
            {
                rb.mass = 1f;
                rb.useGravity = true;
                rb.isKinematic = false;
                rb.linearDamping = 0.5f;
                rb.angularDamping = 0.5f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                EditorUtility.SetDirty(rb);
            }
        }

        if (addColliderIfMissing && target.GetComponent<Collider>() == null)
        {
            BoxCollider box = Undo.AddComponent<BoxCollider>(target);
            if (box != null && fitColliderToRenderers)
                FitBoxColliderToRenderers(target, box);
        }
        else if (fitColliderToRenderers)
        {
            BoxCollider existingBox = target.GetComponent<BoxCollider>();
            if (existingBox != null) FitBoxColliderToRenderers(target, existingBox);
        }

        if (setInteractableLayer)
        {
            int layer = LayerMask.NameToLayer("Interactable");
            if (layer < 0)
            {
                log.Add("WARN Layer named 'Interactable' does not exist. Create it in Project Settings > Tags and Layers.");
            }
            else
            {
                SetLayerOnObjectAndColliderChildren(target, layer);
            }
        }

        if (removeNestedStoreItems)
            RemoveNestedStoreItems(target, storeItemType);

        if (turnOffStaticOnStoreItems)
        {
            target.isStatic = false;
            foreach (Transform child in target.GetComponentsInChildren<Transform>(true))
                child.gameObject.isStatic = false;
        }

        EditorUtility.SetDirty(target);
        log.Add("OK " + target.name + " -> " + match.Item.itemName + " (score " + match.Score + ", " + match.Reason + ")");
        return true;
    }

    private static void RemoveNestedStoreItems(GameObject target, Type storeItemType)
    {
        Component[] components = target.GetComponentsInChildren(storeItemType, true);
        foreach (Component c in components)
        {
            if (c == null) continue;
            if (c.gameObject == target) continue;
            Undo.DestroyObjectImmediate(c);
        }
    }

    private static void FitBoxColliderToRenderers(GameObject obj, BoxCollider box)
    {
        if (obj == null || box == null) return;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        box.center = obj.transform.InverseTransformPoint(bounds.center);
        Vector3 localSize = obj.transform.InverseTransformVector(bounds.size);
        box.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        box.isTrigger = false;
        EditorUtility.SetDirty(box);
    }

    private static void SetLayerOnObjectAndColliderChildren(GameObject obj, int layer)
    {
        Transform[] children = obj.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            bool shouldSet = child == obj.transform ||
                             child.GetComponent<Collider>() != null ||
                             child.GetComponent<Renderer>() != null;
            if (!shouldSet) continue;
            child.gameObject.layer = layer;
            EditorUtility.SetDirty(child.gameObject);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Scene scanning
    // ─────────────────────────────────────────────────────────────────────

    private List<GameObject> GetSceneObjects()
    {
        List<GameObject> result = new List<GameObject>();
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in all)
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

    private static bool HasVisualOrCollider(GameObject obj)
    {
        if (obj == null) return false;
        return obj.GetComponentInChildren<Renderer>(true) != null || obj.GetComponentInChildren<Collider>(true) != null;
    }

    private static GameObject ResolveFurnitureTarget(GameObject obj)
    {
        if (obj == null) return null;

        GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(obj);
        if (root != null && root.scene == obj.scene && HasVisualOrCollider(root))
            return root;

        return obj;
    }

    private static bool HasProcessedAncestor(Transform transform, HashSet<GameObject> processed)
    {
        Transform current = transform.parent;
        while (current != null)
        {
            if (processed.Contains(current.gameObject)) return true;
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

    private static bool ShouldIgnore(GameObject obj)
    {
        if (obj == null) return true;
        if (obj.GetComponentInParent<Canvas>(true) != null) return true;
        if (obj.GetComponentInParent<UnityEngine.EventSystems.EventSystem>(true) != null) return true;
        if (obj.GetComponentInParent<Camera>(true) != null) return true;
        if (obj.GetComponentInParent<Light>(true) != null) return true;

        string clean = CleanName(obj.name);
        if (clean.Contains("player")) return true;
        if (clean.Contains("shoppingcart")) return true;
        if (clean.Contains("cartcanvas")) return true;
        if (clean.Contains("manager")) return true;
        if (clean.Contains("eventsystem")) return true;
        if (clean.Contains("camera")) return true;
        if (clean == "store" || clean == "floor" || clean == "ceiling" || clean.Contains("wall")) return true;
        if (clean.Contains("zone")) return true;
        if (clean == "cube") return true;

        return false;
    }

    // ─────────────────────────────────────────────────────────────────────
    // FurnitureItem matching
    // ─────────────────────────────────────────────────────────────────────

    private List<FurnitureItem> LoadFurnitureItems()
    {
        List<FurnitureItem> result = new List<FurnitureItem>();
        string[] guids = AssetDatabase.FindAssets("t:FurnitureItem");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            FurnitureItem item = AssetDatabase.LoadAssetAtPath<FurnitureItem>(path);
            if (item != null) result.Add(item);
        }
        return result;
    }

    private MatchResult FindBestFurnitureItem(GameObject obj, List<FurnitureItem> items)
    {
        List<string> names = GetMatchNames(obj);
        FurnitureItem best = null;
        int bestScore = 0;
        string bestReason = "";
        bool tied = false;

        foreach (string rawName in names)
        {
            string cleanObjectName = CleanName(rawName);
            if (string.IsNullOrEmpty(cleanObjectName)) continue;

            foreach (FurnitureItem item in items)
            {
                int score = ScoreAgainstItem(cleanObjectName, item);
                if (score > bestScore)
                {
                    best = item;
                    bestScore = score;
                    bestReason = rawName;
                    tied = false;
                }
                else if (score == bestScore && score > 0 && best != item)
                {
                    tied = true;
                }
            }
        }

        if (best == null || bestScore < MinimumScore || tied)
            return MatchResult.None;

        return new MatchResult(best, bestScore, "matched name '" + bestReason + "'");
    }

    private static List<string> GetMatchNames(GameObject obj)
    {
        List<string> names = new List<string>();
        if (obj == null) return names;

        names.Add(obj.name);

        GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(obj);
        if (source != null)
        {
            names.Add(source.name);
            string sourcePath = AssetDatabase.GetAssetPath(source);
            names.AddRange(GetPathSegments(sourcePath));
        }

        GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(obj);
        if (root != null)
        {
            names.Add(root.name);
            GameObject rootSource = PrefabUtility.GetCorrespondingObjectFromSource(root);
            if (rootSource != null)
            {
                names.Add(rootSource.name);
                string rootSourcePath = AssetDatabase.GetAssetPath(rootSource);
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

        return names;
    }

    private static IEnumerable<string> GetPathSegments(string path)
    {
        List<string> result = new List<string>();
        if (string.IsNullOrEmpty(path)) return result;

        string normalized = path.Replace('\\', '/');
        foreach (string segment in normalized.Split('/'))
        {
            string withoutExt = Path.GetFileNameWithoutExtension(segment);
            if (!string.IsNullOrEmpty(withoutExt)) result.Add(withoutExt);
        }
        return result;
    }

    private static int ScoreAgainstItem(string cleanObjectName, FurnitureItem item)
    {
        if (item == null) return 0;

        string cleanItemName = CleanName(item.itemName);
        string cleanAssetName = CleanName(item.name);

        if (!string.IsNullOrEmpty(cleanItemName))
        {
            if (cleanObjectName == cleanItemName) return 130;
            if (cleanObjectName.Contains(cleanItemName) && cleanItemName.Length >= 4) return 115;
            if (cleanItemName.Contains(cleanObjectName) && cleanObjectName.Length >= 4) return 105;
        }

        if (!string.IsNullOrEmpty(cleanAssetName))
        {
            if (cleanObjectName == cleanAssetName) return 125;
            if (cleanObjectName.Contains(cleanAssetName) && cleanAssetName.Length >= 4) return 110;
            if (cleanAssetName.Contains(cleanObjectName) && cleanObjectName.Length >= 4) return 100;
        }

        string itemGroup = GetItemGroup(item);
        if (string.IsNullOrEmpty(itemGroup)) return 0;

        string[] synonyms = KeywordGroups[itemGroup];
        foreach (string raw in synonyms)
        {
            string synonym = CleanName(raw);
            if (string.IsNullOrEmpty(synonym)) continue;

            if (cleanObjectName == synonym) return 120;
            if (cleanObjectName.StartsWith(synonym) && synonym.Length >= 3) return 110;
            if (cleanObjectName.EndsWith(synonym) && synonym.Length >= 4) return 95;
            if (cleanObjectName.Contains(synonym) && synonym.Length >= 5) return 85;
        }

        return 0;
    }

    private static string GetItemGroup(FurnitureItem item)
    {
        string combined = CleanName(item.itemName + " " + item.name);
        foreach (KeyValuePair<string, string[]> group in KeywordGroups)
        {
            string key = CleanName(group.Key);
            if (combined.Contains(key)) return group.Key;

            foreach (string rawSyn in group.Value)
            {
                string syn = CleanName(rawSyn);
                if (syn.Length >= 4 && combined.Contains(syn)) return group.Key;
            }
        }
        return "";
    }

    private static string CleanName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";

        string value = raw;
        value = value.Replace("(Clone)", "");
        value = Regex.Replace(value, @"\s*\(\d+\)\s*$", "");       // Sofa42 (1) -> Sofa42
        value = Regex.Replace(value, @"^\([^)]{1,20}\)\s*", "");    // (Prb)Sofa -> Sofa
        value = Regex.Replace(value, @"^(SO_|so_|Prb_|PRB_|HIP_)", "", RegexOptions.IgnoreCase);
        value = Regex.Replace(value, @"\d+$", "");                  // Sofa42 -> Sofa
        value = value.Replace("_", "").Replace("-", "").Replace(" ", "");
        return value.ToLowerInvariant().Trim();
    }

    private static List<string> ParseCommaList(string input)
    {
        List<string> result = new List<string>();
        foreach (string raw in input.Split(','))
        {
            string trimmed = raw.Trim();
            if (!string.IsNullOrEmpty(trimmed)) result.Add(trimmed);
        }
        return result;
    }

    private static readonly Dictionary<string, string[]> KeywordGroups = new Dictionary<string, string[]>
    {
        { "bathroom", new[] { "bathroom", "bath", "bathtub", "tub", "toilet", "shower", "sink", "washbasin", "washstand", "vanity", "faucet", "soap", "soapdispenser", "towel", "tissue" } },
        { "bed",      new[] { "bed", "beds", "bunkbed", "mattress" } },
        { "chair",    new[] { "chair", "chairs", "stool", "bench" } },
        { "closet",   new[] { "closet", "closets", "wardrobe", "wardrobes", "clotheshanger", "hanger", "clothes" } },
        { "drawer",   new[] { "drawer", "drawers", "dresser", "dressers", "nightstand" } },
        { "kitchen",  new[] { "kitchen", "stove", "gasstove", "oven", "kitchenoven", "microwave", "fridge", "refrigerator", "minifridge", "plate", "cup", "jar", "teapot", "kettle", "toaster", "iron", "ironingboard", "cuttingboard", "utensils", "cabinet" } },
        { "cushion",  new[] { "cushion", "cushions", "cushioins", "pillow", "pillows" } },
        { "sofa",     new[] { "sofa", "sofas", "couch", "couches", "armchair", "loveseat" } },
        { "table",    new[] { "table", "tables", "desk", "desks", "coffeetable", "coffetable", "sidetable", "endtable", "bedsidetable", "launchtable", "worktable" } },
        { "shelf",    new[] { "shelf", "shelves", "rack", "racks", "shoerack", "storagebox", "storage", "container", "containers", "box", "boxes" } },
        { "light",    new[] { "light", "lights", "lamp", "lamps", "ceilinglight", "roomlight", "desklight", "mirrorlight", "cornerlight", "bedsidelight" } },
        { "mirror",   new[] { "mirror", "mirrors" } },
        { "plant",    new[] { "plant", "plants", "vase", "vases", "liana", "flower" } },
        { "decor",    new[] { "painting", "bottle", "vinyl", "vinylplayer", "pcmonitor", "monitor", "keyboard", "mouse", "tshirt", "shirt" } },
    };

    private readonly struct MatchResult
    {
        public static readonly MatchResult None = new MatchResult(null, 0, "");
        public readonly FurnitureItem Item;
        public readonly int Score;
        public readonly string Reason;
        public bool IsValid => Item != null && Score > 0;

        public MatchResult(FurnitureItem item, int score, string reason)
        {
            Item = item;
            Score = score;
            Reason = reason;
        }
    }
}
#endif
