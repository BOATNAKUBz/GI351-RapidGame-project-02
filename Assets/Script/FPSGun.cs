using System;
using System.Collections;
using UnityEngine;

public class FPSGun : MonoBehaviour
{
    [Header("Gun Stats")]
    public float damage = 25f;
    public float fireRate = 0.15f;
    public float range = 100f;
    public int magazineSize = 30;
    public int currentAmmo = 30;
    public int reserveAmmo = 120;
    public float reloadTime = 1.5f;

    [Header("Visual & Recoil")]
    public Transform gunTransform;     // ลาก Prefab ปืน/Model ปืนของคุณมาใส่ช่องนี้
    public Transform muzzlePoint;      // ลากจุดปลายกระบอกปืนมาใส่ช่องนี้
    public float recoilKickback = 0.08f;
    public float recoilReturnSpeed = 12f;

    [Header("References")]
    public Camera playerCamera;
    public LayerMask hitLayers = ~0; // All layers by default

    public bool isReloading { get; private set; }
    public event Action<int, int> OnAmmoChanged;

    private float nextTimeToFire = 0f;
    private Vector3 initialGunLocalPos;
    private Quaternion initialGunLocalRot;
    private Light muzzleFlashLight;
    private LineRenderer tracerLine;

    void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInParent<Camera>();
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }
    }

    void Start()
    {
        currentAmmo = magazineSize;
        SetupEffects();
        OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
    }

    void SetupEffects()
    {
        if (gunTransform != null)
        {
            initialGunLocalPos = gunTransform.localPosition;
            initialGunLocalRot = gunTransform.localRotation;
        }

        if (muzzlePoint != null)
        {
            GameObject lightObj = new GameObject("MuzzleFlashLight");
            lightObj.transform.SetParent(muzzlePoint);
            lightObj.transform.localPosition = Vector3.zero;
            muzzleFlashLight = lightObj.AddComponent<Light>();
            muzzleFlashLight.type = LightType.Point;
            muzzleFlashLight.color = new Color(1f, 0.85f, 0.4f);
            muzzleFlashLight.range = 5f;
            muzzleFlashLight.intensity = 0f;
            muzzleFlashLight.enabled = false;
        }

        // Setup bullet tracer LineRenderer
        GameObject tracerObj = new GameObject("BulletTracer");
        tracerObj.transform.SetParent(transform);
        tracerLine = tracerObj.AddComponent<LineRenderer>();
        tracerLine.startWidth = 0.035f;
        tracerLine.endWidth = 0.015f;
        tracerLine.material = new Material(Shader.Find("Sprites/Default"));
        tracerLine.startColor = new Color(1f, 0.95f, 0.5f, 0.8f);
        tracerLine.endColor = new Color(1f, 0.5f, 0.1f, 0f);
        tracerLine.enabled = false;
    }

    void Update()
    {
        // Smooth return from recoil
        if (gunTransform != null)
        {
            gunTransform.localPosition = Vector3.Lerp(gunTransform.localPosition, initialGunLocalPos, recoilReturnSpeed * Time.deltaTime);
            gunTransform.localRotation = Quaternion.Slerp(gunTransform.localRotation, initialGunLocalRot, recoilReturnSpeed * Time.deltaTime);
        }

        // Reload input
        if (InputBridge.GetReloadDown() && !isReloading && currentAmmo < magazineSize && reserveAmmo > 0)
        {
            StartCoroutine(ReloadRoutine());
            return;
        }

        // Shoot input (hold or click)
        if (InputBridge.GetFire() && Time.time >= nextTimeToFire && !isReloading)
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            nextTimeToFire = Time.time + fireRate;

            if (currentAmmo > 0)
            {
                Shoot();
            }
            else
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayEmptyClick();
                if (reserveAmmo > 0)
                {
                    StartCoroutine(ReloadRoutine());
                }
            }
        }
    }

    void Shoot()
    {
        currentAmmo--;
        OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);

        // Procedural audio
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayShoot();
        }

        // Visual recoil
        if (gunTransform != null)
        {
            gunTransform.localPosition -= Vector3.forward * recoilKickback;
            gunTransform.localRotation *= Quaternion.Euler(-3f, UnityEngine.Random.Range(-1.5f, 1.5f), 0);
        }

        // Flash & Tracer
        StartCoroutine(MuzzleFlashRoutine());

        Vector3 rayOrigin = playerCamera.transform.position;
        Vector3 rayDirection = playerCamera.transform.forward;
        Vector3 hitPoint = rayOrigin + rayDirection * range;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, range, hitLayers))
        {
            hitPoint = hit.point;

            // Damage enemy
            EnemyHealth enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null && !enemyHealth.isDead)
            {
                enemyHealth.TakeDamage(damage, hit.point, hit.normal);

                // Hit feedback
                if (GameUIManager.Instance != null)
                {
                    GameUIManager.Instance.ShowHitMarker();
                }
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayHitMarker();
                }
            }

            // Hit impact spark
            CreateHitSpark(hit.point, hit.normal);
        }

        Vector3 startPos = muzzlePoint != null ? muzzlePoint.position : playerCamera.transform.position;
        StartCoroutine(TracerRoutine(startPos, hitPoint));
    }

    IEnumerator MuzzleFlashRoutine()
    {
        if (muzzleFlashLight != null)
        {
            muzzleFlashLight.enabled = true;
            muzzleFlashLight.intensity = 2.5f;
            yield return new WaitForSeconds(0.04f);
            muzzleFlashLight.enabled = false;
        }
    }

    IEnumerator TracerRoutine(Vector3 start, Vector3 end)
    {
        if (tracerLine != null)
        {
            tracerLine.SetPosition(0, start);
            tracerLine.SetPosition(1, end);
            tracerLine.enabled = true;
            yield return new WaitForSeconds(0.035f);
            tracerLine.enabled = false;
        }
    }

    void CreateHitSpark(Vector3 point, Vector3 normal)
    {
        GameObject spark = new GameObject("HitSpark");
        spark.transform.position = point;
        spark.transform.rotation = Quaternion.LookRotation(normal);

        Light sparkLight = spark.AddComponent<Light>();
        sparkLight.type = LightType.Point;
        sparkLight.color = new Color(1f, 0.7f, 0.2f);
        sparkLight.range = 2f;
        sparkLight.intensity = 2f;

        Destroy(spark, 0.08f);
    }

    IEnumerator ReloadRoutine()
    {
        isReloading = true;

        if (gunTransform != null)
        {
            // Lower gun animation
            gunTransform.localPosition = initialGunLocalPos + new Vector3(0, -0.2f, -0.1f);
        }

        yield return new WaitForSeconds(reloadTime);

        int needed = magazineSize - currentAmmo;
        int toLoad = Mathf.Min(needed, reserveAmmo);
        currentAmmo += toLoad;
        reserveAmmo -= toLoad;

        isReloading = false;
        OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
    }

    public void AddAmmo(int amount)
    {
        reserveAmmo += amount;
        OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
    }
}