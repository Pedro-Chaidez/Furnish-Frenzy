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
        public float aggressiveMoveSpeed = 2f;
        public float aggressiveAttackDamage = 10f;
        public float cowardMoveSpeed = 3f;
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
        // Don't spawn NPCs if we are in the base/house scene
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

        Debug.Log("Loading: " + config.levelName +
                  " | Speed: " + config.aggressiveMoveSpeed +
                  " | Damage: " + config.aggressiveAttackDamage);

        // Spawn Aggressive NPCs and set their difficulty
        for (int i = 0; i < config.aggressiveNPCCount; i++)
        {
            GameObject npc = SpawnNPC(config.aggressiveNPCPrefab, config, i);
            if (npc != null)
            {
                AggressiveNPC script = npc.GetComponent<AggressiveNPC>();
                if (script != null)
                {
                    script.SetDifficulty(config.aggressiveMoveSpeed, config.aggressiveAttackDamage);
                }
            }
        }

        // Spawn Coward NPCs and set their difficulty
        for (int i = 0; i < config.cowardNPCCount; i++)
        {
            GameObject npc = SpawnNPC(config.cowardNPCPrefab, config, config.aggressiveNPCCount + i);
            if (npc != null)
            {
                CowardNPC script = npc.GetComponent<CowardNPC>();
                if (script != null)
                {
                    script.SetDifficulty(config.cowardMoveSpeed);
                }
            }
        }
    }

    // Notice we now return GameObject so we can configure it after spawning
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