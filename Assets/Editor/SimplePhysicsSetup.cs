using UnityEngine;
using UnityEditor;

public class SimplePhysicsSetup : EditorWindow
{
    private bool addRigidbody = true;
    private bool removeOldColliders = true;

    [MenuItem("Tools/Simple Physics Setup")]
    public static void ShowWindow()
    {
        GetWindow<SimplePhysicsSetup>("Simple Physics Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Add Rigidbody + Box Collider", EditorStyles.boldLabel);

        addRigidbody = EditorGUILayout.Toggle("Add Rigidbody", addRigidbody);
        removeOldColliders = EditorGUILayout.Toggle("Remove Old Colliders", removeOldColliders);

        if (GUILayout.Button("Setup Selected Objects"))
        {
            SetupObjects();
        }
    }

    private void SetupObjects()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            if (obj == null)
                continue;

            // Remove old colliders if enabled
            if (removeOldColliders)
            {
                Collider[] oldColliders = obj.GetComponents<Collider>();

                foreach (Collider c in oldColliders)
                {
                    DestroyImmediate(c);
                }
            }

            // Add Box Collider if missing
            BoxCollider box = obj.GetComponent<BoxCollider>();

            if (box == null)
            {
                box = obj.AddComponent<BoxCollider>();
            }

            // Auto-fit collider to renderer bounds
            Renderer rend = obj.GetComponentInChildren<Renderer>();

            if (rend != null)
            {
                Bounds bounds = rend.bounds;

                Vector3 localCenter = obj.transform.InverseTransformPoint(bounds.center);
                Vector3 localSize = obj.transform.InverseTransformVector(bounds.size);

                box.center = localCenter;
                box.size = new Vector3(
                    Mathf.Abs(localSize.x),
                    Mathf.Abs(localSize.y),
                    Mathf.Abs(localSize.z)
                );
            }

            box.isTrigger = false;

            // Add Rigidbody if enabled
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

            Debug.Log("Setup complete for: " + obj.name);
        }

        AssetDatabase.SaveAssets();
    }
}