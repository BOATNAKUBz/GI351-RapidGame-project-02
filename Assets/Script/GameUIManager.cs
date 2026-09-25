using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("UI References (Assigned in Inspector or Auto-Found)")]
    public Canvas hudCanvas;
    public Slider hpSlider;
    public Text hpText;
    public Text ammoText;
    public Text waveText;
    public Text enemyCountText;
    public Text notifText;
    public Image hitMarker;
    public Image damageVignette;
    public Text interactPromptText;
    public Text weaponSlotsText;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;

    [Header("Dash UI References")]
    public Image dashCooldownFill;
    public Text dashCooldownText;

    [Header("Heartbeat UI References")]
    public RectTransform hpHeartRect;
    public Image hpHeartImage;
    private Sprite heartSprite;
    private float currentHealthRatio = 1f;

    private Font defaultFont;
    private PlayerController playerController;

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

        defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Try linking existing UI from Scene Hierarchy first
        if (!TryBindExistingUI())
        {
            BuildHUD();
        }

        SetupButtonListeners();
    }

    bool TryBindExistingUI()
    {
        if (hudCanvas == null)
        {
            GameObject canvasObj = GameObject.Find("GameHUD_Canvas");
            if (canvasObj != null) hudCanvas = canvasObj.GetComponent<Canvas>();
        }

        if (hudCanvas != null)
        {
            Transform t = hudCanvas.transform;
            if (damageVignette == null)
            {
                var vig = t.Find("DamageVignette");
                if (vig != null) damageVignette = vig.GetComponent<Image>();
            }
            if (hitMarker == null)
            {
                var hm = t.Find("CrosshairRoot/HitMarker");
                if (hm != null) hitMarker = hm.GetComponent<Image>();
            }
            if (hpSlider == null)
            {
                var slider = t.Find("HealthPanel/HP_Slider");
                if (slider != null) hpSlider = slider.GetComponent<Slider>();
            }
            if (hpText == null)
            {
                var hpT = t.Find("HealthPanel/HP_Text");
                if (hpT != null) hpText = hpT.GetComponent<Text>();
            }
            if (hpHeartRect == null)
            {
                var iconT = t.Find("HealthPanel/HP_HeartIcon") ?? t.Find("HealthPanel/HP_Icon");
                if (iconT != null) hpHeartRect = iconT.GetComponent<RectTransform>();
            }
            if (ammoText == null)
            {
                var at = t.Find("AmmoPanel");
                if (at != null) ammoText = at.GetComponent<Text>();
            }
            if (waveText == null)
            {
                var wt = t.Find("WavePanel");
                if (wt != null) waveText = wt.GetComponent<Text>();
            }
            if (enemyCountText == null)
            {
                var et = t.Find("WavePanel/EnemySubText");
                if (et != null) enemyCountText = et.GetComponent<Text>();
            }
            if (notifText == null)
            {
                var nt = t.Find("NotifBanner");
                if (nt != null) notifText = nt.GetComponent<Text>();
            }
            if (gameOverPanel == null)
            {
                var gop = t.Find("GameOverPanel");
                if (gop != null) gameOverPanel = gop.gameObject;
            }
            if (gameOverPanel != null)
            {
                var img = gameOverPanel.GetComponent<Image>();
                if (img != null) img.color = new Color(0f, 0f, 0f, 1f);
            }
            if (victoryPanel == null)
            {
                var vp = t.Find("VictoryPanel");
                if (vp != null) victoryPanel = vp.gameObject;
            }
            if (interactPromptText == null)
            {
                var ip = t.Find("InteractPrompt");
                if (ip != null) interactPromptText = ip.GetComponent<Text>();
            }
            if (weaponSlotsText == null)
            {
                var ws = t.Find("WeaponSlotsPanel");
                if (ws != null) weaponSlotsText = ws.GetComponent<Text>();
            }
            if (dashCooldownFill == null)
            {
                var dcFill = t.Find("DashPanel/Fill");
                if (dcFill != null) dashCooldownFill = dcFill.GetComponent<Image>();
            }
            if (dashCooldownText == null)
            {
                var dcText = t.Find("DashPanel/DashText");
                if (dcText != null) dashCooldownText = dcText.GetComponent<Text>();
            }

            return true;
        }

        return false;
    }

    void SetupButtonListeners()
    {
        if (gameOverPanel != null)
        {
            var restartBtn = gameOverPanel.transform.Find("RestartButton")?.GetComponent<Button>();
            if (restartBtn != null)
            {
                restartBtn.onClick.RemoveAllListeners();
                restartBtn.onClick.AddListener(TriggerRestart);
            }

            var menuBtn = gameOverPanel.transform.Find("MainMenuButton")?.GetComponent<Button>();
            if (menuBtn != null)
            {
                menuBtn.onClick.RemoveAllListeners();
                menuBtn.onClick.AddListener(GoToMainMenu);
            }
        }

        if (victoryPanel != null)
        {
            var playAgainBtn = victoryPanel.transform.Find("PlayAgainButton")?.GetComponent<Button>();
            if (playAgainBtn != null)
            {
                playAgainBtn.onClick.RemoveAllListeners();
                playAgainBtn.onClick.AddListener(TriggerRestart);
            }

            var menuBtn = victoryPanel.transform.Find("MainMenuButton")?.GetComponent<Button>();
            if (menuBtn != null)
            {
                menuBtn.onClick.RemoveAllListeners();
                menuBtn.onClick.AddListener(GoToMainMenu);
            }
        }
    }

    void Start()
    {
        SetupHpSlider();
        SetupHeartUI();

        // Bind to Player Controller for Dash UI
        playerController = FindAnyObjectByType<PlayerController>();

        // Bind to Player
        var playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealth;
            playerHealth.OnDamaged += (amt) => FlashDamageVignette();
            playerHealth.OnDied += ShowGameOverScreen;

            UpdateHealth(playerHealth.currentHealth, playerHealth.maxHealth);
        }

        // Bind to Player Inventory (New 3-slot weapon system)
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnSlotsChanged += UpdateWeaponSlots;
            PlayerInventory.Instance.OnWeaponSwitched += (idx, weapon) =>
            {
                if (weapon != null)
                {
                    weapon.OnAmmoChanged -= UpdateAmmo;
                    weapon.OnAmmoChanged += UpdateAmmo;
                    UpdateAmmo(weapon.currentAmmo, weapon.reserveAmmo);
                }
            };
            UpdateWeaponSlots(PlayerInventory.Instance.slots, PlayerInventory.Instance.activeSlotIndex);
        }

        // Legacy gun fallback if present
        var gun = FindAnyObjectByType<FPSGun>();
        if (gun != null)
        {
            gun.OnAmmoChanged += UpdateAmmo;
            UpdateAmmo(gun.currentAmmo, gun.reserveAmmo);
        }
    }

    private void SetupHpSlider()
    {
        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = 1f;
            hpSlider.direction = Slider.Direction.LeftToRight;
            hpSlider.value = 1f;
        }
    }

    void Update()
    {
        // Quick restart with R when game over or victory
        bool isGameOverOrVictory = (gameOverPanel != null && gameOverPanel.activeInHierarchy) ||
                                   (victoryPanel != null && victoryPanel.activeInHierarchy);

        if (!isGameOverOrVictory)
        {
            var ph = FindAnyObjectByType<PlayerHealth>();
            if (ph != null && ph.isDead) isGameOverOrVictory = true;
        }

        if (!isGameOverOrVictory)
        {
            UpdateDashUI();
            UpdateHeartbeatAnimation();
        }
        else
        {
            bool rPressed = InputBridge.GetReloadDown();
#if ENABLE_INPUT_SYSTEM
            if (!rPressed && UnityEngine.InputSystem.Keyboard.current != null)
            {
                rPressed = UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame;
            }
#endif
            if (!rPressed)
            {
                try { rPressed = Input.GetKeyDown(KeyCode.R); } catch { }
            }

            if (rPressed)
            {
                TriggerRestart();
            }
        }
    }

    private void UpdateDashUI()
    {
        if (playerController == null)
        {
            playerController = FindAnyObjectByType<PlayerController>();
            if (playerController == null) return;
        }

        float remaining = playerController.dashCooldownRemaining;
        float totalCooldown = playerController.dashCooldown;

        if (remaining > 0f)
        {
            float fillRatio = 1f - (remaining / totalCooldown);
            if (dashCooldownFill != null)
            {
                dashCooldownFill.fillAmount = fillRatio;
                dashCooldownFill.color = new Color(0.3f, 0.7f, 1f, 0.5f);
            }

            if (dashCooldownText != null)
            {
                dashCooldownText.text = $"DASH: {remaining:F1}s";
                dashCooldownText.color = new Color(0.8f, 0.8f, 0.8f);
            }
        }
        else
        {
            if (dashCooldownFill != null)
            {
                dashCooldownFill.fillAmount = 1f;
                dashCooldownFill.color = new Color(0.2f, 0.8f, 1f, 1f);
            }

            if (dashCooldownText != null)
            {
                dashCooldownText.text = "DASH: READY";
                dashCooldownText.color = new Color(0.3f, 1f, 0.5f);
            }
        }
    }

    public void TriggerRestart()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.RestartGame();
        }
        else
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                SceneManager.LoadScene(0);
            }
        }
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    void BuildHUD()
    {
        // Canvas
        GameObject canvasObj = new GameObject("GameHUD_Canvas");
        canvasObj.transform.SetParent(transform, false);
        hudCanvas = canvasObj.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Damage Vignette (Full screen flash)
        GameObject vigObj = new GameObject("DamageVignette");
        vigObj.transform.SetParent(canvasObj.transform, false);
        damageVignette = vigObj.AddComponent<Image>();
        damageVignette.color = new Color(1f, 0, 0, 0);
        RectTransform vigRt = vigObj.GetComponent<RectTransform>();
        vigRt.anchorMin = Vector2.zero;
        vigRt.anchorMax = Vector2.one;
        vigRt.sizeDelta = Vector2.zero;

        // Crosshair Root (Screen Center)
        GameObject crosshairRoot = new GameObject("CrosshairRoot");
        crosshairRoot.transform.SetParent(canvasObj.transform, false);
        RectTransform cRt = crosshairRoot.AddComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0.5f, 0.5f);
        cRt.anchorMax = new Vector2(0.5f, 0.5f);
        cRt.sizeDelta = new Vector2(40, 40);

        CreateCrosshairLines(crosshairRoot.transform);

        // Hit Marker (Red X)
        GameObject hmObj = new GameObject("HitMarker");
        hmObj.transform.SetParent(crosshairRoot.transform, false);
        hitMarker = hmObj.AddComponent<Image>();
        hitMarker.color = new Color(1f, 0.2f, 0.2f, 0);
        RectTransform hmRt = hmObj.GetComponent<RectTransform>();
        hmRt.sizeDelta = new Vector2(24, 24);
        CreateHitMarkerLines(hmObj.transform);

        // Bottom Left: Health Bar Panel
        GameObject hpPanel = new GameObject("HealthPanel");
        hpPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform hpRt = hpPanel.AddComponent<RectTransform>();
        hpRt.anchorMin = new Vector2(0, 0);
        hpRt.anchorMax = new Vector2(0, 0);
        hpRt.pivot = new Vector2(0, 0);
        hpRt.anchoredPosition = new Vector2(40, 40);
        hpRt.sizeDelta = new Vector2(300, 50);

        // HP Background
        GameObject hpBg = new GameObject("HP_Bg");
        hpBg.transform.SetParent(hpPanel.transform, false);
        Image bgImg = hpBg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        RectTransform bgRt = hpBg.GetComponent<RectTransform>();
        bgRt.sizeDelta = new Vector2(300, 30);
        bgRt.anchoredPosition = new Vector2(150, 25);

        // HP Slider
        GameObject hpSliderObj = new GameObject("HP_Slider");
        hpSliderObj.transform.SetParent(hpPanel.transform, false);
        hpSlider = hpSliderObj.AddComponent<Slider>();
        hpSlider.minValue = 0f;
        hpSlider.maxValue = 1f;
        hpSlider.value = 1f;
        RectTransform sliderRt = hpSliderObj.GetComponent<RectTransform>();
        sliderRt.sizeDelta = new Vector2(300, 30);
        sliderRt.pivot = new Vector2(0.5f, 0.5f);
        sliderRt.anchoredPosition = new Vector2(150, 25);

        // Fill Area
        GameObject fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(hpSliderObj.transform, false);
        RectTransform fillAreaRt = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0, 0);
        fillAreaRt.anchorMax = new Vector2(1, 1);
        fillAreaRt.sizeDelta = Vector2.zero;
        fillAreaRt.anchoredPosition = Vector2.zero;

        // Fill (หลอดเลือดเขียว)
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.85f, 0.3f);
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0, 0);
        fillRt.anchorMax = new Vector2(1, 1);
        fillRt.sizeDelta = Vector2.zero;
        fillRt.anchoredPosition = Vector2.zero;

        hpSlider.fillRect = fillRt;

        // HP Text
        GameObject hpTxtObj = new GameObject("HP_Text");
        hpTxtObj.transform.SetParent(hpPanel.transform, false);
        hpText = hpTxtObj.AddComponent<Text>();
        hpText.font = defaultFont;
        hpText.text = "HP: 100 / 100";
        hpText.fontSize = 20;
        hpText.fontStyle = FontStyle.Bold;
        hpText.alignment = TextAnchor.MiddleCenter;
        hpText.color = Color.white;
        RectTransform txtRt = hpTxtObj.GetComponent<RectTransform>();
        txtRt.sizeDelta = new Vector2(200, 30);
        txtRt.anchoredPosition = new Vector2(150, 25);

        // Bottom Right: Ammo Panel
        GameObject ammoPanel = new GameObject("AmmoPanel");
        ammoPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform ammoRt = ammoPanel.AddComponent<RectTransform>();
        ammoRt.anchorMin = new Vector2(1, 0);
        ammoRt.anchorMax = new Vector2(1, 0);
        ammoRt.pivot = new Vector2(1, 0);
        ammoRt.anchoredPosition = new Vector2(-40, 40);
        ammoRt.sizeDelta = new Vector2(240, 60);

        ammoText = ammoPanel.AddComponent<Text>();
        ammoText.font = defaultFont;
        ammoText.text = "AMMO: 30 / 120";
        ammoText.fontSize = 32;
        ammoText.fontStyle = FontStyle.Bold;
        ammoText.alignment = TextAnchor.MiddleRight;
        ammoText.color = new Color(1f, 0.85f, 0.3f);

        // Bottom Right: Dash Panel (วางเหนือ Ammo Panel)
        CreateDashUI(canvasObj.transform);

        // Top Left: Wave Info Panel
        GameObject wavePanel = new GameObject("WavePanel");
        wavePanel.transform.SetParent(canvasObj.transform, false);
        RectTransform wRt = wavePanel.AddComponent<RectTransform>();
        wRt.anchorMin = new Vector2(0, 1);
        wRt.anchorMax = new Vector2(0, 1);
        wRt.pivot = new Vector2(0, 1);
        wRt.anchoredPosition = new Vector2(40, -40);
        wRt.sizeDelta = new Vector2(350, 90);

        waveText = wavePanel.AddComponent<Text>();
        waveText.font = defaultFont;
        waveText.text = "WAVE 1 / 5";
        waveText.fontSize = 34;
        waveText.fontStyle = FontStyle.Bold;
        waveText.color = new Color(1f, 0.95f, 0.9f);

        GameObject enemySubTextObj = new GameObject("EnemySubText");
        enemySubTextObj.transform.SetParent(wavePanel.transform, false);
        enemyCountText = enemySubTextObj.AddComponent<Text>();
        enemyCountText.font = defaultFont;
        enemyCountText.text = "Enemies Left: 5";
        enemyCountText.fontSize = 22;
        enemyCountText.color = new Color(1f, 0.4f, 0.4f);
        RectTransform ecRt = enemySubTextObj.GetComponent<RectTransform>();
        ecRt.anchoredPosition = new Vector2(0, -40);
        ecRt.sizeDelta = new Vector2(300, 30);

        // Top Center: Notifications Banner
        GameObject notifObj = new GameObject("NotifBanner");
        notifObj.transform.SetParent(canvasObj.transform, false);
        RectTransform nRt = notifObj.AddComponent<RectTransform>();
        nRt.anchorMin = new Vector2(0.5f, 1);
        nRt.anchorMax = new Vector2(0.5f, 1);
        nRt.pivot = new Vector2(0.5f, 1);
        nRt.anchoredPosition = new Vector2(0, -60);
        nRt.sizeDelta = new Vector2(800, 60);

        notifText = notifObj.AddComponent<Text>();
        notifText.font = defaultFont;
        notifText.text = "";
        notifText.fontSize = 28;
        notifText.fontStyle = FontStyle.Bold;
        notifText.alignment = TextAnchor.MiddleCenter;
        notifText.color = new Color(1f, 0.95f, 0.3f);

        // Build End Game Panels
        BuildGameOverPanel(canvasObj.transform);
        BuildVictoryPanel(canvasObj.transform);
    }

    private void CreateDashUI(Transform canvasParent)
    {
        GameObject dashPanel = new GameObject("DashPanel");
        dashPanel.transform.SetParent(canvasParent, false);

        RectTransform dpRt = dashPanel.AddComponent<RectTransform>();
        dpRt.anchorMin = new Vector2(1, 0);
        dpRt.anchorMax = new Vector2(1, 0);
        dpRt.pivot = new Vector2(1, 0);
        dpRt.anchoredPosition = new Vector2(-40, 110);
        dpRt.sizeDelta = new Vector2(200, 24);

        // Background
        GameObject bg = new GameObject("Bg");
        bg.transform.SetParent(dashPanel.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.6f);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;

        // Fill Bar
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(dashPanel.transform, false);
        dashCooldownFill = fill.AddComponent<Image>();
        dashCooldownFill.type = Image.Type.Filled;
        dashCooldownFill.fillMethod = Image.FillMethod.Horizontal;
        dashCooldownFill.fillOrigin = 0;
        dashCooldownFill.color = new Color(0.2f, 0.8f, 1f, 1f);

        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;

        // Dash Text
        GameObject txtObj = new GameObject("DashText");
        txtObj.transform.SetParent(dashPanel.transform, false);
        dashCooldownText = txtObj.AddComponent<Text>();
        dashCooldownText.font = defaultFont;
        dashCooldownText.text = "DASH: READY";
        dashCooldownText.fontSize = 16;
        dashCooldownText.fontStyle = FontStyle.Bold;
        dashCooldownText.alignment = TextAnchor.MiddleCenter;
        dashCooldownText.color = new Color(0.3f, 1f, 0.5f);

        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;
    }

    void CreateCrosshairLines(Transform parent)
    {
        float length = 12f;
        float thickness = 2.5f;
        float offset = 7f;
        Color cColor = new Color(1f, 1f, 1f, 0.85f);

        CreateUiBar(parent, new Vector2(0, offset + length / 2), new Vector2(thickness, length), cColor);
        CreateUiBar(parent, new Vector2(0, -(offset + length / 2)), new Vector2(thickness, length), cColor);
        CreateUiBar(parent, new Vector2(-(offset + length / 2), 0), new Vector2(length, thickness), cColor);
        CreateUiBar(parent, new Vector2(offset + length / 2, 0), new Vector2(length, thickness), cColor);
        CreateUiBar(parent, Vector2.zero, new Vector2(3, 3), new Color(1f, 0.3f, 0.3f, 0.9f));
    }

    void CreateHitMarkerLines(Transform parent)
    {
        float size = 16f;
        float thick = 2.5f;
        Color hColor = new Color(1f, 0.15f, 0.15f, 0.9f);

        GameObject line1 = CreateUiBar(parent, Vector2.zero, new Vector2(size, thick), hColor);
        line1.transform.localRotation = Quaternion.Euler(0, 0, 45f);

        GameObject line2 = CreateUiBar(parent, Vector2.zero, new Vector2(size, thick), hColor);
        line2.transform.localRotation = Quaternion.Euler(0, 0, -45f);
    }

    GameObject CreateUiBar(Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        GameObject obj = new GameObject("CrossLine");
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = color;
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return obj;
    }

    void BuildGameOverPanel(Transform parent)
    {
        gameOverPanel = new GameObject("GameOverPanel");
        gameOverPanel.transform.SetParent(parent, false);
        RectTransform rt = gameOverPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image bg = gameOverPanel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 1f);

        GameObject title = new GameObject("Title");
        title.transform.SetParent(gameOverPanel.transform, false);
        Text t = title.AddComponent<Text>();
        t.font = defaultFont;
        t.text = "GAME OVER";
        t.fontSize = 72;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(1f, 0.2f, 0.2f);
        RectTransform trt = title.GetComponent<RectTransform>();
        trt.anchoredPosition = new Vector2(0, 90);
        trt.sizeDelta = new Vector2(600, 100);

        // Restart Button
        CreateButton(gameOverPanel.transform, "RestartButton", "RESTART (R)", new Vector2(0, 0), new Vector2(240, 50), () => {
            if (WaveManager.Instance != null) WaveManager.Instance.RestartGame();
        });

        // Main Menu Button
        CreateButton(gameOverPanel.transform, "MainMenuButton", "MAIN MENU", new Vector2(0, -70), new Vector2(240, 50), GoToMainMenu);

        gameOverPanel.SetActive(false);
    }

    void BuildVictoryPanel(Transform parent)
    {
        victoryPanel = new GameObject("VictoryPanel");
        victoryPanel.transform.SetParent(parent, false);
        RectTransform rt = victoryPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image bg = victoryPanel.AddComponent<Image>();
        bg.color = new Color(0, 0.1f, 0.2f, 0.88f);

        GameObject title = new GameObject("Title");
        title.transform.SetParent(victoryPanel.transform, false);
        Text t = title.AddComponent<Text>();
        t.font = defaultFont;
        t.text = "VICTORY!";
        t.fontSize = 72;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(1f, 0.85f, 0.2f);
        RectTransform trt = title.GetComponent<RectTransform>();
        trt.anchoredPosition = new Vector2(0, 90);
        trt.sizeDelta = new Vector2(600, 100);

        // Play Again Button
        CreateButton(victoryPanel.transform, "PlayAgainButton", "PLAY AGAIN (R)", new Vector2(0, 0), new Vector2(240, 50), () => {
            if (WaveManager.Instance != null) WaveManager.Instance.RestartGame();
        });

        // Main Menu Button
        CreateButton(victoryPanel.transform, "MainMenuButton", "MAIN MENU", new Vector2(0, -70), new Vector2(240, 50), GoToMainMenu);

        victoryPanel.SetActive(false);
    }

    GameObject CreateButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.25f, 0.35f);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.3f, 0.45f, 0.7f);
        cb.pressedColor = new Color(0.15f, 0.2f, 0.3f);
        btn.colors = cb;
        if (onClick != null) btn.onClick.AddListener(onClick);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        Text t = txtObj.AddComponent<Text>();
        t.font = defaultFont;
        t.text = text;
        t.fontSize = 22;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;

        RectTransform trt = txtObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero;

        return btnObj;
    }

    public void UpdateHealth(float current, float max)
    {
        float clampedCurrent = Mathf.Clamp(current, 0f, max);
        float ratio = max > 0f ? Mathf.Clamp01(clampedCurrent / max) : 0f;
        currentHealthRatio = ratio;

        if (hpSlider != null)
        {
            hpSlider.value = ratio;
            if (hpSlider.fillRect != null)
            {
                hpSlider.fillRect.anchorMin = new Vector2(0, 0);
                hpSlider.fillRect.anchorMax = new Vector2(ratio, 1);
                hpSlider.fillRect.pivot = new Vector2(0, 0.5f);
                hpSlider.fillRect.sizeDelta = Vector2.zero;
                hpSlider.fillRect.anchoredPosition = Vector2.zero;
                hpSlider.fillRect.gameObject.SetActive(ratio > 0.0001f);

                Image fillImage = hpSlider.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    if (ratio > 0.5f)
                    {
                        fillImage.color = Color.Lerp(new Color(1f, 0.82f, 0.15f), new Color(0.2f, 0.88f, 0.35f), (ratio - 0.5f) * 2f);
                    }
                    else
                    {
                        fillImage.color = Color.Lerp(new Color(0.92f, 0.18f, 0.18f), new Color(1f, 0.82f, 0.15f), ratio * 2f);
                    }
                }
            }
        }

        if (hpText != null)
        {
            hpText.text = $"HP: {Mathf.CeilToInt(clampedCurrent)} / {Mathf.CeilToInt(max)}";
        }
    }

    public void UpdateAmmo(int current, int reserve)
    {
        FPSWeapon active = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetActiveWeapon() : null;
        if (ammoText != null)
        {
            ammoText.verticalOverflow = VerticalWrapMode.Overflow;
            ammoText.horizontalOverflow = HorizontalWrapMode.Overflow;

            if (active != null && (active.isMelee || active.weaponType == WeaponType.Axe))
            {
                ammoText.text = "MELEE";
            }
            else
            {
                ammoText.text = $"{current} / {reserve}";
            }
        }
    }

    public void ShowInteractPrompt(string message)
    {
        if (gameOverPanel != null && gameOverPanel.activeInHierarchy) return;
        if (interactPromptText == null) CreateInteractPromptUI();
        if (interactPromptText != null)
        {
            interactPromptText.text = message;
            interactPromptText.gameObject.SetActive(true);
        }
    }

    public void HideInteractPrompt()
    {
        if (interactPromptText != null)
        {
            interactPromptText.text = "";
            interactPromptText.gameObject.SetActive(false);
        }
    }

    public void ShowPickupBanner(string message)
    {
        ShowNotification(message);
    }

    public void UpdateWeaponSlots(FPSWeapon[] slots, int activeIndex)
    {
        if (weaponSlotsText == null) CreateWeaponSlotsUI();
        if (weaponSlotsText != null && slots != null)
        {
            string text = "";
            for (int i = 0; i < slots.Length; i++)
            {
                int slotNum = i + 1;
                string wName = (slots[i] != null && slots[i].isUnlocked) ? slots[i].weaponName : "Empty";
                if (i == activeIndex)
                {
                    text += $"<b><color=#FFD700>[{slotNum}: {wName}]</color></b>   ";
                }
                else if (slots[i] != null && slots[i].isUnlocked)
                {
                    text += $"<color=#E0E0E0>[{slotNum}: {wName}]</color>   ";
                }
                else
                {
                    text += $"<color=#666666>[{slotNum}: ---]</color>   ";
                }
            }
            weaponSlotsText.text = text.TrimEnd();
        }

        var active = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetActiveWeapon() : null;
        if (active != null)
        {
            UpdateAmmo(active.currentAmmo, active.reserveAmmo);
        }
    }

    private void CreateInteractPromptUI()
    {
        if (hudCanvas == null) return;
        GameObject ipObj = new GameObject("InteractPrompt");
        ipObj.transform.SetParent(hudCanvas.transform, false);
        RectTransform rt = ipObj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, -90);
        rt.sizeDelta = new Vector2(500, 50);

        interactPromptText = ipObj.AddComponent<Text>();
        interactPromptText.font = defaultFont != null ? defaultFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        interactPromptText.fontSize = 26;
        interactPromptText.fontStyle = FontStyle.Bold;
        interactPromptText.alignment = TextAnchor.MiddleCenter;
        interactPromptText.color = new Color(1f, 0.9f, 0.2f);

        Outline outline = ipObj.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.8f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        ipObj.SetActive(false);
    }

    private void CreateWeaponSlotsUI()
    {
        if (hudCanvas == null) return;
        GameObject wsObj = new GameObject("WeaponSlotsPanel");
        wsObj.transform.SetParent(hudCanvas.transform, false);
        RectTransform rt = wsObj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 0);
        rt.anchoredPosition = new Vector2(-40, 150); // เขยิบขึ้นเพื่อไม่ให้ทับ Dash UI
        rt.sizeDelta = new Vector2(400, 40);

        weaponSlotsText = wsObj.AddComponent<Text>();
        weaponSlotsText.font = defaultFont != null ? defaultFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        weaponSlotsText.fontSize = 20;
        weaponSlotsText.alignment = TextAnchor.MiddleRight;
        weaponSlotsText.color = Color.white;

        Outline outline = wsObj.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.8f);
        outline.effectDistance = new Vector2(1f, -1f);
    }

    public void UpdateWaveInfo(int currentWave, int totalWaves, int enemiesRemaining)
    {
        if (waveText != null) waveText.text = $"WAVE {currentWave} / {totalWaves}";
        if (enemyCountText != null) enemyCountText.text = $"Enemies Left: {enemiesRemaining}";
    }

    public void ShowNotification(string message)
    {
        if (gameOverPanel != null && gameOverPanel.activeInHierarchy) return;
        if (notifText != null)
        {
            notifText.text = message;
            StopCoroutine("NotificationRoutine");
            StartCoroutine(NotificationRoutine());
        }
    }

    IEnumerator NotificationRoutine()
    {
        yield return new WaitForSeconds(3.0f);
        if (notifText != null) notifText.text = "";
    }

    public void ShowWaveCountdown(int seconds)
    {
        ShowNotification($"Next Wave in {seconds}...");
    }

    public void ShowHitMarker()
    {
        if (gameOverPanel != null && gameOverPanel.activeInHierarchy) return;
        if (hitMarker != null)
        {
            StopCoroutine("HitMarkerRoutine");
            StartCoroutine(HitMarkerRoutine());
        }
    }

    IEnumerator HitMarkerRoutine()
    {
        if (hitMarker != null)
        {
            hitMarker.transform.localScale = Vector3.one * 1.3f;
            foreach (var img in hitMarker.GetComponentsInChildren<Image>())
            {
                img.color = new Color(1f, 0.2f, 0.2f, 1f);
            }

            yield return new WaitForSeconds(0.08f);

            foreach (var img in hitMarker.GetComponentsInChildren<Image>())
            {
                img.color = new Color(1f, 0.2f, 0.2f, 0f);
            }
        }
    }

    public void FlashDamageVignette()
    {
        if (gameOverPanel != null && gameOverPanel.activeInHierarchy) return;
        if (damageVignette != null)
        {
            StopCoroutine("DamageVignetteRoutine");
            StartCoroutine(DamageVignetteRoutine());
        }
    }

    IEnumerator DamageVignetteRoutine()
    {
        damageVignette.color = new Color(1f, 0, 0, 0.35f);
        float elapsed = 0f;
        float dur = 0.25f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(0.35f, 0f, elapsed / dur);
            damageVignette.color = new Color(1f, 0, 0, a);
            yield return null;
        }
        damageVignette.color = new Color(1f, 0, 0, 0f);
    }

    public void SetGameplayHUDVisible(bool visible)
    {
        if (hudCanvas == null) return;
        for (int i = 0; i < hudCanvas.transform.childCount; i++)
        {
            Transform child = hudCanvas.transform.GetChild(i);
            if (child != null && child.gameObject != gameOverPanel && child.gameObject != victoryPanel)
            {
                child.gameObject.SetActive(visible);
            }
        }
    }

    public void ShowGameOverScreen()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (damageVignette != null)
        {
            StopCoroutine("DamageVignetteRoutine");
            damageVignette.color = Color.clear;
        }

        // Hide all gameplay HUD panels (HealthPanel, AmmoPanel, WavePanel, WeaponSlots, Dash, etc.)
        SetGameplayHUDVisible(false);

        if (gameOverPanel != null)
        {
            // Ensure full-screen pure black background
            Image bg = gameOverPanel.GetComponent<Image>();
            if (bg == null) bg = gameOverPanel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 1f); // 100% solid black
            bg.raycastTarget = true;

            RectTransform rt = gameOverPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
            }

            gameOverPanel.transform.SetAsLastSibling();
            gameOverPanel.SetActive(true);

            // Ensure title, restart and menu buttons are active and visible
            for (int i = 0; i < gameOverPanel.transform.childCount; i++)
            {
                gameOverPanel.transform.GetChild(i).gameObject.SetActive(true);
            }
        }
    }

    public void ShowVictoryScreen()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (victoryPanel != null)
        {
            victoryPanel.transform.SetAsLastSibling();
            victoryPanel.SetActive(true);
        }
    }

    public void HideEndScreens()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);

        SetGameplayHUDVisible(true);
    }

    private void SetupHeartUI()
    {
        if (heartSprite == null)
        {
            heartSprite = CreateHeartSprite();
        }

        if (hudCanvas == null) return;

        Transform hpPanel = hudCanvas.transform.Find("HealthPanel");
        if (hpPanel == null) return;

        // Check if there is an existing text HP_Icon; hide or destroy it to avoid component conflict
        Transform oldIcon = hpPanel.Find("HP_Icon");
        if (oldIcon != null)
        {
            if (oldIcon.GetComponent<Text>() != null)
            {
                oldIcon.gameObject.SetActive(false);
                Destroy(oldIcon.gameObject);
            }
        }

        // Look for or create clean dedicated HP_HeartIcon
        Transform heartTr = hpPanel.Find("HP_HeartIcon");
        if (heartTr == null)
        {
            GameObject heartObj = new GameObject("HP_HeartIcon");
            heartObj.transform.SetParent(hpPanel, false);
            hpHeartRect = heartObj.AddComponent<RectTransform>();
            hpHeartImage = heartObj.AddComponent<Image>();
        }
        else
        {
            hpHeartRect = heartTr.GetComponent<RectTransform>();
            hpHeartImage = heartTr.GetComponent<Image>();
            if (hpHeartImage == null) hpHeartImage = heartTr.gameObject.AddComponent<Image>();
        }

        if (hpHeartRect != null)
        {
            hpHeartRect.anchorMin = new Vector2(0, 0.5f);
            hpHeartRect.anchorMax = new Vector2(0, 0.5f);
            hpHeartRect.pivot = new Vector2(0.5f, 0.5f);
            hpHeartRect.anchoredPosition = new Vector2(24f, 0f);
            hpHeartRect.sizeDelta = new Vector2(34f, 34f);
            hpHeartRect.gameObject.SetActive(true);

            Shadow shadow = hpHeartRect.GetComponent<Shadow>();
            if (shadow == null) shadow = hpHeartRect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
        }

        if (hpHeartImage != null)
        {
            hpHeartImage.sprite = heartSprite;
            hpHeartImage.color = new Color(1f, 0.18f, 0.32f, 1f);
            hpHeartImage.raycastTarget = false;
        }
    }

    private Sprite CreateHeartSprite()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color fillColor = new Color(1f, 0.18f, 0.32f, 1f);
        Color highlightColor = new Color(1f, 0.75f, 0.85f, 1f);
        Color shadowColor = new Color(0.72f, 0.05f, 0.18f, 1f);
        Color transparent = new Color(0, 0, 0, 0);

        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = ((float)x / (size - 1) * 2.7f) - 1.35f;
                float ny = ((float)y / (size - 1) * 2.7f) - 1.25f;

                float x2 = nx * nx;
                float y2 = ny * ny;
                float val = Mathf.Pow(x2 + y2 - 1f, 3f) - x2 * ny * y2;

                if (val <= 0f)
                {
                    float edgeDist = Mathf.Clamp01(-val * 10f);

                    float dx = nx + 0.4f;
                    float dy = ny - 0.45f;
                    float distFromHighlight = Mathf.Sqrt(dx * dx + dy * dy);
                    float highlight = Mathf.Clamp01(1f - distFromHighlight / 0.45f);

                    float bottomShadow = Mathf.Clamp01((0.4f - ny) * 0.7f);

                    Color c = Color.Lerp(fillColor, shadowColor, bottomShadow * 0.4f);
                    c = Color.Lerp(c, highlightColor, highlight * 0.55f);
                    c.a = edgeDist;
                    pixels[y * size + x] = c;
                }
                else
                {
                    float alpha = Mathf.Clamp01(1f - val * 14f);
                    if (alpha > 0.02f)
                    {
                        Color c = fillColor;
                        c.a = alpha;
                        pixels[y * size + x] = c;
                    }
                    else
                    {
                        pixels[y * size + x] = transparent;
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private void UpdateHeartbeatAnimation()
    {
        if (hpHeartRect == null) return;

        float beatRate = Mathf.Lerp(2.8f, 1.15f, Mathf.Clamp01(currentHealthRatio));
        float cycle = (Time.time * beatRate) % 1f;

        float scale = 1f;
        float brightness = 1f;

        if (cycle < 0.16f)
        {
            float t = cycle / 0.16f;
            float pulse = Mathf.Sin(t * Mathf.PI);
            scale = 1f + pulse * 0.28f;
            brightness = 1f + pulse * 0.35f;
        }
        else if (cycle < 0.34f)
        {
            float t = (cycle - 0.16f) / 0.18f;
            float pulse = Mathf.Sin(t * Mathf.PI);
            scale = 1f + pulse * 0.16f;
            brightness = 1f + pulse * 0.2f;
        }

        hpHeartRect.localScale = new Vector3(scale, scale, 1f);

        if (hpHeartImage != null)
        {
            Color baseCol;
            if (currentHealthRatio > 0.5f)
            {
                baseCol = new Color(1f, 0.2f, 0.32f);
            }
            else if (currentHealthRatio > 0.25f)
            {
                baseCol = new Color(1f, 0.42f, 0.18f);
            }
            else
            {
                float flash = (Mathf.Sin(Time.time * 12f) + 1f) * 0.5f;
                baseCol = Color.Lerp(new Color(0.85f, 0.08f, 0.08f), new Color(1f, 0.38f, 0.38f), flash);
            }

            hpHeartImage.color = new Color(
                Mathf.Clamp01(baseCol.r * brightness),
                Mathf.Clamp01(baseCol.g * brightness),
                Mathf.Clamp01(baseCol.b * brightness),
                1f
            );
        }
    }
}