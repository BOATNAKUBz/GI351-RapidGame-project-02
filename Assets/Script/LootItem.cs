using UnityEngine;

[System.Serializable]
public class LootItem
{
    public string itemName;        // ชื่อไอเทม (ไว้ดูใน Inspector)
    public GameObject itemPrefab;  // Prefab ไอเทม (Medkit, Ammo ฯลฯ)
    [Range(0f, 100f)]
    public float dropChance;       // โอกาสดรอปของชิ้นนี้ (เช่น 30%)
}