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

    protected override void Update()
    {
        base.Update();

        if (animator != null)
        {
            if (player != null && Vector3.Distance(transform.position, player.position) > stoppingDistance)
            {
                animator.SetFloat("Vert", 1f);
                animator.SetFloat("State", 1f);
            }
            else
            {
                animator.SetFloat("Vert", 0f);
                animator.SetFloat("State", 0f);
            }
        }
    }

    protected override void OnReachPlayer()
    {
        base.OnReachPlayer();

        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;

            if (animator != null)
            {
                try { animator.SetTrigger("Attack"); } catch { }
            }

            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>() ?? player.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }
    }
}