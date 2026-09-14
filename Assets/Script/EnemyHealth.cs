using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isDead { get; private set; }

    private EnemyStats stats;
    private Renderer[] renderers;
    private Color[] originalColors;
    private Transform healthBarRoot;
    private Slider healthSlider;
    private Text healthText;
    private Camera mainCam;

    void Awake()
    {
        currentHealth = maxHealth;
        isDead = false;
        mainCam = Camera.main;
    }

    public void Initialize(EnemyStats enemyStats)
    {
        stats = enemyStats;
        maxHealth = stats.maxHealth;
        currentHealth = maxHealth;
        isDead = false;

        renderers = GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].material != null && renderers[i].material.HasProperty("_Color"))
                {
                    originalColors[i] = renderers[i].material.color;
                }
            }
        }

        CreateWorldHealthBar();
    }

    void CreateWorldHealthBar()
    {
        if (healthBarRoot != null) return;

        GameObject canvasObj = new GameObject("HealthBarCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvasObj.transform.localPosition = new Vector3(0, stats != null ? stats.modelScale * 2.2f + 0.3f : 2f, 0);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObj.AddComponent<CanvasScaler>();

        RectTransform rt = canvasObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1.6f, 0.35f);
        canvasObj.transform.localScale = Vector3.one * 0.015f;

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.75f);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.sizeDelta = new Vector2(100f, 16f);

        GameObject sliderObj = new GameObject("HealthSlider");
        sliderObj.transform.SetParent(canvasObj.transform, false);
        healthSlider = sliderObj.AddComponent<Slider>();
        healthSlider.minValue = 0f;
        healthSlider.maxValue = 1f;
        healthSlider.value = 1f;

        RectTransform sliderRt = sliderObj.GetComponent<RectTransform>();
        sliderRt.sizeDelta = new Vector2(96f, 12f);

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(sliderObj.transform, false);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.9f, 0.2f, 0.2f);
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.sizeDelta = new Vector2(96f, 12f);
        healthSlider.fillRect = fillRt;

        GameObject textObj = new GameObject("NameText");
        textObj.transform.SetParent(canvasObj.transform, false);
        textObj.transform.localPosition = new Vector3(0, 18f, 0);
        healthText = textObj.AddComponent<Text>();
        healthText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        healthText.text = stats != null ? stats.displayName : "Enemy";
        healthText.fontSize = 14;
        healthText.alignment = TextAnchor.MiddleCenter;
        healthText.color = Color.white;
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.sizeDelta = new Vector2(140f, 22f);

        healthBarRoot = canvasObj.transform;
    }

    void LateUpdate()
    {
        if (healthBarRoot != null && mainCam != null)
        {
            healthBarRoot.rotation = Quaternion.LookRotation(healthBarRoot.position - mainCam.transform.position);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        if (healthSlider != null)
        {
            healthSlider.value = currentHealth / maxHealth;
        }

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

    IEnumerator DamageFlashRoutine()
    {
        if (renderers == null) yield break;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null)
            {
                renderers[i].material.color = Color.white;
            }
        }

        yield return new WaitForSeconds(0.06f);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null && originalColors != null && i < originalColors.Length)
            {
                renderers[i].material.color = originalColors[i];
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

        // ดรอปไอเทมตามตั้งค่าใน EnemyAI ของแต่ละตัว
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