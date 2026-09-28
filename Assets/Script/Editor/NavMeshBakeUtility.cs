using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.AI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class NavMeshBakeUtility
{
    [MenuItem("Tools/FPS Prototype/Bake NavMesh For Current Scene")]
    public static void BakeCurrentSceneNavMesh()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        Debug.Log($"[NavMeshBake] Starting NavMesh bake for scene: {currentScene.name} ({currentScene.path})");

        try
        {
            BakeScene(currentScene);

            Debug.Log($"[NavMeshBake] Successfully baked and saved NavMesh for: {currentScene.name}");
            EditorUtility.DisplayDialog("NavMesh Baker",
                $"Bake NavMesh สำเร็จเรียบร้อยสำหรับฉาก {currentScene.name}!\n\nตอนนี้ Enemy ทุกตัวจะเดินอ้อมสิ่งกีดขวาง (บ้าน กำแพง ต้นไม้ หิน) อย่างชาญฉลาด และไม่เดินทะลุ Collider อย่างแน่นอน",
                "ตกลง");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NavMeshBake] Error baking NavMesh: {ex.Message}");
            EditorUtility.DisplayDialog("NavMesh Baker Error",
                $"เกิดข้อผิดพลาดในการ Bake NavMesh: {ex.Message}",
                "OK");
        }
    }

    [MenuItem("Tools/FPS Prototype/Bake NavMesh For All Gameplay Scenes")]
    public static void BakeAllGameplayScenes()
    {
        string[] scenes = new string[]
        {
            "Assets/Scenes/Map_1.unity",
            "Assets/Scenes/SampleScene.unity"
        };

        foreach (string scenePath in scenes)
        {
            if (System.IO.File.Exists(scenePath))
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                BakeScene(scene);
                Debug.Log($"[NavMeshBake] Baked NavMesh for {scenePath}");
            }
        }

        EditorUtility.DisplayDialog("NavMesh Baker",
            "Bake NavMesh สำหรับทุกฉากเรียบร้อยแล้ว!",
            "ตกลง");
    }

    public static void BakeScene(Scene scene)
    {
        // 1. ค้นหาหรือสร้าง NavMeshSurface ในฉาก (ใช้ FindObjectOfType เพื่อรองรับ Unity ทุกเวอร์ชัน)
        NavMeshSurface surface = Object.FindObjectOfType<NavMeshSurface>();
        if (surface == null)
        {
            GameObject surfaceObj = new GameObject("[NavMeshSurface_Manager]");
            surface = surfaceObj.AddComponent<NavMeshSurface>();
            Undo.RegisterCreatedObjectUndo(surfaceObj, "Create NavMeshSurface");
        }

        // 2. กำหนดค่าให้ครอบคลุม Collider ทั้งหมดในฉาก (รวมถึงบ้าน กำแพง วัตถุต่างๆ)
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.agentTypeID = 0; // Humanoid / Default Agent

        // 3. สั่ง Bake NavMesh ผ่าน NavMeshSurface
        surface.BuildNavMesh();

        // 4. บันทึกความเปลี่ยนแปลงลงไฟล์ Scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
