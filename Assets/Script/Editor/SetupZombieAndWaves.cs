using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using System.IO;
using System.Collections.Generic;

[InitializeOnLoad]
public static class SetupZombieAndWaves
{
    static SetupZombieAndWaves()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists("Assets/Prefabs/Enemy_Zombie.prefab"))
            {
                SetupZombiePrefab();
                UpdateWavesWithZombie();
            }
        };
    }

    [MenuItem("Tools/FPS Prototype/Setup Zombie & Update Waves")]
    public static void Execute()
    {
        SetupZombiePrefab();
        UpdateWavesWithZombie();
        EditorUtility.DisplayDialog("FPS Prototype",
            "Zombie enemy successfully created (Enemy_Zombie.prefab) and integrated into Waves with Knockback & Death VFX!",
            "Awesome!");
    }

    public static GameObject SetupZombiePrefab()
    {
        GameObject root = new GameObject("Enemy_Zombie");
        try { root.tag = "Enemy"; } catch { }

        // 1. Collider
        CapsuleCollider col = root.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 0.95f, 0);
        col.radius = 0.45f;
        col.height = 1.9f;

        // 2. NavMeshAgent
        NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.45f;
        agent.height = 1.9f;
        agent.speed = 3.2f;
        agent.stoppingDistance = 1.4f;

        // 3. EnemyHealth
        EnemyHealth health = root.AddComponent<EnemyHealth>();
        health.maxHealth = 65f;
        health.customDisplayName = "Zombie Walker";

        // 4. ZombieAI
        ZombieAI ai = root.AddComponent<ZombieAI>();
        ai.moveSpeed = 3.2f;
        ai.stoppingDistance = 1.4f;
        ai.attackRange = 1.6f;
        ai.attackDamage = 16f;
        ai.attackCooldown = 1.1f;
        ai.surroundRadius = 2.1f;
        ai.orbitSpeed = 8f;

        // 5. Visual Builder
        EnemyStats stats = EnemyStats.GetDefault(EnemyType.Zombie);
        ai.stats = stats;
        MonsterVisualBuilder.BuildVisual(root, stats);

        // 6. Save Prefab
        string prefabPath = "Assets/Prefabs/Enemy_Zombie.prefab";
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        Debug.Log("[SetupZombie] Successfully created and saved Enemy_Zombie.prefab!");
        AssetDatabase.SaveAssets();
        return savedPrefab;
    }

    public static void UpdateWavesWithZombie()
    {
        GameObject zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Zombie.prefab");
        GameObject chickenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_02.prefab") ??
                                   AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Chicken.prefab");
        GameObject raptorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_01.prefab") ??
                                  AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Raptor.prefab");
        GameObject tankPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_03.prefab") ??
                                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Tank.prefab");

        if (zombiePrefab == null) return;

        // 1. Update Managers.prefab
        string managersPrefabPath = "Assets/Prefabs/Managers.prefab";
        if (File.Exists(managersPrefabPath))
        {
            GameObject managersObj = PrefabUtility.LoadPrefabContents(managersPrefabPath);
            WaveManager wm = managersObj.GetComponentInChildren<WaveManager>();
            if (wm != null)
            {
                ApplyWavesConfig(wm, zombiePrefab, chickenPrefab, raptorPrefab, tankPrefab);
                PrefabUtility.SaveAsPrefabAsset(managersObj, managersPrefabPath);
                Debug.Log("[SetupZombie] Updated WaveManager in Managers.prefab with Zombie!");
            }
            PrefabUtility.UnloadPrefabContents(managersObj);
        }

        // 2. Update Active Scene WaveManager if present
        WaveManager sceneWm = Object.FindAnyObjectByType<WaveManager>();
        if (sceneWm != null)
        {
            ApplyWavesConfig(sceneWm, zombiePrefab, chickenPrefab, raptorPrefab, tankPrefab);
            EditorUtility.SetDirty(sceneWm);
            Debug.Log("[SetupZombie] Updated WaveManager in active scene with Zombie!");
        }

        AssetDatabase.SaveAssets();
    }

    private static void ApplyWavesConfig(WaveManager wm, GameObject zombie, GameObject chicken, GameObject raptor, GameObject tank)
    {
        wm.wavesConfigList = new List<DynamicWaveConfig>();

        // Wave 1: Chicken Swarm & Initial Zombies
        var wave1 = new DynamicWaveConfig { waveTitle = "Wave 1: Undead Outbreak", spawnInterval = 1.0f };
        if (chicken != null) wave1.enemies.Add(new CustomEnemySpawnInfo { enemyPrefab = chicken, count = 6 });
        if (zombie != null) wave1.enemies.Add(new CustomEnemySpawnInfo { enemyPrefab = zombie, count = 4 });
        wm.wavesConfigList.Add(wave1);

        // Wave 2: Fast Raptors & Zombie Stalkers
        var wave2 = new DynamicWaveConfig { waveTitle = "Wave 2: Raptor & Zombie Hunt", spawnInterval = 1.2f };
        if (chicken != null) wave2.enemies.Add(new CustomEnemySpawnInfo { enemyPrefab = chicken, count = 6 });
        if (zombie != null) wave2.enemies.Add(new CustomEnemySpawnInfo { enemyPrefab = zombie, count = 8 });
        if (raptor != null) wave2.enemies.Add(new CustomEnemySpawnInfo { enemyPrefab = raptor, count = 3 });
        wm.wavesConfigList.Add(wave2);

        // Wave 3: Heavy Zombie Horde & Raptor Pack & Brute Tank
        var wave3 = new DynamicWaveConfig { waveTitle = "Wave 3: Horde Carnage", spawnInterval = 1.5f };
        if (zombie != null) wave3.enemies.Add(new CustomEnemySpawnInfo { enemyPrefab = zombie, count = 12 });
        if (raptor != null) wave3.enemies.Add(new CustomEnemySpawnInfo { enemyPrefab = raptor, count = 5 });
        if (tank != null) wave3.enemies.Add(new CustomEnemySpawnInfo { enemyPrefab = tank, count = 1 });
        wm.wavesConfigList.Add(wave3);

        // Wave 4: Ultimate Apocalypse Survival
        var wave4 = new DynamicWaveConfig { waveTitle = "Wave 4: Zombie Apocalypse", spawnInterval = 1.2f };
        if (zombie != null) wave4.enemies.Add(new CustomEnemySpawnInfo { enemyPrefab = zombie, count = 18 });
        if (raptor != null) wave4.enemies.Add(new CustomEnemySpawnInfo { enemyPrefab = raptor, count = 6 });
        if (tank != null) wave4.enemies.Add(new CustomEnemySpawnInfo { enemyPrefab = tank, count = 2 });
        wm.wavesConfigList.Add(wave4);
    }
}
