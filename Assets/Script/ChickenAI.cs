using UnityEngine;

public class ChickenAI : EnemyAI
{
    protected override void Start()
    {
        base.Start();

        if (moveSpeed <= 3.5f) moveSpeed = 6f; // ไก่วิ่งไวเป็นพิเศษ
        attackRange = 2.0f;
        stoppingDistance = 1.0f;

        var health = GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.customDisplayName = "Chicken";
        }
    }

    protected override void Update()
    {
        base.Update();

        if (animator != null && player != null)
        {
            Vector3 flatEnemy = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 flatPlayer = new Vector3(player.position.x, 0, player.position.z);
            float hDist = Vector3.Distance(flatEnemy, flatPlayer);

            if (hDist > stoppingDistance)
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
        // ไม่ต้องเขียนระบบลดเลือดซ้ำ เพราะ base.OnReachPlayer() จาก EnemyAI ทำหน้าที่ให้แล้ว
    }
}