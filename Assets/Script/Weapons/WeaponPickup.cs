using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [Header("Pickup Information")]
    public WeaponType weaponType = WeaponType.Shotgun;
    public string weaponName = "Shotgun";
    public int bonusAmmo = 12;

    [Header("Floating Animation")]
    public float rotateSpeed = 60f;
    public float floatSpeed = 2.5f;
    public float floatHeight = 0.15f;

    private Vector3 initialPos;
    public bool isCollected { get; private set; } = false;

    void Start()
    {
        initialPos = transform.position;
        SetupAura();
    }

    void SetupAura()
    {
        // Add subtle point light aura
        Light l = GetComponentInChildren<Light>();
        if (l == null)
        {
            GameObject lObj = new GameObject("PickupLight");
            lObj.transform.SetParent(transform, false);
            lObj.transform.localPosition = new Vector3(0, 0.4f, 0);
            l = lObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 3.5f;
            l.intensity = 1.5f;

            switch (weaponType)
            {
                case WeaponType.Shotgun:
                    l.color = new Color(1f, 0.45f, 0.1f); // Orange/amber
                    break;
                case WeaponType.Revolver:
                    l.color = new Color(0.2f, 0.8f, 1f); // Cyan
                    break;
                case WeaponType.Axe:
                    l.color = new Color(0.9f, 0.2f, 0.2f); // Red
                    break;
            }
        }
    }

    void Update()
    {
        if (isCollected) return;

        // Floating and rotation
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = initialPos + new Vector3(0, yOffset, 0);
    }

    public void Collect()
    {
        if (isCollected) return;
        isCollected = true;

        // Disable colliders immediately so raycasts/overlaps won't hit it in the remaining frame
        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null) colliders[i].enabled = false;
        }

        // Disable renderers immediately
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) renderers[i].enabled = false;
        }

        // Give bonus reserve ammo if weapon was already possessed
        FPSWeapon active = PlayerInventory.Instance?.GetActiveWeapon();
        if (active != null && active.weaponType == weaponType && !active.isMelee)
        {
            active.AddReserveAmmo(bonusAmmo);
        }

        Destroy(gameObject);
    }
}
