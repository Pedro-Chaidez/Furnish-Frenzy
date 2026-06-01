using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
public static LevelManager Instance;

[System.Serializable]
public class LevelConfig
{
    public string levelName; 
    public string sceneToLoad; 
    public GameObject aggressiveNPCPrefab;
    public GameObject cowardNPCPrefab;
    public int aggressiveNPCCount;
    public int cowardNPCCount;
    public Transform[] spawnPoints;
}

public List<LevelConfig> levels = new List<LevelConfig>();

private int currentLevelIndex = 0;
private List<GameObject> spawnedNPCs = new List<GameObject>();

private void Awake()
{
    if (Instance == null)
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    else
    {
        Destroy(gameObject);
    }
}

private void OnDestroy()
{
    if (Instance == this)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}

public void LoadNextStore()
{
    if (levels.Count == 0 || currentLevelIndex >= levels.Count)
    {
        Debug.Log("All levels completed!");
        return;
    }

    string nextScene = levels[currentLevelIndex].sceneToLoad;

    string saveKey = "SceneData_" + nextScene;
    PlayerPrefs.DeleteKey(saveKey);

    SceneManager.LoadScene(nextScene);

    currentLevelIndex++;
}

private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
{
    if (scene.name != "House")
    {
        SpawnNPCsForCurrentScene(scene.name);
    }
}

private void SpawnNPCsForCurrentScene(string currentSceneName)
{
    ClearNPCs();

    LevelConfig config = levels.Find(x => x.sceneToLoad == currentSceneName);
    if (config == null) return;

    for (int i = 0; i < config.aggressiveNPCCount; i++)
    {
        SpawnNPC(config.aggressiveNPCPrefab, config, i);
    }
    for (int i = 0; i < config.cowardNPCCount; i++)
    {
        SpawnNPC(config.cowardNPCPrefab, config, config.aggressiveNPCCount + i);
    }
}

private void SpawnNPC(GameObject prefab, LevelConfig config, int spawnIndex)
{
    if (prefab == null) return;

    Vector3 spawnPos = transform.position;
    Quaternion spawnRot = Quaternion.identity;

    if (config.spawnPoints != null && config.spawnPoints.Length > 0)
    {
        Transform point = config.spawnPoints[spawnIndex % config.spawnPoints.Length];
        spawnPos = point.position;
        spawnRot = point.rotation;
    }

    GameObject npc = Instantiate(prefab, spawnPos, spawnRot);
    spawnedNPCs.Add(npc);
}

private void ClearNPCs()
{
    foreach (GameObject npc in spawnedNPCs)
    {
        if (npc != null) Destroy(npc);
    }
    spawnedNPCs.Clear();
}
}