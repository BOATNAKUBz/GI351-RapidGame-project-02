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
public class WaveConfig
{
    [Tooltip("Spawn points for this wave (uses global spawn points if empty)")]
    public Transform[] spawnPoints;

    [Tooltip("Ordered list of spawn sequences for this wave")]
    public List<SpawnSequence> spawnSequences = new List<SpawnSequence>();
}

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Wave Configuration")]
    public List<WaveConfig> waves = new List<WaveConfig>();

    [Header("Global Fallback Spawn Points")]
    public Transform[] globalSpawnPoints;

    [Header("Wave Settings")]
    [Tooltip("Time in seconds before the next wave starts")]
    public float timeBetweenWaves = 3f;

    public int CurrentWaveIndex { get; private set; } = 0;
    public int TotalWaves => waves.Count;
    public int WavesCleared { get; private set; } = 0;
    public int ActiveEnemyCount => activeEnemies.Count;

    private readonly List<GameObject> activeEnemies = new List<GameObject>();
    private readonly Dictionary<int, int> waveAliveCount = new Dictionary<int, int>();
    private readonly Dictionary<int, bool> waveSpawnDone = new Dictionary<int, bool>();

    private bool isGameFinished = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }
    }

    void Start()
    {
        EnsureGlobalSpawnPoints();
        UpdateWaveUI();

        // เริ่ม Wave แรก (Wave 0) หลังจากเริ่มเกม 2 วินาที
        if (waves.Count > 0)
        {
            StartCoroutine(StartWaveDelayed(0, 2f));
        }
    }

    IEnumerator StartWaveDelayed(int waveIndex, float delay)
    {
        yield return new WaitForSeconds(delay);
        StartWave(waveIndex);
    }

    public void StartWave(int waveIndex)
    {
        if (isGameFinished || waveIndex >= waves.Count) return;

        CurrentWaveIndex = waveIndex;
        waveAliveCount[waveIndex] = 0;
        waveSpawnDone[waveIndex] = false;

        if (GameUIManager.Instance != null)
            GameUIManager.Instance.ShowNotification("WAVE " + (waveIndex + 1) + " INCOMING!");

        if (SoundManager.Instance != null) SoundManager.Instance.PlayWaveStart();

        StartCoroutine(RunWaveSequences(waveIndex));
        UpdateWaveUI();
    }

    IEnumerator RunWaveSequences(int waveIndex)
    {
        var wave = waves[waveIndex];

        foreach (var seq in wave.spawnSequences)
        {
            if (seq.enemyPrefab == null) continue;

            if (seq.delayBeforeSpawn > 0f)
                yield return new WaitForSeconds(seq.delayBeforeSpawn);

            for (int i = 0; i < seq.amount; i++)
            {
                SpawnEnemy(seq.enemyPrefab, waveIndex);

                if (seq.intervalBetweenEach > 0f)
                    yield return new WaitForSeconds(seq.intervalBetweenEach);
            }
        }

        waveSpawnDone[waveIndex] = true;
        CheckWaveClear(waveIndex);
    }

    void SpawnEnemy(GameObject prefab, int waveIndex)
    {
        if (prefab == null) return;

        Vector3 pos = GetSpawnPosition(waveIndex);
        GameObject enemyObj = Instantiate(prefab, pos, Quaternion.identity);
        enemyObj.name = "Enemy_" + prefab.name;

        try { enemyObj.tag = "Enemy"; } catch { }

        EnemyHealth health = enemyObj.GetComponent<EnemyHealth>();
        if (health == null) health = enemyObj.AddComponent<EnemyHealth>();

        EnemyAI ai = enemyObj.GetComponent<EnemyAI>();
        if (ai == null) enemyObj.AddComponent<EnemyAI>();
        else ai.SnapToGround();

        activeEnemies.Add(enemyObj);

        if (!waveAliveCount.ContainsKey(waveIndex)) waveAliveCount[waveIndex] = 0;
        waveAliveCount[waveIndex]++;

        UpdateEnemyCountUI();
    }

    public void OnEnemyKilled(GameObject enemy)
    {
        if (!activeEnemies.Remove(enemy)) return;

        if (waveAliveCount.ContainsKey(CurrentWaveIndex) && waveAliveCount[CurrentWaveIndex] > 0)
        {
            waveAliveCount[CurrentWaveIndex]--;
            UpdateEnemyCountUI();
            CheckWaveClear(CurrentWaveIndex);
        }
    }

    void CheckWaveClear(int waveIndex)
    {
        if (isGameFinished) return;

        bool spawnDone = waveSpawnDone.ContainsKey(waveIndex) && waveSpawnDone[waveIndex];
        bool allDead   = waveAliveCount.ContainsKey(waveIndex) && waveAliveCount[waveIndex] <= 0;

        if (spawnDone && allDead)
        {
            WavesCleared++;

            if (GameUIManager.Instance != null)
                GameUIManager.Instance.ShowNotification(
                    "WAVE " + (waveIndex + 1) + " CLEARED!");

            if (SoundManager.Instance != null) SoundManager.Instance.PlayVictory();

            UpdateWaveUI();

            if (WavesCleared >= TotalWaves)
            {
                isGameFinished = true;
                StartCoroutine(OnVictoryDelayed());
            }
            else
            {
                // ถ้ายังไม่จบเกม ให้หน่วงเวลาตามที่ตั้งไว้ก่อนเริ่ม Wave ถัดไป
                StartCoroutine(StartWaveDelayed(waveIndex + 1, timeBetweenWaves));
            }
        }
    }

    IEnumerator OnVictoryDelayed()
    {
        yield return new WaitForSeconds(1.5f);

        if (GameUIManager.Instance != null) GameUIManager.Instance.ShowVictoryScreen();

        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.UnlockCursor();
    }

    void UpdateWaveUI()
    {
        if (GameUIManager.Instance != null)
            // ส่งค่า Wave ปัจจุบันไปแสดงบน UI
            GameUIManager.Instance.UpdateWaveInfo(CurrentWaveIndex + 1, TotalWaves, activeEnemies.Count); 
    }

    void UpdateEnemyCountUI()
    {
        if (GameUIManager.Instance != null)
            GameUIManager.Instance.UpdateWaveInfo(CurrentWaveIndex + 1, TotalWaves, activeEnemies.Count);
    }

   Vector3 GetSpawnPosition(int waveIndex)
    {
        Transform[] pts = null;

        if (waveIndex >= 0 && waveIndex < waves.Count)
            pts = waves[waveIndex].spawnPoints;

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
            pos = new Vector3(Random.Range(-15f, 15f), 50f, Random.Range(-15f, 15f));
        }

        // แก้ไข: ให้เริ่มสแกนหาพื้นจากความสูงของ Spawn Point ที่ผู้เล่นวางไว้ (บวกเผื่อขึ้นไปอีก 5 เมตร)
        float rayStartY = pos.y + 5f;

        // ยิง Raycast ลงไปหาพื้นดินในระยะ 200 เมตร
        if (Physics.Raycast(new Vector3(pos.x, rayStartY, pos.z), Vector3.down, out RaycastHit hit, 200f,
                            Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            // ดันจุดเกิดให้ลอยสูงกว่าพื้นดินที่ยิงเจอ 1.5 เมตร (กันเหนียวสำหรับโมเดลที่จุดหมุนอยู่ตรงกลางลำตัว จะได้ไม่จม)
            pos.y = hit.point.y + 1.5f;
        }
        else
        {
            // ถ้าหาพื้นไม่เจอจริงๆ ให้เกิดตรงความสูงเดียวกับที่วาง Spawn Point ไว้เลย
            pos.y = pos.y + 1.0f;
        }

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