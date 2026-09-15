using System;
using System.Collections;
using UnityEngine;

public enum WeaponType
{
    Shotgun = 0,
    Revolver = 1,
    Axe = 2
}

public class FPSWeapon : MonoBehaviour
{
    [Header("Weapon Identity")]
    public WeaponType weaponType = WeaponType.Revolver;
    public string weaponName = "Revolver";
    public int slotIndex = 1;
    public bool isUnlocked = false;

    [Header("Combat Stats")]
    public float damage = 55f;
    public float fireRate = 0.35f;
    public float range = 80f;
    public bool isMelee = false;

    [Header("Shotgun / Spread Settings")]
    public int pelletCount = 1;
    public float spreadAngle = 0f;

    [Header("Axe / Melee Settings")]
    public float meleeRadius = 0.65f;
    public TrailRenderer meleeTrail;

    [Header("Ammunition")]
    public int magazineSize = 6;
    public int currentAmmo = 6;
    public int reserveAmmo = 36;
    public float reloadTime = 1.6f;

    [Header("Visuals & Hands")]
    public Transform gunTransform;
    public Transform muzzlePoint;
    public Transform handsTransform;
    public Light muzzleFlashLight;

    [Header("Recoil Parameters")]
    public float recoilKickback = 0.08f;
    public Vector3 recoilRotation = new Vector3(-8f, 0f, 0f);
    public float recoilReturnSpeed = 12f;

    [Header("Weapon Sway & Bob")]
    public float swayAmount = 0.025f;
    public float maxSwayAmount = 0.06f;
    public float swaySmooth = 6f;
    public float bobFrequency = 8f;
    public float bobHorizontalAmount = 0.015f;
    public float bobVerticalAmount = 0.015f;

    // Events
    public event Action<int, int> OnAmmoChanged;

    public bool IsAttacking { get; private set; }
    public bool IsReloading { get; private set; }

    private float nextTimeToFire = 0f;
    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;
    private Camera playerCam;
    private float bobTimer = 0f;
    private CharacterController playerCC;

    private LineRenderer[] tracerPool;
    private int currentTracerIndex = 0;

    void Awake()
    {
        if (weaponType == WeaponType.Axe)
        {
            isMelee = true;
            range = Mathf.Min(range, 2.6f);
        }

        if (gunTransform == null) gunTransform = transform;
        initialLocalPos = gunTransform.localPosition;
        initialLocalRot = gunTransform.localRotation;

        playerCam = Camera.main ?? GetComponentInParent<Camera>();
        playerCC = GetComponentInParent<CharacterController>();

        SetupVisuals();
    }

    void OnValidate()
    {
        if (weaponType == WeaponType.Axe)
        {
            isMelee = true;
            currentAmmo = 0;
            magazineSize = 0;
            reserveAmmo = 0;
            range = Mathf.Min(range, 2.6f);
        }
    }

    void Start()
    {
        if (weaponType == WeaponType.Axe || isMelee)
        {
            isMelee = true;
            currentAmmo = 0;
            magazineSize = 0;
            reserveAmmo = 0;
            range = Mathf.Min(range, 2.6f);
        }
        else if (currentAmmo <= 0)
        {
            currentAmmo = magazineSize;
        }
        NotifyAmmo();
    }

    void OnEnable()
    {
        if (weaponType == WeaponType.Axe)
        {
            isMelee = true;
            currentAmmo = 0;
            magazineSize = 0;
            reserveAmmo = 0;
        }

        IsAttacking = false;
        IsReloading = false;
        if (gunTransform != null)
        {
            gunTransform.localPosition = initialLocalPos;
            gunTransform.localRotation = initialLocalRot;
        }
        NotifyAmmo();
    }

    void SetupVisuals()
    {
        if (weaponType == WeaponType.Axe || isMelee)
        {
            isMelee = true;
            // Setup Melee Trail for axe
            if (meleeTrail == null)
            {
                GameObject trailObj = new GameObject("SlashTrail");
                trailObj.transform.SetParent(gunTransform != null ? gunTransform : transform, false);
                trailObj.transform.localPosition = new Vector3(0, 0.35f, 0.2f);
                meleeTrail = trailObj.AddComponent<TrailRenderer>();
                meleeTrail.time = 0.2f;
                meleeTrail.startWidth = 0.35f;
                meleeTrail.endWidth = 0.02f;
                meleeTrail.material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit"));
                meleeTrail.startColor = new Color(1f, 1f, 1f, 0.75f);
                meleeTrail.endColor = new Color(0.8f, 0.9f, 1f, 0f);
                meleeTrail.emitting = false;
            }
            return; // Axe has NO muzzle flash, NO tracers, and CANNOT shoot!
        }
        // Setup Muzzle Flash Light
        if (muzzlePoint != null && muzzleFlashLight == null)
        {
            GameObject lObj = new GameObject("MuzzleFlashLight");
            lObj.transform.SetParent(muzzlePoint, false);
            muzzleFlashLight = lObj.AddComponent<Light>();
            muzzleFlashLight.type = LightType.Point;
            muzzleFlashLight.color = new Color(1f, 0.85f, 0.4f);
            muzzleFlashLight.range = 5f;
            muzzleFlashLight.intensity = 0f;
            muzzleFlashLight.enabled = false;
        }

        // Setup Tracer LineRenderers
        if (!isMelee)
        {
            int poolCount = (weaponType == WeaponType.Shotgun) ? 12 : 3;
            tracerPool = new LineRenderer[poolCount];
            for (int i = 0; i < poolCount; i++)
            {
                GameObject tObj = new GameObject($"BulletTracer_{i}");
                tObj.transform.SetParent(transform, false);
                LineRenderer lr = tObj.AddComponent<LineRenderer>();
                lr.startWidth = 0.03f;
                lr.endWidth = 0.01f;
                lr.material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit"));
                lr.startColor = new Color(1f, 0.95f, 0.5f, 0.9f);
                lr.endColor = new Color(1f, 0.45f, 0.1f, 0f);
                lr.enabled = false;
                tracerPool[i] = lr;
            }
        }

        // Setup Melee Trail if axe
        if (isMelee && meleeTrail == null)
        {
            GameObject trailObj = new GameObject("SlashTrail");
            trailObj.transform.SetParent(gunTransform, false);
            trailObj.transform.localPosition = new Vector3(0, 0.35f, 0.2f);
            meleeTrail = trailObj.AddComponent<TrailRenderer>();
            meleeTrail.time = 0.2f;
            meleeTrail.startWidth = 0.35f;
            meleeTrail.endWidth = 0.02f;
            meleeTrail.material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit"));
            meleeTrail.startColor = new Color(1f, 1f, 1f, 0.75f);
            meleeTrail.endColor = new Color(0.8f, 0.9f, 1f, 0f);
            meleeTrail.emitting = false;
        }
    }

    void Update()
    {
        HandleSwayAndBob();
    }

    void HandleSwayAndBob()
    {
        if (gunTransform == null || IsAttacking) return;

        // Mouse Sway
        Vector2 mouseDelta = InputBridge.GetMouseDelta();
        float moveX = Mathf.Clamp(-mouseDelta.x * swayAmount, -maxSwayAmount, maxSwayAmount);
        float moveY = Mathf.Clamp(-mouseDelta.y * swayAmount, -maxSwayAmount, maxSwayAmount);
        Vector3 targetSway = initialLocalPos + new Vector3(moveX, moveY, 0);

        // Movement Bobbing
        bool isMoving = playerCC != null && playerCC.velocity.magnitude > 0.1f;
        if (isMoving)
        {
            float speedMultiplier = InputBridge.GetSprint() ? 1.4f : 1f;
            bobTimer += Time.deltaTime * bobFrequency * speedMultiplier;
            float bobX = Mathf.Cos(bobTimer) * bobHorizontalAmount;
            float bobY = Mathf.Sin(bobTimer * 2f) * bobVerticalAmount;
            targetSway += new Vector3(bobX, bobY, 0);
        }
        else
        {
            bobTimer = 0f;
        }

        // Smooth return
        gunTransform.localPosition = Vector3.Lerp(gunTransform.localPosition, targetSway, Time.deltaTime * swaySmooth);
        gunTransform.localRotation = Quaternion.Slerp(gunTransform.localRotation, initialLocalRot, Time.deltaTime * recoilReturnSpeed);
    }

    public bool TryAttack()
    {
        if (IsAttacking || IsReloading) return false;
        if (Time.time < nextTimeToFire) return false;

        // BATTLE AXE: ตีฟันระยะใกล้เท่านั้น ยิงกระสุนไม่ได้เด็ดขาด
        if (weaponType == WeaponType.Axe || isMelee)
        {
            isMelee = true;
            StartCoroutine(AxeSwingRoutine());
            return true;
        }

        if (currentAmmo <= 0)
        {
            if (reserveAmmo > 0)
            {
                StartCoroutine(ReloadRoutine());
            }
            else
            {
                SoundManager.Instance?.PlayEmptyClick();
            }
            return false;
        }

        currentAmmo--;
        nextTimeToFire = Time.time + fireRate;
        NotifyAmmo();

        if (weaponType == WeaponType.Shotgun)
        {
            StartCoroutine(ShotgunShootRoutine());
        }
        else
        {
            StartCoroutine(RevolverShootRoutine());
        }

        return true;
    }

    public bool TryReload()
    {
        // ขวานไม่มีกระสุน ไม่สามารถรีโหลดได้
        if (weaponType == WeaponType.Axe || isMelee || IsReloading || IsAttacking) return false;
        if (currentAmmo >= magazineSize || reserveAmmo <= 0) return false;

        StartCoroutine(ReloadRoutine());
        return true;
    }

    #region Revolver Attack
    private IEnumerator RevolverShootRoutine()
    {
        IsAttacking = true;
        SoundManager.Instance?.PlayRevolverShoot();

        // Recoil Kick
        ApplyRecoil(recoilKickback, recoilRotation);

        // Flash & Tracer
        if (muzzleFlashLight != null) StartCoroutine(FlashLightRoutine());
        FireRaycast(0f);

        // Fast snappy recoil recovery
        float t = 0f;
        Vector3 kickPos = initialLocalPos - new Vector3(0, -0.015f, recoilKickback);
        Quaternion kickRot = initialLocalRot * Quaternion.Euler(recoilRotation);

        while (t < 0.12f)
        {
            t += Time.deltaTime;
            gunTransform.localPosition = Vector3.Lerp(kickPos, initialLocalPos, t / 0.12f);
            gunTransform.localRotation = Quaternion.Slerp(kickRot, initialLocalRot, t / 0.12f);
            yield return null;
        }

        gunTransform.localPosition = initialLocalPos;
        gunTransform.localRotation = initialLocalRot;
        IsAttacking = false;
    }
    #endregion

    #region Shotgun Attack
    private IEnumerator ShotgunShootRoutine()
    {
        IsAttacking = true;
        SoundManager.Instance?.PlayShotgunShoot();

        // Heavy Recoil Kick
        ApplyRecoil(recoilKickback, recoilRotation);
        if (muzzleFlashLight != null) StartCoroutine(FlashLightRoutine());

        // Multi-pellet spread
        int pellets = Mathf.Max(pelletCount, 8);
        for (int i = 0; i < pellets; i++)
        {
            FireRaycast(spreadAngle > 0 ? spreadAngle : 4.5f);
        }

        // Heavy Kickback Phase (0.1s)
        float elapsed = 0f;
        Vector3 kickPos = initialLocalPos - new Vector3(0, -0.02f, recoilKickback);
        Quaternion kickRot = initialLocalRot * Quaternion.Euler(recoilRotation);
        while (elapsed < 0.12f)
        {
            elapsed += Time.deltaTime;
            gunTransform.localPosition = Vector3.Lerp(gunTransform.localPosition, kickPos, elapsed / 0.12f);
            gunTransform.localRotation = Quaternion.Slerp(gunTransform.localRotation, kickRot, elapsed / 0.12f);
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        // Realistic Pump-Action Animation & Sound
        SoundManager.Instance?.PlayShotgunPump();
        Vector3 pumpBackPos = initialLocalPos + new Vector3(0, -0.035f, -0.05f);
        Quaternion pumpRot = initialLocalRot * Quaternion.Euler(3f, -2f, 1f);

        float p = 0f;
        while (p < 0.18f)
        {
            p += Time.deltaTime;
            float factor = Mathf.PingPong(p * 2f, 0.18f) / 0.18f;
            gunTransform.localPosition = Vector3.Lerp(initialLocalPos, pumpBackPos, factor);
            gunTransform.localRotation = Quaternion.Slerp(initialLocalRot, pumpRot, factor);
            yield return null;
        }

        gunTransform.localPosition = initialLocalPos;
        gunTransform.localRotation = initialLocalRot;
        IsAttacking = false;
    }
    #endregion

    #region Axe Melee Attack
    private IEnumerator AxeSwingRoutine()
    {
        IsAttacking = true;
        nextTimeToFire = Time.time + fireRate;

        // Phase 1: Windup (draw back up-right)
        Vector3 windupPos = initialLocalPos + new Vector3(0.08f, 0.08f, -0.06f);
        Quaternion windupRot = initialLocalRot * Quaternion.Euler(-25f, 35f, -15f);

        float t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            gunTransform.localPosition = Vector3.Lerp(initialLocalPos, windupPos, t / 0.15f);
            gunTransform.localRotation = Quaternion.Slerp(initialLocalRot, windupRot, t / 0.15f);
            yield return null;
        }

        // Phase 2: Swing Slash (whoosh sound + trail)
        SoundManager.Instance?.PlayAxeSwing();
        if (meleeTrail != null) meleeTrail.emitting = true;

        Vector3 slashPos = initialLocalPos + new Vector3(-0.16f, -0.1f, 0.12f);
        Quaternion slashRot = initialLocalRot * Quaternion.Euler(40f, -45f, 30f);

        // Perform Melee Hit Check
        PerformMeleeHit();

        t = 0f;
        while (t < 0.14f)
        {
            t += Time.deltaTime;
            gunTransform.localPosition = Vector3.Lerp(windupPos, slashPos, t / 0.14f);
            gunTransform.localRotation = Quaternion.Slerp(windupRot, slashRot, t / 0.14f);
            yield return null;
        }

        if (meleeTrail != null) meleeTrail.emitting = false;

        // Phase 3: Recovery back to idle
        t = 0f;
        while (t < 0.22f)
        {
            t += Time.deltaTime;
            gunTransform.localPosition = Vector3.Lerp(slashPos, initialLocalPos, t / 0.22f);
            gunTransform.localRotation = Quaternion.Slerp(slashRot, initialLocalRot, t / 0.22f);
            yield return null;
        }

        gunTransform.localPosition = initialLocalPos;
        gunTransform.localRotation = initialLocalRot;
        IsAttacking = false;
    }

    private void PerformMeleeHit()
    {
        if (playerCam == null) playerCam = Camera.main;
        Vector3 origin = playerCam != null ? playerCam.transform.position : transform.position;
        Vector3 forward = playerCam != null ? playerCam.transform.forward : transform.forward;

        float attackDist = Mathf.Min(range, 2.5f);
        RaycastHit[] hits = Physics.SphereCastAll(origin, meleeRadius, forward, attackDist);
        bool hitAnyEnemy = false;

        foreach (var hit in hits)
        {
            if (hit.collider.CompareTag("Player")) continue;

            EnemyHealth eh = hit.collider.GetComponentInParent<EnemyHealth>();
            if (eh != null && eh.currentHealth > 0)
            {
                eh.TakeDamage(damage);
                hitAnyEnemy = true;
                SpawnImpactEffect(hit.point, hit.normal, true);
            }
        }

        if (hitAnyEnemy)
        {
            SoundManager.Instance?.PlayAxeHit();
            SoundManager.Instance?.PlayHitMarker();
            GameUIManager.Instance?.ShowHitMarker();
        }
    }
    #endregion

    #region Firing Mechanics & Raycast
    private void ApplyRecoil(float kick, Vector3 rot)
    {
        if (gunTransform == null) return;
        gunTransform.localPosition -= new Vector3(0, 0, kick);
        gunTransform.localRotation *= Quaternion.Euler(rot);
    }

    private void FireRaycast(float coneAngle)
    {
        if (playerCam == null) playerCam = Camera.main;
        Vector3 origin = playerCam != null ? playerCam.transform.position : transform.position;
        Vector3 dir = playerCam != null ? playerCam.transform.forward : transform.forward;

        if (coneAngle > 0f)
        {
            float spreadX = UnityEngine.Random.Range(-coneAngle, coneAngle);
            float spreadY = UnityEngine.Random.Range(-coneAngle, coneAngle);
            dir = Quaternion.Euler(spreadX, spreadY, 0) * dir;
        }

        Vector3 hitPoint = origin + dir * range;
        RaycastHit hit;

        if (Physics.Raycast(origin, dir, out hit, range, ~0, QueryTriggerInteraction.Ignore))
        {
            hitPoint = hit.point;

            EnemyHealth eh = hit.collider.GetComponentInParent<EnemyHealth>();
            if (eh != null && eh.currentHealth > 0)
            {
                eh.TakeDamage(damage);
                SoundManager.Instance?.PlayHitMarker();
                GameUIManager.Instance?.ShowHitMarker();
                SpawnImpactEffect(hit.point, hit.normal, true);
            }
            else
            {
                SpawnImpactEffect(hit.point, hit.normal, false);
            }
        }

        ShowTracer(hitPoint);
    }

    private void ShowTracer(Vector3 targetPoint)
    {
        if (tracerPool == null || tracerPool.Length == 0) return;

        LineRenderer lr = tracerPool[currentTracerIndex];
        currentTracerIndex = (currentTracerIndex + 1) % tracerPool.Length;

        Vector3 start = (muzzlePoint != null) ? muzzlePoint.position : transform.position;
        StartCoroutine(TracerRoutine(lr, start, targetPoint));
    }

    private IEnumerator TracerRoutine(LineRenderer lr, Vector3 start, Vector3 end)
    {
        lr.enabled = true;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        yield return new WaitForSeconds(0.04f);
        lr.enabled = false;
    }

    private IEnumerator FlashLightRoutine()
    {
        muzzleFlashLight.enabled = true;
        muzzleFlashLight.intensity = 3f;
        yield return new WaitForSeconds(0.04f);
        muzzleFlashLight.intensity = 0f;
        muzzleFlashLight.enabled = false;
    }

    private void SpawnImpactEffect(Vector3 pos, Vector3 normal, bool isFlesh)
    {
        GameObject imp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        imp.transform.position = pos;
        imp.transform.localScale = Vector3.one * (isFlesh ? 0.08f : 0.05f);

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = isFlesh ? new Color(0.85f, 0.1f, 0.1f) : new Color(0.9f, 0.85f, 0.4f);
        imp.GetComponent<Renderer>().sharedMaterial = mat;
        Destroy(imp.GetComponent<Collider>());
        Destroy(imp, 0.35f);
    }
    #endregion

    #region Reloading
    private IEnumerator ReloadRoutine()
    {
        IsReloading = true;
        SoundManager.Instance?.PlayEmptyClick();

        // Lower weapon down-right
        Vector3 reloadPos = initialLocalPos + new Vector3(0.06f, -0.18f, -0.05f);
        Quaternion reloadRot = initialLocalRot * Quaternion.Euler(20f, -15f, 10f);

        float half = reloadTime * 0.5f;
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            gunTransform.localPosition = Vector3.Lerp(initialLocalPos, reloadPos, t / half);
            gunTransform.localRotation = Quaternion.Slerp(initialLocalRot, reloadRot, t / half);
            yield return null;
        }

        // Ammo refill logic
        int needed = magazineSize - currentAmmo;
        int take = Mathf.Min(needed, reserveAmmo);
        currentAmmo += take;
        reserveAmmo -= take;
        NotifyAmmo();

        if (weaponType == WeaponType.Shotgun) SoundManager.Instance?.PlayShotgunPump();
        else SoundManager.Instance?.PlayWeaponPickup();

        // Raise weapon back
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            gunTransform.localPosition = Vector3.Lerp(reloadPos, initialLocalPos, t / half);
            gunTransform.localRotation = Quaternion.Slerp(reloadRot, initialLocalRot, t / half);
            yield return null;
        }

        gunTransform.localPosition = initialLocalPos;
        gunTransform.localRotation = initialLocalRot;
        IsReloading = false;
    }
    #endregion

    public void AddReserveAmmo(int amount)
    {
        if (isMelee) return;
        reserveAmmo += amount;
        NotifyAmmo();
    }

    public void NotifyAmmo()
    {
        OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
    }
}
