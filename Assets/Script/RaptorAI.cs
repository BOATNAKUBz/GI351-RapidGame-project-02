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
    private bool isDashing = false;
    private float nextDashTime = 0f;
    private NavMeshAgent agent;

    protected override void Start()
    {
        base.Start();

        if (attackRange < 2.2f) attackRange = 2.2f;
        if (stoppingDistance > 1.3f) stoppingDistance = 1.3f;

        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            {
                agent.enabled = true;
                agent.speed = moveSpeed;
                agent.stoppingDistance = stoppingDistance;
                agent.updateRotation = false; // ปิดเพื่อให้เราคุมการหมุนกลับด้าน 180 องศาได้เอง
            }
            else
            {
                agent.enabled = false; // ปิดถ้าไม่มี NavMesh เพื่อป้องกันการขัดแย้งกับ transform.position
            }
        }

        if (snapToGround)
        {
            SnapToGround();
        }
    }

    protected override void Update()
    {
        // 1. จัดการ Knockback เมื่อโดนขวานหรือลูกซอง
        if (isKnockedBack)
        {
            transform.position += knockbackVelocity * Time.deltaTime;
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, 8f * Time.deltaTime);
            knockbackDuration -= Time.deltaTime;

            if (knockbackDuration <= 0f)
            {
                isKnockedBack = false;
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                }
            }

            if (snapToGround) SnapToGround();
            return;
        }

        if (stunRemaining > 0f)
        {
            stunRemaining -= Time.deltaTime;
        }

        if (player == null || isDashing) return;

        Vector3 flatEnemy = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 flatPlayer = new Vector3(player.position.x, 0, player.position.z);
        float distance = Vector3.Distance(flatEnemy, flatPlayer);
        float verticalDist = Mathf.Abs(transform.position.y - player.position.y);

        if (animator != null)
        {
            try { animator.SetFloat("Speed", distance > stoppingDistance ? moveSpeed : 0f); } catch { }
        }

        // ถ้าอยู่ในระยะพุ่ง และ คูลดาวน์พร้อมใช้งาน และไม่ติดสตัน ให้พุ่งชนทันที
        if (distance <= dashRange && Time.time >= nextDashTime && stunRemaining <= 0f)
        {
            StartCoroutine(DashRoutine());
        }
        else
        {
            bool readyToAttack = (Time.time >= nextAttackTime && stunRemaining <= 0f);

            // คำนวณตำแหน่งเป้าหมาย: ถ้าพร้อมตีธรรมดาให้พุ่งตรงเข้าหาผู้เล่น
            Vector3 targetPosition;
            if (readyToAttack)
            {
                targetPosition = player.position;
            }
            else
            {
                float currentAngle = surroundOffsetAngle + (Time.time * orbitSpeed);
                Vector3 slotOffset = Quaternion.Euler(0, currentAngle, 0) * Vector3.forward * surroundRadius;
                targetPosition = player.position + slotOffset;
            }
            targetPosition.y = transform.position.y;

            // แรงแยกตัวออกจากศัตรูตัวอื่น
            Vector3 separationForce = Vector3.zero;
            Collider[] nearbyCols = Physics.OverlapSphere(transform.position, separationRadius);
            for (int i = 0; i < nearbyCols.Length; i++)
            {
                Collider col = nearbyCols[i];
                if (col.transform.root == transform.root) continue;
                if (col.CompareTag("Enemy"))
                {
                    Vector3 away = transform.position - col.transform.position;
                    away.y = 0;
                    float dist = away.magnitude;
                    if (dist > 0.01f && dist < separationRadius)
                    {
                        separationForce += (away.normalized / dist);
                    }
                }
            }

            Vector3 toSlot = (targetPosition - transform.position);
            toSlot.y = 0;
            float currentSepWeight = readyToAttack ? (separationWeight * 0.25f) : separationWeight;
            Vector3 moveDir = (toSlot.normalized + separationForce * currentSepWeight).normalized;

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
                    agent.SetDestination(targetPosition);
                }
                else
                {
                    transform.position += moveDir * moveSpeed * Time.deltaTime;
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

            // ตีธรรมดาเมื่อเข้าใกล้และไม่ติดสตัน
            if (distance <= attackRange && verticalDist <= 2.5f && stunRemaining <= 0f)
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
        bool hasHitPlayer = false;

        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            transform.position += targetDirection * dashSpeed * Time.deltaTime;

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

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        isDashing = false;
    }
}