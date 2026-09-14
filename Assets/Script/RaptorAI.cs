using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class RaptorAI : EnemyAI
{
    [Header("Model Facing")]
    [Tooltip("องศาการหมุนชดเชยของโมเดล (180 องศาเพื่อกลับด้านให้หัวหันหาผู้เล่น)")]
    public float modelRotationOffset = 180f;

    [Header("Raptor Dash Attack Settings")]
    public float dashRange = 6f;          // ระยะที่เริ่มพุ่งใส่ผู้เล่น
    public float dashSpeed = 14f;         // ความเร็วตอนพุ่ง
    public float dashCooldown = 4f;       // คูลดาวน์สกิลพุ่ง (วินาที)
    public float attackDamage = 30f;      // ดาเมจเมื่อพุ่งชนโดน

    private bool isDashing = false;
    private float nextDashTime = 0f;
    private NavMeshAgent agent;

    protected override void Start()
    {
        base.Start();
        if (moveSpeed <= 3.5f) moveSpeed = 5.5f;

        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            if (!agent.isOnNavMesh)
            {
                agent.enabled = false; // ปิดถ้าไม่มี NavMesh เพื่อป้องกันการขัดแย้งกับ transform.position
            }
            else
            {
                agent.speed = moveSpeed;
                agent.stoppingDistance = stoppingDistance;
                agent.updateRotation = false; // ปิดเพื่อให้เราคุมการหมุนกลับด้าน 180 องศาได้เอง
            }
        }

        if (snapToGround)
        {
            SnapToGround();
        }
    }

    protected override void Update()
    {
        if (player == null || isDashing) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (animator != null)
        {
            try { animator.SetFloat("Speed", distance > stoppingDistance ? moveSpeed : 0f); } catch { }
        }

        // ถ้าอยู่ในระยะพุ่ง และ คูลดาวน์พร้อมใช้งาน ให้พุ่งชนทันที
        if (distance <= dashRange && Time.time >= nextDashTime)
        {
            StartCoroutine(DashRoutine());
        }
        else
        {
            // ควบคุมการเดินและหันหน้า พร้อมหมุนกลับด้าน 180 องศาให้หัวหันไปข้างหน้า
            Vector3 targetPosition = new Vector3(player.position.x, transform.position.y, player.position.z);
            Vector3 moveDir = (targetPosition - transform.position).normalized;
            moveDir.y = 0;

            if (moveDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir) * Quaternion.Euler(0, modelRotationOffset, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 15f * Time.deltaTime);
            }

            if (distance > stoppingDistance)
            {
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                }
                else
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                }
            }
            else
            {
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                }
            }

            // ยึดติดพื้นเสมอ
            if (snapToGround)
            {
                SnapToGround();
            }

            if (distance <= attackRange)
            {
                OnReachPlayer();
            }
        }
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        nextDashTime = Time.time + dashCooldown;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        // 1. หมุนหน้าไปหาผู้เล่น และล็อคเป้าหมาย พร้อมกลับด้าน 180 องศา
        Vector3 targetDirection = (player.position - transform.position).normalized;
        targetDirection.y = 0;
        if (targetDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(targetDirection) * Quaternion.Euler(0, modelRotationOffset, 0);
        }

        // ดึง Animator ของตัวเองมาใช้งาน
        if (animator != null)
        {
            try { animator.SetTrigger("Attack"); } catch { }
        }

        // 2. พุ่งตรงไปข้างหน้าตามทิศทางเป้าหมายหาผู้เล่น
        float dashDuration = 0.35f; // ระยะเวลาในการพุ่ง (วินาที)
        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            transform.position += targetDirection * dashSpeed * Time.deltaTime;

            if (snapToGround)
            {
                SnapToGround();
            }

            yield return null;
        }

        if (snapToGround)
        {
            SnapToGround();
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

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        isDashing = false;
    }
}