using System.Collections;
using UnityEngine;

public class RaptorAI : EnemyAI
{
    [Header("Raptor Dash Attack Settings")]
    public float dashRange = 6f;          // ระยะที่เริ่มพุ่งใส่ผู้เล่น
    public float dashSpeed = 14f;         // ความเร็วตอนพุ่ง
    public float dashCooldown = 4f;       // คูลดาวน์สกิลพุ่ง (วินาที)
    private bool isDashing = false;
    private float nextDashTime = 0f;

    protected override void Awake()
    {
        base.Awake();
        // องศาการหมุนชดเชยของโมเดลแรปเตอร์ (180 องศาเพื่อให้หัวหันไปข้างหน้า)
        modelRotationOffset = 180f;
    }

    protected override void Start()
    {
        base.Start();

        if (attackRange < 2.2f) attackRange = 2.2f;
        if (stoppingDistance > 1.3f) stoppingDistance = 1.3f;
    }

    protected override void Update()
    {
        if (isDashing) return;

        // อัปเดต Speed ให้กับ Animator ของ Raptor (โดยเช็คก่อนว่ามีพารามิเตอร์นี้ไหม)
        if (animator != null && player != null)
        {
            Vector3 flatEnemy = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 flatPlayer = new Vector3(player.position.x, 0, player.position.z);
            float distance = Vector3.Distance(flatEnemy, flatPlayer);

            if (HasAnimatorParameter(animator, "Speed"))
            {
                animator.SetFloat("Speed", distance > stoppingDistance ? moveSpeed : 0f);
            }

            // ถ้าอยู่ในระยะพุ่ง และ คูลดาวน์พร้อมใช้งาน และไม่ติดสตัน ให้พุ่งชนทันที
            if (distance <= dashRange && Time.time >= nextDashTime && stunRemaining <= 0f && !isKnockedBack)
            {
                StartCoroutine(DashRoutine());
                return;
            }
        }

        // ใช้ระบบเดินอ้อมสิ่งกีดขวาง + ไม่เดินทะลุ Collider ของ EnemyAI
        base.Update();
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        nextDashTime = Time.time + dashCooldown;

        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
        }

        // 1. หมุนหน้าไปหาผู้เล่น และล็อคเป้าหมาย พร้อมกลับด้าน 180 องศา
        Vector3 targetDirection = (player.position - transform.position).normalized;
        targetDirection.y = 0;
        if (targetDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(targetDirection) * Quaternion.Euler(0, modelRotationOffset, 0);
        }

        // ดึง Animator ของตัวเองมาใช้งาน (โดยเช็คก่อนว่ามีพารามิเตอร์นี้ไหม)
        if (animator != null && HasAnimatorParameter(animator, "Attack"))
        {
            animator.SetTrigger("Attack");
        }

        // 2. พุ่งตรงไปข้างหน้าตามทิศทางเป้าหมายหาผู้เล่น (ใช้ MoveWithCollisionSweep ไม่พุ่งทะลุกำแพง)
        float dashDuration = 0.35f;
        float elapsed = 0f;
        bool hasHitPlayer = false;

        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            Vector3 dashStep = targetDirection * dashSpeed * Time.deltaTime;
            MoveWithCollisionSweep(dashStep);

            if (snapToGround)
            {
                SnapToGround();
            }

            // ตรวจจับดาเมจระหว่างพุ่งชนทันทีถ้าเข้าใกล้ผู้เล่น
            if (!hasHitPlayer && player != null)
            {
                Vector3 fE = new Vector3(transform.position.x, 0, transform.position.z);
                Vector3 fP = new Vector3(player.position.x, 0, player.position.z);
                float hDist = Vector3.Distance(fE, fP);
                if (hDist <= attackRange + 0.8f && Mathf.Abs(transform.position.y - player.position.y) <= 2.5f)
                {
                    hasHitPlayer = true;
                    PlayerHealth playerHealth = player.GetComponent<PlayerHealth>() 
                        ?? player.GetComponentInParent<PlayerHealth>() 
                        ?? player.GetComponentInChildren<PlayerHealth>()
                        ?? FindAnyObjectByType<PlayerHealth>();

                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(attackDamage);
                    }
                }
            }

            yield return null;
        }

        if (snapToGround)
        {
            SnapToGround();
        }

        // 3. เช็คอีกครั้งเมื่อจบการพุ่งเผื่อยังไม่โดนระหว่างทาง
        if (!hasHitPlayer && player != null)
        {
            Vector3 fE = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 fP = new Vector3(player.position.x, 0, player.position.z);
            float hDist = Vector3.Distance(fE, fP);
            if (hDist <= attackRange + 1.0f && Mathf.Abs(transform.position.y - player.position.y) <= 2.5f)
            {
                PlayerHealth playerHealth = player.GetComponent<PlayerHealth>() 
                    ?? player.GetComponentInParent<PlayerHealth>() 
                    ?? player.GetComponentInChildren<PlayerHealth>()
                    ?? FindAnyObjectByType<PlayerHealth>();

                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                }
            }
        }

        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = false;
        }

        isDashing = false;
    }
}