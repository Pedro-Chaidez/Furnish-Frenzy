using UnityEngine;
using UnityEngine.UI;
using TMPro;
 
public class PoliceTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    public float timeLimit = 60f;
 
    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public Image timerBackground;
 
    [Header("Police Spawning")]
    public GameObject policeNPCPrefab;
    public Transform[] policeSpawnPoints;
    public int numberOfPoliceToSpawn = 3;
 
    private float currentTime;
    private bool policeSpawned = false;
    private bool timerRunning = true;
 
    private void Start()
    {
        currentTime = timeLimit;
        UpdateTimerUI();
    }
 
    private void Update()
    {
        if (!timerRunning || policeSpawned) return;
 
        currentTime -= Time.deltaTime;
        currentTime = Mathf.Max(currentTime, 0f);
 
        UpdateTimerUI();
 
        if (timerText != null)
        {
            timerText.color = currentTime <= 10f ? Color.red : Color.white;
        }
 
        if (currentTime <= 0f)
        {
            SpawnPolice();
        }
    }
 
    private void UpdateTimerUI()
    {
        if (timerText == null) return;
 
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
 
    private void SpawnPolice()
    {
        policeSpawned = true;
        timerRunning = false;
 
        if (timerText != null)
        {
            timerText.text = "POLICE INCOMING!";
            timerText.color = Color.red;
        }
 
        Debug.Log("Time's up! Spawning police!");
 
        if (policeNPCPrefab == null)
        {
            Debug.LogWarning("No police prefab assigned!");
            return;
        }
 
        for (int i = 0; i < numberOfPoliceToSpawn; i++)
        {
            Vector3 spawnPos = transform.position;
            Quaternion spawnRot = Quaternion.identity;
 
            if (policeSpawnPoints != null && policeSpawnPoints.Length > 0)
            {
                Transform point = policeSpawnPoints[i % policeSpawnPoints.Length];
                spawnPos = point.position;
                spawnRot = point.rotation;
            }
 
            Instantiate(policeNPCPrefab, spawnPos, spawnRot);
            Debug.Log("Spawned police officer " + (i + 1));
        }
    }
 
    public void StopTimer()
    {
        timerRunning = false;
        Debug.Log("Timer stopped — player escaped!");
    }
 
    public void ResetTimer()
    {
        currentTime = timeLimit;
        policeSpawned = false;
        timerRunning = true;
        UpdateTimerUI();
    }
}