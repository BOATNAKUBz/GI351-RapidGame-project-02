using System.Collections;
using UnityEngine;

public class ZombieAI : EnemyAI
{
    [Header("Zombie Behavior")]
    public float wobbleSpeed = 6f;
    public float wobbleAngle = 6f;
    public float groanInterval = 7f;

    private float nextGroanTime = 0f;
    private Transform visualChild;
    private Quaternion visualInitialRot = Quaternion.identity;

    protected override void Start()
    {
        base.Start();

        if (moveSpeed <= 3.5f) moveSpeed = 3.2f;
        attackDamage = 16f;
        attackCooldown = 1.1f;
        attackRange = 2.2f;
        stoppingDistance = 1.2f;

        // Find visual child for shambling wobble animation
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
        base.Update();

        // Procedural shambling zombie limp & sway
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

        // Periodic zombie guttural sounds
        if (Time.time >= nextGroanTime && player != null)
        {
            nextGroanTime = Time.time + Random.Range(groanInterval * 0.7f, groanInterval * 1.4f);
            if (Vector3.Distance(transform.position, player.position) < 25f)
            {
                SoundManager.Instance?.PlayEnemyHurt(); // Pitch-shifted zombie sound
            }
        }
    }

    protected override void OnReachPlayer()
    {
        base.OnReachPlayer();

        // Lunge visual bite attack
        if (visualChild != null)
        {
            StartCoroutine(LungeAttackVisual());
        }
    }

    private IEnumerator LungeAttackVisual()
    {
        if (visualChild == null) yield break;
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
}
