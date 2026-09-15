using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 1. เปลี่ยนโครงสร้างข้อมูลให้เก็บเป็น GameObject โดยตรง ไม่ใช้ Enum แล้ว
[System.Serializable]
public class CustomEnemySpawnInfo
{
    [Tooltip("ลาก Prefab มอนสเตอร์ที่ต้องการให้เกิดใน Wave นี้มาใส่ตรงนี้ได้เลย")]
    public GameObject enemyPrefab;
    [Tooltip("จำนวนที่ต้องการให้เกิด")]
    public int count = 5;
}

[System.Serializable]
public class DynamicWaveConfig
{
    public string waveTitle = "Wave 1";
    [Tooltip("กดปุ่ม + เพื่อเพิ่มชนิดมอนสเตอร์ใน Wave นี้ได้ไม่จำกัด")]
    public List<CustomEnemySpawnInfo> enemies = new List<CustomEnemySpawnInfo>();
    public float spawnInterval = 1.0f;
}

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Wave Customization")]
    [Tooltip("รายการ Wave ทั้งหมดในเกม")]
    public List<DynamicWaveConfig> wavesConfigList = new List<DynamicWaveConfig>();

    [Header("Settings")]
    public Transform[] spawnPoints;
    public float timeBetweenWaves = 4f;

    public int CurrentWaveIndex { get; private set; } = 0;
    public int TotalWaves => wavesConfigList.Count;
    public int EnemiesRemaining { get; private set; } = 0;
    public bool IsWaveInProgress { get; private set; } = false;

    private Coroutine waveRoutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }
    }

    void Start()
    {
        EnsureSpawnPoints();
        if (wavesConfigList.Count > 0)
        {
            StartCoroutine(BeginFirstWaveDelayed());
        }
    }

    IEnumerator BeginFirstWaveDelayed()
    {
        yield return new WaitForSeconds(1.5f);
        StartWave(0);
    }

    void EnsureSpawnPoints()
    {
        if (spawnPoints != null && spawnPoints.Length > 0) return;

        GameObject existing = GameObject.Find("SpawnPoints");
        if (existing != null && existing.transform.childCount > 0)
        {
            List<Transform> list = new List<Transform>();
            for (int i = 0; i < existing.transform.childCount; i++)
            {
                list.Add(existing.transform.GetChild(i));
            }
            spawnPoints = list.ToArray();
            return;
        }

        List<Transform> points = new List<Transform>();
        GameObject parentObj = new GameObject("SpawnPoints");
        float radius = 22f;
        int count = 8;

        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            GameObject sp = new GameObject($"SpawnPoint_{i + 1}");
            sp.transform.SetParent(parentObj.transform);
            sp.transform.position = pos;
            points.Add(sp.transform);
        }

        spawnPoints = points.ToArray();
    }

    public void StartWave(int waveIndex)
    {
        if (waveIndex >= wavesConfigList.Count)
        {
            OnAllWavesCompleted();
            return;
        }

        CurrentWaveIndex = waveIndex;
        if (waveRoutine != null) StopCoroutine(waveRoutine);
        waveRoutine = StartCoroutine(RunWaveRoutine(wavesConfigList[waveIndex]));
    }

    IEnumerator RunWaveRoutine(DynamicWaveConfig wave)
    {
        IsWaveInProgress = true;

        List<GameObject> spawnQueue = new List<GameObject>();
        foreach (var info in wave.enemies)
        {
            if (info.enemyPrefab == null) continue;

            for (int i = 0; i < info.count; i++)
            {
                spawnQueue.Add(info.enemyPrefab);
            }
        }

        // สลับลำดับการเสกมอนสเตอร์แบบสุ่ม
        for (int i = 0; i < spawnQueue.Count; i++)
        {
            int r = UnityEngine.Random.Range(i, spawnQueue.Count);
            var temp = spawnQueue[i];
            spawnQueue[i] = spawnQueue[r];
            spawnQueue[r] = temp;
        }

        EnemiesRemaining = spawnQueue.Count;

        if (SoundManager.Instance != null) SoundManager.Instance.PlayWaveStart();

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.UpdateWaveInfo(CurrentWaveIndex + 1, TotalWaves, EnemiesRemaining);
            GameUIManager.Instance.ShowNotification($"WAVE {CurrentWaveIndex + 1}: {wave.waveTitle}");
        }

        foreach (var prefab in spawnQueue)
        {
            SpawnEnemyFromPrefab(prefab);
            yield return new WaitForSeconds(wave.spawnInterval);
        }
    }

    Vector3 GetRandomSpawnPosition()
    {
        Vector3 pos;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform sp = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            Vector2 jitter = UnityEngine.Random.insideUnitCircle * 2f;
            pos = sp.position + new Vector3(jitter.x, 0, jitter.y);
        }
        else
        {
            pos = new Vector3(UnityEngine.Random.Range(-15f, 15f), 0f, UnityEngine.Random.Range(-15f, 15f));
        }

        // หาตำแหน่งพื้นผิวด้วย Raycast เพื่อให้วางติดพื้นพอดี
        if (Physics.Raycast(new Vector3(pos.x, 20f, pos.z), Vector3.down, out RaycastHit hit, 40f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            pos.y = hit.point.y;
        }
        else
        {
            pos.y = 0f;
        }

        return pos;
    }

    public GameObject SpawnEnemyFromPrefab(GameObject prefab)
    {
        if (prefab == null) return null;

        Vector3 spawnPos = GetRandomSpawnPosition();
        GameObject enemyObj = Instantiate(prefab, spawnPos, Quaternion.identity);
        enemyObj.name = $"Enemy_{prefab.name}";

        try { enemyObj.tag = "Enemy"; } catch { }

        // Auto Ensure Components
        EnemyHealth health = enemyObj.GetComponent<EnemyHealth>();
        if (health == null) health = enemyObj.AddComponent<EnemyHealth>();

        EnemyAI ai = enemyObj.GetComponent<EnemyAI>();
        if (ai == null) ai = enemyObj.AddComponent<EnemyAI>();
        else ai.SnapToGround();

        return enemyObj;
    }

    public void OnEnemyKilled(GameObject enemy)
    {
        EnemiesRemaining = Mathf.Max(0, EnemiesRemaining - 1);

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.UpdateWaveInfo(CurrentWaveIndex + 1, TotalWaves, EnemiesRemaining);
        }

        if (EnemiesRemaining <= 0 && IsWaveInProgress)
        {
            IsWaveInProgress = false;
            StartCoroutine(WaveClearRoutine());
        }
    }

    IEnumerator WaveClearRoutine()
    {
        int nextWave = CurrentWaveIndex + 1;

        if (nextWave >= wavesConfigList.Count)
        {
            OnAllWavesCompleted();
            yield break;
        }

        if (SoundManager.Instance != null) SoundManager.Instance.PlayVictory();

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowNotification($"WAVE {CurrentWaveIndex + 1} CLEARED!");
        }

        for (int i = (int)timeBetweenWaves; i > 0; i--)
        {
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.ShowWaveCountdown(i);
            }
            yield return new WaitForSeconds(1f);
        }

        StartWave(nextWave);
    }

    void OnAllWavesCompleted()
    {
        IsWaveInProgress = false;
        if (SoundManager.Instance != null) SoundManager.Instance.PlayVictory();
        if (GameUIManager.Instance != null) GameUIManager.Instance.ShowVictoryScreen();

        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.UnlockCursor();
    }

    public void RestartGame()
    {
        var enemies = FindObjectsByType<EnemyHealth>();
        foreach (var e in enemies) Destroy(e.gameObject);

        var pickups = FindObjectsByType<PickupItem>();
        foreach (var p in pickups) Destroy(p.gameObject);

        var playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null) playerHealth.ResetHealth();

        if (PlayerInventory.Instance != null && PlayerInventory.Instance.slots != null)
        {
            foreach (var w in PlayerInventory.Instance.slots)
            {
                if (w != null && !w.isMelee)
                {
                    w.currentAmmo = w.magazineSize;
                    w.reserveAmmo = 36;
                    w.NotifyAmmo();
                }
            }
        }

        var gun = FindAnyObjectByType<FPSGun>();
        if (gun != null)
        {
            gun.currentAmmo = gun.magazineSize;
            gun.reserveAmmo = 120;
        }

        if (GameUIManager.Instance != null) GameUIManager.Instance.HideEndScreens();

        StartWave(0);
    }
}