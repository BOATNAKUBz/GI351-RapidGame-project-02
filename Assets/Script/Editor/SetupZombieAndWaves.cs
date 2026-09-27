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
            }
        };
    }

    [MenuItem("Tools/FPS Prototype/Setup Zombie Prefab")]
    public static void Execute()
    {
        SetupZombiePrefab();
        EditorUtility.DisplayDialog("FPS Prototype",
            "Zombie prefab created!\n\nPlease configure Zones in WaveManager Inspector.",
            "OK");
    }

    public static GameObject SetupZombiePrefab()
    {
        GameObject root = new GameObject("Enemy_Zombie");
        try { root.tag = "Enemy"; } catch { }

        CapsuleCollider col = root.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 0.95f, 0);
        col.radius = 0.45f;
        col.height = 1.9f;

        NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.45f;
        agent.height = 1.9f;
        agent.speed = 3.2f;
        agent.stoppingDistance = 1.4f;

        EnemyHealth health = root.AddComponent<EnemyHealth>();
        health.maxHealth = 65f;
        health.customDisplayName = "Zombie Walker";

        ZombieAI ai = root.AddComponent<ZombieAI>();
        ai.moveSpeed = 3.2f;
        ai.stoppingDistance = 1.4f;
        ai.attackRange = 1.6f;
        ai.attackDamage = 16f;
        ai.attackCooldown = 1.1f;
        ai.surroundRadius = 2.1f;
        ai.orbitSpeed = 8f;

        EnemyStats stats = EnemyStats.GetDefault(EnemyType.Zombie);
        ai.stats = stats;
        MonsterVisualBuilder.BuildVisual(root, stats);

        string prefabPath = "Assets/Prefabs/Enemy_Zombie.prefab";
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        Debug.Log("[SetupZombie] Enemy_Zombie.prefab created successfully!");
        AssetDatabase.SaveAssets();
        return savedPrefab;
    }
}