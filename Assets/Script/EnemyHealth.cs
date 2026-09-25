using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isDead { get; private set; }

    [Header("UI Display")]
    public string customDisplayName = "";

    [Header("Health Bar Visibility")]
    [Tooltip("แสดงหลอดเลือดเฉพาะตอนที่โดนดาเมจจากผู้เล่น")]
    public bool showHealthBarOnlyOnDamage = true;
    [Tooltip("ระยะเวลาที่หลอดเลือดจะแสดงหลังจากโดนดาเมจ (วินาที)")]
    public float healthBarVisibleDuration = 4.0f;
    private float hideHealthBarTime = 0f;

    private EnemyStats stats;
    private Renderer[] renderers;
    private Color[] originalColors;
    private Transform healthBarRoot;
    private Image healthFillImg;
    private Text healthText;
    private Camera mainCam;

    void Awake()
    {
        currentHealth = maxHealth;
        isDead = false;
        mainCam = Camera.main;
    }

    void Start()
    {
        if (renderers == null || renderers.Length == 0)
        {
            CacheRenderers();
        }

        if (healthBarRoot == null)
        {
            CreateWorldHealthBar();
        }

        UpdateHealthBarVisuals();

        if (healthBarRoot != null && showHealthBarOnlyOnDamage)
        {
            healthBarRoot.gameObject.SetActive(false);
        }
    }

    public void Initialize(EnemyStats enemyStats)
    {
        stats = enemyStats;
        if (stats != null)
        {
            maxHealth = stats.maxHealth;
            customDisplayName = stats.displayName;
        }
        currentHealth = maxHealth;
        isDead = false;

        CacheRenderers();
        CreateWorldHealthBar();
        UpdateHealthBarVisuals();

        if (healthBarRoot != null && showHealthBarOnlyOnDamage)
        {
            healthBarRoot.gameObject.SetActive(false);
        }
    }

    private void CacheRenderers()
    {
        renderers = GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material != null)
                {
                    originalColors[i] = GetMaterialColor(renderers[i].material);
                }
            }
        }
    }

    private Color GetMaterialColor(Material mat)
    {
        if (mat == null) return Color.white;
        if (mat.HasProperty("_BaseColor")) return mat.GetColor("_BaseColor");
        if (mat.HasProperty("_Color")) return mat.GetColor("_Color");
        return Color.white;
    }

    private void SetMaterialColor(Material mat, Color col)
    {
        if (mat == null) return;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
    }

    private float CalculateHealthBarHeight()
    {
        if (stats != null && stats.modelScale > 0)
        {
            return stats.modelScale * 2.2f + 0.35f;
        }

        CapsuleCollider cc = GetComponent<CapsuleCollider>();
        if (cc != null)
        {
            return cc.center.y + (cc.height * 0.5f * transform.localScale.y) + 0.35f;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            return (col.bounds.max.y - transform.position.y) + 0.35f;
        }

        Renderer r = GetComponentInChildren<Renderer>();
        if (r != null)
        {
            return (r.bounds.max.y - transform.position.y) + 0.35f;
        }

        return 1.8f;
    }

    void CreateWorldHealthBar()
    {
        if (healthBarRoot != null) return;

        float spawnHeight = CalculateHealthBarHeight();

        GameObject canvasObj = new GameObject("HealthBarCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvasObj.transform.localPosition = new Vector3(0, spawnHeight, 0);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObj.AddComponent<CanvasScaler>();

        RectTransform canvasRt = canvasObj.GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(160f, 40f);
        canvasObj.transform.localScale = Vector3.one * 0.009f;

        // Background Bar
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.sizeDelta = new Vector2(130f, 15f);
        bgRt.anchoredPosition = new Vector2(0, -6f);

        // Fill Bar (Solid fill inside background)
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(bgObj.transform, false);
        healthFillImg = fillObj.AddComponent<Image>();
        healthFillImg.color = new Color(0.95f, 0.25f, 0.25f, 1f);
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0, 0);
        fillRt.anchorMax = new Vector2(1, 1);
        fillRt.pivot = new Vector2(0, 0.5f);
        fillRt.sizeDelta = new Vector2(-4f, -4f);
        fillRt.anchoredPosition = new Vector2(2f, 0);

        // Name and Health Text
        GameObject textObj = new GameObject("NameText");
        textObj.transform.SetParent(canvasObj.transform, false);
        textObj.transform.localPosition = new Vector3(0, 10f, 0);
        healthText = textObj.AddComponent<Text>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        healthText.font = font;

        healthText.fontSize = 15;
        healthText.fontStyle = FontStyle.Bold;
        healthText.alignment = TextAnchor.MiddleCenter;
        healthText.color = Color.white;

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.sizeDelta = new Vector2(160f, 22f);

        healthBarRoot = canvasObj.transform;
        if (showHealthBarOnlyOnDamage)
        {
            healthBarRoot.gameObject.SetActive(false);
        }
    }

    void LateUpdate()
    {
        if (healthBarRoot != null)
        {
            if (showHealthBarOnlyOnDamage)
            {
                if (healthBarRoot.gameObject.activeSelf)
                {
                    if (Time.time >= hideHealthBarTime || isDead)
                    {
                        healthBarRoot.gameObject.SetActive(false);
                        return;
                    }
                }
                else
                {
                    return; // ซ่อนอยู่ ไม่ต้องหมุนตามกล้องเพื่อประหยัด Performance
                }
            }

            if (mainCam == null) mainCam = Camera.main;
            if (mainCam != null)
            {
                // Face the camera view plane directly
                healthBarRoot.rotation = mainCam.transform.rotation;
            }
        }
    }

    private static float lastGroundBloodTime = 0f;

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, transform.position + Vector3.up * 1.0f, -transform.forward);
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal = default)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        // แสดงหลอดเลือดเมื่อโดนดาเมจจากผู้เล่น และนับเวลาถอยหลังเพื่อซ่อน
        if (healthBarRoot != null && damage > 0f)
        {
            healthBarRoot.gameObject.SetActive(true);
            hideHealthBarTime = Time.time + healthBarVisibleDuration;
        }

        UpdateHealthBarVisuals();

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayEnemyHurt();
        }

        StartCoroutine(DamageFlashRoutine());

        // เอฟเฟกต์เลือดกระจาย (Blood Splatter Effect)
        if (damage > 0f)
        {
            Vector3 normal = (hitNormal != default && hitNormal != Vector3.zero) ? hitNormal : (Camera.main != null ? (Camera.main.transform.position - hitPoint).normalized : Vector3.up);
            float scale = Mathf.Clamp(damage / 35f, 0.7f, 1.6f);
            CreateBloodSplatter(hitPoint, normal, scale);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void UpdateHealthBarVisuals()
    {
        float ratio = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

        if (healthFillImg != null)
        {
            RectTransform fillRt = healthFillImg.rectTransform;
            fillRt.anchorMin = new Vector2(0, 0);
            fillRt.anchorMax = new Vector2(ratio, 1);

            // Dynamic color: Green/Yellow when healthy, Red when low
            if (ratio > 0.5f)
            {
                healthFillImg.color = Color.Lerp(new Color(1f, 0.8f, 0.2f), new Color(0.25f, 0.88f, 0.35f), (ratio - 0.5f) * 2f);
            }
            else
            {
                healthFillImg.color = Color.Lerp(new Color(0.9f, 0.15f, 0.15f), new Color(1f, 0.8f, 0.2f), ratio * 2f);
            }
        }

        if (healthText != null)
        {
            string enemyName = customDisplayName;
            if (string.IsNullOrEmpty(enemyName))
            {
                if (GetComponent<ChickenAI>() != null || gameObject.name.Contains("02") || gameObject.name.Contains("Chicken"))
                {
                    enemyName = "Chicken";
                }
                else if (GetComponent<RaptorAI>() != null || gameObject.name.Contains("01") || gameObject.name.Contains("Raptor"))
                {
                    enemyName = "Raptor";
                }
                else if (GetComponent<ZombieAI>() != null || gameObject.name.Contains("Zombie"))
                {
                    enemyName = "Zombie";
                }
                else
                {
                    enemyName = gameObject.name.Replace("Enemy_", "").Replace("(Clone)", "").Trim();
                    if (enemyName == "02") enemyName = "Chicken";
                    else if (enemyName == "01") enemyName = "Raptor";
                }
            }
            else if (enemyName == "02" || enemyName == "Enemy_02")
            {
                enemyName = "Chicken";
            }
            else if (enemyName == "01" || enemyName == "Enemy_01")
            {
                enemyName = "Raptor";
            }

            healthText.text = $"{enemyName}  {Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
        }
    }

    IEnumerator DamageFlashRoutine()
    {
        if (renderers == null || renderers.Length == 0) yield break;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null)
            {
                SetMaterialColor(renderers[i].material, Color.white);
            }
        }

        yield return new WaitForSeconds(0.06f);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null && originalColors != null && i < originalColors.Length)
            {
                SetMaterialColor(renderers[i].material, originalColors[i]);
            }
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayEnemyDeath();
        }

        // Spawn rich death particle explosion & bloody burst
        Color deathColor = stats != null ? stats.primaryColor : new Color(0.9f, 0.25f, 0.1f);
        SpawnDeathEffect(transform.position + Vector3.up * 0.6f, deathColor);
        CreateBloodSplatter(transform.position + Vector3.up * 0.7f, Vector3.up, 1.6f);

        // Drop loot
        GetComponent<EnemyAI>()?.DropLoot();

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnEnemyKilled(gameObject);
        }

        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (var c in cols) c.enabled = false;

        var ai = GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;

        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        if (healthBarRoot != null) healthBarRoot.gameObject.SetActive(false);

        StartCoroutine(DeathAnimationRoutine());
    }

    private void SpawnDeathEffect(Vector3 pos, Color col)
    {
        GameObject fxObj = new GameObject("DeathFX_Explosion");
        fxObj.transform.position = pos;

        // 1. Particle System Burst
        ParticleSystem ps = fxObj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = 0.7f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 7.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startColor = new ParticleSystem.MinMaxGradient(col, col * 1.5f);
        main.gravityModifier = 1.3f;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 35) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.4f;

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(col, 0f), new GradientColorKey(Color.white, 0.2f), new GradientColorKey(new Color(0.2f, 0.05f, 0.05f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.6f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLife.color = grad;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0.1f);
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystemRenderer psr = fxObj.GetComponent<ParticleSystemRenderer>();
        Shader pShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
        psr.material = new Material(pShader);

        // 2. Point light burst
        GameObject lObj = new GameObject("DeathFlashLight");
        lObj.transform.SetParent(fxObj.transform, false);
        Light l = lObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = col;
        l.range = 6f;
        l.intensity = 4f;

        Destroy(fxObj, 1.2f);
    }

    public static void CreateBloodSplatter(Vector3 pos, Vector3 normal, float scale = 1.0f)
    {
        if (normal == Vector3.zero) normal = Vector3.up;

        GameObject fxObj = new GameObject("BloodSplatterFX");
        fxObj.transform.position = pos;
        fxObj.transform.rotation = Quaternion.LookRotation(normal);

        // 1. ละอองและหยดเลือดพุ่งกระจาย (Blood Spray Particles)
        ParticleSystem ps = fxObj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.8f * scale, 6.2f * scale);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f * scale, 0.22f * scale);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.92f, 0.05f, 0.05f, 1f), // สีเลือดสด (Bright Scarlet)
            new Color(0.42f, 0.015f, 0.015f, 1f) // สีเลือดข้นคล้ำ (Deep Crimson)
        );
        main.gravityModifier = 2.4f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        int count = Mathf.RoundToInt(Random.Range(18, 30) * scale);
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.08f * scale;

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(new Color(0.92f, 0.05f, 0.05f), 0f), 
                new GradientColorKey(new Color(0.55f, 0.02f, 0.02f), 0.5f),
                new GradientColorKey(new Color(0.28f, 0.01f, 0.01f), 1f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f), 
                new GradientAlphaKey(0.9f, 0.5f), 
                new GradientAlphaKey(0f, 1f) 
            }
        );
        colorOverLife.color = grad;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(0.65f, 0.7f);
        sizeCurve.AddKey(1f, 0.15f);
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystemRenderer psr = fxObj.GetComponent<ParticleSystemRenderer>();
        Shader pShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
        psr.material = new Material(pShader);

        // 2. หมอกเลือดฟุ้งกระจาย (Blood Mist Puff)
        GameObject mistObj = new GameObject("BloodMist");
        mistObj.transform.SetParent(fxObj.transform, false);
        ParticleSystem mistPs = mistObj.AddComponent<ParticleSystem>();
        mistPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var mMain = mistPs.main;
        mMain.duration = 0.25f;
        mMain.loop = false;
        mMain.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
        mMain.startSpeed = new ParticleSystem.MinMaxCurve(0.5f * scale, 1.8f * scale);
        mMain.startSize = new ParticleSystem.MinMaxCurve(0.25f * scale, 0.65f * scale);
        mMain.startColor = new Color(0.75f, 0.04f, 0.04f, 0.65f);
        mMain.gravityModifier = 0.35f;
        mMain.simulationSpace = ParticleSystemSimulationSpace.World;
        mMain.playOnAwake = true;

        var mEmission = mistPs.emission;
        mEmission.rateOverTime = 0;
        mEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, Mathf.RoundToInt(8 * scale)) });

        var mShape = mistPs.shape;
        mShape.shapeType = ParticleSystemShapeType.Sphere;
        mShape.radius = 0.12f * scale;

        var mColorOverLife = mistPs.colorOverLifetime;
        mColorOverLife.enabled = true;
        Gradient mGrad = new Gradient();
        mGrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.85f, 0.04f, 0.04f), 0f), new GradientColorKey(new Color(0.38f, 0.01f, 0.01f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0.4f, 0.4f), new GradientAlphaKey(0f, 1f) }
        );
        mColorOverLife.color = mGrad;

        var mSizeOverLife = mistPs.sizeOverLifetime;
        mSizeOverLife.enabled = true;
        AnimationCurve mCurve = new AnimationCurve();
        mCurve.AddKey(0f, 0.4f);
        mCurve.AddKey(1f, 1.25f);
        mSizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, mCurve);

        ParticleSystemRenderer mPsr = mistObj.GetComponent<ParticleSystemRenderer>();
        mPsr.material = new Material(pShader);

        // 3. รอยเลือดตกกระทบพื้น (Ground Blood Puddle/Decal)
        if (Physics.Raycast(pos, Vector3.down, out RaycastHit gHit, 3.5f))
        {
            SpawnGroundBlood(gHit.point, gHit.normal, scale);
        }

        Destroy(fxObj, 1.2f);
    }

    private static void SpawnGroundBlood(Vector3 point, Vector3 normal, float scale)
    {
        if (Time.time - lastGroundBloodTime < 0.12f) return;
        lastGroundBloodTime = Time.time;

        GameObject splat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        splat.name = "GroundBloodDecal";
        float radius = Random.Range(0.25f, 0.5f) * scale;
        splat.transform.localScale = new Vector3(radius, 0.0015f, radius);
        splat.transform.position = point + normal * 0.012f;
        splat.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        Collider col = splat.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(s);
        Color bloodCol = new Color(Random.Range(0.32f, 0.5f), 0.015f, 0.015f, 0.92f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", bloodCol);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", bloodCol);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.85f); // Wet look

        Renderer rend = splat.GetComponent<Renderer>();
        if (rend != null) rend.sharedMaterial = mat;

        Destroy(splat, 6.0f);
    }

    IEnumerator DeathAnimationRoutine()
    {
        // Flash bright white on death
        if (renderers != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material != null)
                {
                    SetMaterialColor(renderers[i].material, Color.white);
                }
            }
        }

        Vector3 origScale = transform.localScale;
        Quaternion origRot = transform.rotation;
        Quaternion tumbleRot = origRot * Quaternion.Euler(75f, Random.Range(-20f, 20f), 0);
        Vector3 startPos = transform.position;

        float elapsed = 0f;
        float duration = 0.55f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Dramatic backwards collapse
            transform.rotation = Quaternion.Slerp(origRot, tumbleRot, t * 1.5f);

            // Shrink and sink into ground after collapsing
            if (t > 0.35f)
            {
                float shrinkT = (t - 0.35f) / 0.65f;
                transform.localScale = Vector3.Lerp(origScale, Vector3.zero, shrinkT);
                transform.position = startPos + Vector3.down * (shrinkT * 0.45f);
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}