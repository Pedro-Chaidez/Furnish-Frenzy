using UnityEngine;
using System.Collections.Generic;
 
[System.Serializable]
public class LevelConfig
{
    public string levelName;
 
    public GameObject aggressiveNPCPrefab;
    public GameObject cowardNPCPrefab;
 
    public int aggressiveNPCCount;
    public int cowardNPCCount;
 
    public Transform[] spawnPoints;
}
 
public class LevelManager : MonoBehaviour
{
    [SerializeField]
    private List<LevelConfig> levels = new List<LevelConfig>()
    {
        new LevelConfig { levelName = "Level 1", aggressiveNPCCount = 1, cowardNPCCount = 1 },
        new LevelConfig { levelName = "Level 2", aggressiveNPCCount = 2, cowardNPCCount = 2 },
        new LevelConfig { levelName = "Level 3", aggressiveNPCCount = 3, cowardNPCCount = 2 },
        new LevelConfig { levelName = "Level 4", aggressiveNPCCount = 4, cowardNPCCount = 3 },
        new LevelConfig { levelName = "Level 5", aggressiveNPCCount = 5, cowardNPCCount = 3 },
    };
 
    private int currentLevelIndex = 0;
 
    private List<GameObject> spawnedNPCs = new List<GameObject>();
 
    private void Start()
    {
        LoadLevel(currentLevelIndex);
    }
 
    public void LoadLevel(int levelIndex)
    {
        if (levelIndex >= levels.Count)
        {
            Debug.Log("You have completed all levels!");
            return;
        }
 
        ClearNPCs();
 
        currentLevelIndex = levelIndex;
        LevelConfig config = levels[currentLevelIndex];
 
        Debug.Log("Loading: " + config.levelName);
 
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
        if (prefab == null)
        {
            Debug.LogWarning("NPC prefab not assigned in Inspector for " + config.levelName);
            return;
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
    }
 
    private void ClearNPCs()
    {
        foreach (GameObject npc in spawnedNPCs)
        {
            if (npc != null) Destroy(npc);
        }
        spawnedNPCs.Clear();
    }
 
    public void NextLevel()
    {
        LoadLevel(currentLevelIndex + 1);
    }
 
    public int GetCurrentLevel()
    {
        return currentLevelIndex + 1;
    }
}
 