using UnityEngine;
using UnityEditor;
using System.IO;

public class FurnitureMegaPackPhysicsSetup : EditorWindow
{
    private string folderPath = "Assets/Furniture Mega Pack";
    private bool addRigidbody = true;
    private bool removeOldColliders = true;

    [MenuItem("Tools/Furniture Mega Pack Physics Setup")]
    public static void ShowWindow()
    {
        GetWindow<FurnitureMegaPackPhysicsSetup>("Furniture Physics Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Furniture Mega Pack Physics Setup", EditorStyles.boldLabel);

        folderPath = EditorGUILayout.TextField("Prefab Folder Path", folderPath);
        addRigidbody = EditorGUILayout.Toggle("Add Rigidbody", addRigidbody);
        removeOldColliders = EditorGUILayout.Toggle("Remove Old Colliders", removeOldColliders);

        if (GUILayout.Button("Setup All Prefabs In Folder"))
        {
            SetupPrefabsInFolder();
        }
    }

    private void SetupPrefabsInFolder()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            SetupObject(prefabRoot);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            Debug.Log("Updated prefab: " + prefabPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Finished setting up furniture prefabs.");
    }

    private void SetupObject(GameObject obj)
    {
        if (removeOldColliders)
        {
            Collider[] oldColliders = obj.GetComponentsInChildren<Collider>();

            foreach (Collider col in oldColliders)
            {
                DestroyImmediate(col);
            }
        }

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Debug.LogWarning(obj.name + " has no renderers. Skipping.");
            return;
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        BoxCollider box = obj.GetComponent<BoxCollider>();

        if (box == null)
        {
            box = obj.AddComponent<BoxCollider>();
        }

        Vector3 localCenter = obj.transform.InverseTransformPoint(bounds.center);
        Vector3 localSize = obj.transform.InverseTransformVector(bounds.size);

        box.center = localCenter;
        box.size = new Vector3(
            Mathf.Abs(localSize.x),
            Mathf.Abs(localSize.y),
            Mathf.Abs(localSize.z)
        );

        box.isTrigger = false;

        if (addRigidbody)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();

            if (rb == null)
            {
                rb = obj.AddComponent<Rigidbody>();
            }

            rb.mass = 1f;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        EditorUtility.SetDirty(obj);
    }
}