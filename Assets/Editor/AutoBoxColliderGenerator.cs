using UnityEngine;
using UnityEditor;

public class AutoBoxColliderGenerator : EditorWindow
{
    private float shrinkAmount = 0.95f;
    private float liftAmount = 0.05f;
    private bool addRigidbody = true;

    [MenuItem("Tools/Auto Box Collider Generator")]
    public static void ShowWindow()
    {
        GetWindow<AutoBoxColliderGenerator>("Auto Collider Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Auto Box Collider Generator", EditorStyles.boldLabel);

        shrinkAmount = EditorGUILayout.Slider("Collider Shrink", shrinkAmount, 0.8f, 1f);
        liftAmount = EditorGUILayout.FloatField("Lift Object Amount", liftAmount);
        addRigidbody = EditorGUILayout.Toggle("Add Rigidbody", addRigidbody);

        if (GUILayout.Button("Fix Selected Objects"))
        {
            FixSelectedObjects();
        }
    }

    private void FixSelectedObjects()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            if (obj == null) continue;

            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
            {
                Debug.LogWarning(obj.name + " has no renderers.");
                continue;
            }

            Bounds worldBounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
            {
                worldBounds.Encapsulate(renderers[i].bounds);
            }

            BoxCollider box = obj.GetComponent<BoxCollider>();

            if (box == null)
                box = obj.AddComponent<BoxCollider>();

            Vector3 localCenter = obj.transform.InverseTransformPoint(worldBounds.center);
            Vector3 localSize = obj.transform.InverseTransformVector(worldBounds.size);

            localSize = new Vector3(
                Mathf.Abs(localSize.x) * shrinkAmount,
                Mathf.Abs(localSize.y) * shrinkAmount,
                Mathf.Abs(localSize.z) * shrinkAmount
            );

            box.center = localCenter;
            box.size = localSize;
            box.isTrigger = false;

            if (addRigidbody)
            {
                Rigidbody rb = obj.GetComponent<Rigidbody>();

                if (rb == null)
                    rb = obj.AddComponent<Rigidbody>();

                rb.mass = 1f;
                rb.useGravity = true;
                rb.isKinematic = false;
                rb.linearDamping = 0.5f;
                rb.angularDamping = 1f;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }

            obj.transform.position += Vector3.up * liftAmount;

            EditorUtility.SetDirty(obj);

            Debug.Log("Fixed collider/rigidbody for: " + obj.name);
        }

        AssetDatabase.SaveAssets();
    }
}