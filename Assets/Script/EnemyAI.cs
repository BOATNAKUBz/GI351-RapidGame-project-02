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

    [Header("Ground Alignment")]
    public bool snapToGround = true;
    public float groundCheckDistance = 10f;
    public float raycastStartHeight = 1.5f;

    protected Animator animator;

    protected virtual void Start()
    {
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

        // 3. ตรวจสอบ NavMeshAgent (ถ้าไม่มี NavMesh ให้ปิดเพื่อไม่ให้ตีกับ transform.position)
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
    }

    protected virtual void Update()
    {
        if (player == null) return;

        // คำนวณระยะห่างระหว่าง Enemy กับ Player
        float distance = Vector3.Distance(transform.position, player.position);

        // ถ้ายังอยู่นอกระยะโจมตี ให้เดินเข้าไปหา
        if (distance > stoppingDistance)
        {
            // หันหน้าเข้าหา Player (ล็อคแกน Y ไม่ให้ตัวเอียง)
            Vector3 targetPosition = new Vector3(player.position.x, transform.position.y, player.position.z);
            transform.LookAt(targetPosition);

            // เคลื่อนที่พุ่งตรงเข้าหาผู้เล่น
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        }

        // ยึดติดพื้นเสมอ
        if (snapToGround)
        {
            SnapToGround();
        }

        // ถ้าเข้าไปถึงระยะโจมตีแล้ว
        if (distance <= attackRange)
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
        // โค้ดโจมตีพื้นฐาน (สามารถเขียนทับในคลาสลูกได้ เช่น ExploderAI)
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