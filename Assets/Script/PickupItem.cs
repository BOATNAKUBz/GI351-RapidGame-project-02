using System.Collections;
using UnityEngine;

public enum ItemType
{
    Medkit,
    Ammo,
    PowerGem
}

public class PickupItem : MonoBehaviour
{
    public ItemType itemType = ItemType.Medkit;
    public float healAmount = 35f;
    public int ammoAmount = 45;
    public int scoreAmount = 100;

    private float floatSpeed = 3f;
    private float floatHeight = 0.18f;
    private float rotateSpeed = 90f;
    private Vector3 basePosition;
    private bool isCollected = false;

    void Start()
    {
        basePosition = transform.position;
        SetupVisual();
    }

    void SetupVisual()
    {
        // Check if visuals already exist
        if (transform.Find("Visual") != null) return;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(transform, false);

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.EnableKeyword("_EMISSION");

        Light itemLight = visual.AddComponent<Light>();
        itemLight.type = LightType.Point;
        itemLight.range = 3f;
        itemLight.intensity = 1.2f;

        switch (itemType)
        {
            case ItemType.Medkit:
                mat.color = new Color(0.95f, 0.95f, 0.95f);
                itemLight.color = new Color(0.2f, 1f, 0.3f);
                mat.SetColor("_EmissionColor", new Color(0.1f, 0.8f, 0.2f) * 1.5f);

                // Main case
                GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.transform.SetParent(visual.transform, false);
                box.transform.localScale = new Vector3(0.5f, 0.35f, 0.35f);
                box.GetComponent<Renderer>().sharedMaterial = mat;
                Destroy(box.GetComponent<Collider>());

                // Green Cross
                Material greenMat = new Material(mat);
                greenMat.color = new Color(0.1f, 0.9f, 0.2f);
                greenMat.SetColor("_EmissionColor", new Color(0.2f, 1f, 0.3f) * 2f);

                GameObject crossV = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crossV.transform.SetParent(visual.transform, false);
                crossV.transform.localPosition = new Vector3(0, 0.18f, 0);
                crossV.transform.localScale = new Vector3(0.08f, 0.02f, 0.22f);
                crossV.GetComponent<Renderer>().sharedMaterial = greenMat;
                Destroy(crossV.GetComponent<Collider>());

                GameObject crossH = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crossH.transform.SetParent(visual.transform, false);
                crossH.transform.localPosition = new Vector3(0, 0.18f, 0);
                crossH.transform.localScale = new Vector3(0.22f, 0.02f, 0.08f);
                crossH.GetComponent<Renderer>().sharedMaterial = greenMat;
                Destroy(crossH.GetComponent<Collider>());
                break;

            case ItemType.Ammo:
                mat.color = new Color(0.25f, 0.35f, 0.45f);
                itemLight.color = new Color(1f, 0.85f, 0.2f);
                mat.SetColor("_EmissionColor", new Color(0.3f, 0.5f, 0.8f) * 1.5f);

                // Ammo Canister
                GameObject ammoBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ammoBox.transform.SetParent(visual.transform, false);
                ammoBox.transform.localScale = new Vector3(0.35f, 0.45f, 0.35f);
                ammoBox.GetComponent<Renderer>().sharedMaterial = mat;
                Destroy(ammoBox.GetComponent<Collider>());

                // Gold stripe
                Material goldMat = new Material(mat);
                goldMat.color = new Color(1f, 0.8f, 0.1f);
                goldMat.SetColor("_EmissionColor", new Color(1f, 0.85f, 0.2f) * 2f);

                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stripe.transform.SetParent(visual.transform, false);
                stripe.transform.localPosition = new Vector3(0, 0.05f, 0);
                stripe.transform.localScale = new Vector3(0.37f, 0.05f, 0.37f);
                stripe.GetComponent<Renderer>().sharedMaterial = goldMat;
                Destroy(stripe.GetComponent<Collider>());
                break;

            case ItemType.PowerGem:
                mat.color = new Color(0.1f, 0.8f, 1f);
                itemLight.color = new Color(0f, 0.9f, 1f);
                mat.SetColor("_EmissionColor", new Color(0f, 0.9f, 1f) * 2.5f);

                // Diamond Crystal
                GameObject gem = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                gem.transform.SetParent(visual.transform, false);
                gem.transform.localScale = new Vector3(0.35f, 0.55f, 0.35f);
                gem.transform.localRotation = Quaternion.Euler(45f, 45f, 0);
                gem.GetComponent<Renderer>().sharedMaterial = mat;
                Destroy(gem.GetComponent<Collider>());
                break;
        }

        // Trigger Collider on root
        SphereCollider sc = GetComponent<SphereCollider>();
        if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 1.0f;
    }

    void Update()
    {
        if (isCollected) return;

        // Floating Sine wave
        float newY = basePosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // Rotation
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null || other.GetComponentInParent<PlayerController>() != null)
        {
            Collect(other.gameObject);
        }
    }

    void Collect(GameObject player)
    {
        isCollected = true;

        var health = player.GetComponent<PlayerHealth>() ?? player.GetComponentInParent<PlayerHealth>();
        var gun = player.GetComponentInChildren<FPSGun>() ?? player.GetComponentInParent<FPSGun>();

        string notifText = "";

        switch (itemType)
        {
            case ItemType.Medkit:
                if (health != null) health.Heal(healAmount);
                if (SoundManager.Instance != null) SoundManager.Instance.PlayMedkit();
                notifText = $"+{healAmount} HP (Medkit)";
                break;

            case ItemType.Ammo:
                var activeW = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetActiveWeapon() : null;
                if (activeW != null && !activeW.isMelee)
                {
                    activeW.AddReserveAmmo(ammoAmount);
                }
                else if (gun != null)
                {
                    gun.AddAmmo(ammoAmount);
                }
                if (SoundManager.Instance != null) SoundManager.Instance.PlayAmmo();
                notifText = $"+{ammoAmount} Ammo";
                break;

            case ItemType.PowerGem:
                if (gun != null) gun.AddAmmo(20);
                if (health != null) health.Heal(15);
                if (SoundManager.Instance != null) SoundManager.Instance.PlayVictory();
                notifText = $"+{scoreAmount} Score / Power Boost!";
                break;
        }

        if (GameUIManager.Instance != null && !string.IsNullOrEmpty(notifText))
        {
            GameUIManager.Instance.ShowNotification(notifText);
        }

        StartCoroutine(CollectEffectRoutine());
    }

    IEnumerator CollectEffectRoutine()
    {
        Vector3 initialScale = transform.localScale;
        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, elapsed / duration);
            transform.position += Vector3.up * Time.deltaTime * 2f;
            yield return null;
        }

        Destroy(gameObject);
    }
}
