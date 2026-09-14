using System.Collections;
using UnityEngine;

public class RaptorAI : EnemyAI
{
    [Header("Raptor Dash Attack Settings")]
    public float dashRange = 6f;          // ระยะที่เริ่มพุ่งใส่ผู้เล่น
    public float dashSpeed = 14f;         // ความเร็วตอนพุ่ง
    public float dashCooldown = 4f;       // คูลดาวน์สกิลพุ่ง (วินาที)
    public float attackDamage = 30f;      // ดาเมจเมื่อพุ่งชนโดน

    private bool isDashing = false;
    private float nextDashTime = 0f;

    protected override void Update()
    {
        if (player == null || isDashing) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // ถ้าอยู่ในระยะพุ่ง และ คูลดาวน์พร้อมใช้งาน ให้พุ่งชนทันที
        if (distance <= dashRange && Time.time >= nextDashTime)
        {
            StartCoroutine(DashRoutine());
        }
        else
        {
            // ถ้าพุ่งไม่ได้ ให้เดินตามปกติด้วย EnemyAI
            base.Update();
        }
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        nextDashTime = Time.time + dashCooldown;

        // 1. หมุนหน้าไปหาผู้เล่น และล็อคเป้าหมาย
        Vector3 targetDirection = (player.position - transform.position).normalized;
        targetDirection.y = 0;
        transform.forward = targetDirection;

        // ดึง Animator ของตัวเองมาใช้งานโดยตรง ป้องกัน Error ขีดแดง
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("Attack");
        }

        // 2. พุ่งตรงไปข้างหน้าตามทิศทางเป้าหมาย
        float dashDuration = 0.35f; // ระยะเวลาในการพุ่ง (วินาที)
        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            transform.position += targetDirection * dashSpeed * Time.deltaTime;
            yield return null;
        }

        // 3. เช็คว่าพุ่งไปโดนผู้เล่นไหมเมื่อจบการพุ่ง
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackRange + 0.5f)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>() ?? player.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }

        isDashing = false;
    }
}