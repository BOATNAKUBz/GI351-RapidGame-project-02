using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Arena Settings")]
    public float arenaSize = 50f;
    public float wallHeight = 4.5f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        BootstrapGameSystems();
    }

    void BootstrapGameSystems()
    {
        // 1. Ensure SoundManager
        if (SoundManager.Instance == null && FindAnyObjectByType<SoundManager>() == null)
        {
            GameObject smObj = new GameObject("SoundManager");
            smObj.AddComponent<SoundManager>();
        }

        // 2. Ensure GameUIManager
        if (GameUIManager.Instance == null && FindAnyObjectByType<GameUIManager>() == null)
        {
            GameObject uiObj = new GameObject("GameUIManager");
            uiObj.AddComponent<GameUIManager>();
        }

        // Ensure EventSystem has correct InputModule
        var es = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            es = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        }
        var standalone = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        if (standalone != null) Destroy(standalone);

#if ENABLE_INPUT_SYSTEM
        if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
        {
            es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
#endif

        // 3. Ensure WaveManager
        if (WaveManager.Instance == null && FindAnyObjectByType<WaveManager>() == null)
        {
            GameObject wmObj = new GameObject("WaveManager");
            wmObj.AddComponent<WaveManager>();
        }

        // 4. Ensure LootDrop
        if (LootDrop.Instance == null && FindAnyObjectByType<LootDrop>() == null)
        {
            GameObject ldObj = new GameObject("LootDrop");
            ldObj.AddComponent<LootDrop>();
        }

        // 5. Setup Arena Walls and Ground
        SetupArena();

        // 6. Ensure Player & FPS Camera
        SetupPlayerAndCamera();
    }

    void SetupArena()
    {
        // Check if arena boundary already exists
        if (GameObject.Find("Arena_Boundaries") != null) return;

        GameObject arenaRoot = new GameObject("Arena_Boundaries");

        // Ground check
        GameObject ground = GameObject.Find("Plane");
        if (ground == null)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Plane";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(arenaSize / 10f, 1, arenaSize / 10f);
        }
        else
        {
            ground.transform.localScale = new Vector3(arenaSize / 10f, 1, arenaSize / 10f);
        }

        Material wallMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        wallMat.color = new Color(0.25f, 0.28f, 0.32f);

        Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        groundMat.color = new Color(0.18f, 0.2f, 0.22f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        float half = arenaSize / 2f;
        float thick = 1f;

        // North Wall
        CreateWall(arenaRoot.transform, new Vector3(0, wallHeight / 2f, half), new Vector3(arenaSize + thick, wallHeight, thick), wallMat);
        // South Wall
        CreateWall(arenaRoot.transform, new Vector3(0, wallHeight / 2f, -half), new Vector3(arenaSize + thick, wallHeight, thick), wallMat);
        // East Wall
        CreateWall(arenaRoot.transform, new Vector3(half, wallHeight / 2f, 0), new Vector3(thick, wallHeight, arenaSize + thick), wallMat);
        // West Wall
        CreateWall(arenaRoot.transform, new Vector3(-half, wallHeight / 2f, 0), new Vector3(thick, wallHeight, arenaSize + thick), wallMat);

        // A few decorative cover pillars
        Material pillarMat = new Material(wallMat);
        pillarMat.color = new Color(0.35f, 0.4f, 0.45f);

        Vector2[] pillarPos = new Vector2[]
        {
            new Vector2(-12, -12),
            new Vector2(12, -12),
            new Vector2(-12, 12),
            new Vector2(12, 12),
            new Vector2(0, 15),
            new Vector2(0, -15)
        };

        foreach (var p in pillarPos)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "CoverPillar";
            pillar.transform.SetParent(arenaRoot.transform);
            pillar.transform.position = new Vector3(p.x, 2f, p.y);
            pillar.transform.localScale = new Vector3(2.5f, 4f, 2.5f);
            pillar.GetComponent<Renderer>().sharedMaterial = pillarMat;
        }
    }

    void CreateWall(Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall";
        wall.transform.SetParent(parent);
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().sharedMaterial = mat;
    }

    void SetupPlayerAndCamera()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            var pCtrl = FindAnyObjectByType<PlayerController>();
            if (pCtrl != null) player = pCtrl.gameObject;
        }

        if (player == null)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = new Vector3(0, 1.5f, 0);
        }

        // Make sure capsule mesh doesn't block player vision
        var meshRenderer = player.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }

        // Setup CharacterController
        var cc = player.GetComponent<CharacterController>();
        if (cc == null) cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = new Vector3(0, 1f, 0);

        // Remove old CapsuleCollider if any
        var oldCapsule = player.GetComponent<CapsuleCollider>();
        if (oldCapsule != null) Destroy(oldCapsule);

        // Ensure PlayerController
        var pController = player.GetComponent<PlayerController>();
        if (pController == null) pController = player.AddComponent<PlayerController>();
        pController.walkSpeed = 6.5f;
        pController.sprintSpeed = 10.5f;

        // Ensure PlayerHealth
        var pHealth = player.GetComponent<PlayerHealth>();
        if (pHealth == null) pHealth = player.AddComponent<PlayerHealth>();

        // Ensure Camera is childed to Player at eye level
        Camera cam = Camera.main;
        if (cam == null) cam = player.GetComponentInChildren<Camera>();
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

        // Remove old follow script if attached
        var oldFollow = cam.GetComponent("CameraFollow");
        if (oldFollow != null) Destroy(oldFollow);

        pController.cameraTransform = cam.transform;

        // Ensure FPSGun is on Camera
        var gun = cam.GetComponent<FPSGun>();
        if (gun == null)
        {
            gun = cam.gameObject.AddComponent<FPSGun>();
            gun.playerCamera = cam;
        }
    }
}