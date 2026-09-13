using UnityEngine;

public class LootDrop : MonoBehaviour
{
    public static LootDrop Instance { get; private set; }

    [Header("Item Drop Prefabs (Assigned in Inspector)")]
    public GameObject medkitPrefab;
    public GameObject ammoPrefab;
    public GameObject powerGemPrefab;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public static GameObject SpawnLoot(Vector3 position, float dropChance = 0.5f)
    {
        if (Random.value > dropChance)
        {
            return null; 
        }

        float roll = Random.value;
        ItemType selectedType;

        if (roll < 0.45f)
        {
            selectedType = ItemType.Medkit;
        }
        else if (roll < 0.85f)
        {
            selectedType = ItemType.Ammo;
        }
        else
        {
            selectedType = ItemType.PowerGem;
        }

        Vector3 spawnPos = position + Vector3.up * 0.4f;
        Vector2 randomCircle = Random.insideUnitCircle * 0.8f;
        spawnPos += new Vector3(randomCircle.x, 0, randomCircle.y);

        GameObject lootObj = null;

       
        if (Instance != null)
        {
            GameObject prefab = null;
            switch (selectedType)
            {
                case ItemType.Medkit: prefab = Instance.medkitPrefab; break;
                case ItemType.Ammo: prefab = Instance.ammoPrefab; break;
                case ItemType.PowerGem: prefab = Instance.powerGemPrefab; break;
            }

            if (prefab != null)
            {
                lootObj = Instantiate(prefab, spawnPos, Quaternion.identity);
                lootObj.name = $"Loot_{selectedType}";
                return lootObj;
            }
        }

        lootObj = new GameObject($"Loot_{selectedType}");
        lootObj.transform.position = spawnPos;

        PickupItem pickup = lootObj.AddComponent<PickupItem>();
        pickup.itemType = selectedType;

        return lootObj;
    }
}
