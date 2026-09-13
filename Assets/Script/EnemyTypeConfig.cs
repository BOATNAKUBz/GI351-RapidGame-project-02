using System;
using UnityEngine;

public enum EnemyType
{
    Swarmer, // Small, fast, low HP
    Grunt,   // Balanced humanoid zombie
    Tank,    // Huge, high HP, slow, high damage
    Elite    // Fast, glowing assassin/stalker
}

[System.Serializable]
public class EnemyStats
{
    public EnemyType type;
    public string displayName;
    public float maxHealth;
    public float moveSpeed;
    public float damage;
    public float attackCooldown;
    public float attackRange;
    public float modelScale;
    public Color primaryColor;
    public Color emissionColor;
    public float dropChance;

    public static EnemyStats GetDefault(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Swarmer:
                return new EnemyStats
                {
                    type = EnemyType.Swarmer,
                    displayName = "Swarmer",
                    maxHealth = 35f,
                    moveSpeed = 7.5f,
                    damage = 6f,
                    attackCooldown = 0.7f,
                    attackRange = 1.3f,
                    modelScale = 0.7f,
                    primaryColor = new Color(0.9f, 0.25f, 0.1f), // Fiery Orange-Red
                    emissionColor = new Color(1f, 0.4f, 0.1f) * 2f,
                    dropChance = 0.35f
                };

            case EnemyType.Grunt:
                return new EnemyStats
                {
                    type = EnemyType.Grunt,
                    displayName = "Grunt",
                    maxHealth = 85f,
                    moveSpeed = 3.6f,
                    damage = 15f,
                    attackCooldown = 1.2f,
                    attackRange = 1.6f,
                    modelScale = 1.0f,
                    primaryColor = new Color(0.2f, 0.65f, 0.3f), // Toxic Green
                    emissionColor = new Color(0.8f, 0.9f, 0.2f) * 1.5f,
                    dropChance = 0.5f
                };

            case EnemyType.Tank:
                return new EnemyStats
                {
                    type = EnemyType.Tank,
                    displayName = "Brute Tank",
                    maxHealth = 260f,
                    moveSpeed = 2.1f,
                    damage = 35f,
                    attackCooldown = 1.8f,
                    attackRange = 2.2f,
                    modelScale = 1.85f,
                    primaryColor = new Color(0.4f, 0.15f, 0.5f), // Dark Purple / Armor
                    emissionColor = new Color(0.9f, 0.1f, 0.2f) * 2.5f,
                    dropChance = 1.0f // Always drops on Tank death
                };

            case EnemyType.Elite:
                return new EnemyStats
                {
                    type = EnemyType.Elite,
                    displayName = "Shadow Elite",
                    maxHealth = 135f,
                    moveSpeed = 5.6f,
                    damage = 22f,
                    attackCooldown = 0.9f,
                    attackRange = 1.7f,
                    modelScale = 1.2f,
                    primaryColor = new Color(0.12f, 0.15f, 0.2f), // Obsidian Black
                    emissionColor = new Color(0f, 0.9f, 1f) * 3f, // Bright Cyan Core
                    dropChance = 0.85f
                };

            default:
                return new EnemyStats();
        }
    }
}
