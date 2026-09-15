using System.Collections;
using UnityEngine;

public class ExploderAI : EnemyAI
{
    [Header("Explode Settings")]
    public float explosionRadius = 4f;
    public float explosionDamage = 80f;
    public float fuseTime = 0.5f;
    public GameObject explosionFX; // Optional: เผื่ออยากใส่ Particle ตอนระเบิด
    private bool isExploding = false;

    protected override void OnReachPlayer()
    {
        // ตัด base.OnReachPlayer(); ออก เพื่อไม่ให้ตีธรรมดาก่อนระเบิด

        if (!isExploding)
        {
            StartCoroutine(ExplodeRoutine());
        }
    }

    private IEnumerator ExplodeRoutine()
    {
        isExploding = true;
        moveSpeed = 0f; // หยุดเดินเมื่อเริ่มจุดชนวน

        if (animator != null)
        {
            try { animator.SetTrigger("Attack"); } catch { }
        }

        yield return new WaitForSeconds(fuseTime);

        // แสดงเอฟเฟกต์ระเบิด (ถ้ามี)
        if (explosionFX != null)
        {
            Instantiate(explosionFX, transform.position, Quaternion.identity);
        }

        // คำนวณดาเมจวงกว้าง (AOE)
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hit in hits)
        {
            PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>() ?? hit.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(explosionDamage);
            }
        }

        // ดรอปไอเทมก่อนทำลายตัวเอง
        DropLoot();

        // แจ้ง WaveManager ว่าศัตรูตายแล้ว
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnEnemyKilled(gameObject);
        }

        Destroy(gameObject);
    }

    // วาดวงกลมรัศมีระเบิดในหน้าต่าง Scene เพื่อให้กะระยะได้ง่ายขึ้น
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}