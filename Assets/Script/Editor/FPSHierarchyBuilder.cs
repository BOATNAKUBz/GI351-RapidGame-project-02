using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.AI;

[InitializeOnLoad]
public static class FPSHierarchyBuilder
{
    private static readonly string PrefabsFolder = "Assets/Prefabs";
    private static readonly string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private static readonly string GameplayScenePath = "Assets/Scenes/SampleScene.unity";

    static FPSHierarchyBuilder()
    {
        EditorApplication.delayCall += () =>
        {
            if (!System.IO.File.Exists(MainMenuScenePath) || !System.IO.Directory.Exists(PrefabsFolder))
            {
                BuildAll();
            }
        };
    }

    [MenuItem("Tools/FPS Prototype/Build Hierarchy & Scenes Now")]
    public static void BuildAllMenu()
    {
        BuildAll();
        EditorUtility.DisplayDialog("FPS Prototype",
            "Hierarchy and Scenes built successfully!\n\n" +
            "1. Prefabs created in Assets/Prefabs/\n" +
            "2. SampleScene hierarchy populated with all 3D objects, UI, and Managers\n" +
            "3. MainMenu scene created with Play & Quit buttons\n" +
            "4. Build Settings updated (MainMenu is Scene 0, SampleScene is Scene 1)\n\n" +
            "Open MainMenu and press PLAY to test!", "Awesome!");
    }

    public static void BuildAll()
    {
        Debug.Log("[FPSHierarchyBuilder] Starting full project build...");

        EnsureFolder(PrefabsFolder);

        // 1. Build Prefabs
        var monsterPrefabs = BuildMonsterPrefabs();
        var itemPrefabs = BuildItemPrefabs();

        // 2. Build Gameplay Scene (SampleScene)
        BuildGameplayScene();

        // 3. Build MainMenu Scene
        BuildMainMenuScene();

        // 4. Update Build Settings
        UpdateBuildSettings();

        // Open MainMenu scene ready for playing
        EditorSceneManager.OpenScene(MainMenuScenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FPSHierarchyBuilder] Full build completed successfully!");
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string folderName = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    // Helper ในการดึง Font ที่ปลอดภัยต่อ Unity ทุกเวอร์ชัน
    private static Font GetSafeDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        return font;
    }

    private static Dictionary<EnemyType, GameObject> BuildMonsterPrefabs()
    {
        var result = new Dictionary<EnemyType, GameObject>();

        foreach (EnemyType type in System.Enum.GetValues(typeof(EnemyType)))
        {
            string prefabPath = $"{PrefabsFolder}/Enemy_{type}.prefab";

            GameObject temp = new GameObject($"Enemy_{type}");
            try { temp.tag = "Enemy"; } catch { }

            EnemyStats stats = EnemyStats.GetDefault(type);

            CapsuleCollider col = temp.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, stats.modelScale * 1f, 0);
            col.radius = 0.5f * stats.modelScale;
            col.height = 2f * stats.modelScale;

            NavMeshAgent agent = temp.AddComponent<NavMeshAgent>();
            agent.height = col.height;
            agent.radius = col.radius;

            MonsterVisualBuilder.BuildVisual(temp, stats);

            EnemyAI ai = temp.AddComponent<EnemyAI>();
            EnemyHealth health = temp.AddComponent<EnemyHealth>();
            health.maxHealth = stats.maxHealth;
            ai.stats = stats;

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
            result[type] = saved;
            Object.DestroyImmediate(temp);
            Debug.Log($"Created Monster Prefab: {prefabPath}");
        }

        return result;
    }

    private static Dictionary<ItemType, GameObject> BuildItemPrefabs()
    {
        var result = new Dictionary<ItemType, GameObject>();

        foreach (ItemType itemType in System.Enum.GetValues(typeof(ItemType)))
        {
            string prefabPath = $"{PrefabsFolder}/Pickup_{itemType}.prefab";

            GameObject temp = new GameObject($"Pickup_{itemType}");
            PickupItem pickup = temp.AddComponent<PickupItem>();
            pickup.itemType = itemType;

            SphereCollider sc = temp.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.0f;

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(temp.transform, false);

            Shader defaultShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(defaultShader);
            mat.EnableKeyword("_EMISSION");

            Light itemLight = visual.AddComponent<Light>();
            itemLight.type = LightType.Point;
            itemLight.range = 3f;
            itemLight.intensity = 1.2f;

            switch (itemType)
            {
                case ItemType.Medkit:
                    mat.color = new Color(0.95f, 0.95f, 0.95f);
                    itemLight.color = new Color(0.2f, 1f, 0.3f);
                    mat.SetColor("_EmissionColor", new Color(0.1f, 0.8f, 0.2f) * 1.5f);

                    GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    box.transform.SetParent(visual.transform, false);
                    box.transform.localScale = new Vector3(0.5f, 0.35f, 0.35f);
                    box.GetComponent<Renderer>().sharedMaterial = mat;
                    Object.DestroyImmediate(box.GetComponent<Collider>());

                    Material greenMat = new Material(mat);
                    greenMat.color = new Color(0.1f, 0.9f, 0.2f);
                    greenMat.SetColor("_EmissionColor", new Color(0.2f, 1f, 0.3f) * 2f);

                    GameObject crossV = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    crossV.transform.SetParent(visual.transform, false);
                    crossV.transform.localPosition = new Vector3(0, 0.18f, 0);
                    crossV.transform.localScale = new Vector3(0.08f, 0.02f, 0.22f);
                    crossV.GetComponent<Renderer>().sharedMaterial = greenMat;
                    Object.DestroyImmediate(crossV.GetComponent<Collider>());

                    GameObject crossH = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    crossH.transform.SetParent(visual.transform, false);
                    crossH.transform.localPosition = new Vector3(0, 0.18f, 0);
                    crossH.transform.localScale = new Vector3(0.22f, 0.02f, 0.08f);
                    crossH.GetComponent<Renderer>().sharedMaterial = greenMat;
                    Object.DestroyImmediate(crossH.GetComponent<Collider>());
                    break;

                case ItemType.Ammo:
                    mat.color = new Color(0.25f, 0.35f, 0.45f);
                    itemLight.color = new Color(1f, 0.85f, 0.2f);
                    mat.SetColor("_EmissionColor", new Color(0.3f, 0.5f, 0.8f) * 1.5f);

                    GameObject ammoBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    ammoBox.transform.SetParent(visual.transform, false);
                    ammoBox.transform.localScale = new Vector3(0.35f, 0.45f, 0.35f);
                    ammoBox.GetComponent<Renderer>().sharedMaterial = mat;
                    Object.DestroyImmediate(ammoBox.GetComponent<Collider>());

                    Material goldMat = new Material(mat);
                    goldMat.color = new Color(1f, 0.8f, 0.1f);
                    goldMat.SetColor("_EmissionColor", new Color(1f, 0.85f, 0.2f) * 2f);

                    GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    stripe.transform.SetParent(visual.transform, false);
                    stripe.transform.localPosition = new Vector3(0, 0.05f, 0);
                    stripe.transform.localScale = new Vector3(0.37f, 0.05f, 0.37f);
                    stripe.GetComponent<Renderer>().sharedMaterial = goldMat;
                    Object.DestroyImmediate(stripe.GetComponent<Collider>());
                    break;

                case ItemType.PowerGem:
                    mat.color = new Color(0.1f, 0.8f, 1f);
                    itemLight.color = new Color(0f, 0.9f, 1f);
                    mat.SetColor("_EmissionColor", new Color(0f, 0.9f, 1f) * 2.5f);

                    GameObject gem = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    gem.transform.SetParent(visual.transform, false);
                    gem.transform.localScale = new Vector3(0.35f, 0.55f, 0.35f);
                    gem.transform.localRotation = Quaternion.Euler(45f, 45f, 0);
                    gem.GetComponent<Renderer>().sharedMaterial = mat;
                    Object.DestroyImmediate(gem.GetComponent<Collider>());
                    break;
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
            result[itemType] = saved;
            Object.DestroyImmediate(temp);
            Debug.Log($"Created Item Prefab: {prefabPath}");
        }

        return result;
    }

    private static void BuildGameplayScene()
    {
        EditorSceneManager.OpenScene(GameplayScenePath);

        // 1. Setup Arena
        GameObject arena = GameObject.Find("Arena_Boundaries") ?? new GameObject("Arena_Boundaries");

        float arenaSize = 50f;
        float wallHeight = 4.5f;

        GameObject ground = GameObject.Find("Plane");
        if (ground == null)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Plane";
        }
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(arenaSize / 10f, 1, arenaSize / 10f);

        Shader defaultShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material groundMat = new Material(defaultShader);
        groundMat.color = new Color(0.18f, 0.2f, 0.22f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        Material wallMat = new Material(groundMat);
        wallMat.color = new Color(0.25f, 0.28f, 0.32f);

        for (int i = arena.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(arena.transform.GetChild(i).gameObject);
        }

        float half = arenaSize / 2f;
        float thick = 1f;

        CreateWall(arena.transform, "Wall_North", new Vector3(0, wallHeight / 2f, half), new Vector3(arenaSize + thick, wallHeight, thick), wallMat);
        CreateWall(arena.transform, "Wall_South", new Vector3(0, wallHeight / 2f, -half), new Vector3(arenaSize + thick, wallHeight, thick), wallMat);
        CreateWall(arena.transform, "Wall_East", new Vector3(half, wallHeight / 2f, 0), new Vector3(thick, wallHeight, arenaSize + thick), wallMat);
        CreateWall(arena.transform, "Wall_West", new Vector3(-half, wallHeight / 2f, 0), new Vector3(thick, wallHeight, arenaSize + thick), wallMat);

        Material pillarMat = new Material(wallMat);
        pillarMat.color = new Color(0.35f, 0.4f, 0.45f);

        Vector2[] pillarPos = new Vector2[]
        {
            new Vector2(-12, -12), new Vector2(12, -12),
            new Vector2(-12, 12), new Vector2(12, 12),
            new Vector2(0, 15), new Vector2(0, -15)
        };

        foreach (var p in pillarPos)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "CoverPillar";
            pillar.transform.SetParent(arena.transform);
            pillar.transform.position = new Vector3(p.x, 2f, p.y);
            pillar.transform.localScale = new Vector3(2.5f, 4f, 2.5f);
            pillar.GetComponent<Renderer>().sharedMaterial = pillarMat;
        }

        // 2. Setup Player
        GameObject player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        if (player == null)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
        }
        player.tag = "Player";
        player.transform.position = new Vector3(0, 1.5f, 0);

        var mr = player.GetComponent<MeshRenderer>();
        if (mr != null) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

        var oldCapsule = player.GetComponent<CapsuleCollider>();
        if (oldCapsule != null) Object.DestroyImmediate(oldCapsule);

        var cc = player.GetComponent<CharacterController>() ?? player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = new Vector3(0, 1f, 0);

        var pCtrl = player.GetComponent<PlayerController>() ?? player.AddComponent<PlayerController>();
        pCtrl.walkSpeed = 6.5f;
        pCtrl.sprintSpeed = 10.5f;

        var pHealth = player.GetComponent<PlayerHealth>() ?? player.AddComponent<PlayerHealth>();

        Camera cam = Camera.main ?? player.GetComponentInChildren<Camera>();
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();
        }

        cam.transform.SetParent(player.transform);
        cam.transform.localPosition = new Vector3(0, 1.65f, 0);
        cam.transform.localRotation = Quaternion.identity;

        pCtrl.cameraTransform = cam.transform;

        var gun = cam.GetComponent<FPSGun>() ?? cam.gameObject.AddComponent<FPSGun>();
        gun.playerCamera = cam;

        // 3. Setup SpawnPoints
        GameObject spRoot = GameObject.Find("SpawnPoints");
        if (spRoot != null) Object.DestroyImmediate(spRoot);

        spRoot = new GameObject("SpawnPoints");
        float radius = 22f;
        int count = 8;
        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0.5f, Mathf.Sin(angle) * radius);
            GameObject sp = new GameObject($"SpawnPoint_{i + 1}");
            sp.transform.SetParent(spRoot.transform);
            sp.transform.position = pos;
        }

        // 4. Setup HUD Canvas
        GameObject oldCanvas = GameObject.Find("GameHUD_Canvas");
        if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);

        GameObject canvasObj = new GameObject("GameHUD_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<GraphicRaycaster>();
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        Font defaultFont = GetSafeDefaultFont();

        GameObject vigObj = new GameObject("DamageVignette");
        vigObj.transform.SetParent(canvasObj.transform, false);
        Image damageVignette = vigObj.AddComponent<Image>();
        damageVignette.color = new Color(1f, 0, 0, 0);
        RectTransform vigRt = vigObj.GetComponent<RectTransform>();
        vigRt.anchorMin = Vector2.zero;
        vigRt.anchorMax = Vector2.one;
        vigRt.sizeDelta = Vector2.zero;

        GameObject crosshairRoot = new GameObject("CrosshairRoot");
        crosshairRoot.transform.SetParent(canvasObj.transform, false);
        RectTransform cRt = crosshairRoot.AddComponent<RectTransform>();
        cRt.anchorMin = cRt.anchorMax = new Vector2(0.5f, 0.5f);
        cRt.sizeDelta = new Vector2(40, 40);
        CreateCrosshairLines(crosshairRoot.transform);

        GameObject hmObj = new GameObject("HitMarker");
        hmObj.transform.SetParent(crosshairRoot.transform, false);
        Image hitMarker = hmObj.AddComponent<Image>();
        hitMarker.color = new Color(1f, 0.2f, 0.2f, 0);
        CreateHitMarkerLines(hmObj.transform);

        GameObject hpPanel = new GameObject("HealthPanel");
        hpPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform hpRt = hpPanel.AddComponent<RectTransform>();
        hpRt.anchorMin = hpRt.anchorMax = hpRt.pivot = Vector2.zero;
        hpRt.anchoredPosition = new Vector2(40, 40);
        hpRt.sizeDelta = new Vector2(300, 50);

        GameObject hpBg = new GameObject("HP_Bg");
        hpBg.transform.SetParent(hpPanel.transform, false);
        Image bgImg = hpBg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        RectTransform bgRt = hpBg.GetComponent<RectTransform>();
        bgRt.sizeDelta = new Vector2(300, 30);
        bgRt.anchoredPosition = new Vector2(150, 25);

        GameObject hpSliderObj = new GameObject("HP_Slider");
        hpSliderObj.transform.SetParent(hpPanel.transform, false);
        Slider hpSlider = hpSliderObj.AddComponent<Slider>();
        hpSlider.minValue = 0f;
        hpSlider.maxValue = 1f;
        hpSlider.value = 1f;
        RectTransform sliderRt = hpSliderObj.GetComponent<RectTransform>();
        sliderRt.sizeDelta = new Vector2(290, 22);
        sliderRt.anchoredPosition = new Vector2(150, 25);

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(hpSliderObj.transform, false);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.85f, 0.3f);
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0, 0);
        fillRt.anchorMax = new Vector2(1, 1);
        fillRt.sizeDelta = Vector2.zero;
        fillRt.anchoredPosition = Vector2.zero;
        hpSlider.fillRect = fillRt;

        GameObject hpTxtObj = new GameObject("HP_Text");
        hpTxtObj.transform.SetParent(hpPanel.transform, false);
        Text hpText = hpTxtObj.AddComponent<Text>();
        hpText.font = defaultFont;
        hpText.text = "HP: 100 / 100";
        hpText.fontSize = 20;
        hpText.fontStyle = FontStyle.Bold;
        hpText.alignment = TextAnchor.MiddleCenter;
        hpText.color = Color.white;
        RectTransform txtRt = hpTxtObj.GetComponent<RectTransform>();
        txtRt.sizeDelta = new Vector2(200, 30);
        txtRt.anchoredPosition = new Vector2(150, 25);

        GameObject ammoPanel = new GameObject("AmmoPanel");
        ammoPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform ammoRt = ammoPanel.AddComponent<RectTransform>();
        ammoRt.anchorMin = ammoRt.anchorMax = ammoRt.pivot = new Vector2(1, 0);
        ammoRt.anchoredPosition = new Vector2(-40, 40);
        ammoRt.sizeDelta = new Vector2(240, 60);

        Text ammoText = ammoPanel.AddComponent<Text>();
        ammoText.font = defaultFont;
        ammoText.text = "AMMO: 30 / 120";
        ammoText.fontSize = 32;
        ammoText.fontStyle = FontStyle.Bold;
        ammoText.alignment = TextAnchor.MiddleRight;
        ammoText.color = new Color(1f, 0.85f, 0.3f);

        GameObject wavePanel = new GameObject("WavePanel");
        wavePanel.transform.SetParent(canvasObj.transform, false);
        RectTransform wRt = wavePanel.AddComponent<RectTransform>();
        wRt.anchorMin = wRt.anchorMax = wRt.pivot = new Vector2(0, 1);
        wRt.anchoredPosition = new Vector2(40, -40);
        wRt.sizeDelta = new Vector2(350, 90);

        Text waveText = wavePanel.AddComponent<Text>();
        waveText.font = defaultFont;
        waveText.text = "WAVE 1 / 5";
        waveText.fontSize = 34;
        waveText.fontStyle = FontStyle.Bold;
        waveText.color = new Color(1f, 0.95f, 0.9f);

        GameObject enemySubTextObj = new GameObject("EnemySubText");
        enemySubTextObj.transform.SetParent(wavePanel.transform, false);
        Text enemyCountText = enemySubTextObj.AddComponent<Text>();
        enemyCountText.font = defaultFont;
        enemyCountText.text = "Enemies Left: 5";
        enemyCountText.fontSize = 22;
        enemyCountText.color = new Color(1f, 0.4f, 0.4f);
        RectTransform ecRt = enemySubTextObj.GetComponent<RectTransform>();
        ecRt.anchoredPosition = new Vector2(0, -40);
        ecRt.sizeDelta = new Vector2(300, 30);

        GameObject notifObj = new GameObject("NotifBanner");
        notifObj.transform.SetParent(canvasObj.transform, false);
        RectTransform nRt = notifObj.AddComponent<RectTransform>();
        nRt.anchorMin = nRt.anchorMax = nRt.pivot = new Vector2(0.5f, 1);
        nRt.anchoredPosition = new Vector2(0, -60);
        nRt.sizeDelta = new Vector2(800, 60);

        Text notifText = notifObj.AddComponent<Text>();
        notifText.font = defaultFont;
        notifText.text = "";
        notifText.fontSize = 28;
        notifText.fontStyle = FontStyle.Bold;
        notifText.alignment = TextAnchor.MiddleCenter;
        notifText.color = new Color(1f, 0.95f, 0.3f);

        EnsureEventSystem();

        // 5. Setup Managers Object
        GameObject managers = GameObject.Find("Managers") ?? new GameObject("Managers");

        GameManager gm = managers.GetComponent<GameManager>() ?? managers.AddComponent<GameManager>();
        SoundManager sm = managers.GetComponent<SoundManager>() ?? managers.AddComponent<SoundManager>();
        WaveManager wm = managers.GetComponent<WaveManager>() ?? managers.AddComponent<WaveManager>();
        LootDrop ld = managers.GetComponent<LootDrop>() ?? managers.AddComponent<LootDrop>();

        GameUIManager ui = managers.GetComponent<GameUIManager>() ?? managers.AddComponent<GameUIManager>();
        ui.hudCanvas = canvas;
        ui.hpSlider = hpSlider;
        ui.hpText = hpText;
        ui.ammoText = ammoText;
        ui.waveText = waveText;
        ui.enemyCountText = enemyCountText;
        ui.notifText = notifText;
        ui.hitMarker = hitMarker;
        ui.damageVignette = damageVignette;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log($"[FPSHierarchyBuilder] Saved Gameplay Scene to {GameplayScenePath}");
    }

    private static void BuildMainMenuScene()
    {
        Scene menuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject lightObj = new GameObject("Directional Light");
        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.2f;
        l.color = new Color(0.9f, 0.95f, 1f);
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0);

        GameObject camObj = new GameObject("Main Camera");
        Camera cam = camObj.AddComponent<Camera>();
        camObj.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.1f, 0.14f);
        camObj.AddComponent<AudioListener>();

        EnsureEventSystem();

        GameObject canvasObj = new GameObject("MainMenu_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<GraphicRaycaster>();
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        Font defaultFont = GetSafeDefaultFont();

        MainMenuController menuCtrl = canvasObj.AddComponent<MainMenuController>();
        menuCtrl.gameplaySceneName = "SampleScene";

        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(canvasObj.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.font = defaultFont;
        titleText.text = "3D FPS SURVIVAL";
        titleText.fontSize = 80;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.85f, 0.2f);
        RectTransform tRt = titleObj.GetComponent<RectTransform>();
        tRt.anchoredPosition = new Vector2(0, 240);
        tRt.sizeDelta = new Vector2(1000, 120);

        EditorSceneManager.SaveScene(menuScene, MainMenuScenePath);
        Debug.Log($"[FPSHierarchyBuilder] Saved MainMenu Scene to {MainMenuScenePath}");
    }

    private static void UpdateBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(MainMenuScenePath, true),
            new EditorBuildSettingsScene(GameplayScenePath, true)
        };

        EditorBuildSettings.scenes = scenes;
        Debug.Log("[FPSHierarchyBuilder] Updated EditorBuildSettings with MainMenu and SampleScene!");
    }

    private static void EnsureEventSystem()
    {
        EventSystem es = Object.FindAnyObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            es = esObj.AddComponent<EventSystem>();
        }

        var standalone = es.GetComponent<StandaloneInputModule>();
        if (standalone != null) Object.DestroyImmediate(standalone);

#if ENABLE_INPUT_SYSTEM
        if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
        {
            es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
#else
        if (es.GetComponent<StandaloneInputModule>() == null)
        {
            es.gameObject.AddComponent<StandaloneInputModule>();
        }
#endif
    }

    private static void CreateWall(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static void CreateCrosshairLines(Transform parent)
    {
        float length = 12f;
        float thickness = 2.5f;
        float offset = 7f;
        Color cColor = new Color(1f, 1f, 1f, 0.85f);

        CreateUiBar(parent, new Vector2(0, offset + length / 2), new Vector2(thickness, length), cColor);
        CreateUiBar(parent, new Vector2(0, -(offset + length / 2)), new Vector2(thickness, length), cColor);
        CreateUiBar(parent, new Vector2(-(offset + length / 2), 0), new Vector2(length, thickness), cColor);
        CreateUiBar(parent, new Vector2(offset + length / 2, 0), new Vector2(length, thickness), cColor);
        CreateUiBar(parent, Vector2.zero, new Vector2(3, 3), new Color(1f, 0.3f, 0.3f, 0.9f));
    }

    private static void CreateHitMarkerLines(Transform parent)
    {
        float size = 16f;
        float thick = 2.5f;
        Color hColor = new Color(1f, 0.15f, 0.15f, 0.9f);

        GameObject line1 = CreateUiBar(parent, Vector2.zero, new Vector2(size, thick), hColor);
        line1.transform.localRotation = Quaternion.Euler(0, 0, 45f);

        GameObject line2 = CreateUiBar(parent, Vector2.zero, new Vector2(size, thick), hColor);
        line2.transform.localRotation = Quaternion.Euler(0, 0, -45f);
    }

    private static GameObject CreateUiBar(Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        GameObject obj = new GameObject("Line");
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = color;
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return obj;
    }
}