#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class BatchSetLayer : EditorWindow
{
    private string layerName = "Interactable";
    private bool includeChildren = true;

    [MenuItem("Tools/Furniture Frenzy/Batch Set Layer")]
    public static void ShowWindow()
    {
        GetWindow<BatchSetLayer>("Batch Set Layer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Batch Set Layer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        layerName = EditorGUILayout.TextField("Layer Name", layerName);
        includeChildren = EditorGUILayout.Toggle("Include Children", includeChildren);

        EditorGUILayout.HelpBox(
            "Select objects in the Hierarchy, then click the button. This tool only sets layers. It does not define StoreItem.",
            MessageType.Info);

        if (GUILayout.Button("Set Selected Objects To Layer", GUILayout.Height(36)))
        {
            SetSelectedObjectsToLayer();
        }
    }

    private void SetSelectedObjectsToLayer()
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
        {
            Debug.LogError($"Layer '{layerName}' does not exist. Create it in Project Settings > Tags and Layers first.");
            return;
        }

        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            Debug.LogWarning("No GameObjects selected.");
            return;
        }

        int changed = 0;
        foreach (GameObject selectedObject in selectedObjects)
        {
            if (selectedObject == null) continue;

            if (includeChildren)
            {
                foreach (Transform child in selectedObject.GetComponentsInChildren<Transform>(true))
                {
                    Undo.RecordObject(child.gameObject, "Batch Set Layer");
                    child.gameObject.layer = layer;
                    EditorUtility.SetDirty(child.gameObject);
                    changed++;
                }
            }
            else
            {
                Undo.RecordObject(selectedObject, "Batch Set Layer");
                selectedObject.layer = layer;
                EditorUtility.SetDirty(selectedObject);
                changed++;
            }
        }

        Debug.Log($"BatchSetLayer: Set {changed} object(s) to layer '{layerName}'.");
    }
}
#endif
