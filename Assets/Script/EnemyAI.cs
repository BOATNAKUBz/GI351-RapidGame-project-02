using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Loot Settings")]
    public GameObject dropItemPrefab;
    [Range(0f, 1f)]
    public float dropChance = 0.5f;
    public EnemyStats stats;

    [Header("Movement & Target")]
    public Transform player;
    public float moveSpeed = 3.5f;
    public float stoppingDistance = 1.5f;

    [Header("Model Facing & Rotation")]
    [Tooltip("องศาการหมุนชดเชยของโมเดล (เช่น 180 สำหรับโมเดลที่หันหลังโดยดีฟอลต์)")]
    public float modelRotationOffset = 0f;

    [Header("Attack Settings")]
    public float attackRange = 2f;
    public float attackDamage = 15f;
    public float attackCooldown = 1.0f;
    protected float nextAttackTime = 0f;

    [Header("Ground Alignment")]
    public bool snapToGround = true;
    public float groundCheckDistance = 10f;
    public float raycastStartHeight = 1.5f;

    [Header("Surround & Flocking")]
    public float surroundRadius = 2.2f;
    public float separationRadius = 1.3f;
    public float separationWeight = 1.4f;
    public float orbitSpeed = 10f;
    protected float surroundOffsetAngle = 0f;

    [Header("Knockback & Stun")]
    public bool isKnockedBack { get; protected set; }
    public float stunRemaining { get; protected set; }
    protected Vector3 knockbackVelocity = Vector3.zero;
    protected float knockbackDuration = 0f;

    [Header("Obstacle Avoidance & Collision Sweep")]
    [Tooltip("เลเยอร์ที่นับเป็นสิ่งกีดขวาง (กำแพง, สิ่งปลูกสร้าง, ก้อนหิน, ต้นไม้ ฯลฯ)")]
    public LayerMask obstacleLayers = ~0;
    public float obstacleCheckDistance = 3.5f;
    public float skinWidth = 0.05f;

    protected NavMeshAgent navAgent;
    protected CapsuleCollider capsuleCol;
    protected Animator animator;

    // ความจำทิศทางในการเดินอ้อม (ป้องกันการสะบัดซ้ายขวาสลับไปมา)
    protected float avoidanceSideMemory = 0f; // -1 = ซ้าย, +1 = ขวา
    protected float avoidanceMemoryTimer = 0f;

    protected virtual void Awake()
    {
        capsuleCol = GetComponent<CapsuleCollider>();
        if (capsuleCol == null)
        {
            capsuleCol = gameObject.AddComponent<CapsuleCollider>();
            capsuleCol.center = new Vector3(0, 1f, 0);
            capsuleCol.radius = 0.5f;
            capsuleCol.height = 2f;
        }

        navAgent = GetComponent<NavMeshAgent>();
    }

    protected virtual void Start()
    {
        // สุ่มมุมรอบตัวผู้เล่น เพื่อให้ศัตรูแต่ละตัวเข้าหาจากคนละมุม
        surroundOffsetAngle = Random.Range(0f, 360f);

        // 1. หาตำแหน่ง Player อัตโนมัติจาก Tag
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        // 2. ดึง Component Animator (ถ้ามี)
        animator = GetComponent<Animator>();

        // 3. กำหนดค่าและตรวจสอบ NavMeshAgent อย่างปลอดภัย
        if (navAgent == null) navAgent = GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.speed = moveSpeed;
            navAgent.stoppingDistance = stoppingDistance;
            navAgent.acceleration = 20f;
            navAgent.angularSpeed = 360f;
            navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            navAgent.autoBraking = true;
            navAgent.updateRotation = false; // เราคุมการหมุนเองเพื่อให้หันหน้าได้อย่างนุ่มนวลและรองรับ modelRotationOffset

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
            {
                navAgent.enabled = true;
                navAgent.Warp(hit.position);
            }
            else
            {
                navAgent.enabled = false;
            }
        }

        // 4. ติดพื้นทันทีตอนเกิด (กรณีที่ไม่ได้อยู่บน NavMesh)
        if (snapToGround)
        {
            SnapToGround();
        }

        // 5. ปรับความสมดุลของ stoppingDistance และ attackRange
        if (attackRange < 2.0f) attackRange = 2.0f;
        if (stoppingDistance > attackRange - 0.6f) stoppingDistance = Mathf.Max(0.9f, attackRange - 0.7f);
    }

    public virtual void ApplyKnockback(Vector3 direction, float force, float stunDuration = 0.4f)
    {
        direction.y = Mathf.Clamp(direction.y, 0.1f, 0.35f);
        direction = direction.normalized;
        knockbackVelocity = direction * force;
        knockbackDuration = 0.25f;
        isKnockedBack = true;
        stunRemaining = Mathf.Max(stunRemaining, stunDuration);

        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
            navAgent.velocity = knockbackVelocity;
        }
    }

    protected virtual void Update()
    {
        // 1. จัดการ Knockback เมื่อโดนขวานหรือลูกซอง (ใช้ Collision Sweep ป้องกันการกระเด็นทะลุกำแพง)
        if (isKnockedBack)
        {
            Vector3 kbStep = knockbackVelocity * Time.deltaTime;
            MoveWithCollisionSweep(kbStep);

            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, 8f * Time.deltaTime);
            knockbackDuration -= Time.deltaTime;

            if (knockbackDuration <= 0f)
            {
                isKnockedBack = false;
                if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
                {
                    navAgent.isStopped = false;
                }
            }

            if (snapToGround) SnapToGround();
            return;
        }

        if (stunRemaining > 0f)
        {
            stunRemaining -= Time.deltaTime;
            if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = true;
            }
            return;
        }

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            else return;
        }

        // คำนวณระยะห่างแนวนอนและแนวตั้ง
        Vector3 flatEnemy = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 flatPlayer = new Vector3(player.position.x, 0, player.position.z);
        float horizontalDist = Vector3.Distance(flatEnemy, flatPlayer);
        float verticalDist = Mathf.Abs(transform.position.y - player.position.y);

        bool readyToAttack = (Time.time >= nextAttackTime && stunRemaining <= 0f);

        // คำนวณตำแหน่งเป้าหมาย
        Vector3 targetPos;
        if (readyToAttack)
        {
            targetPos = player.position;
        }
        else
        {
            float currentAngle = surroundOffsetAngle + (Time.time * orbitSpeed);
            Vector3 slotOffset = Quaternion.Euler(0, currentAngle, 0) * Vector3.forward * surroundRadius;
            targetPos = player.position + slotOffset;
        }

        // เช็คความพร้อมของ NavMeshAgent
        bool agentUsable = (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh);

        // อัปเดตพารามิเตอร์ Speed ใน Animator (ถ้ามี)
        if (animator != null && HasAnimatorParameter(animator, "Speed"))
        {
            float currentSpeed = horizontalDist > stoppingDistance ? moveSpeed : 0f;
            animator.SetFloat("Speed", currentSpeed);
        }

        // =========================================================================
        // โหมด 1: เดินโดยใช้ NavMeshAgent
        // =========================================================================
        if (agentUsable)
        {
            if (horizontalDist > stoppingDistance)
            {
                navAgent.isStopped = false;
                navAgent.speed = moveSpeed;
                navAgent.stoppingDistance = stoppingDistance;
                
                if (Vector3.Distance(navAgent.destination, targetPos) > 0.5f)
                {
                    navAgent.SetDestination(targetPos);
                }

                Vector3 moveDir = navAgent.desiredVelocity;
                moveDir.y = 0;
                
                if (moveDir.sqrMagnitude < 0.05f)
                {
                    moveDir = (targetPos - transform.position);
                    moveDir.y = 0;
                }

                if (moveDir.sqrMagnitude > 0.05f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized) * Quaternion.Euler(0, modelRotationOffset, 0);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
                }
            }
            else
            {
                navAgent.isStopped = true;

                Vector3 lookTarget = new Vector3(player.position.x, transform.position.y, player.position.z);
                Vector3 lookDir = (lookTarget - transform.position).normalized;
                if (lookDir != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir) * Quaternion.Euler(0, modelRotationOffset, 0);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 12f * Time.deltaTime);
                }
            }
        }
        // =========================================================================
        // โหมด 2: Dynamic Obstacle Avoidance + Physics Collision Sweep
        // =========================================================================
        else
        {
            if (navAgent != null && !navAgent.enabled && Time.frameCount % 60 == 0)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                {
                    navAgent.enabled = true;
                    navAgent.Warp(hit.position);
                }
            }

            if (horizontalDist > stoppingDistance)
            {
                Vector3 separationForce = Vector3.zero;
                Collider[] nearbyCols = Physics.OverlapSphere(transform.position, separationRadius);
                for (int i = 0; i < nearbyCols.Length; i++)
                {
                    Collider col = nearbyCols[i];
                    if (col == null || col.transform.root == transform.root) continue;
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

                Vector3 toTarget = (targetPos - transform.position);
                toTarget.y = 0;
                float currentSepWeight = readyToAttack ? (separationWeight * 0.25f) : separationWeight;
                Vector3 baseDir = (toTarget.normalized + separationForce * currentSepWeight).normalized;
                if (baseDir == Vector3.zero) baseDir = toTarget.normalized;

                Vector3 steerDir = CalculateAvoidanceDirection(baseDir, toTarget.magnitude);

                if (steerDir != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(steerDir) * Quaternion.Euler(0, modelRotationOffset, 0);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);

                    Vector3 moveStep = steerDir * moveSpeed * Time.deltaTime;
                    MoveWithCollisionSweep(moveStep);
                }
            }
            else
            {
                Vector3 lookTarget = new Vector3(player.position.x, transform.position.y, player.position.z);
                Vector3 lookDir = (lookTarget - transform.position).normalized;
                if (lookDir != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir) * Quaternion.Euler(0, modelRotationOffset, 0);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 12f * Time.deltaTime);
                }
            }

            if (snapToGround)
            {
                SnapToGround();
            }
        }

        // โจมตีเมื่ออยู่ในระยะและไม่ติดสตัน
        if (horizontalDist <= attackRange && verticalDist <= 2.5f && stunRemaining <= 0f)
        {
            OnReachPlayer();
        }
    }

    protected virtual Vector3 CalculateAvoidanceDirection(Vector3 directDir, float distToTarget)
    {
        if (directDir == Vector3.zero) return Vector3.zero;

        Vector3 rayStart = transform.position + Vector3.up * Mathf.Clamp(raycastStartHeight, 0.5f, 1.5f);
        float checkDist = Mathf.Min(obstacleCheckDistance, Mathf.Max(1.5f, distToTarget));
        float probeRadius = (capsuleCol != null) ? Mathf.Max(0.2f, capsuleCol.radius * 0.8f) : 0.35f;

        bool directBlocked = ProbeForObstacle(rayStart, directDir, checkDist, probeRadius, out RaycastHit directHit);

        if (!directBlocked)
        {
            if (avoidanceMemoryTimer > 0f)
            {
                avoidanceMemoryTimer -= Time.deltaTime;
                if (avoidanceMemoryTimer <= 0f) avoidanceSideMemory = 0f;
            }
            return directDir;
        }

        float[] baseAngles = new float[] { 30f, 60f, 90f, 120f, 150f };
        float preferredSign = (avoidanceSideMemory != 0f) ? avoidanceSideMemory : 1f;

        Vector3 bestClearDir = Vector3.zero;
        float bestScore = float.MinValue;

        for (int i = 0; i < baseAngles.Length; i++)
        {
            float angle = baseAngles[i];
            float[] signs = new float[] { preferredSign, -preferredSign };

            for (int s = 0; s < 2; s++)
            {
                float signedAngle = angle * signs[s];
                Vector3 candidateDir = Quaternion.Euler(0, signedAngle, 0) * directDir;
                candidateDir.y = 0;
                candidateDir.Normalize();

                bool blocked = ProbeForObstacle(rayStart, candidateDir, checkDist * 0.85f, probeRadius, out RaycastHit hit);
                if (!blocked)
                {
                    float consistencyBonus = (signs[s] == avoidanceSideMemory) ? 0.35f : 0f;
                    float anglePenalty = Mathf.Abs(signedAngle) / 180f;
                    float score = (1f - anglePenalty) + consistencyBonus;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestClearDir = candidateDir;
                        avoidanceSideMemory = signs[s];
                        avoidanceMemoryTimer = 0.6f;
                    }
                }
            }

            if (bestClearDir != Vector3.zero)
            {
                break;
            }
        }

        if (bestClearDir == Vector3.zero && directHit.collider != null)
        {
            Vector3 slide = Vector3.ProjectOnPlane(directDir, directHit.normal);
            slide.y = 0;
            if (slide.sqrMagnitude > 0.01f)
            {
                bestClearDir = slide.normalized;
            }
        }

        return (bestClearDir != Vector3.zero) ? bestClearDir : directDir;
    }

    protected bool ProbeForObstacle(Vector3 origin, Vector3 dir, float distance, float radius, out RaycastHit validHit)
    {
        validHit = default;
        RaycastHit[] hits = Physics.SphereCastAll(origin, radius, dir, distance, obstacleLayers, QueryTriggerInteraction.Ignore);

        float closestDist = float.MaxValue;
        bool found = false;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit h = hits[i];
            Collider c = h.collider;
            if (c == null || c.isTrigger) continue;
            if (c.transform.root == transform.root) continue;
            if (c.CompareTag("Enemy") || c.CompareTag("Player")) continue;

            // มองข้ามการชนพื้นหรือเนิน
            if (h.normal.y > 0.65f) continue;

            if (h.distance < closestDist)
            {
                closestDist = h.distance;
                validHit = h;
                found = true;
            }
        }

        return found;
    }

    public virtual void MoveWithCollisionSweep(Vector3 deltaMove)
    {
        if (deltaMove.sqrMagnitude < 0.000001f) return;

        float moveDist = deltaMove.magnitude;
        Vector3 moveDir = deltaMove / moveDist;

        if (capsuleCol == null) capsuleCol = GetComponent<CapsuleCollider>();

        Vector3 p1, p2;
        float radius;
        
        // เพิ่มระยะก้าวข้าม (Step Height)
        float stepHeight = 0.5f; 

        if (capsuleCol != null)
        {
            Vector3 center = transform.position + capsuleCol.center;
            float halfHeight = Mathf.Max(0.05f, (capsuleCol.height * 0.5f) - capsuleCol.radius);
            p1 = center + Vector3.up * halfHeight;
            p2 = center - Vector3.up * Mathf.Max(0f, halfHeight - stepHeight); 
            radius = Mathf.Max(0.15f, capsuleCol.radius * 0.92f);
        }
        else
        {
            Vector3 center = transform.position + Vector3.up * 1f;
            p1 = center + Vector3.up * 0.45f;
            p2 = center - Vector3.up * Mathf.Max(0f, 0.45f - stepHeight);
            radius = 0.4f;
        }

        float sweepDist = moveDist + skinWidth;
        RaycastHit[] hits = Physics.CapsuleCastAll(p1, p2, radius, moveDir, sweepDist, obstacleLayers, QueryTriggerInteraction.Ignore);

        RaycastHit closestHit = default;
        float closestDist = float.MaxValue;
        bool hasHit = false;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit h = hits[i];
            Collider c = h.collider;
            if (c == null || c.isTrigger) continue;
            if (c.transform.root == transform.root) continue;
            if (c.CompareTag("Enemy") || c.CompareTag("Player")) continue;

            // มองข้ามการชนพื้นหรือเนิน
            if (h.normal.y > 0.65f) continue;

            if (h.distance < closestDist)
            {
                closestDist = h.distance;
                closestHit = h;
                hasHit = true;
            }
        }

        if (!hasHit)
        {
            transform.position += deltaMove;
        }
        else
        {
            float allowedDist = Mathf.Max(0f, closestDist - skinWidth);
            transform.position += moveDir * allowedDist;

            float remainingDist = moveDist - allowedDist;
            if (remainingDist > 0.001f && closestHit.normal.sqrMagnitude > 0.01f)
            {
                Vector3 slideDir = Vector3.ProjectOnPlane(moveDir, closestHit.normal);
                slideDir.y = 0;
                if (slideDir.sqrMagnitude > 0.001f)
                {
                    slideDir.Normalize();
                    Vector3 slideMove = slideDir * remainingDist;

                    Vector3 newP1 = p1 + moveDir * allowedDist;
                    Vector3 newP2 = p2 + moveDir * allowedDist;
                    float slideCheckDist = slideMove.magnitude + skinWidth;

                    RaycastHit[] slideHits = Physics.CapsuleCastAll(newP1, newP2, radius, slideDir, slideCheckDist, obstacleLayers, QueryTriggerInteraction.Ignore);
                    float closestSlideDist = float.MaxValue;
                    bool slideBlocked = false;

                    for (int j = 0; j < slideHits.Length; j++)
                    {
                        RaycastHit sh = slideHits[j];
                        Collider sc = sh.collider;
                        if (sc == null || sc.isTrigger) continue;
                        if (sc.transform.root == transform.root) continue;
                        if (sc.CompareTag("Enemy") || sc.CompareTag("Player")) continue;

                        if (sh.normal.y > 0.65f) continue;

                        if (sh.distance < closestSlideDist)
                        {
                            closestSlideDist = sh.distance;
                            slideBlocked = true;
                        }
                    }

                    if (!slideBlocked)
                    {
                        transform.position += slideMove;
                    }
                    else if (closestSlideDist > skinWidth)
                    {
                        transform.position += slideDir * Mathf.Max(0f, closestSlideDist - skinWidth);
                    }
                }
            }
        }
    }

    public virtual void SnapToGround()
    {
        if (!snapToGround) return;

        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            return;
        }

        Vector3 rayStart = new Vector3(transform.position.x, transform.position.y + raycastStartHeight, transform.position.z);
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, groundCheckDistance + raycastStartHeight, Physics.AllLayers, QueryTriggerInteraction.Ignore);

        float highestGroundY = float.MinValue;
        bool foundGround = false;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            Collider col = hit.collider;
            if (col == null || col.isTrigger) continue;
            if (col.transform.root == transform.root) continue;
            if (col.CompareTag("Enemy") || col.CompareTag("Player")) continue;

            if (hit.point.y > highestGroundY)
            {
                highestGroundY = hit.point.y;
                foundGround = true;
            }
        }

        if (foundGround)
        {
            Vector3 pos = transform.position;
            pos.y = highestGroundY;
            transform.position = pos;
        }
        else if (transform.position.y > 0f)
        {
            Vector3 pos = transform.position;
            pos.y = 0f;
            transform.position = pos;
        }
    }

    protected virtual void OnReachPlayer()
    {
        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;

            if (animator != null && HasAnimatorParameter(animator, "Attack"))
            {
                animator.SetTrigger("Attack");
            }

            if (player != null)
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
    }

    public virtual void DropLoot()
    {
        if (dropItemPrefab != null && Random.value <= dropChance)
        {
            Instantiate(dropItemPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }
    }

    protected bool HasAnimatorParameter(Animator anim, string paramName)
    {
        if (anim == null || anim.runtimeAnimatorController == null) return false;
        foreach (var param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }
}