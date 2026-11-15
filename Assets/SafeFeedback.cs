using UnityEngine;
using UnityEngine.UI;

public class SafeFeedback : MonoBehaviour
{
    public Text objectiveText;
    public Text stanceText;
    public Image vignette;
    public SafeZoneTrigger zone;
    public FirstPersonCrouchController player;
    public Camera cam;
    public ParticleSystem burst;

    AudioSource audioSrc;
    AudioClip chime, bump;

    float popT = 1f, stancePopT = 1f;
    float vignetteAlpha;
    float fovKick, baseFov = 75f;
    string lastStance = "";
    float lastSound = -1f;

    static readonly Color VignetteGreen = new Color(0.3f, 1f, 0.45f, 0f);
    static readonly Color Warning = new Color(1f, 0.65f, 0.25f);

    void Start()
    {
        audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.spatialBlend = 0f;
        GameAudio.Bind(audioSrc, GameAudio.Bus.Feedback);

        chime = MakeTone("SafeChime", new[] { 523.25f, 659.25f, 783.99f }, 0.09f, 0.9f, 0.28f, 5f);
        bump = MakeTone("Bump", new[] { 85f }, 0f, 0.2f, 0.5f, 18f);

        if (cam) baseFov = cam.fieldOfView;
        if (zone) zone.OnSafeChanged += OnSafeChanged;
    }

    void OnDestroy()
    {
        if (zone) zone.OnSafeChanged -= OnSafeChanged;
    }

    void OnSafeChanged(bool safe)
    {
        popT = 0f;
        if (!safe) return;

        if (Time.unscaledTime - lastSound > .3f)
        { audioSrc.PlayOneShot(chime); lastSound = Time.unscaledTime; }
        vignetteAlpha = 0.7f;
        fovKick = 6f;

        if (burst && player)
        {
            burst.transform.position = player.transform.position + Vector3.up * 0.5f;
            burst.Emit(40);
        }
    }

    public void Clear()
    {
        GameAudio.FadeOut(audioSrc);
        if (burst) burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (vignette) vignette.color = VignetteGreen;
        if (cam) cam.fieldOfView = baseFov;
        if (objectiveText) objectiveText.rectTransform.localScale = Vector3.one;
        if (stanceText) stanceText.text = "";
        vignetteAlpha = fovKick = 0f;
        enabled = false;
    }

    public void ApplyCameraSetting()
    {
        if (cam) cam.fieldOfView = baseFov - fovKick * GameAudio.Shake;
    }

    void Update()
    {
        if (objectiveText && popT < 1f)
        {
            popT = Mathf.Min(1f, popT + Time.deltaTime / 0.5f);
            float s = 1f + 0.35f * Mathf.Sin(popT * Mathf.PI);
            objectiveText.rectTransform.localScale = Vector3.one * s;
        }

        if (vignette && zone)
        {
            float target = zone.IsSafe ? 0.25f : 0f;
            vignetteAlpha = Mathf.MoveTowards(vignetteAlpha, target, Time.deltaTime * 0.9f);
            Color c = VignetteGreen;
            c.a = vignetteAlpha;
            vignette.color = c;
        }

        if (cam)
        {
            fovKick = Mathf.Lerp(fovKick, 0f, Time.deltaTime * 6f);
            ApplyCameraSetting();
        }

        if (stanceText && player)
        {
            bool blocked = player.StandBlocked;
            string stance = blocked ? "Can't stand up - something is above you"
                          : player.IsCrouching ? "CROUCHING" : "STANDING";

            if (stance != lastStance)
            {
                stanceText.text = stance;
                lastStance = stance;
                stancePopT = 0f;
                if (blocked && Time.unscaledTime - lastSound > .3f)
                { audioSrc.PlayOneShot(bump); lastSound = Time.unscaledTime; }
            }
            stanceText.color = blocked ? Warning : new Color(1f, 1f, 1f, 0.85f);

            if (stancePopT < 1f)
            {
                stancePopT = Mathf.Min(1f, stancePopT + Time.deltaTime / 0.25f);
                stanceText.rectTransform.localScale = Vector3.one * (1f + 0.2f * Mathf.Sin(stancePopT * Mathf.PI));
            }
        }
    }

    public static AudioClip MakeTone(string name, float[] freqs, float noteGap, float length, float volume, float decay)
    {
        const int sampleRate = 44100;
        int n = Mathf.CeilToInt(sampleRate * length);
        var data = new float[n];

        for (int i = 0; i < freqs.Length; i++)
        {
            int start = Mathf.RoundToInt(sampleRate * noteGap * i);
            for (int s = start; s < n; s++)
            {
                float t = (s - start) / (float)sampleRate;
                data[s] += Mathf.Sin(2f * Mathf.PI * freqs[i] * t) * Mathf.Exp(-decay * t) * volume;
            }
        }

        float peak = 0;
        for (int i = 0; i < n; i++)
        {
            // Smooth both edges, including loop seams; preserve headroom for mixed sources.
            float envelope = Mathf.Min(1f, Mathf.Min(i, n - 1 - i) / (sampleRate * .025f));
            data[i] *= envelope;
            peak = Mathf.Max(peak, Mathf.Abs(data[i]));
        }
        float gain = peak > .6f ? .6f / peak : 1f;
        for (int i = 0; i < n; i++) data[i] *= gain;
        var clip = AudioClip.Create(name, n, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}