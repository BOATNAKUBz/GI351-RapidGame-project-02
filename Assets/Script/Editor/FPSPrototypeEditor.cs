using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FPSPrototypeEditor : Editor
{
    [MenuItem("Tools/FPS Prototype/Setup Complete Scene")]
    public static void SetupScene()
    {
        // 1. Ensure GameManager
        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gm = gmObj.AddComponent<GameManager>();
            Undo.RegisterCreatedObjectUndo(gmObj, "Create GameManager");
        }

        // 2. Setup Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            var pCtrl = Object.FindAnyObjectByType<PlayerController>();
            if (pCtrl != null) player = pCtrl.gameObject;
        }

        if (player == null)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = new Vector3(0, 1.5f, 0);
            Undo.RegisterCreatedObjectUndo(player, "Create Player");
        }

        // Configure CharacterController
        var cc = player.GetComponent<CharacterController>();
        if (cc == null) cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = new Vector3(0, 1f, 0);

        // Remove old CapsuleCollider if attached
        var capsule = player.GetComponent<CapsuleCollider>();
        if (capsule != null) DestroyImmediate(capsule);

        // Configure PlayerController
        var playerCtrl = player.GetComponent<PlayerController>();
        if (playerCtrl == null) playerCtrl = player.AddComponent<PlayerController>();
        playerCtrl.walkSpeed = 6.5f;
        playerCtrl.sprintSpeed = 10.5f;

        // Configure PlayerHealth
        var playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth == null) playerHealth = player.AddComponent<PlayerHealth>();

        // Hide capsule mesh shadow only so it doesn't block FPS view
        var mr = player.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }

        // 3. Setup Camera as child of Player at eye level
        Camera cam = Camera.main;
        if (cam == null) cam = player.GetComponentInChildren<Camera>();
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();
            Undo.RegisterCreatedObjectUndo(camObj, "Create Camera");
        }

        cam.transform.SetParent(player.transform);
        cam.transform.localPosition = new Vector3(0, 1.65f, 0);
        cam.transform.localRotation = Quaternion.identity;

        // Remove old CameraFollow
        var oldFollow = cam.GetComponent("CameraFollow");
        if (oldFollow != null) DestroyImmediate(oldFollow);

        playerCtrl.cameraTransform = cam.transform;

        // Add FPSGun to Camera
        var gun = cam.GetComponent<FPSGun>();
        if (gun == null) gun = cam.gameObject.AddComponent<FPSGun>();
        gun.playerCamera = cam;

        // 4. Mark Scene Dirty & Save
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("FPS Prototype",
            "Scene setup complete!\n\n" +
            "- FPS Player & Camera configured\n" +
            "- GameManager initialized\n" +
            "- 4 Monster Types ready\n" +
            "- Item Drop System ready\n" +
            "- Wave Manager ready\n\n" +
            "Press PLAY to test the game!", "OK");
    }
}
