using System.Collections;
using UnityEngine;

public class ZombieAI : EnemyAI
{
    [Header("Zombie Behavior")]
    public float wobbleSpeed = 6f;
    public float wobbleAngle = 6f;
    public float groanInterval = 7f;

    [System.Serializable]
    public class ZombieLoot
    {
    public GameObject itemPrefab;
    [Range(0f, 1f)] public float dropChance = 0.5f;
    }

    [Header("Attack Post-Pause Settings")]
    [Tooltip("ระยะเวลาหยุดเดินหลังโจมตีเสร็จ (วินาที)")]
    public float postAttackPauseDuration = 1.2f;

    [Tooltip("มุมเอียงก้มตัว/แขนตกขณะพัก (องศา)")]
    public float droopAngle = 25f;

    [Tooltip("ความเร็วในการหันหน้ามองตามผู้เล่นขณะหยุดพัก")]
    public float lookAtPlayerSpeed = 6f;

    [Header("Audio Settings")]
    [Tooltip("ลำโพง AudioSource (หากไม่ใส่ ระบบจะหาอัตโนมัติจากตัวมันเอง)")]
    public AudioSource audioSource;

    public ZombieLoot[] extraLoots;

    [Tooltip("คลิปเสียงคำรามเล่นวนตามช่วงเวลา")]
    public AudioClip[] groanClips;

    [Tooltip("คลิปเสียงตอนพุ่งกัด / โจมตี")]
    public AudioClip attackClip;

    private float nextGroanTime = 0f;
    private Transform visualChild;
    private Quaternion visualInitialRot = Quaternion.identity;

    // สถานะสำหรับควบคุมการหยุดเคลื่อนที่หลังโจมตี
    private bool isPostAttackPausing = false;

    protected override void Start()
    {
        base.Start();

        // ตรวจเช็ก AudioSource หากไม่ได้ใส่ไว้ใน Inspector
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // หา Visual child เพื่อใช้ปรับท่าทาง procedural
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("Visual_"))
            {
                visualChild = child;
                visualInitialRot = visualChild.localRotation;
                break;
            }
        }

        nextGroanTime = Time.time + Random.Range(1f, groanInterval);
    }

    protected override void Update()
    {
        // 1. ขณะหยุดพักหลังโจมตี: ยืนนิ่ง + หันมองผู้เล่น + เอียงก้มตัว/มือตก
        if (isPostAttackPausing)
        {
            if (player != null)
            {
                // หมุนตัวหลัก (Root Transform) ให้หันหน้าไปทางผู้เล่นอย่างนุ่มนวล
                Vector3 lookDir = player.position - transform.position;
                lookDir.y = 0; // ล็อกให้อยู่ในแนวราบ ไม่เอียงตามความสูง
                if (lookDir != Vector3.zero)
                {
                    Quaternion targetLook = Quaternion.LookRotation(lookDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetLook, lookAtPlayerSpeed * Time.deltaTime);
                }
            }

            // ปรับ Visual ให้ก้มตัว/มือตก (เอียง Pitch ลงไปด้านหน้า)
            if (visualChild != null)
            {
                Quaternion droopRot = visualInitialRot * Quaternion.Euler(droopAngle, 0f, 0f);
                visualChild.localRotation = Quaternion.Slerp(visualChild.localRotation, droopRot, 8f * Time.deltaTime);
            }

            return; // ข้ามระบบเดินปกติ
        }

        base.Update();

        // 2. ท่าทางเดินส่ายตามปกติ (Shambling Limp & Sway)
        if (visualChild != null && !isKnockedBack && player != null)
        {
            Vector3 flatEnemy = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 flatPlayer = new Vector3(player.position.x, 0, player.position.z);
            float dist = Vector3.Distance(flatEnemy, flatPlayer);

            if (dist > stoppingDistance && stunRemaining <= 0f)
            {
                float wobble = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAngle;
                float bob = Mathf.Abs(Mathf.Cos(Time.time * wobbleSpeed)) * 0.08f;
                visualChild.localRotation = visualInitialRot * Quaternion.Euler(bob * 20f, 0, wobble);
            }
            else
            {
                visualChild.localRotation = Quaternion.Slerp(visualChild.localRotation, visualInitialRot, 10f * Time.deltaTime);
            }
        }

        // เสียงคำรามซอมบี้
        if (Time.time >= nextGroanTime && player != null)
        {
            nextGroanTime = Time.time + Random.Range(groanInterval * 0.7f, groanInterval * 1.4f);
            if (Vector3.Distance(transform.position, player.position) < 25f)
            {
                PlayGroanSound();
            }
        }
    }

    private void PlayGroanSound()
    {
        // 1. ลองเล่นเสียงจาก Array สุ่มคลิปที่ใส่ใน Inspector ก่อน
        if (groanClips != null && groanClips.Length > 0 && audioSource != null)
        {
            AudioClip randomClip = groanClips[Random.Range(0, groanClips.Length)];
            if (randomClip != null)
            {
                audioSource.PlayOneShot(randomClip);
                return;
            }
        }

        // 2. หากไม่ได้ใส่คลิปไว้ ให้ย้อนกลับไปใช้ SoundManager ตัวเดิม
        SoundManager.Instance?.PlayEnemyHurt();
    }

    protected override void OnReachPlayer()
    {
        base.OnReachPlayer();

        // เริ่มแสดงการพุ่งกัด + พักเอียงมือตกมองตามผู้เล่น
        StartCoroutine(LungeAttackAndPauseRoutine());
    }

    private IEnumerator LungeAttackAndPauseRoutine()
    {
        isPostAttackPausing = true;

        // เล่นเสียงโจมตี/กัด (ถ้ามีใส่ไว้)
        if (attackClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackClip);
        }

        // ขั้นที่ 1: แอนิเมชันพุ่งกัดไปข้างหน้า (Lunge)
        if (visualChild != null)
        {
            Vector3 forwardOffset = visualChild.localPosition + new Vector3(0, 0, 0.25f);
            Vector3 origPos = visualChild.localPosition;

            float t = 0f;
            while (t < 0.15f)
            {
                t += Time.deltaTime;
                visualChild.localPosition = Vector3.Lerp(origPos, forwardOffset, t / 0.15f);
                yield return null;
            }

            t = 0f;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                visualChild.localPosition = Vector3.Lerp(forwardOffset, origPos, t / 0.2f);
                yield return null;
            }

            visualChild.localPosition = origPos;
        }

        // ขั้นที่ 2: หยุดนิ่งตามเวลาที่กำหนด (ช่วงนี้จะเกิดอาการมือตก + หันมองตามผู้เล่นใน Update)
        if (postAttackPauseDuration > 0f)
        {
            yield return new WaitForSeconds(postAttackPauseDuration);
        }

        // คืนค่าตำแหน่งเอียงตัวนุ่มนวลก่อนกลับไปเดิน
        if (visualChild != null)
        {
            float resetTimer = 0f;
            Quaternion currentRot = visualChild.localRotation;
            while (resetTimer < 0.2f)
            {
                resetTimer += Time.deltaTime;
                visualChild.localRotation = Quaternion.Slerp(currentRot, visualInitialRot, resetTimer / 0.2f);
                yield return null;
            }
        }

        isPostAttackPausing = false;
    }

    public override void DropLoot()
    {
        // 1. ดรอปไอเทมชิ้นหลัก (ถ้าคุณมีการใส่ไว้ในช่อง Drop Item Prefab เดิมของ EnemyAI)
        base.DropLoot();

        // 2. ดรอปไอเทมเสริมหลายๆ ชิ้นจาก Array
        if (extraLoots != null && extraLoots.Length > 0)
        {
            foreach (var loot in extraLoots)
            {
                if (loot.itemPrefab != null && Random.value <= loot.dropChance)
                {
                    // สุ่มตำแหน่งกระจายออกด้านข้างเล็กน้อย (รัศมี 0.6 เมตร) เพื่อไม่ให้โมเดลไอเทมซ้อนทับกัน
                    Vector2 offset = Random.insideUnitCircle * 0.6f;
                    Vector3 dropPos = transform.position + new Vector3(offset.x, 0.5f, offset.y);
                    
                    Instantiate(loot.itemPrefab, dropPos, Quaternion.identity);
                }
            }
        }
    }
}