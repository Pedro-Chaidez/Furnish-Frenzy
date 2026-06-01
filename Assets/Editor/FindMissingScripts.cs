using UnityEngine;
using UnityEditor;

public class FindMissingScripts : EditorWindow
{
    [MenuItem("Tools/Find Missing Scripts In Scene")]
    static void FindMissingScriptsInScene()
    {
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);

        int missingCount = 0;

        foreach (GameObject obj in allObjects)
        {
            Component[] components = obj.GetComponents<Component>();

            foreach (Component component in components)
            {
                if (component == null)
                {
                    Debug.LogWarning("Missing script found on: " + GetFullPath(obj), obj);
                    missingCount++;
                }
            }
        }

        Debug.Log("Done searching. Missing scripts found: " + missingCount);
    }

    private static string GetFullPath(GameObject obj)
    {
        string path = obj.name;

        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = obj.name + "/" + path;
        }

        return path;
    }
}