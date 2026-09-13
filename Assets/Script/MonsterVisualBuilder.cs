using UnityEngine;

public static class MonsterVisualBuilder
{
    private static Material CreateMaterial(Color baseColor, Color emissionColor)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.color = baseColor;
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", baseColor);
        }

        if (emissionColor != Color.black)
        {
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", emissionColor);
            }
        }
        return mat;
    }

    public static void BuildVisual(GameObject root, EnemyStats stats)
    {
        // Remove existing visual children if any
        for (int i = root.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = root.transform.GetChild(i);
            if (child.name.StartsWith("Visual_"))
            {
                Object.Destroy(child.gameObject);
            }
        }

        GameObject visualContainer = new GameObject("Visual_" + stats.type);
        visualContainer.transform.SetParent(root.transform, false);

        Material bodyMat = CreateMaterial(stats.primaryColor, Color.black);
        Material glowMat = CreateMaterial(stats.primaryColor * 0.4f, stats.emissionColor);

        switch (stats.type)
        {
            case EnemyType.Swarmer:
                BuildSwarmer(visualContainer, bodyMat, glowMat);
                break;
            case EnemyType.Grunt:
                BuildGrunt(visualContainer, bodyMat, glowMat);
                break;
            case EnemyType.Tank:
                BuildTank(visualContainer, bodyMat, glowMat);
                break;
            case EnemyType.Elite:
                BuildElite(visualContainer, bodyMat, glowMat);
                break;
        }

        visualContainer.transform.localScale = Vector3.one * stats.modelScale;
    }

    private static void BuildSwarmer(GameObject parent, Material bodyMat, Material glowMat)
    {
        // Low hunched body
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Body";
        body.transform.SetParent(parent.transform, false);
        body.transform.localPosition = new Vector3(0, 0.45f, 0);
        body.transform.localScale = new Vector3(0.7f, 0.5f, 0.9f);
        body.GetComponent<Renderer>().sharedMaterial = bodyMat;
        Object.Destroy(body.GetComponent<Collider>());

        // Glowing Eyes
        CreateEye(parent.transform, new Vector3(0.18f, 0.55f, 0.38f), new Vector3(0.15f, 0.15f, 0.15f), glowMat);
        CreateEye(parent.transform, new Vector3(-0.18f, 0.55f, 0.38f), new Vector3(0.15f, 0.15f, 0.15f), glowMat);

        // Mandibles / Spikes
        CreatePart(parent.transform, PrimitiveType.Cube, new Vector3(0.22f, 0.35f, 0.55f), new Vector3(0.08f, 0.08f, 0.35f), Quaternion.Euler(15f, 25f, 0), glowMat);
        CreatePart(parent.transform, PrimitiveType.Cube, new Vector3(-0.22f, 0.35f, 0.55f), new Vector3(0.08f, 0.08f, 0.35f), Quaternion.Euler(15f, -25f, 0), glowMat);

        // Back Spikes
        CreatePart(parent.transform, PrimitiveType.Cylinder, new Vector3(0, 0.75f, -0.1f), new Vector3(0.08f, 0.22f, 0.08f), Quaternion.Euler(-30f, 0, 0), glowMat);
    }

    private static void BuildGrunt(GameObject parent, Material bodyMat, Material glowMat)
    {
        // Torso
        GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        torso.name = "Torso";
        torso.transform.SetParent(parent.transform, false);
        torso.transform.localPosition = new Vector3(0, 1.1f, 0);
        torso.transform.localScale = new Vector3(0.6f, 0.8f, 0.45f);
        torso.GetComponent<Renderer>().sharedMaterial = bodyMat;
        Object.Destroy(torso.GetComponent<Collider>());

        // Head
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "Head";
        head.transform.SetParent(parent.transform, false);
        head.transform.localPosition = new Vector3(0, 1.9f, 0.05f);
        head.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        head.GetComponent<Renderer>().sharedMaterial = bodyMat;
        Object.Destroy(head.GetComponent<Collider>());

        // Glowing Eyes
        CreateEye(parent.transform, new Vector3(0.12f, 1.95f, 0.24f), new Vector3(0.1f, 0.1f, 0.1f), glowMat);
        CreateEye(parent.transform, new Vector3(-0.12f, 1.95f, 0.24f), new Vector3(0.1f, 0.1f, 0.1f), glowMat);

        // Reaching zombie arms
        CreatePart(parent.transform, PrimitiveType.Cube, new Vector3(0.45f, 1.4f, 0.45f), new Vector3(0.18f, 0.18f, 0.8f), Quaternion.Euler(-10f, 5f, 0), bodyMat);
        CreatePart(parent.transform, PrimitiveType.Cube, new Vector3(-0.45f, 1.4f, 0.45f), new Vector3(0.18f, 0.18f, 0.8f), Quaternion.Euler(-10f, -5f, 0), bodyMat);
    }

    private static void BuildTank(GameObject parent, Material bodyMat, Material glowMat)
    {
        // Massive Heavy Torso
        GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
        torso.name = "Torso";
        torso.transform.SetParent(parent.transform, false);
        torso.transform.localPosition = new Vector3(0, 1.25f, 0);
        torso.transform.localScale = new Vector3(1.1f, 1.1f, 0.8f);
        torso.GetComponent<Renderer>().sharedMaterial = bodyMat;
        Object.Destroy(torso.GetComponent<Collider>());

        // Massive Shoulder Pads
        CreatePart(parent.transform, PrimitiveType.Cube, new Vector3(0.75f, 1.7f, 0), new Vector3(0.5f, 0.4f, 0.6f), Quaternion.Euler(0, 0, -20f), bodyMat);
        CreatePart(parent.transform, PrimitiveType.Cube, new Vector3(-0.75f, 1.7f, 0), new Vector3(0.5f, 0.4f, 0.6f), Quaternion.Euler(0, 0, 20f), bodyMat);

        // Glowing Core on Chest
        CreatePart(parent.transform, PrimitiveType.Sphere, new Vector3(0, 1.35f, 0.42f), new Vector3(0.35f, 0.35f, 0.18f), Quaternion.identity, glowMat);

        // Head with Heavy Horns
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "Head";
        head.transform.SetParent(parent.transform, false);
        head.transform.localPosition = new Vector3(0, 2.0f, 0.15f);
        head.transform.localScale = new Vector3(0.5f, 0.45f, 0.5f);
        head.GetComponent<Renderer>().sharedMaterial = bodyMat;
        Object.Destroy(head.GetComponent<Collider>());

        // Horns
        CreatePart(parent.transform, PrimitiveType.Cylinder, new Vector3(0.32f, 2.35f, 0.1f), new Vector3(0.12f, 0.3f, 0.12f), Quaternion.Euler(20f, 0, 35f), glowMat);
        CreatePart(parent.transform, PrimitiveType.Cylinder, new Vector3(-0.32f, 2.35f, 0.1f), new Vector3(0.12f, 0.3f, 0.12f), Quaternion.Euler(20f, 0, -35f), glowMat);

        // Heavy Fist Clubs
        CreatePart(parent.transform, PrimitiveType.Sphere, new Vector3(0.7f, 0.6f, 0.4f), new Vector3(0.45f, 0.45f, 0.45f), Quaternion.identity, bodyMat);
        CreatePart(parent.transform, PrimitiveType.Sphere, new Vector3(-0.7f, 0.6f, 0.4f), new Vector3(0.45f, 0.45f, 0.45f), Quaternion.identity, bodyMat);
    }

    private static void BuildElite(GameObject parent, Material bodyMat, Material glowMat)
    {
        // Sleek Assassin Torso
        GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
        torso.name = "Torso";
        torso.transform.SetParent(parent.transform, false);
        torso.transform.localPosition = new Vector3(0, 1.25f, 0);
        torso.transform.localScale = new Vector3(0.55f, 0.95f, 0.4f);
        torso.GetComponent<Renderer>().sharedMaterial = bodyMat;
        Object.Destroy(torso.GetComponent<Collider>());

        // Glowing Spinal Crest
        CreatePart(parent.transform, PrimitiveType.Cube, new Vector3(0, 1.4f, -0.22f), new Vector3(0.08f, 0.8f, 0.12f), Quaternion.identity, glowMat);

        // Sharp Head
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "Head";
        head.transform.SetParent(parent.transform, false);
        head.transform.localPosition = new Vector3(0, 1.95f, 0.08f);
        head.transform.localScale = new Vector3(0.35f, 0.4f, 0.4f);
        head.transform.localRotation = Quaternion.Euler(15f, 0, 0);
        head.GetComponent<Renderer>().sharedMaterial = bodyMat;
        Object.Destroy(head.GetComponent<Collider>());

        // Glowing Visor / Eye Slit
        CreatePart(parent.transform, PrimitiveType.Cube, new Vector3(0, 1.98f, 0.28f), new Vector3(0.28f, 0.08f, 0.08f), Quaternion.identity, glowMat);

        // Twin Arm Blades
        CreatePart(parent.transform, PrimitiveType.Cube, new Vector3(0.42f, 1.15f, 0.35f), new Vector3(0.06f, 0.1f, 0.75f), Quaternion.Euler(-15f, 10f, 0), glowMat);
        CreatePart(parent.transform, PrimitiveType.Cube, new Vector3(-0.42f, 1.15f, 0.35f), new Vector3(0.06f, 0.1f, 0.75f), Quaternion.Euler(-15f, -10f, 0), glowMat);
    }

    private static void CreateEye(Transform parent, Vector3 localPos, Vector3 scale, Material mat)
    {
        CreatePart(parent, PrimitiveType.Sphere, localPos, scale, Quaternion.identity, mat);
    }

    private static void CreatePart(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 scale, Quaternion localRot, Material mat)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPos;
        part.transform.localRotation = localRot;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = mat;
        Object.Destroy(part.GetComponent<Collider>());
    }
}
