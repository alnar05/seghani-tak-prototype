using UnityEngine;

public class GameAudio : MonoBehaviour
{
    public enum Bus { Earthquake, Warning, Impact, Feedback }
    public const string MasterKey = "audio.master";
    public const string ShakeKey = "camera.shake";

    const int SampleRate = 22050;
    const float Tau = 2f * Mathf.PI;
    static AudioClip rumbleClip, creakClip;

    public static float Master => Read(MasterKey, .8f);
    public static float Shake => Read(ShakeKey, 1f);

    static float Read(string key, float fallback)
    {
        float value = PlayerPrefs.GetFloat(key, fallback);
        return float.IsNaN(value) ? fallback : Mathf.Clamp01(value);
    }

    public static void ApplyMasterVolume() => AudioListener.volume = Master;

    public static void ApplySettings()
    {
        ApplyMasterVolume();
        foreach (var shake in FindObjectsByType<CameraShake>(FindObjectsSortMode.None)) shake.ApplySettings();
        foreach (var feedback in FindObjectsByType<SafeFeedback>(FindObjectsSortMode.None)) feedback.ApplyCameraSetting();
    }

    AudioSource source;
    float peakGain, level = 1f;
    bool stopAfterFade;

    public static GameAudio Bind(AudioSource source, Bus bus)
    {
        var gain = source.gameObject.AddComponent<GameAudio>();
        gain.source = source;
        gain.peakGain = bus switch
        {
            Bus.Earthquake => .22f,
            Bus.Warning => .52f,
            Bus.Impact => .27f,
            _ => .2f
        };
        // Allow Unity to suspend both looping sounds and one-shots while menus are open.
        source.ignoreListenerPause = false;
        source.mute = false;
        source.outputAudioMixerGroup = null;
        source.dopplerLevel = 0f;
        source.volume = gain.peakGain;
        return gain;
    }

    public void SetLevel(float value) => level = Mathf.Clamp01(value);

    public static void FadeOut(AudioSource source)
    {
        if (!source) return;
        foreach (var gain in source.GetComponents<GameAudio>())
        {
            if (gain.source != source) continue;
            gain.SetLevel(0f);
            gain.stopAfterFade = true;
        }
    }

    void Update()
    {
        if (!source) return;
        float amount = peakGain * Time.unscaledDeltaTime / .13f;
        source.volume = Mathf.MoveTowards(source.volume, peakGain * level, amount);
        if (!stopAfterFade || source.volume > 0f) return;
        source.Stop();
        stopAfterFade = false;
    }

    public static AudioClip MakeRumble()
    {
        if (rumbleClip) return rumbleClip;
        const float length = 2.5f;
        var samples = new float[(int)(SampleRate * length)];
        var random = new System.Random(1473);

        // Integer cycle counts keep the loop continuous. Spread the partials above deep sub-bass.
        for (int partial = 0; partial < 24; partial++)
        {
            int cycles = random.Next(205, 835);
            float phase = (float)random.NextDouble() * Tau;
            float strength = .029f - .014f * (cycles - 205f) / 630f;
            float samplePhase = Tau * cycles / samples.Length;
            for (int i = 0; i < samples.Length; i++)
                samples[i] += strength * Mathf.Sin(i * samplePhase + phase);
        }

        for (int i = 0; i < samples.Length; i++)
            samples[i] *= .82f + .18f * Mathf.Sin(Tau * i / samples.Length);

        rumbleClip = CreateClip("Earthquake room rumble", samples);
        return rumbleClip;
    }

    public static AudioClip MakeCreak()
    {
        if (creakClip) return creakClip;
        var samples = new float[(int)(SampleRate * 1.4f)];
        var random = new System.Random(3829);
        float grain = 0f;

        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)SampleRate;
            grain = Mathf.Lerp(grain, (float)random.NextDouble() * 2f - 1f, .24f);
            float stress = Mathf.Max(0f, Mathf.Sin(Tau * 2.25f * t));
            float pulse = .55f * stress * stress + .45f * Mathf.Pow(stress, 7f);
            float edge = Mathf.Clamp01(Mathf.Min(i, samples.Length - 1 - i) / (SampleRate * .055f));
            float flex = Mathf.Sin(Tau * (172f * t + 10f * Mathf.Sin(4.3f * t))) * .12f;
            float scrape = Mathf.Sin(Tau * (399f * t + 14f * Mathf.Sin(7.2f * t))) * .07f;
            samples[i] = (flex + scrape + grain * .65f) * pulse * edge;
        }

        creakClip = CreateClip("Furniture stress creak", samples);
        return creakClip;
    }

    static AudioClip CreateClip(string name, float[] samples)
    {
        var clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
