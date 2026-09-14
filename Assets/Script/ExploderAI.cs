using System.Collections;
using UnityEngine;

public class ExploderAI : EnemyAI
{
    [Header("Explode Settings")]
    public float explosionRadius = 4f;
    public float explosionDamage = 80f;
    public float fuseTime = 0.5f;
    private bool isExploding = false;

    protected override void OnReachPlayer()
    {
        base.OnReachPlayer();

        if (!isExploding)
        {
            StartCoroutine(ExplodeRoutine());
        }
    }

    private IEnumerator ExplodeRoutine()
    {
        isExploding = true;
        moveSpeed = 0; // หยุดเดินเมื่อเริ่มจุดชนวน

        // เช็ค Component Animator ของตัวเองโดยตรงเพื่อป้องกัน Error
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("Attack");
        }

        yield return new WaitForSeconds(fuseTime);

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
}