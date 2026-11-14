using System.Text;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-50)]
public class EarthquakeDirector : MonoBehaviour
{
    public enum Phase { Calm, Shaking, Ended }
    public Phase CurrentPhase { get; private set; } = Phase.Calm;

    public float quakeStart = 10f;
    public float quakeEnd = 34f;
    public SafeFeedback feedback;
    public HazardScheduler hazards;
    public float RoundTime => elapsed;

    public FirstPersonCrouchController player;
    public CameraShake shake;
    public SafeZoneTrigger shelter;
    
    public GameObject resultPanel;
    public Text resultTitle, resultBody;

    [HideInInspector] public bool isDead;

    AnimationCurve intensityCurve;
    AudioSource rumble, sfx;
    AudioClip crashClip;
    GameAudio rumbleGain;
    float lastCrash = -1f;
    public const string Briefing = "Survive until the earthquake stops. Crouch under the sturdy desk for protection. Watch for falling furniture.";

    float elapsed, coverTime = -1f, exposedSeconds;
    bool hasReachedCover;

    public void PlayCrash()
    {
        if (sfx && crashClip && Time.unscaledTime - lastCrash > .4f)
        { lastCrash = Time.unscaledTime; sfx.PlayOneShot(crashClip); }
    }

    public void KillPlayer(string reason)
    {
        if (!isDead && CurrentPhase != Phase.Ended)
        {
            isDead = true;
            End(false, reason, "Take cover under sturdy furniture and avoid moving hazards.");
        }
    }

    void Awake()
    {
        intensityCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(10f, 0f), new Keyframe(12f, 0.35f),
            new Keyframe(18f, 0.5f), new Keyframe(20f, 1f), new Keyframe(30f, 1f),
            new Keyframe(32.5f, 0.3f), new Keyframe(34f, 0f));
    }

    void Start()
    {
        shelter.listening = false;
        shelter.objectiveMessage = "Survive until the earthquake stops  •  H: How to play";
        shelter.safeMessage = "Safe! Hold on until the shaking stops.";
        shelter.Refresh();

        var ambience = new GameObject("Earthquake ambience");
        ambience.transform.SetParent(transform);
        rumble = ambience.AddComponent<AudioSource>();
        rumble.playOnAwake = false;
        rumble.clip = MakeRumble();
        rumble.loop = true;
        rumble.volume = 0f;
        rumble.spatialBlend = 0f;
        rumbleGain = GameAudio.Bind(rumble, GameAudio.Bus.Earthquake);
        rumbleGain.SetLevel(0f);
        var filter = ambience.AddComponent<AudioLowPassFilter>();
        filter.cutoffFrequency = 650f;
        filter.lowpassResonanceQ = 1f;

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.spatialBlend = 0f;
        GameAudio.Bind(sfx, GameAudio.Bus.Impact);
        crashClip = SafeFeedback.MakeTone("Crash", new[] { 70f, 110f, 45f }, 0.015f, 0.6f, 0.5f, 7f);
    }

    void LateUpdate()
    {
        if (CurrentPhase == Phase.Ended)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                GameFlow.Retry();
            }
            return;
        }

        if (Time.timeScale <= 0f) return;

        elapsed += Time.deltaTime;

        if (CurrentPhase == Phase.Calm && elapsed >= quakeStart) BeginQuake();

        if (CurrentPhase == Phase.Ended) return;
        shelter.Evaluate(player);
        if (CurrentPhase == Phase.Shaking)
        {
            ApplyHazards();
            CheckRules();
            if (CurrentPhase == Phase.Ended) return;
        }

        float curveTime = Mathf.Lerp(10f, 34f, Mathf.InverseLerp(quakeStart, quakeEnd, elapsed));
        float intensity = CurrentPhase == Phase.Shaking ? Mathf.Clamp01(intensityCurve.Evaluate(curveTime)) : 0f;

        // Initiate audio/visual shaking at onset without changing the existing physical sway/slowdown curve.
        float sensoryIntensity = CurrentPhase == Phase.Shaking ?
            Mathf.Max(intensity,.05f*Mathf.Clamp01((quakeEnd-elapsed)/2f)) : 0f;
        shake.intensity = sensoryIntensity;
        rumbleGain.SetLevel(sensoryIntensity);
        if (CurrentPhase == Phase.Shaking)
        {
            shelter.objectiveMessage = "Survive the earthquake — " + Mathf.CeilToInt(quakeEnd - elapsed) + " s remaining";
            shelter.safeMessage = shelter.objectiveMessage + "  •  In cover";
            shelter.Refresh();
        }
        player.speedMultiplier = Mathf.Lerp(1f, 0.6f, intensity);
        player.swayStrength = shelter.IsSafe ? 0f : intensity;
        if (hazards) hazards.Tick(elapsed,Time.deltaTime);

    }

    void BeginQuake()
    {
        CurrentPhase = Phase.Shaking;
        shelter.listening = true;
        shelter.objectiveMessage = "Survive the earthquake";
        shelter.Refresh();
        rumble.Play();
    }

    void ApplyHazards()
    {
        if (!shelter.IsSafe)
        {
            exposedSeconds += Time.deltaTime;
        }
    }

    void CheckRules()
    {
        if (isDead) return;

        if (shelter.IsSafe && !hasReachedCover)
        {
            hasReachedCover = true;
            coverTime = elapsed;
        }
        if (elapsed >= quakeEnd) End(true, "", "");
    }

    void End(bool win, string reason, string loseTip)
    {
        CurrentPhase = Phase.Ended;

        foreach (var prop in FindObjectsByType<FallingProp>(FindObjectsSortMode.None)) prop.ClearWarning();
        shelter.Clear();
        if (feedback) feedback.Clear();
        shake.ResetShake();
        GameAudio.FadeOut(rumble);
        GameAudio.FadeOut(sfx);
        Time.timeScale = 0f;
        AudioListener.pause = true;
        player.speedMultiplier = 1f;
        player.swayStrength = 0f;
        player.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var sb = new StringBuilder();
        if (!win) sb.AppendLine(reason).AppendLine();
        sb.AppendLine("[" + (win ? "x" : " ") + "]  Survive the earthquake");
        sb.AppendLine("Shelter used: " + (hasReachedCover ? "yes" : "no"));
        sb.AppendLine("Time to cover: " + (hasReachedCover ? (coverTime - quakeStart).ToString("0.0") + " s after the quake began" : "never"));
        sb.AppendLine("Time exposed: " + exposedSeconds.ToString("0.0") + " s").AppendLine();

        sb.AppendLine("Drop, cover, and hold on.");
        if (win) sb.AppendLine(hasReachedCover ? "You survived and used the desk shelter." : "You survived without using the desk shelter.");
        else sb.AppendLine(loseTip);
        sb.AppendLine().Append("Press R to retry");

        if (resultTitle)
        {
            resultTitle.text = win ? "YOU SURVIVED" : "NOT THIS TIME";
            resultTitle.color = win ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.45f, 0.4f);
        }
        if (resultBody) resultBody.text = sb.ToString();
        if (resultPanel) resultPanel.SetActive(true);
    }

    public static AudioClip MakeRumble()
    {
        const int sr = 22050;
        int n = sr * 2;
        var d = new float[n];
        // Integer harmonics make a seamless two-second loop with no clipped noise spikes.
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)sr;
            d[i] = .22f * Mathf.Sin(2f * Mathf.PI * 72f * t) +
                   .18f * Mathf.Sin(2f * Mathf.PI * 126f * t) +
                   .12f * Mathf.Sin(2f * Mathf.PI * 213f * t);
        }
        var clip = AudioClip.Create("Rumble", n, 1, sr, false);
        clip.SetData(d, 0);
        return clip;
    }
}