using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class TerrainSculptorEditor
{
    private const string ScenePath = "Assets/Scenes/NewMapWithTerrain.unity";

    static TerrainSculptorEditor()
    {
        // Delay call removed so it doesn't overwrite customized map layout
    }

    [MenuItem("Tools/Terrain/Sculpt Village Plain & Mountains (NewMapWithTerrain)")]
    public static void ExecuteSculpting()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
        {
            EditorSceneManager.OpenScene(ScenePath);
        }

        Terrain terrain = Terrain.activeTerrain ?? Object.FindAnyObjectByType<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("[TerrainSculptor] No Terrain found in scene: " + ScenePath);
            return;
        }

        TerrainData td = terrain.terrainData;
        if (td == null)
        {
            Debug.LogError("[TerrainSculptor] TerrainData is null!");
            return;
        }

        Undo.RegisterCompleteObjectUndo(td, "Sculpt Terrain Plain and Mountains");

        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = td.size;
        int res = td.heightmapResolution;

        // พิกัดศูนย์กลางของหมู่บ้าน
        Vector3 villageCenter = new Vector3(258.0f, 13.5f, 542.5f);
        float flatHeight = 13.5f; // ความสูงที่ราบเสมอกันของหมู่บ้าน (เมตร)
        float flatRadius = 85.0f; // รัศมีที่ราบเรียบ 100% ครอบคลุมสิ่งปลูกสร้างในหมู่บ้านทั้งหมด
        float transitionRadius = 145.0f; // รัศมีรอยต่อ ค่อยๆ ไต่ระดับขึ้นสู่ภูเขาอย่างเป็นธรรมชาติ
        float maxDist = Mathf.Sqrt(terrainSize.x * terrainSize.x + terrainSize.z * terrainSize.z) * 0.5f;

        Debug.Log($"[TerrainSculptor] Sculpting Terrain... Res: {res}x{res}, VillageCenter: {villageCenter}, FlatRadius: {flatRadius}m, FlatHeight: {flatHeight}m");

        float[,] heights = new float[res, res];

        for (int y = 0; y < res; y++)
        {
            float normZ = (float)y / (res - 1);
            float worldZ = terrainPos.z + normZ * terrainSize.z;

            for (int x = 0; x < res; x++)
            {
                float normX = (float)x / (res - 1);
                float worldX = terrainPos.x + normX * terrainSize.x;

                float dx = worldX - villageCenter.x;
                float dz = worldZ - villageCenter.z;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);

                float finalHeightMeters = flatHeight;

                // -------------------------------------------------------------
                // โซน 1: พื้นที่หมู่บ้านตรงกลาง (เรียบเสมอกัน 100%)
                // -------------------------------------------------------------
                if (dist <= flatRadius)
                {
                    finalHeightMeters = flatHeight;
                }
                // -------------------------------------------------------------
                // โซน 2: รอยต่อลาดเอียงสู่ภูเขา (Transition Zone)
                // -------------------------------------------------------------
                else if (dist <= transitionRadius)
                {
                    float t = (dist - flatRadius) / (transitionRadius - flatRadius);
                    float smoothT = Mathf.SmoothStep(0f, 1f, t);

                    // คำนวณความสูงภูเขาที่จุดเริ่มต้นของเนิน
                    float mountainHeight = CalculateMountainHeight(worldX, worldZ, dist, transitionRadius, maxDist, terrainPos, terrainSize, flatHeight);
                    finalHeightMeters = Mathf.Lerp(flatHeight, mountainHeight, smoothT);
                }
                // -------------------------------------------------------------
                // โซน 3: เทือกเขารอบนอก (Surrounding Mountains)
                // -------------------------------------------------------------
                else
                {
                    finalHeightMeters = CalculateMountainHeight(worldX, worldZ, dist, transitionRadius, maxDist, terrainPos, terrainSize, flatHeight);
                }

                // แปลงเป็นค่าความสูงแบบ Normalized (0.0 ถึง 1.0)
                heights[y, x] = Mathf.Clamp01(finalHeightMeters / terrainSize.y);
            }
        }

        // นำค่าความสูงใหม่ไปใส่ใน TerrainData
        td.SetHeights(0, 0, heights);

        // ปรับระดับพื้นผิว Splatmap / Textures ให้สอดคล้องกับภูเขา (ถ้ามี Layers)
        PaintTerrainTextures(td, heights, res, villageCenter, terrainPos, terrainSize, flatRadius, transitionRadius);

        // ตรวจสอบและปรับตำแหน่ง Y ของสิ่งปลูกสร้างในหมู่บ้านให้แนบสนิทกับพื้นที่ราบ
        AlignVillageBuildings(terrain, villageCenter, flatRadius + 10f, flatHeight);

        // บันทึก Asset และ Scene
        EditorUtility.SetDirty(td);
        AssetDatabase.SaveAssets();

        Scene activeScene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        // Bake NavMesh สำหรับฉากใหม่เพื่อให้ศัตรูเดินอ้อมสิ่งกีดขวางบน Terrain ใหม่ได้ทันที
        try
        {
            NavMeshBakeUtility.BakeScene(activeScene);
            Debug.Log("[TerrainSculptor] Successfully baked NavMesh for NewMapWithTerrain!");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[TerrainSculptor] NavMesh bake note: " + ex.Message);
        }

        Debug.Log("[TerrainSculptor] Terrain sculpting COMPLETED successfully! Village is flat, surrounded by majestic mountains.");
    }

    private static float CalculateMountainHeight(float worldX, float worldZ, float dist, float transitionRadius, float maxDist, Vector3 terrainPos, Vector3 terrainSize, float flatHeight)
    {
        float outerDist = dist - transitionRadius;
        float distFactor = Mathf.Clamp01(outerDist / (maxDist - transitionRadius));
        float riseFactor = Mathf.Pow(distFactor, 1.25f);

        // ความสูงฐานของภูเขา ค่อยๆ ไต่ระดับขึ้นตามระยะห่าง
        float baseElevation = Mathf.Lerp(flatHeight, 85f, riseFactor);

        // 1. สันเขาหลักและยอดเขาสูงชัน (Ridge Noise)
        float ridge1 = 1.0f - Mathf.Abs(Mathf.PerlinNoise(worldX * 0.0075f + 120f, worldZ * 0.0075f + 120f) * 2f - 1f);
        float ridge2 = 1.0f - Mathf.Abs(Mathf.PerlinNoise(worldX * 0.015f + 250f, worldZ * 0.015f + 250f) * 2f - 1f);
        float peakFeatures = Mathf.Pow((ridge1 * 0.7f + ridge2 * 0.3f), 1.5f);

        // 2. เนินเขาลอนคลื่นและร่องเขา (Secondary Hills)
        float hillNoise = Mathf.PerlinNoise(worldX * 0.022f + 45f, worldZ * 0.022f + 45f);

        // 3. รายละเอียดผาหินขนาดเล็ก (Fine Crag Noise)
        float cragNoise = Mathf.PerlinNoise(worldX * 0.055f + 310f, worldZ * 0.055f + 310f);

        // รวมความสูงของภูเขา
        float mountainElevation = baseElevation + (peakFeatures * 65f + (hillNoise - 0.5f) * 25f + (cragNoise - 0.5f) * 8f) * riseFactor;

        // 4. แนวขอบแผนที่ด้านนอก ยกตัวสูงขึ้นเป็นกำแพงเขากั้นขอบโลก (Border Mountain Wall)
        float borderDistX = Mathf.Min(worldX - terrainPos.x, (terrainPos.x + terrainSize.x) - worldX);
        float borderDistZ = Mathf.Min(worldZ - terrainPos.z, (terrainPos.z + terrainSize.z) - worldZ);
        float minBorderDist = Mathf.Min(borderDistX, borderDistZ);

        if (minBorderDist < 45f)
        {
            float edgeFactor = 1f - (minBorderDist / 45f);
            mountainElevation += Mathf.Pow(edgeFactor, 1.5f) * 55f;
        }

        return mountainElevation;
    }

    private static void PaintTerrainTextures(TerrainData td, float[,] heights, int heightRes, Vector3 villageCenter, Vector3 terrainPos, Vector3 terrainSize, float flatRadius, float transitionRadius)
    {
        if (td.terrainLayers == null || td.terrainLayers.Length < 2) return;

        int alphaRes = td.alphamapResolution;
        int layerCount = td.terrainLayers.Length;
        float[,,] splatmapData = new float[alphaRes, alphaRes, layerCount];

        for (int y = 0; y < alphaRes; y++)
        {
            float normZ = (float)y / (alphaRes - 1);
            float worldZ = terrainPos.z + normZ * terrainSize.z;

            for (int x = 0; x < alphaRes; x++)
            {
                float normX = (float)x / (alphaRes - 1);
                float worldX = terrainPos.x + normX * terrainSize.x;

                float dx = worldX - villageCenter.x;
                float dz = worldZ - villageCenter.z;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);

                // คำนวณความชัน (Steepness)
                float steepness = td.GetSteepness(normX, normZ);

                // Layer 0: Grass / Ground สำหรับหมู่บ้านและที่ราบ
                // Layer 1+: Rock / Cliff สำหรับภูเขาและทางลาดชัน
                float rockWeight = 0f;
                float grassWeight = 1f;

                if (dist > flatRadius)
                {
                    float distBlend = Mathf.Clamp01((dist - flatRadius) / (transitionRadius - flatRadius));
                    float slopeBlend = Mathf.Clamp01((steepness - 18f) / 25f);
                    rockWeight = Mathf.Clamp01(distBlend * 0.6f + slopeBlend * 0.7f);
                    grassWeight = 1f - rockWeight;
                }

                splatmapData[y, x, 0] = grassWeight;
                splatmapData[y, x, 1] = rockWeight;

                for (int l = 2; l < layerCount; l++)
                {
                    splatmapData[y, x, l] = 0f;
                }
            }
        }

        td.SetAlphamaps(0, 0, splatmapData);
        Debug.Log("[TerrainSculptor] Terrain textures painted: Grass on plain, Rock/Cliff on mountains.");
    }

    private static void AlignVillageBuildings(Terrain terrain, Vector3 villageCenter, float radius, float targetY)
    {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        int alignedCount = 0;

        void CheckAndAlign(Transform t)
        {
            string nameLower = t.name.ToLower();
            bool isBuilding = nameLower.Contains("house") || nameLower.Contains("village") || nameLower.Contains("building") || 
                              nameLower.Contains("hut") || nameLower.Contains("barn") || nameLower.Contains("well");

            if (isBuilding && t.parent != null && !t.parent.name.ToLower().Contains("house"))
            {
                Vector3 flatPos = new Vector3(t.position.x, 0, t.position.z);
                Vector3 flatCenter = new Vector3(villageCenter.x, 0, villageCenter.z);
                if (Vector3.Distance(flatPos, flatCenter) <= radius)
                {
                    Vector3 pos = t.position;
                    // ปรับความสูงให้สอดคล้องกับพื้นที่ราบเสมอ
                    if (Mathf.Abs(pos.y - targetY) > 0.05f)
                    {
                        pos.y = targetY;
                        t.position = pos;
                        alignedCount++;
                    }
                }
            }

            for (int i = 0; i < t.childCount; i++)
            {
                CheckAndAlign(t.GetChild(i));
            }
        }

        foreach (var r in roots)
        {
            CheckAndAlign(r.transform);
        }

        Debug.Log($"[TerrainSculptor] Checked village buildings, aligned {alignedCount} root structures to {targetY}m ground.");
    }
}
