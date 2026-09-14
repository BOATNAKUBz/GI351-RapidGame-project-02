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

        // ถ้าเข้าไปถึงระยะโจมตีแล้ว
        if (distance <= attackRange)
        {
            OnReachPlayer();
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