using UnityEngine;

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

    protected Animator animator;

    protected virtual void Start()
    {
        // Unique angle around the player so each enemy approaches from a different side
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

        // 3. ตรวจสอบ NavMeshAgent
        var navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null && !navAgent.isOnNavMesh)
        {
            navAgent.enabled = false;
        }

        // 4. ติดพื้นทันทีตอนเกิด
        if (snapToGround)
        {
            SnapToGround();
        }

        // 5. ปรับความสมดุลของ stoppingDistance และ attackRange เพื่อไม่ให้หยุดก่อนถึงระยะโจมตี
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

        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = knockbackVelocity;
        }
    }

    protected virtual void Update()
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
                var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                }
            }

            if (snapToGround) SnapToGround();
            return; // ข้ามการเคลื่อนที่ปกติขณะกระเด็น
        }

        if (stunRemaining > 0f)
        {
            stunRemaining -= Time.deltaTime;
        }

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            else return;
        }

        // คำนวณระยะห่างแนวนอน (Horizontal Distance) และแนวตั้ง เพื่อป้องกันปัญหาเรื่องความต่างระดับพื้นหรือ pivot
        Vector3 flatEnemy = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 flatPlayer = new Vector3(player.position.x, 0, player.position.z);
        float horizontalDist = Vector3.Distance(flatEnemy, flatPlayer);
        float verticalDist = Mathf.Abs(transform.position.y - player.position.y);

        bool readyToAttack = (Time.time >= nextAttackTime && stunRemaining <= 0f);

        // 2. ระบบการเคลื่อนที่:
        // ถ้าพร้อมตี ให้พุ่งตรงเข้าหาผู้เล่นโดยตรงเพื่อตีให้โดนอย่างแม่นยำ
        // ถ้าอยู่ในช่วงคูลดาวน์ ให้หมุนวนล้อมรอบผู้เล่นตามรัศมี surroundRadius
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
        targetPos.y = transform.position.y;

        // แรงผลักแยกออกจากศัตรูตัวอื่น (Separation)
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

        // ทิศทางรวมในการเดิน (หากพร้อมโจมตี ลดแรงผลักของเพื่อนลงเพื่อไม่ให้เพื่อนเบียดจนตีไม่ถึงตัว)
        Vector3 toTarget = (targetPos - transform.position);
        toTarget.y = 0;
        float currentSepWeight = readyToAttack ? (separationWeight * 0.25f) : separationWeight;
        Vector3 desiredMoveDir = (toTarget.normalized + separationForce * currentSepWeight).normalized;

        if (horizontalDist > stoppingDistance)
        {
            if (desiredMoveDir != Vector3.zero)
            {
                // หันหน้าไปยังทิศทางที่จะเดิน
                Quaternion targetRot = Quaternion.LookRotation(desiredMoveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);

                // เคลื่อนที่ตามทิศทาง
                transform.position += desiredMoveDir * moveSpeed * Time.deltaTime;
            }
        }
        else
        {
            // หันหน้าประจันกับผู้เล่นโดยตรงเมื่อเข้ามาอยู่ในระยะ
            Vector3 lookTarget = new Vector3(player.position.x, transform.position.y, player.position.z);
            Vector3 lookDir = (lookTarget - transform.position).normalized;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), 12f * Time.deltaTime);
            }
        }

        // ยึดติดพื้นเสมอ
        if (snapToGround)
        {
            SnapToGround();
        }

        // โจมตีเมื่ออยู่ในระยะแนวนอนและแนวตั้ง และไม่ติดสตัน
        if (horizontalDist <= attackRange && verticalDist <= 2.5f && stunRemaining <= 0f)
        {
            OnReachPlayer();
        }
    }

    /// <summary>
    /// ฟังก์ชันยึดศัตรูให้ติดพื้นเสมอ โดยยิง Raycast ลงข้างล่าง
    /// </summary>
    public virtual void SnapToGround()
    {
        if (!snapToGround) return;

        var navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
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

            // ไม่นับชนตัวเองหรือชิ้นส่วนของตัวเอง
            if (col.transform.root == transform.root) continue;

            // ไม่นับชนศัตรูตัวอื่นหรือผู้เล่น
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
            // Fallback ถ้าอยู่นอกระยะหรือไม่มี Collider แต่ลอยอยู่
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

            if (animator != null)
            {
                try { animator.SetTrigger("Attack"); } catch { }
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

    // ฟังก์ชัน DropLoot
    public virtual void DropLoot()
    {
        if (dropItemPrefab != null && Random.value <= dropChance)
        {
            Instantiate(dropItemPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }
    }
}