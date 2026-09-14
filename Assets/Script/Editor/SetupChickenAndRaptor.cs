using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using System.IO;

[InitializeOnLoad]
public static class SetupChickenAndRaptor
{
    static SetupChickenAndRaptor()
    {
        EditorApplication.delayCall += () =>
        {
            FixAllPurpleMaterials();
            SetupEnemies();
        };
    }

    [MenuItem("Tools/FPS Prototype/Fix Purple Shaders & Setup Chicken and Raptor")]
    public static void ExecuteAll()
    {
        FixAllPurpleMaterials();
        SetupEnemies();
        EditorUtility.DisplayDialog("FPS Prototype", 
            "Successfully fixed purple materials to URP Lit and configured Chicken & Raptor as Enemies with Animators!", 
            "Great!");
    }

    public static void FixAllPurpleMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogWarning("[FixShaders] Universal Render Pipeline/Lit shader not found!");
            return;
        }

        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        int fixedCount = 0;

        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            // Check if shader is Built-in Standard, error shader, or null
            if (mat.shader == null || 
                mat.shader.name == "Standard" || 
                mat.shader.name.StartsWith("Standard ") ||
                mat.shader.name.Contains("InternalErrorShader") ||
                mat.shader.name.Contains("Error"))
            {
                Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                Texture bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
                Color col = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

                mat.shader = urpLit;

                if (mainTex != null)
                {
                    mat.SetTexture("_BaseMap", mainTex);
                }
                if (bumpMap != null)
                {
                    mat.SetTexture("_BumpMap", bumpMap);
                    mat.EnableKeyword("_NORMALMAP");
                }
                mat.SetColor("_BaseColor", col);

                EditorUtility.SetDirty(mat);
                fixedCount++;
            }
            // If already URP Lit, ensure _BaseMap has texture if _MainTex was set
            else if (mat.shader.name == "Universal Render Pipeline/Lit")
            {
                if (mat.HasProperty("_MainTex") && mat.HasProperty("_BaseMap"))
                {
                    Texture baseMap = mat.GetTexture("_BaseMap");
                    Texture mainTex = mat.GetTexture("_MainTex");
                    if (baseMap == null && mainTex != null)
                    {
                        mat.SetTexture("_BaseMap", mainTex);
                        EditorUtility.SetDirty(mat);
                        fixedCount++;
                    }
                }
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[FixShaders] Fixed/Updated {fixedCount} materials to URP Lit.");
    }

    public static void SetupEnemies()
    {
        SetupChicken();
        SetupRaptor();
        UpdateWaveManagerWithNewEnemies();
        AssetDatabase.SaveAssets();
    }

    private static void SetupChicken()
    {
        string sourcePrefabPath = "Assets/Prefabs/Chicken_001.prefab";
        if (!File.Exists(sourcePrefabPath))
        {
            sourcePrefabPath = "Assets/ithappy/Animals_FREE/Prefabs/Chicken_001.prefab";
        }

        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePrefabPath);
        if (source == null)
        {
            Debug.LogWarning("[SetupChicken] Chicken source prefab not found!");
            return;
        }

        GameObject instance = Object.Instantiate(source);
        instance.name = "Enemy_Chicken";

        try { instance.tag = "Enemy"; } catch { }

        // Remove Demo player controller scripts if present
        var comps = instance.GetComponents<MonoBehaviour>();
        foreach (var c in comps)
        {
            if (c == null) continue;
            string typeName = c.GetType().Name;
            if (typeName.Contains("Demo") || typeName.Contains("Input") || typeName.Contains("Movement") || typeName.Contains("ThirdPerson"))
            {
                Object.DestroyImmediate(c);
            }
        }

        // Remove CharacterController if exists
        var cc = instance.GetComponent<CharacterController>();
        if (cc != null) Object.DestroyImmediate(cc);

        // Add or configure CapsuleCollider
        var col = instance.GetComponent<CapsuleCollider>();
        if (col == null) col = instance.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 0.35f, 0);
        col.radius = 0.35f;
        col.height = 0.7f;

        // Add NavMeshAgent
        var agent = instance.GetComponent<NavMeshAgent>();
        if (agent == null) agent = instance.AddComponent<NavMeshAgent>();
        agent.radius = 0.35f;
        agent.height = 0.7f;
        agent.speed = 6f;
        agent.stoppingDistance = 1.2f;

        // Animator
        var anim = instance.GetComponent<Animator>();
        if (anim == null) anim = instance.AddComponent<Animator>();
        anim.applyRootMotion = false;
        RuntimeAnimatorController chickenController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/ithappy/Animals_FREE/Animations/Animation_Controllers/Chicken.controller");
        if (chickenController != null)
        {
            anim.runtimeAnimatorController = chickenController;
        }

        // EnemyHealth
        var health = instance.GetComponent<EnemyHealth>();
        if (health == null) health = instance.AddComponent<EnemyHealth>();
        health.maxHealth = 40f;

        // ChickenAI
        var ai = instance.GetComponent<ChickenAI>();
        if (ai == null) ai = instance.AddComponent<ChickenAI>();
        ai.moveSpeed = 6f;
        ai.attackRange = 1.5f;
        ai.stoppingDistance = 1.2f;
        ai.attackDamage = 8f;
        ai.attackCooldown = 0.6f;

        // Save as Enemy_Chicken.prefab and Enemy_02.prefab
        PrefabUtility.SaveAsPrefabAsset(instance, "Assets/Prefabs/Enemy_Chicken.prefab");
        PrefabUtility.SaveAsPrefabAsset(instance, "Assets/Prefabs/Enemy_02.prefab");

        Object.DestroyImmediate(instance);
        Debug.Log("[SetupChicken] Successfully created and configured Enemy_Chicken prefab!");
    }

    private static void SetupRaptor()
    {
        string sourcePrefabPath = "Assets/Prefabs/Raptor_Animated_LODG_Blue.prefab";
        if (!File.Exists(sourcePrefabPath))
        {
            sourcePrefabPath = "Assets/FerociousIndustries/PBRDinosaurs/PBRVelociraptor/Prefabs/PBR/LODG/Raptor_Animated_LODG_Blue.prefab";
        }

        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePrefabPath);
        if (source == null)
        {
            Debug.LogWarning("[SetupRaptor] Raptor source prefab not found!");
            return;
        }

        GameObject instance = Object.Instantiate(source);
        instance.name = "Enemy_Raptor";

        try { instance.tag = "Enemy"; } catch { }

        // Add or configure CapsuleCollider
        var col = instance.GetComponent<CapsuleCollider>();
        if (col == null) col = instance.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 0.9f, 0);
        col.radius = 0.5f;
        col.height = 1.8f;

        // Add NavMeshAgent
        var agent = instance.GetComponent<NavMeshAgent>();
        if (agent == null) agent = instance.AddComponent<NavMeshAgent>();
        agent.radius = 0.5f;
        agent.height = 1.8f;
        agent.speed = 5.5f;
        agent.stoppingDistance = 1.6f;

        // Animator
        var anim = instance.GetComponent<Animator>();
        if (anim == null) anim = instance.AddComponent<Animator>();
        anim.applyRootMotion = false;
        RuntimeAnimatorController raptorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/FerociousIndustries/PBRDinosaurs/PBRVelociraptor/Animators/DemoAnimatorRun.controller");
        if (raptorController != null)
        {
            anim.runtimeAnimatorController = raptorController;
        }

        // EnemyHealth
        var health = instance.GetComponent<EnemyHealth>();
        if (health == null) health = instance.AddComponent<EnemyHealth>();
        health.maxHealth = 110f;

        // RaptorAI
        var ai = instance.GetComponent<RaptorAI>();
        if (ai == null) ai = instance.AddComponent<RaptorAI>();
        ai.moveSpeed = 5.5f;
        ai.attackRange = 2f;
        ai.stoppingDistance = 1.6f;
        ai.dashRange = 6.5f;
        ai.dashSpeed = 13f;
        ai.dashCooldown = 4f;
        ai.attackDamage = 25f;

        // Save as Enemy_Raptor.prefab and Enemy_01.prefab
        PrefabUtility.SaveAsPrefabAsset(instance, "Assets/Prefabs/Enemy_Raptor.prefab");
        PrefabUtility.SaveAsPrefabAsset(instance, "Assets/Prefabs/Enemy_01.prefab");

        Object.DestroyImmediate(instance);
        Debug.Log("[SetupRaptor] Successfully created and configured Enemy_Raptor prefab!");
    }

    public static void UpdateWaveManagerWithNewEnemies()
    {
        GameObject chickenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Chicken.prefab");
        GameObject raptorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Raptor.prefab");

        if (chickenPrefab == null && raptorPrefab == null) return;

        // Open and update SampleScene
        string scenePath = "Assets/Scenes/SampleScene.unity";
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);

        WaveManager wm = Object.FindAnyObjectByType<WaveManager>();
        if (wm != null)
        {
            // If wavesConfigList is empty or has default entries, populate it nicely
            if (wm.wavesConfigList == null || wm.wavesConfigList.Count == 0)
            {
                wm.wavesConfigList = new System.Collections.Generic.List<DynamicWaveConfig>();

                // Wave 1: Chickens
                var wave1 = new DynamicWaveConfig { waveTitle = "Chicken Invasions" };
                if (chickenPrefab != null) wave1.enemies.Add(new CustomEnemySpawnInfo { enemyPrefab = chickenPrefab, count = 6 });
                wm.wavesConfigList.Add(wave1);

                // Wave 2: Chickens & Raptors
                var wave2 = new DynamicWaveConfig { waveTitle = "Raptors & Chickens Attack" };
                if (chickenPrefab != null) wave2.enemies.Add(new CustomEnemySpawnInfo { enemyPrefab = chickenPrefab, count = 8 });
                if (raptorPrefab != null) wave2.enemies.Add(new CustomEnemySpawnInfo { enemyPrefab = raptorPrefab, count = 3 });
                wm.wavesConfigList.Add(wave2);

                // Wave 3: Heavy Raptors & Swarm
                var wave3 = new DynamicWaveConfig { waveTitle = "Raptor Pack Frenzy" };
                if (chickenPrefab != null) wave3.enemies.Add(new CustomEnemySpawnInfo { enemyPrefab = chickenPrefab, count = 10 });
                if (raptorPrefab != null) wave3.enemies.Add(new CustomEnemySpawnInfo { enemyPrefab = raptorPrefab, count = 6 });
                wm.wavesConfigList.Add(wave3);
            }
            else
            {
                // Ensure chicken and raptor are included in waves
                foreach (var w in wm.wavesConfigList)
                {
                    bool hasChicken = false;
                    bool hasRaptor = false;
                    foreach (var e in w.enemies)
                    {
                        if (e.enemyPrefab == chickenPrefab || (chickenPrefab != null && e.enemyPrefab != null && e.enemyPrefab.name.Contains("Chicken"))) hasChicken = true;
                        if (e.enemyPrefab == raptorPrefab || (raptorPrefab != null && e.enemyPrefab != null && e.enemyPrefab.name.Contains("Raptor"))) hasRaptor = true;
                    }
                    if (!hasChicken && chickenPrefab != null)
                    {
                        w.enemies.Add(new CustomEnemySpawnInfo { enemyPrefab = chickenPrefab, count = 4 });
                    }
                    if (!hasRaptor && raptorPrefab != null)
                    {
                        w.enemies.Add(new CustomEnemySpawnInfo { enemyPrefab = raptorPrefab, count = 2 });
                    }
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log("[WaveManager] Wave configurations updated with Chicken and Raptor!");
        }
    }
}
