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
 
    [Header("Difficulty Settings")]
    public float aggressiveMoveSpeed    = 2f;
    public float aggressiveAttackDamage = 10f;
    public float cowardMoveSpeed        = 3f;
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
        new LevelConfig {
            levelName = "Level 1",
            aggressiveNPCCount = 1, cowardNPCCount = 1,
            aggressiveMoveSpeed = 2f, aggressiveAttackDamage = 5f, cowardMoveSpeed = 3f
        },
        new LevelConfig {
            levelName = "Level 2",
            aggressiveNPCCount = 2, cowardNPCCount = 2,
            aggressiveMoveSpeed = 3f, aggressiveAttackDamage = 8f, cowardMoveSpeed = 4f
        },
        new LevelConfig {
            levelName = "Level 3",
            aggressiveNPCCount = 3, cowardNPCCount = 2,
            aggressiveMoveSpeed = 4f, aggressiveAttackDamage = 12f, cowardMoveSpeed = 5f
        },
        new LevelConfig {
            levelName = "Level 4",
            aggressiveNPCCount = 4, cowardNPCCount = 3,
            aggressiveMoveSpeed = 5f, aggressiveAttackDamage = 16f, cowardMoveSpeed = 6f
        },
        new LevelConfig {
            levelName = "Level 5",
            aggressiveNPCCount = 5, cowardNPCCount = 3,
            aggressiveMoveSpeed = 6f, aggressiveAttackDamage = 20f, cowardMoveSpeed = 7f
        },
    };
 
    private int currentLevelIndex = 0;
    private List<GameObject> spawnedNPCs = new List<GameObject>();
 
    private void Start()
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
        if (levelIndex >= levels.Count)
        {
            Debug.Log("You have completed all levels!");
            return;
        }
 
        ClearNPCs();
 
        currentLevelIndex = levelIndex;
        LevelConfig config = levels[currentLevelIndex];
 
        Debug.Log("Loading: " + config.levelName +
                  " | Speed: " + config.aggressiveMoveSpeed +
                  " | Damage: " + config.aggressiveAttackDamage);
 
        for (int i = 0; i < config.aggressiveNPCCount; i++)
        {
            GameObject npc = SpawnNPC(config.aggressiveNPCPrefab, config, i);
            if (npc != null)
            {
                AggressiveNPC script = npc.GetComponent<AggressiveNPC>();
                if (script != null)
                    script.SetDifficulty(config.aggressiveMoveSpeed, config.aggressiveAttackDamage);
            }
        }
 
        for (int i = 0; i < config.cowardNPCCount; i++)
        {
            GameObject npc = SpawnNPC(config.cowardNPCPrefab, config, config.aggressiveNPCCount + i);
            if (npc != null)
            {
                CowardNPC script = npc.GetComponent<CowardNPC>();
                if (script != null)
                    script.SetDifficulty(config.cowardMoveSpeed);
            }
        }
    }
 
    private GameObject SpawnNPC(GameObject prefab, LevelConfig config, int spawnIndex)
    {
        if (prefab == null)
        {
            Debug.LogWarning("NPC prefab not assigned in Inspector for " + config.levelName);
            return null;
        }
 
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
        return npc;
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