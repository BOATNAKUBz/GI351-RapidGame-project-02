using UnityEngine;

public class ChickenAI : EnemyAI
{
    [Header("Chicken Combat")]
    public float attackDamage = 5f;
    public float attackCooldown = 0.5f;
    private float nextAttackTime = 0f;

    protected override void Start()
    {
        base.Start();
        if (moveSpeed <= 3.5f) moveSpeed = 6f; // ไก่วิ่งไวเป็นพิเศษ
    }

    protected override void OnReachPlayer()
    {
        base.OnReachPlayer();

        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;

            // ดึง Animator ของตัวเองมาใช้งานโดยตรง ป้องกัน Error ขีดแดง
            Animator anim = GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetTrigger("Attack");
            }

            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>() ?? player.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }
    }
}