using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public EnemyStats stats;

    private Transform playerTransform;
    private PlayerHealth playerHealth;
    private NavMeshAgent agent;
    private float nextAttackTime = 0f;
    private bool isAttacking = false;
    private Vector3 initialScale;

    // เพิ่มตัวแปร Animator
    private Animator anim;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null && !agent.isOnNavMesh)
        {
            agent.enabled = false;
        }
        initialScale = transform.localScale;

        // ดึง Animator ที่ติดอยู่กับตัว หรือโมเดลลูก
        anim = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        FindPlayer();
    }

    public void Initialize(EnemyStats enemyStats)
    {
        stats = enemyStats;

        if (agent != null)
        {
            agent.speed = stats.moveSpeed;
            agent.stoppingDistance = stats.attackRange * 0.85f;
        }

        FindPlayer();
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            var pCtrl = FindAnyObjectByType<PlayerController>();
            if (pCtrl != null) playerObj = pCtrl.gameObject;
        }

        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
        }
    }

    void Update()
    {
        if (stats == null) return;
        if (playerTransform == null || (playerHealth != null && playerHealth.isDead))
        {
            UpdateWalkAnimation(false);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= stats.attackRange)
        {
            // Within attack range: Face player and attack
            Vector3 lookPos = playerTransform.position;
            lookPos.y = transform.position.y;
            transform.LookAt(lookPos);

            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }

            UpdateWalkAnimation(false);

            if (Time.time >= nextAttackTime && !isAttacking)
            {
                StartCoroutine(AttackRoutine());
            }
        }
        else
        {
            // Pursue player
            if (!isAttacking)
            {
                MoveTowardsPlayer();
                UpdateWalkAnimation(true);
            }
        }
    }

    void MoveTowardsPlayer()
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(playerTransform.position);
        }
        else
        {
            // Direct movement fallback
            Vector3 targetPos = playerTransform.position;
            Vector3 dir = (targetPos - transform.position);
            dir.y = 0;

            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
                Vector3 newPos = transform.position + transform.forward * stats.moveSpeed * Time.deltaTime;
                newPos.y = 0.5f;
                transform.position = newPos;
            }
        }
    }

    void UpdateWalkAnimation(bool isMoving)
    {
        if (anim != null)
        {
            // ถ้ามี Animator ให้ส่งค่าความเร็ว/สถานะเดินไปที่ Animator Controller (ปรับชื่อ Parameter ตามที่คุณตั้งไว้ เช่น "IsWalking" หรือ "Speed")
            anim.SetBool("IsWalking", isMoving);
        }
        else if (isMoving)
        {
            // ถ้าไม่มี Animator ให้ใช้ procedural walking bob เดิม
            float bob = Mathf.Sin(Time.time * stats.moveSpeed * 2.5f) * 0.05f;
            float roll = Mathf.Cos(Time.time * stats.moveSpeed * 2f) * 2.5f;

            Transform visual = transform.Find("Visual_" + stats.type);
            if (visual != null)
            {
                visual.localPosition = new Vector3(0, Mathf.Abs(bob), 0);
                visual.localRotation = Quaternion.Euler(0, 0, roll);
            }
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        nextAttackTime = Time.time + stats.attackCooldown;

        if (anim != null)
        {
            // สั่งเล่น Animation ท่าโจมตี (ตั้งชื่อ Parameter ใน Animator Controller เป็น Trigger ชื่อ "Attack")
            anim.SetTrigger("Attack");
        }

        Transform visual = transform.Find("Visual_" + stats.type);
        Vector3 originalPos = visual != null ? visual.localPosition : Vector3.zero;

        // ถ้าไม่มี Animator ค่อยใช้ Code โยกตัว (Windup & Lunge)
        if (anim == null && visual != null)
        {
            visual.localPosition = originalPos - Vector3.forward * 0.15f;
        }

        yield return new WaitForSeconds(0.12f);

        if (anim == null && visual != null)
        {
            visual.localPosition = originalPos + Vector3.forward * 0.35f;
        }

        // Deal damage
        if (playerHealth != null && Vector3.Distance(transform.position, playerTransform.position) <= stats.attackRange * 1.35f)
        {
            playerHealth.TakeDamage(stats.damage);
        }

        yield return new WaitForSeconds(0.15f);

        if (anim == null && visual != null)
        {
            visual.localPosition = originalPos;
        }

        isAttacking = false;
    }
}