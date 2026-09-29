using System.Collections;
using UnityEngine;

// ตรวจสอบให้แน่ใจว่าคลาสนี้สืบทอดมาจาก EnemyAI ของคุณ
public class ExploderAI : EnemyAI
{
    [Header("Explode Settings")]
    [Tooltip("รัศมีการระเบิดแบบวงกว้าง (AOE)")]
    public float explosionRadius = 4f;
    [Tooltip("ดาเมจเมื่อระเบิด")]
    public float explosionDamage = 80f;
    [Tooltip("เวลาจุดชนวนก่อนระเบิด (วินาที)")]
    public float fuseTime = 0.5f;
    [Tooltip("เอฟเฟกต์ Particle เมื่อระเบิด")]
    public GameObject explosionFX;
    private bool isExploding = false;

    [Header("Advanced Human Procedural Walk")]
    [Tooltip("ความเร็วในการก้าวเท้า (จังหวะ Sin) - ยิ่งเยอะยิ่งเดินถี่")]
    public float walkCycleSpeed = 10f;

    [Tooltip("ระยะตัวเด้งขึ้นลงตามก้าวเดิน (Vertical Bob)")]
    public float bobAmount = 0.06f;

    [Tooltip("ระยะตัวโยกไปซ้ายขวา (Sideways Sway)")]
    public float swayAmount = 0.03f;

    [Tooltip("องศาไหล่เอียงซ้ายขวาสลับกันตามจังหวะเดิน (Side Tilt)")]
    public float tiltAngle = 3f;

    [Tooltip("องศาตัวโน้มไปข้างหน้าเล็กน้อยเวลาเดิน (Forward Lean)")]
    public float leanAngle = 5f;

    private Transform visualChild;
    private Quaternion visualInitialRot = Quaternion.identity;
    private Vector3 visualInitialPos = Vector3.zero;

    protected override void Start()
    {
        base.Start();

        // ค้นหา Visual Child เหมือน ZombieAI
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("Visual_"))
            {
                visualChild = child;
                visualInitialRot = visualChild.localRotation;
                visualInitialPos = visualChild.localPosition;
                break;
            }
        }

        // แจ้งเตือนถ้าหา Visual ไม่เจอ
        if (visualChild == null)
        {
            Debug.LogError($"[ExploderAI] Cannot find child object starting with 'Visual_' on {gameObject.name}. Procedural animation will not work.");
        }
    }

    protected override void Update()
    {
        base.Update();

        // อนิเมชั่นเดินมนุษย์ (ถ้ายังไม่เริ่มระเบิด, ไม่ถูกชน, และกำลังเดิน)
        if (visualChild != null && !isExploding && !isKnockedBack && player != null)
        {
            // ใช้ฟังก์ชันตรวจสอบความเร็วจากการเคลื่อนที่จริง (ถ้า EnemyAI มี)
            // หรือตรวจสอบระยะห่างแทน
            Vector3 flatEnemy = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 flatPlayer = new Vector3(player.position.x, 0, player.position.z);
            float dist = Vector3.Distance(flatEnemy, flatPlayer);

            // ตรวจสอบว่ากำลังเคลื่อนที่อยู่หรือไม่
            bool isActuallyMoving = dist > stoppingDistance && stunRemaining <= 0f && moveSpeed > 0.1f;

            if (isActuallyMoving)
            {
                // -- คํานวณจังหวะก้าวเดินโดยใช้เวลา --
                float time = Time.time * walkCycleSpeed;

                // 1. การเด้งขึ้นลง (Vertical Bobing - Sin จังหวะคู่)
                // คนเราเดินเด้งขึ้นเมื่อเท้าทั้งสองเหยียบพื้น
                float bob = Mathf.Abs(Mathf.Sin(time)) * bobAmount;

                // 2. การโยกซ้ายขวา (Sideways Sway - Sin จังหวะเดี่ยว)
                // คนเราเดินน้ำหนักลงเท้าซ้าย/ขวาสลับกัน
                float sway = Mathf.Cos(time) * swayAmount;

                // 3. การเอียงตัวซ้ายขวา (Side Tilt - Sin จังหวะเดี่ยว)
                // สลับไหล่ซ้ายขวาตามจังหวะเท้า
                float tilt = Mathf.Cos(time) * tiltAngle;

                // 4. การโน้มตัวไปข้างหน้า (Forward Lean)
                // คนเวลาเดินจะโน้มตัวไปข้างหน้าเล็กน้อย
                float lean = leanAngle;

                // -- นำค่าที่ได้มาประยุกต์ใช้ --
                // ปรับตำแหน่ง (localPosition)
                visualChild.localPosition = visualInitialPos + new Vector3(sway, bob, 0);

                // ปรับองศา (localRotation)
                visualChild.localRotation = visualInitialRot * Quaternion.Euler(lean, 0, tilt);
            }
            else
            {
                // คืนค่าตำแหน่ง/องศาเดิมเมื่อหยุดเดิน อย่างนุ่มนวล
                visualChild.localPosition = Vector3.Lerp(visualChild.localPosition, visualInitialPos, 10f * Time.deltaTime);
                visualChild.localRotation = Quaternion.Slerp(visualChild.localRotation, visualInitialRot, 10f * Time.deltaTime);
            }
        }
    }

    protected override void OnReachPlayer()
    {
        // หยุด base.OnReachPlayer() ไม่ให้ใช้ดาเมจตีปกติ

        if (!isExploding)
        {
            StartCoroutine(ExplodeRoutine());
        }
    }

    private IEnumerator ExplodeRoutine()
    {
        isExploding = true;
        moveSpeed = 0f; // หยุดเดินทันที

        // คืนค่าตำแหน่ง Visual ให้อยู่ตรงกลางเพื่อให้ดูเตรียมตัวระเบิด
        if (visualChild != null)
        {
            visualChild.localPosition = visualInitialPos;
            visualChild.localRotation = visualInitialRot;
        }

        // เล่นอนิเมชั่น "ระเบิด" (ถ้า EnemyAI มี Animator ให้เรียกใช้)
        if (animator != null)
        {
            try { animator.SetTrigger("Attack"); } catch { }
        }

        yield return new WaitForSeconds(fuseTime);

        // แสดงเอฟเฟกต์ระเบิด
        if (explosionFX != null)
        {
            Instantiate(explosionFX, transform.position, Quaternion.identity);
        }

        // คำนวณดาเมจวงกว้าง
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hit in hits)
        {
            // ตรวจสอบ PlayerHealth จาก Collider (รวมถึงในวัตถุลูก)
            PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>() ?? hit.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(explosionDamage);
            }
        }

        // ฟังก์ชันเสริมของ EnemyAI
        DropLoot();

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnEnemyKilled(gameObject);
        }

        // ทำลายตัวเอง
        Destroy(gameObject);
    }

    // วาดวงกลมรัศมีระเบิดในหน้าต่าง Scene
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}