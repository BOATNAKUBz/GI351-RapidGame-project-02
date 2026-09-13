using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    private AudioSource audioSource;

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
            float freq = (t < 0.12f) ? 587.33f : 880f; // D5 to A5
            return Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t * 8f) * 0.35f;
        });

        pickupAmmoClip = CreateProceduralClip("AmmoPickup", 0.2f, (t, dur) =>
        {
            float freq = (t < 0.1f) ? 440f : 659.25f; // A4 to E5
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

    public void PlayShoot() => PlayOneShot(shootClip, 0.6f);
    public void PlayHitMarker() => PlayOneShot(hitMarkerClip, 0.4f);
    public void PlayEnemyHurt() => PlayOneShot(enemyHurtClip, 0.4f);
    public void PlayEnemyDeath() => PlayOneShot(enemyDeathClip, 0.5f);
    public void PlayPlayerHurt() => PlayOneShot(playerHurtClip, 0.7f);
    public void PlayMedkit() => PlayOneShot(pickupMedkitClip, 0.5f);
    public void PlayAmmo() => PlayOneShot(pickupAmmoClip, 0.5f);
    public void PlayWaveStart() => PlayOneShot(waveStartClip, 0.6f);
    public void PlayVictory() => PlayOneShot(victoryClip, 0.7f);
    public void PlayGameOver() => PlayOneShot(gameOverClip, 0.7f);
    public void PlayEmptyClick() => PlayOneShot(emptyClickClip, 0.4f);

    private void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
