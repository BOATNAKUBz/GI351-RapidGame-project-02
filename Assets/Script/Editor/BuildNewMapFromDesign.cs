using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class BuildNewMapFromDesign
{
    private const string ScenePath = "Assets/Scenes/NewMapWithTerrain.unity";

    // Asset paths
    private const string ThaiHousePath = "Assets/THAI/H2/isan_thai_house_hernkeay (1).obj";
    private const string ThaiHouseMatPath = "Assets/THAI/H2/MatH.mat";
    private const string ChickenPrefabPath = "Assets/Prefabs/Chicken_001.prefab";
    private const string HayBalePrefabPath = "Assets/EmaceArt/Slavic World Free/Prefabs/Prop/Container/EA03_Prop_Container_Bale_01a_PRE.prefab";
    private const string FencePrefabPath = "Assets/EmaceArt/Slavic World Free/Prefabs/Fence/Plank2/EA03_Village_Fence_01a_PRE.prefab";
    private const string FencePlankPath = "Assets/EmaceArt/Slavic World Free/Prefabs/Fence/Plank2/EA03_Fence_Plank_02a_PRE.prefab";
    private const string TempleWallPath = "Assets/EmaceArt/Slavic World Free/Prefabs/Fence/Wall/EA03_Fence_Wall_01b_PRE.prefab";
    private const string TempleGatePath = "Assets/EmaceArt/Slavic World Free/Prefabs/Fence/Wall/EA03_Fence_WallGate_01a_PRE.prefab";
    private const string BridgePierPath = "Assets/EmaceArt/Slavic World Free/Prefabs/Environment/Bridge/EA03_Prop_Pier_03d_PRE.prefab";
    private const string WaterMatPath = "Assets/EmaceArt/Slavic World Free/Materials/Water.mat";
    private const string LadderPrefabPath = "Assets/NikolayFedorov/OldVillage/VilagePropsPack/Prefab/Ladder/ladder_01.prefab";
    private const string TreeModelPath = "Assets/Anime Trees/Models/AnimeTree_02.fbx";
    private const string WellPrefabPath = "Assets/Prefabs/Well.prefab";
    private const string FirewoodPrefabPath = "Assets/NikolayFedorov/OldVillage/VilagePropsPack/Prefab/Drova/Drova01.prefab";
    private const string BarrelPrefabPath = "Assets/EmaceArt/Slavic World Free/Prefabs/Prop/Container/EA03_Prop_Container_Barrel_01d_PRE.prefab";

    // Enemy prefabs
    private const string ZombiePrefabPath = "Assets/Prefabs/Enemy_Zombie.prefab";
    private const string GruntPrefabPath = "Assets/Prefabs/Enemy_Grunt.prefab";
    private const string SwarmerPrefabPath = "Assets/Prefabs/Enemy_Swarmer.prefab";
    private const string TankPrefabPath = "Assets/Prefabs/Enemy_Tank.prefab";

    static BuildNewMapFromDesign()
    {
        // Delay call completed
    }

    [MenuItem("Tools/Map/Build NewMapWithTerrain from Design")]
    public static void BuildMapMenu()
    {
        BuildMap();
        EditorUtility.DisplayDialog("Map Builder", "Map successfully created in NewMapWithTerrain according to design diagram!\n\nZone 1: Open Entrance, Road, Abandoned Pickup Truck, Thai Temple Hall, 4 Chedis, Sacred Banyan Tree\nZone 2: Narrow Alleyways, Thai Stilt Houses, Bamboo Fences, Chicken Coop, Buffalo Corral, Jump Scare\nZone 3: Canal, Wooden Bridge, Kubota Tractor, Tiang Na, Hay Bales, Granary Barn, Boss Tank, Goal", "OK");
    }

    public static void BuildMap()
    {
        Debug.Log("[BuildNewMap] Starting map generation for NewMapWithTerrain...");

        if (SceneManager.GetActiveScene().path != ScenePath)
        {
            EditorSceneManager.OpenScene(ScenePath);
        }

        Scene scene = SceneManager.GetActiveScene();

        // 1. Clean up old placeholder cubes and redundant objects, keep Terrain, Light, HUD, Managers, Player
        CleanupScene(scene);

        // 2. Prepare Materials
        var mats = CreateMaterials();

        // 3. Terrain Sculpting & Canal Carving
        SculptTerrainWithCanal();

        // 4. Create root groups
        GameObject envRoot = GetOrCreateRoot("--- [ENVIRONMENT & LIGHTING] ---");
        GameObject zone1Root = GetOrCreateRoot("--- [ZONE 1: OPEN ENTRANCE] ---");
        GameObject zone2Root = GetOrCreateRoot("--- [ZONE 2: NARROW ALLEYWAYS] ---");
        GameObject zone3Root = GetOrCreateRoot("--- [ZONE 3: VERTICAL VANTAGE & GOAL] ---");

        // 5. Build Zone 1
        BuildZone1(zone1Root.transform, mats);

        // 6. Build Zone 2
        BuildZone2(zone2Root.transform, mats);

        // 7. Build Zone 3
        BuildZone3(zone3Root.transform, mats);

        // 8. Configure Player Spawn Position
        SetupPlayerPosition();

        // 9. Configure WaveManager & Zone Triggers
        SetupWaveManager(zone1Root.transform, zone2Root.transform, zone3Root.transform);

        // 10. Atmosphere & Lighting (Atmospheric dusk / fog)
        SetupLightingAndAtmosphere();

        // 11. Bake NavMesh
        BakeNavMesh(scene);

        // 12. Save Scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[BuildNewMap] Map build COMPLETED successfully!");
    }

    private static void CleanupScene(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        foreach (var r in roots)
        {
            string n = r.name.ToLower();
            if (n.Contains("cube") || n.Contains("village") || n.Contains("zone1") || 
                n.Contains("isan_thai_house") || n.Contains("--- [zone") || n.Contains("--- [environment"))
            {
                Object.DestroyImmediate(r);
            }
        }
    }

    private static GameObject GetOrCreateRoot(string name)
    {
        GameObject obj = GameObject.Find(name);
        if (obj == null) obj = new GameObject(name);
        obj.transform.position = Vector3.zero;
        return obj;
    }

    private static Dictionary<string, Material> CreateMaterials()
    {
        var dict = new Dictionary<string, Material>();
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Standard");

        Material CreateMat(string name, Color color, float smoothness = 0.2f, float metallic = 0.0f)
        {
            Material m = new Material(litShader);
            m.name = name;
            m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            return m;
        }

        // Temple materials
        dict["TempleWhite"] = CreateMat("TempleWhite", new Color(0.92f, 0.90f, 0.86f), 0.3f);
        dict["TempleRoofRed"] = CreateMat("TempleRoofRed", new Color(0.72f, 0.18f, 0.12f), 0.5f);
        dict["TempleGold"] = CreateMat("TempleGold", new Color(0.92f, 0.75f, 0.22f), 0.75f, 0.65f);
        dict["ChediStone"] = CreateMat("ChediStone", new Color(0.85f, 0.82f, 0.75f), 0.25f);
        dict["WoodDark"] = CreateMat("WoodDark", new Color(0.28f, 0.18f, 0.11f), 0.15f);
        dict["WoodPlank"] = CreateMat("WoodPlank", new Color(0.48f, 0.34f, 0.22f), 0.1f);
        dict["RoadDirt"] = CreateMat("RoadDirt", new Color(0.38f, 0.33f, 0.28f), 0.05f);

        // Ribbon materials (Thai Three-Color Sacred Cloth)
        dict["RibbonYellow"] = CreateMat("RibbonYellow", new Color(0.98f, 0.82f, 0.15f), 0.6f);
        dict["RibbonRed"] = CreateMat("RibbonRed", new Color(0.88f, 0.12f, 0.15f), 0.6f);
        dict["RibbonCyan"] = CreateMat("RibbonCyan", new Color(0.12f, 0.75f, 0.85f), 0.6f);

        // Vehicle materials
        dict["PickupRust"] = CreateMat("PickupRust", new Color(0.32f, 0.35f, 0.38f), 0.3f, 0.2f);
        dict["TireBlack"] = CreateMat("TireBlack", new Color(0.12f, 0.12f, 0.12f), 0.1f);
        dict["KubotaOrange"] = CreateMat("KubotaOrange", new Color(0.95f, 0.32f, 0.08f), 0.6f, 0.3f);
        dict["TractorIron"] = CreateMat("TractorIron", new Color(0.22f, 0.24f, 0.26f), 0.4f, 0.7f);

        // Farm & Barn
        dict["HayStraw"] = CreateMat("HayStraw", new Color(0.85f, 0.72f, 0.35f), 0.05f);
        dict["TinRoof"] = CreateMat("TinRoof", new Color(0.42f, 0.44f, 0.46f), 0.45f, 0.5f);
        dict["MudGround"] = CreateMat("MudGround", new Color(0.26f, 0.20f, 0.15f), 0.35f);

        return dict;
    }

    private static void SculptTerrainWithCanal()
    {
        Terrain terrain = Terrain.activeTerrain ?? Object.FindAnyObjectByType<Terrain>();
        if (terrain == null || terrain.terrainData == null) return;

        TerrainData td = terrain.terrainData;
        int res = td.heightmapResolution;
        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = td.size;

        Vector3 villageCenter = new Vector3(258.0f, 13.5f, 542.5f);
        float flatHeight = 13.5f;
        float flatRadius = 88.0f;
        float transitionRadius = 145.0f;

        float[,] heights = td.GetHeights(0, 0, res, res);

        // Carve canal between Zone 2 and Zone 3 (line from roughly (230, 595) to (330, 580))
        Vector2 canalStart = new Vector2(230f, 595f);
        Vector2 canalEnd = new Vector2(330f, 580f);
        float canalWidth = 5.5f;
        float canalDepth = 2.2f;

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

                if (dist <= flatRadius)
                {
                    // Check distance to canal line segment
                    float distToCanal = DistanceToLineSegment(new Vector2(worldX, worldZ), canalStart, canalEnd);
                    if (distToCanal < canalWidth)
                    {
                        float t = distToCanal / canalWidth;
                        // Smooth canal profile
                        float depression = Mathf.Cos(t * Mathf.PI * 0.5f) * canalDepth;
                        float h = flatHeight - depression;
                        heights[y, x] = Mathf.Clamp01(h / terrainSize.y);
                    }
                    else
                    {
                        heights[y, x] = flatHeight / terrainSize.y;
                    }
                }
            }
        }

        td.SetHeights(0, 0, heights);
    }

    private static float DistanceToLineSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 pa = p - a;
        Vector2 ba = b - a;
        float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
        return (pa - ba * h).magnitude;
    }

    // -------------------------------------------------------------------------------------------------
    // ZONE 1: OPEN ENTRANCE (Road, Abandoned Pickup Truck, Thai Temple Hall, 4 Chedis, Sacred Banyan Tree)
    // -------------------------------------------------------------------------------------------------
    private static void BuildZone1(Transform root, Dictionary<string, Material> mats)
    {
        GameObject z1 = new GameObject("Zone1_Content");
        z1.transform.SetParent(root);

        // 1. Road Path
        BuildCurvedRoad(z1.transform, mats["RoadDirt"]);

        // 2. Abandoned Pickup Truck (with Hay Bales and crates)
        BuildAbandonedPickupTruck(z1.transform, new Vector3(246.5f, 13.5f, 498.0f), Quaternion.Euler(0, 28, 0), mats);

        // 3. Thai Temple Compound
        BuildThaiTempleCompound(z1.transform, mats);
    }

    private static void BuildCurvedRoad(Transform parent, Material roadMat)
    {
        GameObject roadGroup = new GameObject("Road_Entrance");
        roadGroup.transform.SetParent(parent);

        // Waypoints from Player Spawn to Zone 2 entrance
        Vector3[] waypoints = new Vector3[]
        {
            new Vector3(255f, 13.51f, 465f),
            new Vector3(254f, 13.51f, 480f),
            new Vector3(250f, 13.51f, 495f),
            new Vector3(248f, 13.51f, 510f),
            new Vector3(254f, 13.51f, 525f),
            new Vector3(260f, 13.51f, 538f)
        };

        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            Vector3 a = waypoints[i];
            Vector3 b = waypoints[i + 1];
            Vector3 mid = (a + b) * 0.5f;
            Vector3 dir = (b - a);
            float len = dir.magnitude;

            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = $"Road_Segment_{i}";
            segment.transform.SetParent(roadGroup.transform);
            segment.transform.position = mid + Vector3.up * 0.02f;
            segment.transform.rotation = Quaternion.LookRotation(dir);
            segment.transform.localScale = new Vector3(4.8f, 0.05f, len + 0.3f);
            segment.GetComponent<Renderer>().material = roadMat;
            Object.DestroyImmediate(segment.GetComponent<Collider>());
        }
    }

    private static void BuildAbandonedPickupTruck(Transform parent, Vector3 pos, Quaternion rot, Dictionary<string, Material> mats)
    {
        GameObject truck = new GameObject("Abandoned_Pickup_Truck");
        truck.transform.SetParent(parent);
        truck.transform.position = pos;
        truck.transform.rotation = rot;

        Material rustMat = mats["PickupRust"];
        Material tireMat = mats["TireBlack"];
        Material hayMat = mats["HayStraw"];

        // Chassis & Main Body
        CreateBox(truck.transform, "Chassis", new Vector3(0, 0.45f, 0), new Vector3(1.9f, 0.35f, 4.6f), rustMat);

        // Cab (Cabin)
        CreateBox(truck.transform, "Cab", new Vector3(0, 1.25f, -0.4f), new Vector3(1.85f, 1.15f, 1.8f), rustMat);
        // Cab Windshield (dark glass)
        CreateBox(truck.transform, "Windshield", new Vector3(0, 1.45f, 0.48f), new Vector3(1.6f, 0.65f, 0.1f), mats["TractorIron"]);

        // Engine Hood & Front Grille
        CreateBox(truck.transform, "EngineHood", new Vector3(0, 0.95f, 1.25f), new Vector3(1.8f, 0.7f, 1.5f), rustMat);
        CreateBox(truck.transform, "FrontBumper", new Vector3(0, 0.5f, 2.05f), new Vector3(1.95f, 0.35f, 0.25f), mats["TractorIron"]);
        CreateBox(truck.transform, "Headlight_L", new Vector3(-0.65f, 0.95f, 2.02f), new Vector3(0.35f, 0.25f, 0.1f), mats["TempleGold"]);
        CreateBox(truck.transform, "Headlight_R", new Vector3(0.65f, 0.95f, 2.02f), new Vector3(0.35f, 0.25f, 0.1f), mats["TempleGold"]);

        // Flatbed (Cargo Bed)
        CreateBox(truck.transform, "BedFloor", new Vector3(0, 0.7f, -1.5f), new Vector3(1.85f, 0.15f, 2.2f), rustMat);
        CreateBox(truck.transform, "BedWall_L", new Vector3(-0.9f, 1.05f, -1.5f), new Vector3(0.1f, 0.6f, 2.2f), rustMat);
        CreateBox(truck.transform, "BedWall_R", new Vector3(0.9f, 1.05f, -1.5f), new Vector3(0.1f, 0.6f, 2.2f), rustMat);
        CreateBox(truck.transform, "Tailgate", new Vector3(0, 1.05f, -2.55f), new Vector3(1.85f, 0.6f, 0.1f), rustMat);

        // 4 Wheels
        Vector3[] wheelOffsets = new Vector3[]
        {
            new Vector3(-0.95f, 0.4f, 1.25f),
            new Vector3(0.95f, 0.4f, 1.25f),
            new Vector3(-0.95f, 0.4f, -1.45f),
            new Vector3(0.95f, 0.4f, -1.45f)
        };
        foreach (var wOff in wheelOffsets)
        {
            GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = "Wheel";
            wheel.transform.SetParent(truck.transform);
            wheel.transform.localPosition = wOff;
            wheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
            wheel.transform.localScale = new Vector3(0.8f, 0.22f, 0.8f);
            wheel.GetComponent<Renderer>().material = tireMat;
        }

        // Hay bales piled on truck bed
        CreateBox(truck.transform, "Hay_Layer1_A", new Vector3(-0.4f, 1.1f, -1.3f), new Vector3(0.85f, 0.65f, 1.1f), hayMat);
        CreateBox(truck.transform, "Hay_Layer1_B", new Vector3(0.4f, 1.1f, -1.5f), new Vector3(0.85f, 0.65f, 1.1f), hayMat);
        CreateBox(truck.transform, "Hay_Layer2_Top", new Vector3(0.0f, 1.7f, -1.4f), new Vector3(0.9f, 0.65f, 1.2f), hayMat);

        // Wooden supply crates near truck
        CreateBox(truck.transform, "Crate_Ground_A", new Vector3(1.4f, 0.4f, -0.6f), new Vector3(0.8f, 0.8f, 0.8f), mats["WoodPlank"]);
        CreateBox(truck.transform, "Crate_Ground_B", new Vector3(1.3f, 0.35f, 0.3f), new Vector3(0.7f, 0.7f, 0.7f), mats["WoodDark"]);
    }

    private static void BuildThaiTempleCompound(Transform parent, Dictionary<string, Material> mats)
    {
        GameObject compound = new GameObject("Thai_Temple_Compound");
        compound.transform.SetParent(parent);

        // 1. Perimeter Wall & Gate
        BuildTempleWalls(compound.transform, mats);

        // 2. Temple Hall (Ubosot)
        BuildTempleHallUbosot(compound.transform, new Vector3(215f, 13.5f, 534f), Quaternion.Euler(0, 180, 0), mats);

        // 3. Four Chedis (Stupas) at the 4 corners of the courtyard
        Vector3[] chediPositions = new Vector3[]
        {
            new Vector3(203f, 13.5f, 542f), // NW
            new Vector3(227f, 13.5f, 542f), // NE
            new Vector3(203f, 13.5f, 510f), // SW
            new Vector3(227f, 13.5f, 510f)  // SE
        };
        for (int i = 0; i < chediPositions.Length; i++)
        {
            BuildChediStupa(compound.transform, chediPositions[i], $"Chedi_Stupa_{i + 1}", mats);
        }

        // 4. Sacred Banyan Tree with Tri-Color Cloth (ผ้าสามสี) & Altar
        BuildSacredBanyanTree(compound.transform, new Vector3(215f, 13.5f, 521f), mats);
    }

    private static void BuildTempleWalls(Transform parent, Dictionary<string, Material> mats)
    {
        GameObject walls = new GameObject("Temple_Perimeter_Walls");
        walls.transform.SetParent(parent);

        Material whiteMat = mats["TempleWhite"];
        Material redMat = mats["TempleRoofRed"];

        float minX = 196f;
        float maxX = 234f;
        float minZ = 502f;
        float maxZ = 548f;
        float wallH = 1.6f;
        float wallThick = 0.4f;

        void MakeWallSegment(Vector3 pos, Vector3 size)
        {
            // Base wall
            CreateBox(walls.transform, "Wall_Base", pos, size, whiteMat);
            // Decorative coping ridge on top
            CreateBox(walls.transform, "Wall_Ridge", pos + Vector3.up * (size.y * 0.5f + 0.1f), new Vector3(size.x + 0.1f, 0.18f, size.z + 0.1f), redMat);
        }

        // West Wall (Back)
        MakeWallSegment(new Vector3(minX, 13.5f + wallH * 0.5f, (minZ + maxZ) * 0.5f), new Vector3(wallThick, wallH, maxZ - minZ));
        // North Wall
        MakeWallSegment(new Vector3((minX + maxX) * 0.5f, 13.5f + wallH * 0.5f, maxZ), new Vector3(maxX - minX, wallH, wallThick));
        // South Wall
        MakeWallSegment(new Vector3((minX + maxX) * 0.5f, 13.5f + wallH * 0.5f, minZ), new Vector3(maxX - minX, wallH, wallThick));

        // East Wall (Front) with Gate Opening at center (Z ~ 518)
        float gateZ = 518f;
        float halfGate = 3.0f;
        MakeWallSegment(new Vector3(maxX, 13.5f + wallH * 0.5f, (minZ + (gateZ - halfGate)) * 0.5f), new Vector3(wallThick, wallH, (gateZ - halfGate) - minZ));
        MakeWallSegment(new Vector3(maxX, 13.5f + wallH * 0.5f, ((gateZ + halfGate) + maxZ) * 0.5f), new Vector3(wallThick, wallH, maxZ - (gateZ + halfGate)));

        // Arched Temple Entrance Gate (ซุ้มประตูกำแพงแก้ววัด)
        GameObject gate = new GameObject("Temple_Entrance_Gate");
        gate.transform.SetParent(walls.transform);
        gate.transform.position = new Vector3(maxX, 13.5f, gateZ);

        CreateBox(gate.transform, "GatePillar_L", new Vector3(0, 2.0f, -halfGate), new Vector3(0.9f, 4.0f, 0.9f), whiteMat);
        CreateBox(gate.transform, "GatePillar_R", new Vector3(0, 2.0f, halfGate), new Vector3(0.9f, 4.0f, 0.9f), whiteMat);
        CreateBox(gate.transform, "GateArch", new Vector3(0, 4.2f, 0), new Vector3(1.1f, 0.6f, halfGate * 2f + 1.2f), redMat);
        CreateBox(gate.transform, "GateCrown", new Vector3(0, 5.0f, 0), new Vector3(0.8f, 1.0f, 2.5f), mats["TempleGold"]);
    }

    private static void BuildTempleHallUbosot(Transform parent, Vector3 pos, Quaternion rot, Dictionary<string, Material> mats)
    {
        GameObject hall = new GameObject("Temple_Hall_Ubosot");
        hall.transform.SetParent(parent);
        hall.transform.position = pos;
        hall.transform.rotation = rot;

        Material whiteMat = mats["TempleWhite"];
        Material redMat = mats["TempleRoofRed"];
        Material goldMat = mats["TempleGold"];

        // 1. Raised Plinth Platform (ฐานพระอุโบสถ)
        CreateBox(hall.transform, "Plinth_Tier1", new Vector3(0, 0.5f, 0), new Vector3(14.0f, 1.0f, 18.0f), whiteMat);
        CreateBox(hall.transform, "Plinth_Tier2", new Vector3(0, 1.3f, 0), new Vector3(12.5f, 0.6f, 16.5f), whiteMat);

        // Front Entrance Stairs
        for (int s = 0; s < 5; s++)
        {
            float stepY = s * 0.25f + 0.15f;
            float stepZ = 9.0f + (4 - s) * 0.45f;
            CreateBox(hall.transform, $"Stair_{s}", new Vector3(0, stepY, stepZ), new Vector3(4.5f, 0.25f, 0.5f), whiteMat);
        }

        // 2. Main Temple Walls
        CreateBox(hall.transform, "MainWalls", new Vector3(0, 4.0f, -0.5f), new Vector3(10.0f, 4.8f, 13.0f), whiteMat);

        // 3. Portico Entrance Columns (เสาระเบียงด้านหน้า)
        float[] colX = new float[] { -4.2f, -1.4f, 1.4f, 4.2f };
        foreach (float cx in colX)
        {
            CreateBox(hall.transform, "FrontColumn", new Vector3(cx, 3.8f, 6.8f), new Vector3(0.7f, 4.4f, 0.7f), whiteMat);
        }

        // 4. Multi-tiered Thai Gable Roof (หลังคาซ้อนชั้นแบบไทย)
        // Tier 1 (Lower Main Roof)
        CreateBox(hall.transform, "Roof_Tier1", new Vector3(0, 6.8f, 0), new Vector3(13.6f, 0.8f, 17.5f), redMat);
        CreateBox(hall.transform, "Roof_Eaves_Gold1", new Vector3(0, 6.4f, 0), new Vector3(14.0f, 0.25f, 18.0f), goldMat);

        // Tier 2 (Middle Roof)
        CreateBox(hall.transform, "Roof_Tier2", new Vector3(0, 8.2f, -0.2f), new Vector3(10.5f, 1.2f, 14.5f), redMat);
        CreateBox(hall.transform, "Roof_Eaves_Gold2", new Vector3(0, 7.6f, -0.2f), new Vector3(11.0f, 0.25f, 15.0f), goldMat);

        // Tier 3 (Top Ridge Roof)
        CreateBox(hall.transform, "Roof_Tier3_Apex", new Vector3(0, 9.6f, -0.4f), new Vector3(7.0f, 1.4f, 11.5f), redMat);

        // Central Gold Ridge Finial (สันหลังคาทอง)
        CreateBox(hall.transform, "Roof_GoldRidge", new Vector3(0, 10.45f, -0.4f), new Vector3(0.5f, 0.35f, 11.8f), goldMat);

        // Front Chofah (ช่อฟ้า ปลายจั่ว)
        CreateBox(hall.transform, "Chofah_Front", new Vector3(0, 11.0f, 5.5f), new Vector3(0.4f, 1.2f, 0.4f), goldMat);
        CreateBox(hall.transform, "Chofah_Back", new Vector3(0, 11.0f, -6.3f), new Vector3(0.4f, 1.2f, 0.4f), goldMat);

        // Front Gilded Pediment (หน้าบันอุโบสถปิดทอง)
        CreateBox(hall.transform, "Pediment_Gilded", new Vector3(0, 7.8f, 6.0f), new Vector3(8.5f, 1.8f, 0.4f), goldMat);

        // Temple Main Entrance Doors (ประดับมุข/ทอง)
        CreateBox(hall.transform, "TempleDoor", new Vector3(0, 3.2f, 6.05f), new Vector3(2.6f, 3.2f, 0.15f), mats["WoodDark"]);
        CreateBox(hall.transform, "DoorTrim", new Vector3(0, 3.2f, 6.08f), new Vector3(2.8f, 3.4f, 0.05f), goldMat);
    }

    private static void BuildChediStupa(Transform parent, Vector3 pos, string name, Dictionary<string, Material> mats)
    {
        GameObject chedi = new GameObject(name);
        chedi.transform.SetParent(parent);
        chedi.transform.position = pos;

        Material stoneMat = mats["ChediStone"];
        Material goldMat = mats["TempleGold"];

        // Tiered Square Plinths (ฐานสี่เหลี่ยมย่อมุม)
        CreateBox(chedi.transform, "Base_Tier1", new Vector3(0, 0.5f, 0), new Vector3(4.8f, 1.0f, 4.8f), stoneMat);
        CreateBox(chedi.transform, "Base_Tier2", new Vector3(0, 1.4f, 0), new Vector3(3.9f, 0.8f, 3.9f), stoneMat);
        CreateBox(chedi.transform, "Base_Tier3", new Vector3(0, 2.1f, 0), new Vector3(3.1f, 0.6f, 3.1f), stoneMat);

        // Bell Body (องค์ระฆัง)
        GameObject bell = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bell.name = "Bell_Body";
        bell.transform.SetParent(chedi.transform);
        bell.transform.localPosition = new Vector3(0, 3.2f, 0);
        bell.transform.localScale = new Vector3(2.4f, 0.8f, 2.4f);
        bell.GetComponent<Renderer>().material = goldMat;

        // Octagonal / Round Throne (บัลลังก์)
        CreateBox(chedi.transform, "Throne", new Vector3(0, 4.3f, 0), new Vector3(1.7f, 0.5f, 1.7f), goldMat);

        // Ringed Spire (ปล้องไฉน)
        float[] ringRadii = new float[] { 1.4f, 1.2f, 1.0f, 0.8f, 0.6f, 0.45f };
        for (int r = 0; r < ringRadii.Length; r++)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = $"Spire_Ring_{r}";
            ring.transform.SetParent(chedi.transform);
            ring.transform.localPosition = new Vector3(0, 4.8f + r * 0.35f, 0);
            ring.transform.localScale = new Vector3(ringRadii[r], 0.15f, ringRadii[r]);
            ring.GetComponent<Renderer>().material = goldMat;
        }

        // Golden Pinnacle Spire Peak (ยอดปลียอดแหลม)
        GameObject pinnacle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pinnacle.name = "Pinnacle_Spire";
        pinnacle.transform.SetParent(chedi.transform);
        pinnacle.transform.localPosition = new Vector3(0, 7.3f, 0);
        pinnacle.transform.localScale = new Vector3(0.25f, 0.65f, 0.25f);
        pinnacle.GetComponent<Renderer>().material = goldMat;
    }

    private static void BuildSacredBanyanTree(Transform parent, Vector3 pos, Dictionary<string, Material> mats)
    {
        GameObject treeRoot = new GameObject("Sacred_Banyan_Tree");
        treeRoot.transform.SetParent(parent);
        treeRoot.transform.position = pos;

        // Tree visual
        GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TreeModelPath);
        if (treePrefab != null)
        {
            GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, treeRoot.transform);
            tree.transform.localPosition = Vector3.zero;
            tree.transform.localScale = Vector3.one * 1.65f;
        }
        else
        {
            // Fallback majestic trunk & canopy
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(treeRoot.transform);
            trunk.transform.localPosition = new Vector3(0, 4f, 0);
            trunk.transform.localScale = new Vector3(2.5f, 4f, 2.5f);
            trunk.GetComponent<Renderer>().material = mats["WoodDark"];

            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "Canopy";
            canopy.transform.SetParent(treeRoot.transform);
            canopy.transform.localPosition = new Vector3(0, 9f, 0);
            canopy.transform.localScale = new Vector3(12f, 6f, 12f);
            canopy.GetComponent<Renderer>().material = mats["TempleWhite"];
        }

        // Trunk Collider
        CapsuleCollider col = treeRoot.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 3f, 0);
        col.radius = 1.6f;
        col.height = 6.0f;

        // Thai Three-Color Sacred Silk Cloths (ผ้าสามสี: เหลือง แดง ฟ้า/เขียว)
        float[] ribbonY = new float[] { 1.8f, 2.2f, 2.6f };
        string[] ribbonMatKeys = new string[] { "RibbonYellow", "RibbonRed", "RibbonCyan" };

        for (int i = 0; i < 3; i++)
        {
            GameObject ribbon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ribbon.name = $"Sacred_Ribbon_{ribbonMatKeys[i]}";
            ribbon.transform.SetParent(treeRoot.transform);
            ribbon.transform.localPosition = new Vector3(0, ribbonY[i], 0);
            ribbon.transform.localScale = new Vector3(2.6f + i * 0.05f, 0.16f, 2.6f + i * 0.05f);
            ribbon.GetComponent<Renderer>().material = mats[ribbonMatKeys[i]];
            Object.DestroyImmediate(ribbon.GetComponent<Collider>());
        }

        // Offering Altar Table (โต๊ะบูชาเครื่องเซ่นไหว้)
        GameObject altar = new GameObject("Offering_Altar");
        altar.transform.SetParent(treeRoot.transform);
        altar.transform.localPosition = new Vector3(0, 0, 2.2f);

        CreateBox(altar.transform, "Altar_Table", new Vector3(0, 0.5f, 0), new Vector3(1.6f, 0.8f, 0.9f), mats["TempleWhite"]);
        CreateBox(altar.transform, "Incense_Burner", new Vector3(0, 0.98f, 0), new Vector3(0.35f, 0.15f, 0.35f), mats["TempleGold"]);

        // Spirit Shrine (ศาลพระภูมิไม้เล็กๆ)
        GameObject shrine = new GameObject("Spirit_Shrine");
        shrine.transform.SetParent(treeRoot.transform);
        shrine.transform.localPosition = new Vector3(1.5f, 0, 1.8f);
        CreateBox(shrine.transform, "Shrine_Post", new Vector3(0, 0.85f, 0), new Vector3(0.2f, 1.7f, 0.2f), mats["WoodDark"]);
        CreateBox(shrine.transform, "Shrine_House", new Vector3(0, 1.85f, 0), new Vector3(0.8f, 0.6f, 0.8f), mats["TempleGold"]);
        CreateBox(shrine.transform, "Shrine_Roof", new Vector3(0, 2.25f, 0), new Vector3(1.1f, 0.35f, 1.1f), mats["TempleRoofRed"]);
    }

    // -------------------------------------------------------------------------------------------------
    // ZONE 2: NARROW ALLEYWAYS (Thai Stilt Houses, Bamboo Fences, Chicken Coop, Buffalo Corral, Jump Scare)
    // -------------------------------------------------------------------------------------------------
    private static void BuildZone2(Transform root, Dictionary<string, Material> mats)
    {
        GameObject z2 = new GameObject("Zone2_Content");
        z2.transform.SetParent(root);

        // 1. Thai Traditional Wooden Stilt Houses lining the narrow alley
        BuildThaiStiltHouses(z2.transform, mats);

        // 2. Bamboo & Wooden Fences forming narrow alley maze
        BuildAlleyFences(z2.transform);

        // 3. Chicken Coop
        BuildChickenCoop(z2.transform, new Vector3(276f, 13.5f, 542f), mats);

        // 4. Buffalo Corral
        BuildBuffaloCorral(z2.transform, new Vector3(282f, 13.5f, 562f), mats);

        // 5. Jump Scare Ambush Point Setup
        BuildJumpScareSetup(z2.transform);
    }

    private static void BuildThaiStiltHouses(Transform parent, Dictionary<string, Material> mats)
    {
        GameObject houseGroup = new GameObject("Thai_Stilt_Houses");
        houseGroup.transform.SetParent(parent);

        GameObject thaiHousePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ThaiHousePath);
        Material thaiHouseMat = AssetDatabase.LoadAssetAtPath<Material>(ThaiHouseMatPath);

        // House Placements (Left and Right sides of alley)
        (Vector3 pos, float yaw)[] houseConfigs = new (Vector3, float)[]
        {
            // Left row (West side of alley)
            (new Vector3(248f, 13.5f, 538f), 95f),
            (new Vector3(245f, 13.5f, 555f), 85f),
            (new Vector3(250f, 13.5f, 572f), 100f),

            // Right row (East side of alley)
            (new Vector3(268f, 13.5f, 532f), 275f),
            (new Vector3(272f, 13.5f, 548f), 265f),
            (new Vector3(269f, 13.5f, 565f), 280f),
            (new Vector3(286f, 13.5f, 554f), 185f)
        };

        for (int i = 0; i < houseConfigs.Length; i++)
        {
            var cfg = houseConfigs[i];
            GameObject h = null;

            if (thaiHousePrefab != null)
            {
                h = (GameObject)PrefabUtility.InstantiatePrefab(thaiHousePrefab, houseGroup.transform);
                h.name = $"Thai_Isan_House_{i + 1}";
                h.transform.position = cfg.pos;
                h.transform.rotation = Quaternion.Euler(0, cfg.yaw, 0);

                if (thaiHouseMat != null)
                {
                    foreach (var rend in h.GetComponentsInChildren<Renderer>(true))
                    {
                        rend.material = thaiHouseMat;
                    }
                }
            }
            else
            {
                // Fallback traditional Thai stilt house
                h = new GameObject($"Thai_House_Procedural_{i + 1}");
                h.transform.SetParent(houseGroup.transform);
                h.transform.position = cfg.pos;
                h.transform.rotation = Quaternion.Euler(0, cfg.yaw, 0);

                // Stilts (เสาใต้ถุน)
                for (int sx = -1; sx <= 1; sx++)
                {
                    for (int sz = -1; sz <= 1; sz++)
                    {
                        CreateBox(h.transform, "Stilt", new Vector3(sx * 3.5f, 1.8f, sz * 4.0f), new Vector3(0.35f, 3.6f, 0.35f), mats["WoodDark"]);
                    }
                }
                // Living deck floor & walls
                CreateBox(h.transform, "Deck", new Vector3(0, 3.6f, 0), new Vector3(8.5f, 0.3f, 9.5f), mats["WoodPlank"]);
                CreateBox(h.transform, "LivingWalls", new Vector3(0, 5.2f, 0), new Vector3(7.5f, 3.0f, 8.5f), mats["WoodPlank"]);
                CreateBox(h.transform, "ThaiGableRoof", new Vector3(0, 7.5f, 0), new Vector3(9.5f, 1.8f, 10.5f), mats["TinRoof"]);
            }

            // Ensure Solid Box Colliders for House Body and Stilts so zombies/player can't walk through walls
            BoxCollider col = h.GetComponent<BoxCollider>();
            if (col == null) col = h.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 4.0f, 0);
            col.size = new Vector3(8.0f, 7.0f, 9.0f);

            // Add Under-stilt props (ใต้ถุนบ้าน: โอ่งน้ำ, ฟืน, แคร่ไม้)
            AddUnderStiltProps(h.transform, mats);
        }
    }

    private static void AddUnderStiltProps(Transform house, Dictionary<string, Material> mats)
    {
        // Firewood
        GameObject firewoodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FirewoodPrefabPath);
        if (firewoodPrefab != null)
        {
            GameObject fw = (GameObject)PrefabUtility.InstantiatePrefab(firewoodPrefab, house);
            fw.transform.localPosition = new Vector3(-2.8f, 0, -2.5f);
        }

        // Water Well / Jar
        GameObject wellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WellPrefabPath);
        if (wellPrefab != null)
        {
            GameObject w = (GameObject)PrefabUtility.InstantiatePrefab(wellPrefab, house);
            w.transform.localPosition = new Vector3(3.2f, 0, -2.8f);
            w.transform.localScale = Vector3.one * 0.75f;
        }

        // Bamboo Resting Bench (แคร่ไม้ไผ่ใต้ถุน)
        CreateBox(house, "Bamboo_Bench", new Vector3(1.8f, 0.45f, 1.8f), new Vector3(1.8f, 0.5f, 1.0f), mats["WoodPlank"]);
    }

    private static void BuildAlleyFences(Transform parent)
    {
        GameObject fenceGroup = new GameObject("Bamboo_Alley_Fences");
        fenceGroup.transform.SetParent(parent);

        GameObject fencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FencePrefabPath);
        if (fencePrefab == null) fencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FencePlankPath);

        // Line segments along alleyways
        (Vector3 pos, float yaw)[] fenceSpans = new (Vector3, float)[]
        {
            // Left alley fences
            (new Vector3(254f, 13.5f, 532f), 8f),
            (new Vector3(253f, 13.5f, 545f), 12f),
            (new Vector3(252f, 13.5f, 558f), -5f),
            (new Vector3(255f, 13.5f, 570f), 20f),

            // Right alley fences
            (new Vector3(262f, 13.5f, 535f), 10f),
            (new Vector3(264f, 13.5f, 548f), -12f),
            (new Vector3(263f, 13.5f, 560f), 5f),
            (new Vector3(266f, 13.5f, 574f), -18f)
        };

        foreach (var span in fenceSpans)
        {
            if (fencePrefab != null)
            {
                GameObject f = (GameObject)PrefabUtility.InstantiatePrefab(fencePrefab, fenceGroup.transform);
                f.transform.position = span.pos;
                f.transform.rotation = Quaternion.Euler(0, span.yaw, 0);
            }
            else
            {
                GameObject f = GameObject.CreatePrimitive(PrimitiveType.Cube);
                f.name = "Fence_Fallback";
                f.transform.SetParent(fenceGroup.transform);
                f.transform.position = span.pos + Vector3.up * 0.75f;
                f.transform.rotation = Quaternion.Euler(0, span.yaw, 0);
                f.transform.localScale = new Vector3(0.15f, 1.5f, 3.5f);
            }
        }
    }

    private static void BuildChickenCoop(Transform parent, Vector3 pos, Dictionary<string, Material> mats)
    {
        GameObject coop = new GameObject("Chicken_Coop");
        coop.transform.SetParent(parent);
        coop.transform.position = pos;

        // Wooden Hen House
        CreateBox(coop.transform, "HenHouse", new Vector3(0, 1.1f, 0), new Vector3(2.5f, 1.8f, 2.0f), mats["WoodPlank"]);
        CreateBox(coop.transform, "CoopRoof", new Vector3(0, 2.1f, 0), new Vector3(2.9f, 0.3f, 2.4f), mats["TinRoof"]);

        // Straw bedding inside
        CreateBox(coop.transform, "StrawBed", new Vector3(0, 0.2f, 0), new Vector3(2.2f, 0.2f, 1.8f), mats["HayStraw"]);

        // Surrounding wire/wood mesh fence
        CreateBox(coop.transform, "CoopFence_F", new Vector3(0, 0.65f, 2.4f), new Vector3(3.8f, 1.3f, 0.1f), mats["WoodDark"]);
        CreateBox(coop.transform, "CoopFence_L", new Vector3(-1.9f, 0.65f, 1.2f), new Vector3(0.1f, 1.3f, 2.6f), mats["WoodDark"]);
        CreateBox(coop.transform, "CoopFence_R", new Vector3(1.9f, 0.65f, 1.2f), new Vector3(0.1f, 1.3f, 2.6f), mats["WoodDark"]);

        // Spawn Chickens
        GameObject chickenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChickenPrefabPath);
        if (chickenPrefab != null)
        {
            Vector3[] chickenOffsets = new Vector3[]
            {
                new Vector3(-0.6f, 0, 1.2f),
                new Vector3(0.4f, 0, 1.6f),
                new Vector3(0.0f, 0, 0.5f)
            };
            foreach (var off in chickenOffsets)
            {
                GameObject ch = (GameObject)PrefabUtility.InstantiatePrefab(chickenPrefab, coop.transform);
                ch.transform.localPosition = off;
                ch.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            }
        }
    }

    private static void BuildBuffaloCorral(Transform parent, Vector3 pos, Dictionary<string, Material> mats)
    {
        GameObject corral = new GameObject("Buffalo_Corral");
        corral.transform.SetParent(parent);
        corral.transform.position = pos;

        // Mud floor patch
        GameObject mud = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mud.name = "MudPatch";
        mud.transform.SetParent(corral.transform);
        mud.transform.localPosition = new Vector3(0, 0.02f, 0);
        mud.transform.localScale = new Vector3(8.5f, 0.05f, 7.5f);
        mud.GetComponent<Renderer>().material = mats["MudGround"];
        Object.DestroyImmediate(mud.GetComponent<Collider>());

        // Heavy Log Fences around pen
        float hw = 4.2f;
        float hd = 3.6f;
        CreateBox(corral.transform, "LogFence_North", new Vector3(0, 0.8f, hd), new Vector3(hw * 2, 1.4f, 0.35f), mats["WoodDark"]);
        CreateBox(corral.transform, "LogFence_South", new Vector3(0, 0.8f, -hd), new Vector3(hw * 2, 1.4f, 0.35f), mats["WoodDark"]);
        CreateBox(corral.transform, "LogFence_West", new Vector3(-hw, 0.8f, 0), new Vector3(0.35f, 1.4f, hd * 2), mats["WoodDark"]);
        CreateBox(corral.transform, "LogFence_East", new Vector3(hw, 0.8f, 0), new Vector3(0.35f, 1.4f, hd * 2), mats["WoodDark"]);

        // Feeding Trough with Hay
        CreateBox(corral.transform, "FeedTrough", new Vector3(0, 0.5f, hd - 0.7f), new Vector3(3.5f, 0.6f, 0.9f), mats["WoodPlank"]);
        CreateBox(corral.transform, "HayInTrough", new Vector3(0, 0.75f, hd - 0.7f), new Vector3(3.2f, 0.3f, 0.7f), mats["HayStraw"]);

        // Rustic Shade Shelter
        CreateBox(corral.transform, "ShelterPost1", new Vector3(-2.8f, 1.6f, -1.8f), new Vector3(0.3f, 3.2f, 0.3f), mats["WoodDark"]);
        CreateBox(corral.transform, "ShelterPost2", new Vector3(2.8f, 1.6f, -1.8f), new Vector3(0.3f, 3.2f, 0.3f), mats["WoodDark"]);
        CreateBox(corral.transform, "ShelterRoof", new Vector3(0, 3.3f, -1.8f), new Vector3(6.5f, 0.25f, 3.2f), mats["TinRoof"]);
    }

    private static void BuildJumpScareSetup(Transform parent)
    {
        GameObject js = new GameObject("Jump_Scare_Ambush_Point");
        js.transform.SetParent(parent);
        js.transform.position = new Vector3(258f, 13.5f, 528f);

        // Ambush Barricade sign / broken cart
        CreateBox(js.transform, "WarningBarricade", new Vector3(0, 0.6f, 0), new Vector3(3.2f, 1.2f, 0.4f), null);
    }

    // -------------------------------------------------------------------------------------------------
    // ZONE 3: VERTICAL VANTAGE & COVERS / GOAL: BARN (Canal, Bridge, Kubota Tractor, Tiang Na, Barn, Goal)
    // -------------------------------------------------------------------------------------------------
    private static void BuildZone3(Transform root, Dictionary<string, Material> mats)
    {
        GameObject z3 = new GameObject("Zone3_Content");
        z3.transform.SetParent(root);

        // 1. Canal Water & Stream Bed
        BuildCanalWater(z3.transform, mats);

        // 2. Wooden Bridge crossing the canal
        BuildWoodenBridge(z3.transform, new Vector3(276f, 13.5f, 586f), Quaternion.Euler(0, 16f, 0), mats);

        // 3. Kubota Tractor
        BuildKubotaTractor(z3.transform, new Vector3(272f, 13.5f, 604f), Quaternion.Euler(0, 125f, 0), mats);

        // 4. Tiang Na (Thai Farm Hut - เถียงนา)
        BuildTiangNaFarmHut(z3.transform, new Vector3(322f, 13.5f, 606f), Quaternion.Euler(0, 290f, 0), mats);

        // 5. Tactical Hay Bales
        BuildHayBaleStacks(z3.transform, mats);

        // 6. Granary (Barn - ยุ้งฉางข้าว) on stilts with Wooden Ladder (Vertical Vantage Point)
        BuildGranaryBarn(z3.transform, new Vector3(298f, 13.5f, 622f), Quaternion.Euler(0, 195f, 0), mats);

        // 7. Goal Barn Extraction Beacon
        BuildGoalBarnBeacon(z3.transform, new Vector3(298f, 16.5f, 620f));
    }

    private static void BuildCanalWater(Transform parent, Dictionary<string, Material> mats)
    {
        GameObject canal = new GameObject("Canal_Water_Stream");
        canal.transform.SetParent(parent);

        Material waterMat = AssetDatabase.LoadAssetAtPath<Material>(WaterMatPath);
        if (waterMat == null)
        {
            waterMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            waterMat.color = new Color(0.12f, 0.45f, 0.55f, 0.85f);
        }

        // Water surface plane at Y = 12.0m (under the 13.5m ground and bridge)
        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cube);
        water.name = "WaterSurface";
        water.transform.SetParent(canal.transform);
        water.transform.position = new Vector3(280f, 12.0f, 588f);
        water.transform.rotation = Quaternion.Euler(0, -9f, 0);
        water.transform.localScale = new Vector3(8.0f, 0.2f, 110.0f);
        water.GetComponent<Renderer>().material = waterMat;
        Object.DestroyImmediate(water.GetComponent<Collider>());
    }

    private static void BuildWoodenBridge(Transform parent, Vector3 pos, Quaternion rot, Dictionary<string, Material> mats)
    {
        GameObject bridge = new GameObject("Wooden_Bridge");
        bridge.transform.SetParent(parent);
        bridge.transform.position = pos;
        bridge.transform.rotation = rot;

        Material woodMat = mats["WoodPlank"];
        Material darkMat = mats["WoodDark"];

        // Bridge Deck Planks (spanning the canal)
        GameObject deck = CreateBox(bridge.transform, "Bridge_Deck", new Vector3(0, 0.25f, 0), new Vector3(3.8f, 0.4f, 10.5f), woodMat);

        // Support Piers (underneath)
        CreateBox(bridge.transform, "Pier_South", new Vector3(0, -0.8f, -3.8f), new Vector3(4.2f, 2.2f, 0.6f), darkMat);
        CreateBox(bridge.transform, "Pier_Center", new Vector3(0, -1.0f, 0), new Vector3(4.2f, 2.6f, 0.6f), darkMat);
        CreateBox(bridge.transform, "Pier_North", new Vector3(0, -0.8f, 3.8f), new Vector3(4.2f, 2.2f, 0.6f), darkMat);

        // Side Railings
        CreateBox(bridge.transform, "Railing_L", new Vector3(-1.95f, 1.0f, 0), new Vector3(0.2f, 1.1f, 10.5f), darkMat);
        CreateBox(bridge.transform, "Railing_R", new Vector3(1.95f, 1.0f, 0), new Vector3(0.2f, 1.1f, 10.5f), darkMat);
    }

    private static void BuildKubotaTractor(Transform parent, Vector3 pos, Quaternion rot, Dictionary<string, Material> mats)
    {
        GameObject tractor = new GameObject("Kubota_Tractor");
        tractor.transform.SetParent(parent);
        tractor.transform.position = pos;
        tractor.transform.rotation = rot;

        Material orangeMat = mats["KubotaOrange"];
        Material tireMat = mats["TireBlack"];
        Material ironMat = mats["TractorIron"];

        // Engine Hood (Kubota Orange)
        CreateBox(tractor.transform, "EngineHood", new Vector3(0, 1.25f, 0.6f), new Vector3(1.1f, 0.85f, 1.8f), orangeMat);
        CreateBox(tractor.transform, "FrontGrill", new Vector3(0, 1.25f, 1.52f), new Vector3(0.95f, 0.75f, 0.08f), ironMat);

        // Lower Chassis & Engine Block
        CreateBox(tractor.transform, "Chassis", new Vector3(0, 0.65f, 0), new Vector3(1.0f, 0.65f, 3.2f), ironMat);

        // Driver Platform & Mudguards
        CreateBox(tractor.transform, "DriverPlatform", new Vector3(0, 0.95f, -0.8f), new Vector3(1.5f, 0.25f, 1.4f), orangeMat);
        CreateBox(tractor.transform, "Seat", new Vector3(0, 1.4f, -0.9f), new Vector3(0.55f, 0.45f, 0.55f), ironMat);
        CreateBox(tractor.transform, "SteeringWheel", new Vector3(0, 1.6f, -0.2f), new Vector3(0.45f, 0.45f, 0.1f), ironMat);

        // Upright Exhaust Chimney Pipe (ท่อไอเสียตั้งขึ้น)
        GameObject exhaust = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        exhaust.name = "ExhaustPipe";
        exhaust.transform.SetParent(tractor.transform);
        exhaust.transform.localPosition = new Vector3(0.45f, 2.1f, 1.0f);
        exhaust.transform.localScale = new Vector3(0.12f, 0.8f, 0.12f);
        exhaust.GetComponent<Renderer>().material = ironMat;

        // Roll Bar & Sun Canopy (หลังคากันแดดรถไถ)
        CreateBox(tractor.transform, "CanopyPillar_L", new Vector3(-0.75f, 2.1f, -0.9f), new Vector3(0.1f, 2.2f, 0.1f), ironMat);
        CreateBox(tractor.transform, "CanopyPillar_R", new Vector3(0.75f, 2.1f, -0.9f), new Vector3(0.1f, 2.2f, 0.1f), ironMat);
        CreateBox(tractor.transform, "CanopyRoof", new Vector3(0, 3.25f, -0.7f), new Vector3(1.7f, 0.1f, 1.8f), orangeMat);

        // Huge Rear Tires (ล้อหลังขนาดใหญ่)
        GameObject rearL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rearL.name = "RearTire_L";
        rearL.transform.SetParent(tractor.transform);
        rearL.transform.localPosition = new Vector3(-0.95f, 0.85f, -0.9f);
        rearL.transform.localRotation = Quaternion.Euler(0, 0, 90);
        rearL.transform.localScale = new Vector3(1.7f, 0.35f, 1.7f);
        rearL.GetComponent<Renderer>().material = tireMat;

        GameObject rearR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rearR.name = "RearTire_R";
        rearR.transform.SetParent(tractor.transform);
        rearR.transform.localPosition = new Vector3(0.95f, 0.85f, -0.9f);
        rearR.transform.localRotation = Quaternion.Euler(0, 0, 90);
        rearR.transform.localScale = new Vector3(1.7f, 0.35f, 1.7f);
        rearR.GetComponent<Renderer>().material = tireMat;

        // Smaller Front Steering Tires (ล้อหน้า)
        GameObject frontL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        frontL.name = "FrontTire_L";
        frontL.transform.SetParent(tractor.transform);
        frontL.transform.localPosition = new Vector3(-0.75f, 0.45f, 1.1f);
        frontL.transform.localRotation = Quaternion.Euler(0, 0, 90);
        frontL.transform.localScale = new Vector3(0.9f, 0.25f, 0.9f);
        frontL.GetComponent<Renderer>().material = tireMat;

        GameObject frontR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        frontR.name = "FrontTire_R";
        frontR.transform.SetParent(tractor.transform);
        frontR.transform.localPosition = new Vector3(0.75f, 0.45f, 1.1f);
        frontR.transform.localRotation = Quaternion.Euler(0, 0, 90);
        frontR.transform.localScale = new Vector3(0.9f, 0.25f, 0.9f);
        frontR.GetComponent<Renderer>().material = tireMat;
    }

    private static void BuildTiangNaFarmHut(Transform parent, Vector3 pos, Quaternion rot, Dictionary<string, Material> mats)
    {
        GameObject hut = new GameObject("Tiang_Na_Farm_Hut");
        hut.transform.SetParent(parent);
        hut.transform.position = pos;
        hut.transform.rotation = rot;

        Material woodMat = mats["WoodPlank"];
        Material darkMat = mats["WoodDark"];
        Material roofMat = mats["TinRoof"];

        // 4 Raised Stilts (เสาเถียงนา)
        float w = 2.4f;
        float d = 2.4f;
        CreateBox(hut.transform, "Post_1", new Vector3(-w, 1.2f, -d), new Vector3(0.25f, 2.4f, 0.25f), darkMat);
        CreateBox(hut.transform, "Post_2", new Vector3(w, 1.2f, -d), new Vector3(0.25f, 2.4f, 0.25f), darkMat);
        CreateBox(hut.transform, "Post_3", new Vector3(-w, 1.2f, d), new Vector3(0.25f, 2.4f, 0.25f), darkMat);
        CreateBox(hut.transform, "Post_4", new Vector3(w, 1.2f, d), new Vector3(0.25f, 2.4f, 0.25f), darkMat);

        // Bamboo / Wood Raised Floor Deck (พื้นนั่งเล่นเถียงนา)
        CreateBox(hut.transform, "FloorDeck", new Vector3(0, 1.8f, 0), new Vector3(5.6f, 0.3f, 5.6f), woodMat);

        // Open Railings (ราวกั้น)
        CreateBox(hut.transform, "Railing_Back", new Vector3(0, 2.4f, -d + 0.1f), new Vector3(5.2f, 0.8f, 0.15f), woodMat);
        CreateBox(hut.transform, "Railing_Left", new Vector3(-w + 0.1f, 2.4f, 0), new Vector3(0.15f, 0.8f, 5.2f), woodMat);

        // Roof Posts & Thatch/Tin Roof (หลังคาเถียงนา)
        CreateBox(hut.transform, "RoofPost_1", new Vector3(-w, 3.0f, -d), new Vector3(0.2f, 2.2f, 0.2f), darkMat);
        CreateBox(hut.transform, "RoofPost_2", new Vector3(w, 3.0f, -d), new Vector3(0.2f, 2.2f, 0.2f), darkMat);
        CreateBox(hut.transform, "RoofPost_3", new Vector3(-w, 3.0f, d), new Vector3(0.2f, 2.2f, 0.2f), darkMat);
        CreateBox(hut.transform, "RoofPost_4", new Vector3(w, 3.0f, d), new Vector3(0.2f, 2.2f, 0.2f), darkMat);
        CreateBox(hut.transform, "PitchedRoof", new Vector3(0, 4.4f, 0), new Vector3(6.4f, 0.6f, 6.4f), roofMat);
    }

    private static void BuildHayBaleStacks(Transform parent, Dictionary<string, Material> mats)
    {
        GameObject hayGroup = new GameObject("Hay_Bale_Tactical_Covers");
        hayGroup.transform.SetParent(parent);

        Material hayMat = mats["HayStraw"];
        GameObject hayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HayBalePrefabPath);

        Vector3[] stackOrigins = new Vector3[]
        {
            new Vector3(308f, 13.5f, 598f), // Main pile between bridge and Tiang Na
            new Vector3(288f, 13.5f, 612f), // Cover near Tractor & Barn approach
            new Vector3(316f, 13.5f, 620f)  // Perimeter cover
        };

        foreach (var orig in stackOrigins)
        {
            // Pyramid 3-2-1 formation for tactical cover
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 2; c++)
                {
                    Vector3 p = orig + new Vector3((c - 0.5f) * 1.3f, 0.45f, (r - 1f) * 1.6f);
                    CreateHayBale(hayGroup.transform, p, hayPrefab, hayMat);
                }
            }
            // 2nd layer
            CreateHayBale(hayGroup.transform, orig + new Vector3(0, 1.25f, -0.6f), hayPrefab, hayMat);
            CreateHayBale(hayGroup.transform, orig + new Vector3(0, 1.25f, 0.6f), hayPrefab, hayMat);
        }
    }

    private static void CreateHayBale(Transform parent, Vector3 pos, GameObject prefab, Material hayMat)
    {
        if (prefab != null)
        {
            GameObject b = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            b.transform.position = pos;
            b.transform.rotation = Quaternion.Euler(0, Random.Range(0, 4) * 90f, 0);
        }
        else
        {
            CreateBox(parent, "HayBale", pos, new Vector3(1.2f, 0.8f, 1.5f), hayMat);
        }
    }

    private static void BuildGranaryBarn(Transform parent, Vector3 pos, Quaternion rot, Dictionary<string, Material> mats)
    {
        GameObject barn = new GameObject("Granary_Barn_Goal");
        barn.transform.SetParent(parent);
        barn.transform.position = pos;
        barn.transform.rotation = rot;

        Material woodMat = mats["WoodPlank"];
        Material darkMat = mats["WoodDark"];
        Material roofMat = mats["TinRoof"];

        // 1. Heavy High Stilts (เสายุ้งฉางสูง 4 เมตร)
        float bw = 4.0f;
        float bd = 5.0f;
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                CreateBox(barn.transform, "BarnStilt", new Vector3(x * bw, 2.0f, z * bd), new Vector3(0.55f, 4.0f, 0.55f), darkMat);
            }
        }

        // 2. High Elevated Balcony Deck (ชั้นระเบียงยุ้งฉาง Y = 4.0m)
        CreateBox(barn.transform, "BarnPlatform", new Vector3(0, 4.15f, 0), new Vector3(9.6f, 0.35f, 12.0f), woodMat);

        // 3. Granary Storage Room (ตัวเรือนยุ้งข้าว)
        CreateBox(barn.transform, "GranaryRoom", new Vector3(0, 6.2f, -1.0f), new Vector3(8.5f, 3.8f, 9.5f), woodMat);
        CreateBox(barn.transform, "BarnDoor", new Vector3(0, 5.7f, 3.8f), new Vector3(2.2f, 2.8f, 0.15f), darkMat);

        // 4. Overhanging Pitched Roof (หลังคายุ้งข้าวยื่นกว้าง)
        CreateBox(barn.transform, "BarnRoof_Lower", new Vector3(0, 8.4f, 0), new Vector3(11.2f, 0.8f, 13.5f), roofMat);
        CreateBox(barn.transform, "BarnRoof_Apex", new Vector3(0, 9.6f, 0), new Vector3(7.0f, 1.8f, 9.5f), roofMat);

        // 5. Wooden Ladder (บันไดไม้พาดขึ้นยุ้งฉาง - Vertical Vantage Point)
        GameObject ladder = new GameObject("Wooden_Ladder_Vantage");
        ladder.transform.SetParent(barn.transform);
        ladder.transform.localPosition = new Vector3(-2.8f, 0, 5.8f);

        // Lean ladder from ground (Z = 7.5, Y = 0) up to platform (Z = 5.8, Y = 4.15)
        Vector3 ladderBottom = new Vector3(0, 0, 2.0f);
        Vector3 ladderTop = new Vector3(0, 4.15f, 0);
        Vector3 ladderMid = (ladderBottom + ladderTop) * 0.5f;
        Vector3 ladderDir = ladderTop - ladderBottom;

        GameObject ladderRamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ladderRamp.name = "Ladder_Walkable_Ramp";
        ladderRamp.transform.SetParent(ladder.transform);
        ladderRamp.transform.localPosition = ladderMid;
        ladderRamp.transform.localRotation = Quaternion.LookRotation(ladderDir, Vector3.up);
        ladderRamp.transform.localScale = new Vector3(1.6f, 0.15f, ladderDir.magnitude);
        ladderRamp.GetComponent<Renderer>().material = darkMat;

        // Visual rungs
        int rungs = 10;
        for (int r = 0; r <= rungs; r++)
        {
            float t = (float)r / rungs;
            Vector3 rungPos = Vector3.Lerp(ladderBottom, ladderTop, t);
            CreateBox(ladder.transform, $"Rung_{r}", rungPos, new Vector3(1.5f, 0.1f, 0.15f), woodMat);
        }
    }

    private static void BuildGoalBarnBeacon(Transform parent, Vector3 pos)
    {
        GameObject beacon = new GameObject("GOAL_BARN_BEACON");
        beacon.transform.SetParent(parent);
        beacon.transform.position = pos;

        // Light & Visual
        Light l = beacon.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.2f, 0.9f, 0.4f);
        l.range = 15f;
        l.intensity = 3.5f;

        // Goal Trigger
        BoxCollider col = beacon.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(6f, 4f, 6f);
    }

    // -------------------------------------------------------------------------------------------------
    // GAMEPLAY SYSTEMS: PLAYER POSITION, LIGHTING, ATMOSPHERE & WAVEMANAGER
    // -------------------------------------------------------------------------------------------------
    private static void SetupPlayerPosition()
    {
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            // South entrance spawn point facing North along the curved road
            Vector3 spawnPos = new Vector3(255.0f, 14.5f, 468.0f);
            player.transform.position = spawnPos;
            player.transform.rotation = Quaternion.Euler(0, 12.0f, 0);

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                player.transform.position = spawnPos;
                cc.enabled = true;
            }
            Debug.Log($"[BuildNewMap] Player positioned at Zone 1 Spawn: {spawnPos}");
        }
    }

    private static void SetupLightingAndAtmosphere()
    {
        // Directional Light
        Light dirLight = Object.FindAnyObjectByType<Light>();
        if (dirLight != null && dirLight.type == LightType.Directional)
        {
            dirLight.transform.rotation = Quaternion.Euler(32.0f, 145.0f, 0);
            dirLight.color = new Color(0.85f, 0.80f, 0.72f);
            dirLight.intensity = 1.05f;
        }

        // Atmosphere Fog (Eerie mist as in concept art)
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0065f;
        RenderSettings.fogColor = new Color(0.18f, 0.20f, 0.22f);
        RenderSettings.ambientSkyColor = new Color(0.28f, 0.30f, 0.34f);
    }

    private static void SetupWaveManager(Transform z1, Transform z2, Transform z3)
    {
        WaveManager wm = Object.FindAnyObjectByType<WaveManager>();
        if (wm == null)
        {
            Debug.LogWarning("[BuildNewMap] WaveManager not found!");
            return;
        }

        GameObject zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZombiePrefabPath);
        GameObject gruntPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GruntPrefabPath);
        GameObject swarmerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SwarmerPrefabPath);
        GameObject tankPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TankPrefabPath);

        if (zombiePrefab == null) zombiePrefab = gruntPrefab;
        if (swarmerPrefab == null) swarmerPrefab = zombiePrefab;
        if (tankPrefab == null) tankPrefab = zombiePrefab;

        wm.waves.Clear();

        // ----------------- WAVE 1 CONFIG -----------------
        WaveConfig wave1 = new WaveConfig();

        // Wave 1 Spawners (Temple yard and roadside)
        Transform[] sp1 = new Transform[]
        {
            CreateSpawner(z1, "Sp_W1_Temple_1", new Vector3(210f, 13.5f, 515f)),
            CreateSpawner(z1, "Sp_W1_Temple_2", new Vector3(225f, 13.5f, 525f)),
            CreateSpawner(z1, "Sp_W1_Road_Pickup", new Vector3(242f, 13.5f, 502f)),
            CreateSpawner(z1, "Sp_W1_Road_Bend", new Vector3(258f, 13.5f, 515f))
        };
        wave1.spawnPoints = sp1;

        wave1.spawnSequences.Add(new SpawnSequence { enemyPrefab = zombiePrefab, amount = 4, delayBeforeSpawn = 0.5f, intervalBetweenEach = 1.0f });
        wave1.spawnSequences.Add(new SpawnSequence { enemyPrefab = gruntPrefab ?? zombiePrefab, amount = 2, delayBeforeSpawn = 3.0f, intervalBetweenEach = 1.5f });
        wm.waves.Add(wave1);

        // ----------------- WAVE 2 CONFIG (Narrow Alleyways / Jump Scare) -----------------
        WaveConfig wave2 = new WaveConfig();

        // Wave 2 Spawners (Under stilt houses, behind bamboo fences, Chicken Coop)
        Transform[] sp2 = new Transform[]
        {
            CreateSpawner(z2, "Sp_W2_JumpScare_House1", new Vector3(246f, 13.5f, 542f)),
            CreateSpawner(z2, "Sp_W2_Alley_Right", new Vector3(270f, 13.5f, 540f)),
            CreateSpawner(z2, "Sp_W2_ChickenCoop", new Vector3(275f, 13.5f, 546f)),
            CreateSpawner(z2, "Sp_W2_BuffaloCorral", new Vector3(280f, 13.5f, 562f)),
            CreateSpawner(z2, "Sp_W2_DeepAlley", new Vector3(268f, 13.5f, 568f))
        };
        wave2.spawnPoints = sp2;

        wave2.spawnSequences.Add(new SpawnSequence { enemyPrefab = swarmerPrefab, amount = 5, delayBeforeSpawn = 0.2f, intervalBetweenEach = 0.4f });
        wave2.spawnSequences.Add(new SpawnSequence { enemyPrefab = zombiePrefab, amount = 4, delayBeforeSpawn = 2.0f, intervalBetweenEach = 0.8f });
        wave2.spawnSequences.Add(new SpawnSequence { enemyPrefab = gruntPrefab ?? zombiePrefab, amount = 3, delayBeforeSpawn = 4.0f, intervalBetweenEach = 1.0f });
        wm.waves.Add(wave2);

        // ----------------- WAVE 3 CONFIG (Bridge, Farmyard, Granary Barn, Boss Tank) -----------------
        WaveConfig wave3 = new WaveConfig();

        // Wave 3 Spawners (Around Bridge, Tractor, Tiang Na, Granary)
        Transform[] sp3 = new Transform[]
        {
            CreateSpawner(z3, "Sp_W3_Tractor", new Vector3(270f, 13.5f, 606f)),
            CreateSpawner(z3, "Sp_W3_TiangNa", new Vector3(320f, 13.5f, 610f)),
            CreateSpawner(z3, "Sp_W3_HayBales", new Vector3(305f, 13.5f, 602f)),
            CreateSpawner(z3, "Sp_W3_BarnBoss", new Vector3(298f, 13.5f, 616f)) // Boss Spawn
        };
        wave3.spawnPoints = sp3;

        wave3.spawnSequences.Add(new SpawnSequence { enemyPrefab = zombiePrefab, amount = 6, delayBeforeSpawn = 0.5f, intervalBetweenEach = 0.6f });
        wave3.spawnSequences.Add(new SpawnSequence { enemyPrefab = swarmerPrefab, amount = 4, delayBeforeSpawn = 2.0f, intervalBetweenEach = 0.5f });
        // Boss Zombie Tank Spawn!
        wave3.spawnSequences.Add(new SpawnSequence { enemyPrefab = tankPrefab, amount = 1, delayBeforeSpawn = 4.0f, intervalBetweenEach = 0.1f });
        wm.waves.Add(wave3);

        Debug.Log("[BuildNewMap] Configured WaveManager with 3 Waves automatically!");
    }

    private static Transform CreateSpawner(Transform parent, string name, Vector3 pos)
    {
        GameObject sp = new GameObject(name);
        sp.transform.SetParent(parent);
        sp.transform.position = pos;
        return sp.transform;
    }

    private static void BakeNavMesh(Scene scene)
    {
        try
        {
            NavMeshSurface surface = Object.FindAnyObjectByType<NavMeshSurface>();
            if (surface == null)
            {
                GameObject obj = new GameObject("[NavMeshSurface_Manager]");
                surface = obj.AddComponent<NavMeshSurface>();
            }

            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.agentTypeID = 0;
            surface.BuildNavMesh();
            Debug.Log("[BuildNewMap] Successfully baked NavMesh for all 3 Zones!");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[BuildNewMap] NavMesh bake note: " + ex.Message);
        }
    }

    // Helper: Create a Box primitive with MeshRenderer and BoxCollider
    private static GameObject CreateBox(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent);
        box.transform.localPosition = localPos;
        box.transform.localScale = localScale;
        if (mat != null) box.GetComponent<Renderer>().material = mat;
        return box;
    }
}
