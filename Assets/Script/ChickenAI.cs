using UnityEngine;

public class ChickenAI : EnemyAI
{
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
        // ไม่ต้องเขียนระบบลดเลือดซ้ำ เพราะ base.OnReachPlayer() จาก EnemyAI ทำหน้าที่ให้แล้ว
    }
}