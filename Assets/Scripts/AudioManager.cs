using UnityEngine;

public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const int SampleRate = 22050;

    [Header("Volumes")]
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.18f;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.75f;
    [SerializeField, Range(0f, 1f)] private float engineVolume = 0.28f;
    [SerializeField, Range(0f, 1f)] private float driftVolume = 0.22f;

    [Header("References")]
    [SerializeField] private CarController playerCar;

    private AudioSource musicSource;
    private AudioSource engineSource;
    private AudioSource driftSource;
    private AudioSource sfxSource;
    private AudioClip buttonClip;
    private AudioClip coinClip;
    private AudioClip crashClip;
    private AudioClip driftRewardClip;
    private bool musicEnabled = true;
    private bool soundEnabled = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CreateSources();
        CreateClips();
        LoadSettings();
    }

    private void Start()
    {
        PlayLoopsIfNeeded();
    }

    private void Update()
    {
        if (playerCar == null)
        {
            playerCar = FindFirstObjectByType<CarController>();
        }

        UpdateVehicleLoops();
    }

    public void SetPlayerCar(CarController car)
    {
        playerCar = car;
    }

    public void SetMusicEnabled(bool enabled)
    {
        musicEnabled = enabled;
        SaveManager.SetMusicEnabled(enabled);
        ApplyVolumes();
        PlayLoopsIfNeeded();
    }

    public void SetSoundEnabled(bool enabled)
    {
        soundEnabled = enabled;
        SaveManager.SetSoundEnabled(enabled);
        ApplyVolumes();
        PlayLoopsIfNeeded();
    }

    public void PlayButton()
    {
        PlayOneShot(buttonClip, 0.42f);
    }

    public void PlayCoin()
    {
        PlayOneShot(coinClip, 0.75f);
    }

    public void PlayCrash()
    {
        PlayOneShot(crashClip, 0.9f);
    }

    public void PlayDriftReward()
    {
        PlayOneShot(driftRewardClip, 0.8f);
    }

    private void CreateSources()
    {
        musicSource = CreateSource("MusicSource", true);
        engineSource = CreateSource("EngineSource", true);
        driftSource = CreateSource("DriftSource", true);
        sfxSource = CreateSource("SfxSource", false);
    }

    private AudioSource CreateSource(string sourceName, bool loop)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.name = sourceName;
        return source;
    }

    private void CreateClips()
    {
        musicSource.clip = CreateMusicLoop();
        engineSource.clip = CreateEngineLoop();
        driftSource.clip = CreateNoiseLoop("DriftLoop", 0.65f, 0.18f);
        buttonClip = CreateToneClip("ButtonClick", 520f, 0.06f, 0.45f);
        coinClip = CreateArpeggioClip("CoinPickup", 740f, 0.18f);
        crashClip = CreateNoiseBurst("CrashHit", 0.24f);
        driftRewardClip = CreateArpeggioClip("DriftReward", 430f, 0.24f);
    }

    private void LoadSettings()
    {
        musicEnabled = SaveManager.MusicEnabled;
        soundEnabled = SaveManager.SoundEnabled;
        ApplyVolumes();
    }

    private void PlayLoopsIfNeeded()
    {
        if (musicEnabled && !musicSource.isPlaying)
        {
            musicSource.Play();
        }

        if (soundEnabled)
        {
            if (!engineSource.isPlaying)
            {
                engineSource.Play();
            }

            if (!driftSource.isPlaying)
            {
                driftSource.Play();
            }
        }
    }

    private void ApplyVolumes()
    {
        musicSource.volume = musicEnabled ? musicVolume : 0f;
        if (!soundEnabled)
        {
            engineSource.volume = 0f;
            driftSource.volume = 0f;
        }
    }

    private void UpdateVehicleLoops()
    {
        if (!soundEnabled || playerCar == null)
        {
            engineSource.volume = 0f;
            driftSource.volume = 0f;
            return;
        }

        float speed01 = Mathf.InverseLerp(0f, 90f, playerCar.SpeedKmh);
        engineSource.volume = Mathf.Lerp(engineVolume * 0.22f, engineVolume, speed01);
        engineSource.pitch = Mathf.Lerp(0.7f, 1.65f, speed01);

        float drift01 = playerCar.IsDrifting ? Mathf.Clamp01(playerCar.LateralSpeed / 8f) : 0f;
        driftSource.volume = driftVolume * drift01;
        driftSource.pitch = Mathf.Lerp(0.85f, 1.35f, drift01);
    }

    private void PlayOneShot(AudioClip clip, float volumeScale)
    {
        if (!soundEnabled || clip == null || sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip, soundVolume * volumeScale);
    }

    private static AudioClip CreateToneClip(string clipName, float frequency, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        float[] data = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)SampleRate;
            float envelope = 1f - i / (float)sampleCount;
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * amplitude * envelope;
        }

        return CreateClip(clipName, data);
    }

    private static AudioClip CreateArpeggioClip(string clipName, float rootFrequency, float duration)
    {
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        float[] data = new float[sampleCount];
        float[] ratios = { 1f, 1.25f, 1.5f };
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)SampleRate;
            int step = Mathf.Clamp(Mathf.FloorToInt(t / Mathf.Max(0.01f, duration / ratios.Length)), 0, ratios.Length - 1);
            float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(i / (float)sampleCount));
            data[i] = Mathf.Sin(2f * Mathf.PI * rootFrequency * ratios[step] * t) * 0.45f * envelope;
        }

        return CreateClip(clipName, data);
    }

    private static AudioClip CreateNoiseBurst(string clipName, float duration)
    {
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        float[] data = new float[sampleCount];
        uint seed = 1234567u;
        for (int i = 0; i < sampleCount; i++)
        {
            seed = seed * 1664525u + 1013904223u;
            float noise = ((seed >> 16) / 32768f) - 1f;
            float envelope = 1f - i / (float)sampleCount;
            data[i] = noise * envelope * 0.75f;
        }

        return CreateClip(clipName, data);
    }

    private static AudioClip CreateEngineLoop()
    {
        int sampleCount = SampleRate;
        float[] data = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)SampleRate;
            float low = Mathf.Sin(2f * Mathf.PI * 72f * t) * 0.32f;
            float mid = Mathf.Sin(2f * Mathf.PI * 144f * t) * 0.16f;
            data[i] = low + mid;
        }

        return CreateClip("EngineLoop", data);
    }

    private static AudioClip CreateNoiseLoop(string clipName, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        float[] data = new float[sampleCount];
        uint seed = 7654321u;
        for (int i = 0; i < sampleCount; i++)
        {
            seed = seed * 1103515245u + 12345u;
            float noise = ((seed >> 16) / 32768f) - 1f;
            data[i] = noise * amplitude;
        }

        return CreateClip(clipName, data);
    }

    private static AudioClip CreateMusicLoop()
    {
        int sampleCount = SampleRate * 2;
        float[] data = new float[sampleCount];
        float[] notes = { 196f, 246.94f, 293.66f, 329.63f };
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)SampleRate;
            int noteIndex = Mathf.Clamp(Mathf.FloorToInt((t / 2f) * notes.Length), 0, notes.Length - 1);
            float carrier = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * t);
            float pulse = Mathf.Sin(2f * Mathf.PI * 2f * t) * 0.5f + 0.5f;
            data[i] = carrier * Mathf.Lerp(0.08f, 0.18f, pulse);
        }

        return CreateClip("MusicLoop", data);
    }

    private static AudioClip CreateClip(string clipName, float[] data)
    {
        AudioClip clip = AudioClip.Create(clipName, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
