using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class SetupWeaponsAndPlayer
{
    private static readonly string ShotgunPrefabPath = "Assets/Low Poly ShotGun Weapon Pack 1/Prefabs/Weapons/ShotGun_A.prefab";
    private static readonly string RevolverPrefabPath = "Assets/eretichable Technologies/Revolver Gun (Low Poly)/Prefabs/Revolver_LP.prefab";
    private static readonly string AxePrefabPath = "Assets/NzBulletStudio/NZ Melee Pack/Prefabs/NZ Battle Axe.prefab";
    private static readonly string PlayerModelPath = "Assets/Models/Player/Man.obj";

    static SetupWeaponsAndPlayer()
    {
        EditorApplication.delayCall += () =>
        {
            if (!SessionState.GetBool("SetupWeaponsDone_v3", false))
            {
                SessionState.SetBool("SetupWeaponsDone_v3", true);
                ExecuteAllQuiet();
            }
        };
    }

    [MenuItem("Tools/FPS Prototype/Setup 3 Weapons, Inventory & Player Hands")]
    public static void ExecuteAll()
    {
        ExecuteAllQuiet();

        EditorUtility.DisplayDialog("FPS Weapon & Player Setup",
            "Setup completed successfully!\n\n" +
            "1. 3 Weapon Slots configured:\n" +
            "   [1] Shotgun (Pellet spread, heavy kickback, pump-action)\n" +
            "   [2] Revolver (Single-shot, high accuracy & damage)\n" +
            "   [3] Battle Axe (Close-range melee swing with slash effect)\n\n" +
            "2. First-person view with visible hands/arms holding each weapon.\n" +
            "3. Interactive 'E' weapon pickups placed in the world.\n" +
            "4. Player HP bar preserved for easy Canvas editing!",
            "Great!");
    }

    public static void ExecuteAllQuiet()
    {
        SetupPlayerModelAsset();

        // Update active scene and Map 1 / SampleScene
        string[] targetScenes = new string[] { "Assets/Scenes/SampleScene.unity", "Assets/Scenes/Map 1.unity" };

        foreach (var scenePath in targetScenes)
        {
            if (File.Exists(scenePath))
            {
                SetupScene(scenePath);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void SetupPlayerModelAsset()
    {
        if (File.Exists(PlayerModelPath))
        {
            ModelImporter importer = AssetImporter.GetAtPath(PlayerModelPath) as ModelImporter;
            if (importer != null)
            {
                importer.globalScale = 0.01f; // Convert cm to meters
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.SaveAndReimport();
            }
        }
    }

    public static void SetupScene(string scenePath)
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        var scene = activeScene;
        if (activeScene.path != scenePath)
        {
            scene = EditorSceneManager.OpenScene(scenePath);
        }

        // 1. Setup Player
        GameObject player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogWarning($"[SetupWeapons] Player not found in {scenePath}");
            return;
        }

        Camera cam = player.GetComponentInChildren<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning($"[SetupWeapons] Main Camera not found on Player in {scenePath}");
            return;
        }

        // Remove old FPSGun if present
        FPSGun oldGun = cam.GetComponent<FPSGun>();
        if (oldGun != null) Object.DestroyImmediate(oldGun);

        // Remove old Gun prefab instances under camera
        for (int i = cam.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = cam.transform.GetChild(i);
            if (child.name.Contains("Gun") || child.name.Contains("ViewModel") || child.name.Contains("Weapon"))
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        // Attach Character 3D Model to player body for shadows
        SetupPlayerBodyModel(player);

        // 2. Setup ViewModel Root & 3 Weapons
        GameObject vmRoot = new GameObject("FPSViewModelRoot");
        vmRoot.transform.SetParent(cam.transform, false);
        vmRoot.transform.localPosition = Vector3.zero;
        vmRoot.transform.localRotation = Quaternion.identity;

        PlayerInventory inventory = cam.GetComponent<PlayerInventory>() ?? cam.gameObject.AddComponent<PlayerInventory>();

        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material sleeveMat = new Material(urpShader);
        sleeveMat.color = new Color(0.2f, 0.25f, 0.3f); // Dark tactical shirt sleeve
        Material skinMat = new Material(urpShader);
        skinMat.color = new Color(0.86f, 0.68f, 0.54f); // Skin tone

        // Build Slot 0: Shotgun
        FPSWeapon shotgun = BuildShotgunViewModel(vmRoot.transform, sleeveMat, skinMat);

        // Build Slot 1: Revolver
        FPSWeapon revolver = BuildRevolverViewModel(vmRoot.transform, sleeveMat, skinMat);

        // Build Slot 2: Battle Axe
        FPSWeapon axe = BuildAxeViewModel(vmRoot.transform, sleeveMat, skinMat);

        inventory.slots = new FPSWeapon[] { shotgun, revolver, axe };
        inventory.activeSlotIndex = 1; // Default starting weapon is Revolver

        // 3. Place In-World Weapon Pickups
        PlaceWorldPickups(player.transform.position);

        // 4. Update HUD Canvas for Interact Prompt & Weapon Slots (Preserving HP Slider!)
        UpdateCanvasUI();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[SetupWeapons] Configured weapons and player for {scenePath}");
    }

    private static void SetupPlayerBodyModel(GameObject player)
    {
        // Remove existing model child if already added
        Transform existing = player.transform.Find("PlayerBodyModel");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerModelPath);
        if (modelAsset != null)
        {
            GameObject bodyObj = Object.Instantiate(modelAsset, player.transform);
            bodyObj.name = "PlayerBodyModel";
            bodyObj.transform.localPosition = new Vector3(0, 0, 0);
            bodyObj.transform.localRotation = Quaternion.Euler(0, 0, 0);
            bodyObj.transform.localScale = Vector3.one * 1.0f;

            // Set all renderers to ShadowsOnly so camera FOV isn't blocked by head/torso
            Renderer[] renderers = bodyObj.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }

            // Remove any colliders on the visual model
            Collider[] cols = bodyObj.GetComponentsInChildren<Collider>();
            foreach (var c in cols) Object.DestroyImmediate(c);
        }
    }

    private static FPSWeapon BuildShotgunViewModel(Transform parent, Material sleeveMat, Material skinMat)
    {
        GameObject wObj = new GameObject("Weapon_Shotgun");
        wObj.transform.SetParent(parent, false);
        wObj.transform.localPosition = new Vector3(0.25f, -0.23f, 0.48f);
        wObj.transform.localRotation = Quaternion.Euler(1.5f, -1f, 0f);

        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShotgunPrefabPath);
        GameObject gunVisual;
        if (modelPrefab != null)
        {
            gunVisual = Object.Instantiate(modelPrefab, wObj.transform);
            gunVisual.name = "ShotgunModel";
            gunVisual.transform.localPosition = Vector3.zero;
            gunVisual.transform.localRotation = Quaternion.Euler(0, 0, 0);
            gunVisual.transform.localScale = Vector3.one * 0.9f;
        }
        else
        {
            gunVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gunVisual.transform.SetParent(wObj.transform, false);
            gunVisual.transform.localScale = new Vector3(0.08f, 0.1f, 0.8f);
            Object.DestroyImmediate(gunVisual.GetComponent<Collider>());
        }

        // Attach FPS Hands for Shotgun
        GameObject handsRoot = new GameObject("Hands");
        handsRoot.transform.SetParent(wObj.transform, false);
        BuildFPSHands(handsRoot.transform, sleeveMat, skinMat,
            new Vector3(-0.04f, -0.06f, 0.05f), Quaternion.Euler(-15f, 10f, -20f),  // Right hand (grip/trigger)
            new Vector3(-0.06f, -0.04f, 0.42f), Quaternion.Euler(15f, -15f, 35f));   // Left hand (pump/fore-end)

        // Muzzle Point
        GameObject muzzle = new GameObject("MuzzlePoint");
        muzzle.transform.SetParent(wObj.transform, false);
        muzzle.transform.localPosition = new Vector3(0, 0.06f, 0.85f);

        FPSWeapon weapon = wObj.AddComponent<FPSWeapon>();
        weapon.weaponType = WeaponType.Shotgun;
        weapon.weaponName = "Shotgun";
        weapon.slotIndex = 0;
        weapon.isUnlocked = false; // Starts locked, pick up with 'E'!
        weapon.damage = 22f; // CS2 shotgun: 22 dmg x 9 pellets = 198 point-blank burst!
        weapon.pelletCount = 9;
        weapon.spreadAngle = 3.8f;
        weapon.fireRate = 0.82f;
        weapon.range = 45f;
        weapon.magazineSize = 6;
        weapon.currentAmmo = 6;
        weapon.reserveAmmo = 24;
        weapon.reloadTime = 2.2f;
        weapon.gunTransform = wObj.transform;
        weapon.muzzlePoint = muzzle.transform;
        weapon.handsTransform = handsRoot.transform;
        weapon.recoilKickback = 0.13f;
        weapon.recoilRotation = new Vector3(-14f, 1.5f, -1f);

        return weapon;
    }

    private static FPSWeapon BuildRevolverViewModel(Transform parent, Material sleeveMat, Material skinMat)
    {
        GameObject wObj = new GameObject("Weapon_Revolver");
        wObj.transform.SetParent(parent, false);
        wObj.transform.localPosition = new Vector3(0.22f, -0.2f, 0.42f);
        wObj.transform.localRotation = Quaternion.Euler(1f, -1f, 0f);

        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RevolverPrefabPath);
        GameObject gunVisual;
        if (modelPrefab != null)
        {
            gunVisual = Object.Instantiate(modelPrefab, wObj.transform);
            gunVisual.name = "RevolverModel";
            gunVisual.transform.localPosition = Vector3.zero;
            gunVisual.transform.localRotation = Quaternion.Euler(0, 0, 0);
            gunVisual.transform.localScale = Vector3.one * 0.9f;
        }
        else
        {
            gunVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gunVisual.transform.SetParent(wObj.transform, false);
            gunVisual.transform.localScale = new Vector3(0.06f, 0.12f, 0.35f);
            Object.DestroyImmediate(gunVisual.GetComponent<Collider>());
        }

        // Attach FPS Hands for Revolver
        GameObject handsRoot = new GameObject("Hands");
        handsRoot.transform.SetParent(wObj.transform, false);
        BuildFPSHands(handsRoot.transform, sleeveMat, skinMat,
            new Vector3(-0.02f, -0.06f, -0.05f), Quaternion.Euler(-10f, 5f, -15f), // Right hand
            new Vector3(-0.05f, -0.09f, -0.07f), Quaternion.Euler(-5f, 15f, 20f));  // Left hand supporting

        // Muzzle Point
        GameObject muzzle = new GameObject("MuzzlePoint");
        muzzle.transform.SetParent(wObj.transform, false);
        muzzle.transform.localPosition = new Vector3(0, 0.05f, 0.35f);

        FPSWeapon weapon = wObj.AddComponent<FPSWeapon>();
        weapon.weaponType = WeaponType.Revolver;
        weapon.weaponName = "Revolver";
        weapon.slotIndex = 1;
        weapon.isUnlocked = true; // Initial starting weapon!
        weapon.damage = 55f;
        weapon.fireRate = 0.32f;
        weapon.range = 85f;
        weapon.magazineSize = 6;
        weapon.currentAmmo = 6;
        weapon.reserveAmmo = 36;
        weapon.reloadTime = 1.6f;
        weapon.gunTransform = wObj.transform;
        weapon.muzzlePoint = muzzle.transform;
        weapon.handsTransform = handsRoot.transform;
        weapon.recoilKickback = 0.08f;
        weapon.recoilRotation = new Vector3(-8f, 0.5f, 0f);

        return weapon;
    }

    private static FPSWeapon BuildAxeViewModel(Transform parent, Material sleeveMat, Material skinMat)
    {
        GameObject wObj = new GameObject("Weapon_Axe");
        wObj.transform.SetParent(parent, false);
        wObj.transform.localPosition = new Vector3(0.24f, -0.24f, 0.44f);
        wObj.transform.localRotation = Quaternion.Euler(8f, -15f, 10f);

        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AxePrefabPath);
        GameObject axeVisual;
        if (modelPrefab != null)
        {
            axeVisual = Object.Instantiate(modelPrefab, wObj.transform);
            axeVisual.name = "AxeModel";
            axeVisual.transform.localPosition = new Vector3(0, 0, 0);
            axeVisual.transform.localRotation = Quaternion.Euler(0, 90f, 0);
            axeVisual.transform.localScale = Vector3.one * 0.8f;
        }
        else
        {
            axeVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            axeVisual.transform.SetParent(wObj.transform, false);
            axeVisual.transform.localScale = new Vector3(0.06f, 0.45f, 0.06f);
            Object.DestroyImmediate(axeVisual.GetComponent<Collider>());
        }

        // Attach FPS Hands for Battle Axe
        GameObject handsRoot = new GameObject("Hands");
        handsRoot.transform.SetParent(wObj.transform, false);
        BuildFPSHands(handsRoot.transform, sleeveMat, skinMat,
            new Vector3(-0.03f, 0.05f, 0.02f), Quaternion.Euler(-20f, 10f, -25f),  // Right hand (upper grip)
            new Vector3(-0.04f, -0.15f, 0.01f), Quaternion.Euler(-15f, 10f, -25f)); // Left hand (lower grip)

        FPSWeapon weapon = wObj.AddComponent<FPSWeapon>();
        weapon.weaponType = WeaponType.Axe;
        weapon.weaponName = "Battle Axe";
        weapon.slotIndex = 2;
        weapon.isUnlocked = false; // Starts locked, pick up with 'E'!
        weapon.isMelee = true;
        weapon.damage = 80f;
        weapon.fireRate = 0.55f;
        weapon.range = 2.6f;
        weapon.meleeRadius = 0.75f;
        weapon.gunTransform = wObj.transform;
        weapon.handsTransform = handsRoot.transform;
        weapon.recoilKickback = 0.05f;
        weapon.recoilRotation = new Vector3(5f, -10f, 5f);

        return weapon;
    }

    private static void BuildFPSHands(Transform parent, Material sleeveMat, Material skinMat,
        Vector3 rightHandPos, Quaternion rightHandRot,
        Vector3 leftHandPos, Quaternion leftHandRot)
    {
        // Right Arm & Hand
        GameObject rArm = new GameObject("RightArm");
        rArm.transform.SetParent(parent, false);
        rArm.transform.localPosition = rightHandPos;
        rArm.transform.localRotation = rightHandRot;

        // Sleeve (Forearm)
        GameObject rSleeve = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rSleeve.name = "ForearmSleeve";
        rSleeve.transform.SetParent(rArm.transform, false);
        rSleeve.transform.localPosition = new Vector3(0, -0.16f, -0.16f);
        rSleeve.transform.localRotation = Quaternion.Euler(45f, 0, 0);
        rSleeve.transform.localScale = new Vector3(0.08f, 0.14f, 0.08f);
        rSleeve.GetComponent<Renderer>().sharedMaterial = sleeveMat;
        Object.DestroyImmediate(rSleeve.GetComponent<Collider>());

        // Hand
        GameObject rHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rHand.name = "HandGrip";
        rHand.transform.SetParent(rArm.transform, false);
        rHand.transform.localPosition = Vector3.zero;
        rHand.transform.localScale = new Vector3(0.075f, 0.065f, 0.085f);
        rHand.GetComponent<Renderer>().sharedMaterial = skinMat;
        Object.DestroyImmediate(rHand.GetComponent<Collider>());

        // Left Arm & Hand
        GameObject lArm = new GameObject("LeftArm");
        lArm.transform.SetParent(parent, false);
        lArm.transform.localPosition = leftHandPos;
        lArm.transform.localRotation = leftHandRot;

        // Sleeve
        GameObject lSleeve = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lSleeve.name = "ForearmSleeve";
        lSleeve.transform.SetParent(lArm.transform, false);
        lSleeve.transform.localPosition = new Vector3(0, -0.16f, -0.16f);
        lSleeve.transform.localRotation = Quaternion.Euler(45f, 0, 0);
        lSleeve.transform.localScale = new Vector3(0.08f, 0.14f, 0.08f);
        lSleeve.GetComponent<Renderer>().sharedMaterial = sleeveMat;
        Object.DestroyImmediate(lSleeve.GetComponent<Collider>());

        // Hand
        GameObject lHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lHand.name = "HandGrip";
        lHand.transform.SetParent(lArm.transform, false);
        lHand.transform.localPosition = Vector3.zero;
        lHand.transform.localScale = new Vector3(0.075f, 0.065f, 0.085f);
        lHand.GetComponent<Renderer>().sharedMaterial = skinMat;
        Object.DestroyImmediate(lHand.GetComponent<Collider>());
    }

    private static void PlaceWorldPickups(Vector3 playerPos)
    {
        // Place Pickups near player start
        Vector3 shotgunPos = playerPos + new Vector3(2.5f, 0.7f, 3.5f);
        Vector3 axePos = playerPos + new Vector3(-2.5f, 0.7f, 3.5f);
        Vector3 revolverPos = playerPos + new Vector3(0f, 0.7f, 5.0f);

        CreateWorldPickup("Pickup_Shotgun", ShotgunPrefabPath, WeaponType.Shotgun, "Shotgun", shotgunPos, new Color(1f, 0.5f, 0.1f));
        CreateWorldPickup("Pickup_Axe", AxePrefabPath, WeaponType.Axe, "Battle Axe", axePos, new Color(0.9f, 0.2f, 0.2f));
        CreateWorldPickup("Pickup_Revolver", RevolverPrefabPath, WeaponType.Revolver, "Revolver", revolverPos, new Color(0.2f, 0.8f, 1f));
    }

    private static void CreateWorldPickup(string name, string prefabPath, WeaponType type, string wName, Vector3 position, Color glowColor)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null) Object.DestroyImmediate(existing);

        GameObject pObj = new GameObject(name);
        pObj.transform.position = position;

        SphereCollider sc = pObj.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 1.6f;

        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (modelPrefab != null)
        {
            GameObject visual = Object.Instantiate(modelPrefab, pObj.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(0, 0, 0);
            visual.transform.localScale = Vector3.one * 1.2f;

            // Remove existing colliders from visual
            foreach (var col in visual.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
        }

        // Add Light aura
        GameObject lightObj = new GameObject("PickupLight");
        lightObj.transform.SetParent(pObj.transform, false);
        lightObj.transform.localPosition = new Vector3(0, 0.3f, 0);
        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = glowColor;
        l.range = 4f;
        l.intensity = 1.8f;

        WeaponPickup pickup = pObj.AddComponent<WeaponPickup>();
        pickup.weaponType = type;
        pickup.weaponName = wName;
        pickup.bonusAmmo = (type == WeaponType.Shotgun) ? 12 : 18;
    }

    private static void UpdateCanvasUI()
    {
        GameObject canvasObj = GameObject.Find("GameHUD_Canvas");
        if (canvasObj == null) return;

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 1. Ensure Interact Prompt Text exists
        Transform ip = canvasObj.transform.Find("InteractPrompt");
        if (ip == null)
        {
            GameObject ipObj = new GameObject("InteractPrompt");
            ipObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rt = ipObj.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, -90);
            rt.sizeDelta = new Vector2(500, 50);

            Text txt = ipObj.AddComponent<Text>();
            txt.font = defaultFont;
            txt.fontSize = 24;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(1f, 0.9f, 0.2f);

            Outline outline = ipObj.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            ipObj.SetActive(false);
        }

        // 2. Ensure Weapon Slots Panel exists
        Transform ws = canvasObj.transform.Find("WeaponSlotsPanel");
        if (ws == null)
        {
            GameObject wsObj = new GameObject("WeaponSlotsPanel");
            wsObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rt = wsObj.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(-40, 110);
            rt.sizeDelta = new Vector2(400, 40);

            Text txt = wsObj.AddComponent<Text>();
            txt.font = defaultFont;
            txt.fontSize = 20;
            txt.alignment = TextAnchor.MiddleRight;
            txt.color = Color.white;

            Outline outline = wsObj.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.85f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        // Build or ensure clean HealthBar exists in Canvas
        BuildCleanHealthBar(canvasObj.GetComponent<Canvas>());
    }

    [MenuItem("Tools/FPS Prototype/Create Clean HP Bar Now")]
    public static void CreateCleanHpBarMenu()
    {
        string[] targetScenes = new string[] { "Assets/Scenes/Map 1.unity", "Assets/Scenes/SampleScene.unity" };
        foreach (var scenePath in targetScenes)
        {
            if (File.Exists(scenePath))
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                GameObject canvasObj = GameObject.Find("GameHUD_Canvas");
                if (canvasObj != null)
                {
                    Canvas c = canvasObj.GetComponent<Canvas>();
                    BuildCleanHealthBar(c);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
        }
        EditorUtility.DisplayDialog("HP Bar Builder", "Created a clean, modern, and beautiful HP Bar in Canvas!", "Awesome!");
    }

    public static void BuildCleanHealthBar(Canvas canvas)
    {
        if (canvas == null) return;

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Remove old / corrupted HealthPanel if present
        Transform oldHp = canvas.transform.Find("HealthPanel");
        if (oldHp != null)
        {
            Object.DestroyImmediate(oldHp.gameObject);
        }

        // 1. Root HealthPanel
        GameObject hpPanel = new GameObject("HealthPanel");
        hpPanel.transform.SetParent(canvas.transform, false);
        RectTransform hpRt = hpPanel.AddComponent<RectTransform>();
        hpRt.anchorMin = new Vector2(0, 0);
        hpRt.anchorMax = new Vector2(0, 0);
        hpRt.pivot = new Vector2(0, 0);
        hpRt.anchoredPosition = new Vector2(50, 45);
        hpRt.sizeDelta = new Vector2(360, 46);

        // 2. Dark Frame Background
        GameObject bgObj = new GameObject("HP_Frame_Bg");
        bgObj.transform.SetParent(hpPanel.transform, false);
        RectTransform bgRt = bgObj.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        bgRt.anchoredPosition = Vector2.zero;

        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.1f, 0.13f, 0.92f);

        Outline bgOutline = bgObj.AddComponent<Outline>();
        bgOutline.effectColor = new Color(0.25f, 0.35f, 0.45f, 0.85f);
        bgOutline.effectDistance = new Vector2(2f, -2f);

        // 3. Heart / Medical Cross Icon
        GameObject iconObj = new GameObject("HP_Icon");
        iconObj.transform.SetParent(hpPanel.transform, false);
        RectTransform iconRt = iconObj.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0, 0.5f);
        iconRt.anchorMax = new Vector2(0, 0.5f);
        iconRt.pivot = new Vector2(0, 0.5f);
        iconRt.anchoredPosition = new Vector2(10, 0);
        iconRt.sizeDelta = new Vector2(30, 30);

        Text iconTxt = iconObj.AddComponent<Text>();
        iconTxt.font = defaultFont;
        iconTxt.text = "+";
        iconTxt.fontSize = 28;
        iconTxt.fontStyle = FontStyle.Bold;
        iconTxt.alignment = TextAnchor.MiddleCenter;
        iconTxt.color = new Color(0.2f, 0.92f, 0.4f);

        Outline iconOutline = iconObj.AddComponent<Outline>();
        iconOutline.effectColor = new Color(0, 0, 0, 0.9f);
        iconOutline.effectDistance = new Vector2(1f, -1f);

        // 4. Track Background for Slider
        GameObject trackObj = new GameObject("HP_Track");
        trackObj.transform.SetParent(hpPanel.transform, false);
        RectTransform trackRt = trackObj.AddComponent<RectTransform>();
        trackRt.anchorMin = new Vector2(0, 0);
        trackRt.anchorMax = new Vector2(1, 1);
        trackRt.pivot = new Vector2(0.5f, 0.5f);
        trackRt.anchoredPosition = new Vector2(18, 0);
        trackRt.sizeDelta = new Vector2(-52, -14);

        Image trackImg = trackObj.AddComponent<Image>();
        trackImg.color = new Color(0.14f, 0.16f, 0.18f, 1f);

        // 5. HP_Slider
        GameObject sliderObj = new GameObject("HP_Slider");
        sliderObj.transform.SetParent(hpPanel.transform, false);
        RectTransform sliderRt = sliderObj.AddComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0, 0);
        sliderRt.anchorMax = new Vector2(1, 1);
        sliderRt.pivot = new Vector2(0.5f, 0.5f);
        sliderRt.anchoredPosition = new Vector2(18, 0);
        sliderRt.sizeDelta = new Vector2(-52, -14);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };

        // Fill Area & Fill
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.sizeDelta = Vector2.zero;
        fillAreaRt.anchoredPosition = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;
        fillRt.anchoredPosition = Vector2.zero;

        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.88f, 0.35f);
        slider.fillRect = fillRt;

        // 6. HP_Text
        GameObject txtObj = new GameObject("HP_Text");
        txtObj.transform.SetParent(hpPanel.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = new Vector2(0, 0);
        txtRt.anchorMax = new Vector2(1, 1);
        txtRt.pivot = new Vector2(0.5f, 0.5f);
        txtRt.anchoredPosition = new Vector2(18, 0);
        txtRt.sizeDelta = new Vector2(-52, 0);

        Text hpText = txtObj.AddComponent<Text>();
        hpText.font = defaultFont;
        hpText.text = "HP: 100 / 100";
        hpText.fontSize = 17;
        hpText.fontStyle = FontStyle.Bold;
        hpText.alignment = TextAnchor.MiddleCenter;
        hpText.color = Color.white;

        Outline txtOutline = txtObj.AddComponent<Outline>();
        txtOutline.effectColor = new Color(0, 0, 0, 0.95f);
        txtOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // 7. Update GameUIManager references
        GameUIManager ui = Object.FindAnyObjectByType<GameUIManager>();
        if (ui != null)
        {
            ui.hpSlider = slider;
            ui.hpText = hpText;
            EditorUtility.SetDirty(ui);
        }
    }
}

