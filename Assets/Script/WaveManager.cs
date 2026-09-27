using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnSequence
{
    [Tooltip("Enemy Prefab to spawn in this sequence")]
    public GameObject enemyPrefab;

    [Tooltip("Number of enemies to spawn")]
    public int amount = 3;

    [Tooltip("Delay in seconds before this sequence starts spawning")]
    public float delayBeforeSpawn = 0f;

    [Tooltip("Delay in seconds between each individual enemy spawn")]
    public float intervalBetweenEach = 0.25f;
}

[System.Serializable]
public class ZoneConfig
{
    [Tooltip("Trigger Collider (Box/Sphere) that detects the player entering this zone")]
    public Collider zoneTrigger;

    [Tooltip("Spawn points inside this zone (uses global spawn points if empty)")]
    public Transform[] spawnPoints;

    [Tooltip("Ordered list of spawn sequences for this zone")]
    public List<SpawnSequence> spawnSequences = new List<SpawnSequence>();
}

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Zone Configuration")]
    public List<ZoneConfig> zones = new List<ZoneConfig>();

    [Header("Global Fallback Spawn Points")]
    public Transform[] globalSpawnPoints;

    public int CurrentZoneIndex { get; private set; } = -1;
    public int TotalZones => zones.Count;
    public int ZonesCleared { get; private set; } = 0;
    public int ActiveEnemyCount => activeEnemies.Count;

    private readonly List<GameObject> activeEnemies = new List<GameObject>();
    private readonly HashSet<int> triggeredZones = new HashSet<int>();
    private readonly HashSet<int> clearedZones   = new HashSet<int>();
    private readonly Dictionary<int, int>  zoneAliveCount = new Dictionary<int, int>();
    private readonly Dictionary<int, bool> zoneSpawnDone  = new Dictionary<int, bool>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }
    }

    void Start()
    {
        EnsureGlobalSpawnPoints();
        SetupZoneTriggers();
        UpdateZoneUI();
    }

    void SetupZoneTriggers()
    {
        for (int i = 0; i < zones.Count; i++)
        {
            var zone = zones[i];
            if (zone.zoneTrigger == null) continue;

            zone.zoneTrigger.isTrigger = true;

            var proxy = zone.zoneTrigger.gameObject.GetComponent<ZoneTriggerProxy>();
            if (proxy == null) proxy = zone.zoneTrigger.gameObject.AddComponent<ZoneTriggerProxy>();
            proxy.Init(this, i);

            zoneAliveCount[i] = 0;
            zoneSpawnDone[i]  = false;
        }
    }

    public void OnPlayerEnterZone(int zoneIndex)
    {
        if (triggeredZones.Contains(zoneIndex)) return;
        if (clearedZones.Contains(zoneIndex))   return;

        triggeredZones.Add(zoneIndex);
        CurrentZoneIndex = zoneIndex;

        if (GameUIManager.Instance != null)
            GameUIManager.Instance.ShowNotification("ZONE " + (zoneIndex + 1) + " ENEMIES INCOMING!");

        if (SoundManager.Instance != null) SoundManager.Instance.PlayWaveStart();

        StartCoroutine(RunZoneSequences(zoneIndex));
    }

    IEnumerator RunZoneSequences(int zoneIndex)
    {
        var zone = zones[zoneIndex];

        foreach (var seq in zone.spawnSequences)
        {
            if (seq.enemyPrefab == null) continue;

            if (seq.delayBeforeSpawn > 0f)
                yield return new WaitForSeconds(seq.delayBeforeSpawn);

            for (int i = 0; i < seq.amount; i++)
            {
                SpawnEnemy(seq.enemyPrefab, zoneIndex);

                if (seq.intervalBetweenEach > 0f)
                    yield return new WaitForSeconds(seq.intervalBetweenEach);
            }
        }

        zoneSpawnDone[zoneIndex] = true;
        CheckZoneClear(zoneIndex);
    }

    void SpawnEnemy(GameObject prefab, int zoneIndex)
    {
        if (prefab == null) return;

        Vector3 pos = GetSpawnPosition(zoneIndex);
        GameObject enemyObj = Instantiate(prefab, pos, Quaternion.identity);
        enemyObj.name = "Enemy_" + prefab.name;

        try { enemyObj.tag = "Enemy"; } catch { }

        EnemyHealth health = enemyObj.GetComponent<EnemyHealth>();
        if (health == null) health = enemyObj.AddComponent<EnemyHealth>();

        EnemyAI ai = enemyObj.GetComponent<EnemyAI>();
        if (ai == null) enemyObj.AddComponent<EnemyAI>();
        else ai.SnapToGround();

        activeEnemies.Add(enemyObj);

        if (!zoneAliveCount.ContainsKey(zoneIndex)) zoneAliveCount[zoneIndex] = 0;
        zoneAliveCount[zoneIndex]++;

        UpdateEnemyCountUI();
    }

    public void OnEnemyKilled(GameObject enemy)
    {
        if (!activeEnemies.Remove(enemy)) return;

        foreach (int zi in triggeredZones)
        {
            if (clearedZones.Contains(zi)) continue;
            if (zoneAliveCount.ContainsKey(zi) && zoneAliveCount[zi] > 0)
            {
                zoneAliveCount[zi]--;
                UpdateEnemyCountUI();
                CheckZoneClear(zi);
                break;
            }
        }
    }

    void CheckZoneClear(int zoneIndex)
    {
        if (clearedZones.Contains(zoneIndex)) return;

        bool spawnDone = zoneSpawnDone.ContainsKey(zoneIndex) && zoneSpawnDone[zoneIndex];
        bool allDead   = zoneAliveCount.ContainsKey(zoneIndex) && zoneAliveCount[zoneIndex] <= 0;

        if (spawnDone && allDead)
        {
            clearedZones.Add(zoneIndex);
            ZonesCleared++;

            if (GameUIManager.Instance != null)
                GameUIManager.Instance.ShowNotification(
                    "ZONE " + (zoneIndex + 1) + " CLEARED!  (" + ZonesCleared + "/" + TotalZones + ")");

            if (SoundManager.Instance != null) SoundManager.Instance.PlayVictory();

            UpdateZoneUI();

            if (ZonesCleared >= TotalZones)
                StartCoroutine(OnVictoryDelayed());
        }
    }

    IEnumerator OnVictoryDelayed()
    {
        yield return new WaitForSeconds(1.5f);

        if (GameUIManager.Instance != null) GameUIManager.Instance.ShowVictoryScreen();

        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.UnlockCursor();
    }

    void UpdateZoneUI()
    {
        if (GameUIManager.Instance != null)
            GameUIManager.Instance.UpdateWaveInfo(ZonesCleared, TotalZones, activeEnemies.Count);
    }

    void UpdateEnemyCountUI()
    {
        if (GameUIManager.Instance != null)
            GameUIManager.Instance.UpdateWaveInfo(ZonesCleared, TotalZones, activeEnemies.Count);
    }

    Vector3 GetSpawnPosition(int zoneIndex)
    {
        Transform[] pts = null;

        if (zoneIndex >= 0 && zoneIndex < zones.Count)
            pts = zones[zoneIndex].spawnPoints;

        if (pts == null || pts.Length == 0)
            pts = globalSpawnPoints;

        Vector3 pos;
        if (pts != null && pts.Length > 0)
        {
            Transform sp = pts[Random.Range(0, pts.Length)];
            Vector2 jitter = Random.insideUnitCircle * 2f;
            pos = sp.position + new Vector3(jitter.x, 0, jitter.y);
        }
        else
        {
            pos = new Vector3(Random.Range(-15f, 15f), 0f, Random.Range(-15f, 15f));
        }

        if (Physics.Raycast(new Vector3(pos.x, 20f, pos.z), Vector3.down, out RaycastHit hit, 40f,
                            Physics.AllLayers, QueryTriggerInteraction.Ignore))
            pos.y = hit.point.y;
        else
            pos.y = 0f;

        return pos;
    }

    void EnsureGlobalSpawnPoints()
    {
        if (globalSpawnPoints != null && globalSpawnPoints.Length > 0) return;

        GameObject existing = GameObject.Find("SpawnPoints");
        if (existing != null && existing.transform.childCount > 0)
        {
            var list = new List<Transform>();
            for (int i = 0; i < existing.transform.childCount; i++)
                list.Add(existing.transform.GetChild(i));
            globalSpawnPoints = list.ToArray();
            return;
        }

        var points    = new List<Transform>();
        var parentObj = new GameObject("SpawnPoints");
        float radius  = 22f;
        int count     = 8;

        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            var sp = new GameObject("SpawnPoint_" + (i + 1));
            sp.transform.SetParent(parentObj.transform);
            sp.transform.position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            points.Add(sp.transform);
        }

        globalSpawnPoints = points.ToArray();
    }

    public GameObject SpawnEnemyFromPrefab(GameObject prefab)
    {
        if (prefab == null) return null;

        Vector3 pos = GetSpawnPosition(-1);
        GameObject enemyObj = Instantiate(prefab, pos, Quaternion.identity);
        enemyObj.name = "Enemy_" + prefab.name;

        try { enemyObj.tag = "Enemy"; } catch { }

        EnemyHealth health = enemyObj.GetComponent<EnemyHealth>();
        if (health == null) health = enemyObj.AddComponent<EnemyHealth>();

        EnemyAI ai = enemyObj.GetComponent<EnemyAI>();
        if (ai == null) enemyObj.AddComponent<EnemyAI>();
        else ai.SnapToGround();

        activeEnemies.Add(enemyObj);
        UpdateEnemyCountUI();

        return enemyObj;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(sceneName))
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}

public class ZoneTriggerProxy : MonoBehaviour
{
    private WaveManager manager;
    private int zoneIndex;

    public void Init(WaveManager mgr, int idx)
    {
        manager   = mgr;
        zoneIndex = idx;
    }

    void OnTriggerEnter(Collider other)
    {
        if (manager == null) return;
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            manager.OnPlayerEnterZone(zoneIndex);
        }
    }
}