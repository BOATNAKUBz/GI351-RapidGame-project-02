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
    }

    void LateUpdate()
    {
        if (healthBarRoot != null)
        {
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam != null)
            {
                // Face the camera view plane directly
                healthBarRoot.rotation = mainCam.transform.rotation;
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        UpdateHealthBarVisuals();

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayEnemyHurt();
        }

        StartCoroutine(DamageFlashRoutine());

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
                enemyName = gameObject.name.Replace("Enemy_", "").Replace("(Clone)", "").Trim();
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

        if (healthBarRoot != null) healthBarRoot.gameObject.SetActive(false);

        StartCoroutine(DeathShrinkRoutine());
    }

    IEnumerator DeathShrinkRoutine()
    {
        Vector3 origScale = transform.localScale;
        float elapsed = 0f;
        float duration = 0.4f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(origScale, Vector3.zero, t);
            transform.position += Vector3.down * Time.deltaTime * 0.5f;
            yield return null;
        }

        Destroy(gameObject);
    }
}