using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    private AudioSource audioSource;

    [Header("Custom Sound Clips (Leave empty to use Procedural Audio)")]
    public AudioClip customShootClip;
    public AudioClip customHitMarkerClip;
    public AudioClip customEnemyHurtClip;
    public AudioClip customEnemyDeathClip;
    public AudioClip customPlayerHurtClip;
    public AudioClip customPickupMedkitClip;
    public AudioClip customPickupAmmoClip;
    public AudioClip customWaveStartClip;
    public AudioClip customVictoryClip;
    public AudioClip customGameOverClip;
    public AudioClip customEmptyClickClip;
    public AudioClip customShotgunShootClip;
    public AudioClip customRevolverShootClip;
    public AudioClip customShotgunPumpClip;
    public AudioClip customAxeSwingClip;
    public AudioClip customAxeHitClip;
    public AudioClip customWeaponPickupClip;
    public AudioClip customDashClip;

    // Procedural Clips (Fallback)
    private AudioClip shootClip;
    private AudioClip hitMarkerClip;
    private AudioClip enemyHurtClip;
    private AudioClip enemyDeathClip;
    private AudioClip playerHurtClip;
    private AudioClip pickupMedkitClip;
    private AudioClip pickupAmmoClip;
    private AudioClip waveStartClip;
    private AudioClip victoryClip;
    private AudioClip gameOverClip;
    private AudioClip emptyClickClip;
    private AudioClip shotgunShootClip;
    private AudioClip revolverShootClip;
    private AudioClip shotgunPumpClip;
    private AudioClip axeSwingClip;
    private AudioClip axeHitClip;
    private AudioClip weaponPickupClip;
    private AudioClip dashClip;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D Sound for UI/Player

        GenerateClips();
    }

    private void GenerateClips()
    {
        shootClip = CreateProceduralClip("Shoot", 0.18f, (t, dur) =>
        {
            float noise = (Random.value * 2f - 1f) * Mathf.Exp(-t * 28f);
            float punch = Mathf.Sin(2f * Mathf.PI * 140f * (1f - t / dur) * t) * Mathf.Exp(-t * 18f);
            return Mathf.Clamp(noise * 0.7f + punch * 0.5f, -1f, 1f);
        });

        hitMarkerClip = CreateProceduralClip("HitMarker", 0.08f, (t, dur) =>
        {
            float tone = Mathf.Sin(2f * Mathf.PI * 1800f * t) * Mathf.Exp(-t * 35f);
            return tone * 0.4f;
        });

        enemyHurtClip = CreateProceduralClip("EnemyHurt", 0.14f, (t, dur) =>
        {
            float noise = (Random.value * 2f - 1f) * 0.4f;
            float low = Mathf.Sin(2f * Mathf.PI * 90f * t);
            return (noise + low) * Mathf.Exp(-t * 18f) * 0.5f;
        });

        enemyDeathClip = CreateProceduralClip("EnemyDeath", 0.35f, (t, dur) =>
        {
            float freq = Mathf.Lerp(180f, 40f, t / dur);
            float rumble = Mathf.Sin(2f * Mathf.PI * freq * t) * (Random.value * 0.6f + 0.4f);
            return rumble * Mathf.Exp(-t * 6f) * 0.6f;
        });

        playerHurtClip = CreateProceduralClip("PlayerHurt", 0.22f, (t, dur) =>
        {
            float thud = Mathf.Sin(2f * Mathf.PI * 75f * t) * Mathf.Exp(-t * 12f);
            float pain = Mathf.Sin(2f * Mathf.PI * 220f * t) * Mathf.Exp(-t * 16f) * 0.4f;
            return (thud + pain) * 0.7f;
        });

        pickupMedkitClip = CreateProceduralClip("MedkitPickup", 0.25f, (t, dur) =>
        {
            float freq = (t < 0.12f) ? 587.33f : 880f;
            return Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t * 8f) * 0.35f;
        });

        pickupAmmoClip = CreateProceduralClip("AmmoPickup", 0.2f, (t, dur) =>
        {
            float freq = (t < 0.1f) ? 440f : 659.25f;
            return Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t * 10f) * 0.35f;
        });

        waveStartClip = CreateProceduralClip("WaveStart", 0.6f, (t, dur) =>
        {
            float freq = (t < 0.2f) ? 330f : (t < 0.4f) ? 440f : 660f;
            return Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-(t % 0.2f) * 6f) * 0.4f;
        });

        victoryClip = CreateProceduralClip("Victory", 1.2f, (t, dur) =>
        {
            float f = (t < 0.25f) ? 523.25f : (t < 0.5f) ? 659.25f : (t < 0.75f) ? 783.99f : 1046.50f;
            return Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-(t % 0.25f) * 5f) * 0.45f;
        });

        gameOverClip = CreateProceduralClip("GameOver", 1.0f, (t, dur) =>
        {
            float f = Mathf.Lerp(300f, 65f, t / dur);
            return Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 3f) * 0.5f;
        });

        emptyClickClip = CreateProceduralClip("EmptyClick", 0.05f, (t, dur) =>
        {
            return (Random.value * 2f - 1f) * Mathf.Exp(-t * 60f) * 0.3f;
        });

        shotgunShootClip = CreateProceduralClip("ShotgunShoot", 0.28f, (t, dur) =>
        {
            float noise = (Random.value * 2f - 1f) * Mathf.Exp(-t * 22f);
            float boom = Mathf.Sin(2f * Mathf.PI * 75f * (1f - t / dur) * t) * Mathf.Exp(-t * 12f);
            float punch = Mathf.Sin(2f * Mathf.PI * 180f * (1f - t / dur) * t) * Mathf.Exp(-t * 26f);
            return Mathf.Clamp(noise * 0.85f + boom * 0.8f + punch * 0.4f, -1f, 1f);
        });

        revolverShootClip = CreateProceduralClip("RevolverShoot", 0.22f, (t, dur) =>
        {
            float crack = (Random.value * 2f - 1f) * Mathf.Exp(-t * 32f);
            float pop = Mathf.Sin(2f * Mathf.PI * 220f * (1f - t / dur) * t) * Mathf.Exp(-t * 20f);
            return Mathf.Clamp(crack * 0.8f + pop * 0.6f, -1f, 1f);
        });

        shotgunPumpClip = CreateProceduralClip("ShotgunPump", 0.25f, (t, dur) =>
        {
            float click1 = (t < 0.1f) ? (Random.value * 2f - 1f) * Mathf.Exp(-t * 50f) * 0.7f : 0f;
            float click2 = (t > 0.14f && t < 0.24f) ? (Random.value * 2f - 1f) * Mathf.Exp(-(t - 0.14f) * 50f) * 0.7f : 0f;
            return Mathf.Clamp(click1 + click2, -1f, 1f);
        });

        axeSwingClip = CreateProceduralClip("AxeSwing", 0.26f, (t, dur) =>
        {
            float f = Mathf.Lerp(120f, 380f, t / dur);
            float whoosh = Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Sin(Mathf.PI * (t / dur));
            float air = (Random.value * 2f - 1f) * Mathf.Sin(Mathf.PI * (t / dur)) * 0.35f;
            return (whoosh * 0.6f + air) * 0.7f;
        });

        axeHitClip = CreateProceduralClip("AxeHit", 0.24f, (t, dur) =>
        {
            float flesh = Mathf.Sin(2f * Mathf.PI * 90f * t) * Mathf.Exp(-t * 18f);
            float chop = (Random.value * 2f - 1f) * Mathf.Exp(-t * 30f);
            return Mathf.Clamp(flesh * 0.7f + chop * 0.7f, -1f, 1f);
        });

        weaponPickupClip = CreateProceduralClip("WeaponPickup", 0.24f, (t, dur) =>
        {
            float tone1 = (t < 0.1f) ? Mathf.Sin(2f * Mathf.PI * 520f * t) : 0f;
            float tone2 = (t >= 0.1f) ? Mathf.Sin(2f * Mathf.PI * 780f * t) : 0f;
            float click = (Random.value * 2f - 1f) * Mathf.Exp(-t * 40f) * 0.3f;
            return Mathf.Clamp((tone1 + tone2) * 0.4f + click, -1f, 1f);
        });

        dashClip = CreateProceduralClip("Dash", 0.2f, (t, dur) =>
        {
            float freq = Mathf.Lerp(450f, 120f, t / dur);
            float whoosh = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Sin(Mathf.PI * (t / dur));
            float airNoise = (Random.value * 2f - 1f) * Mathf.Sin(Mathf.PI * (t / dur)) * 0.5f;
            return Mathf.Clamp((whoosh * 0.5f + airNoise * 0.5f) * Mathf.Exp(-t * 4f), -1f, 1f);
        });
    }

    private AudioClip CreateProceduralClip(string clipName, float duration, System.Func<float, float, float> sampleFunc)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.FloorToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            samples[i] = sampleFunc(t, duration);
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Public Methods (เช็กว่ามี Custom Clip ไหม ถ้าไม่มีให้ใช้ Procedural Clip)
    public void PlayShoot() => PlayOneShot(customShootClip != null ? customShootClip : shootClip, 0.6f);
    public void PlayShotgunShoot() => PlayOneShot(customShotgunShootClip != null ? customShotgunShootClip : shotgunShootClip, 0.75f);
    public void PlayRevolverShoot() => PlayOneShot(customRevolverShootClip != null ? customRevolverShootClip : revolverShootClip, 0.65f);
    public void PlayShotgunPump() => PlayOneShot(customShotgunPumpClip != null ? customShotgunPumpClip : shotgunPumpClip, 0.55f);
    public void PlayAxeSwing() => PlayOneShot(customAxeSwingClip != null ? customAxeSwingClip : axeSwingClip, 0.6f);
    public void PlayAxeHit() => PlayOneShot(customAxeHitClip != null ? customAxeHitClip : axeHitClip, 0.7f);
    public void PlayWeaponPickup() => PlayOneShot(customWeaponPickupClip != null ? customWeaponPickupClip : weaponPickupClip, 0.6f);
    public void PlayHitMarker() => PlayOneShot(customHitMarkerClip != null ? customHitMarkerClip : hitMarkerClip, 0.4f);
    public void PlayEnemyHurt() => PlayOneShot(customEnemyHurtClip != null ? customEnemyHurtClip : enemyHurtClip, 0.4f);
    public void PlayEnemyDeath() => PlayOneShot(customEnemyDeathClip != null ? customEnemyDeathClip : enemyDeathClip, 0.5f);
    public void PlayPlayerHurt() => PlayOneShot(customPlayerHurtClip != null ? customPlayerHurtClip : playerHurtClip, 0.7f);
    public void PlayMedkit() => PlayOneShot(customPickupMedkitClip != null ? customPickupMedkitClip : pickupMedkitClip, 0.5f);
    public void PlayAmmo() => PlayOneShot(customPickupAmmoClip != null ? customPickupAmmoClip : pickupAmmoClip, 0.5f);
    public void PlayWaveStart() => PlayOneShot(customWaveStartClip != null ? customWaveStartClip : waveStartClip, 0.6f);
    public void PlayVictory() => PlayOneShot(customVictoryClip != null ? customVictoryClip : victoryClip, 0.7f);
    public void PlayGameOver() => PlayOneShot(customGameOverClip != null ? customGameOverClip : gameOverClip, 1.0f);
    public void PlayEmptyClick() => PlayOneShot(customEmptyClickClip != null ? customEmptyClickClip : emptyClickClip, 0.4f);
    public void PlayDash() => PlayOneShot(customDashClip != null ? customDashClip : dashClip, 0.65f);

    private void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
}