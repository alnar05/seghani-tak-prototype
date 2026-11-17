using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-50)]
public class EarthquakeDirector : MonoBehaviour
{
    public enum Phase { Calm, Shaking, Ended }
    public Phase CurrentPhase { get; private set; } = Phase.Calm;

    public float quakeStart = 10f;
    public float quakeEnd = 25f;
    public SafeFeedback feedback;
    public HazardScheduler hazards;
    public float RoundTime => elapsed;

    public FirstPersonCrouchController player;
    public CameraShake shake;
    public SafeZoneTrigger shelter;
    
    public GameObject resultPanel, hud;
    public Text resultTitle, resultBody;
    public Button nextLevelButton;
    public string levelTitle, orientationTip;
    public string coverInstruction = Briefing;

    [HideInInspector] public bool isDead;

    AnimationCurve intensityCurve;
    AudioSource rumble, sfx;
    AudioClip crashClip;
    GameAudio rumbleGain;
    float lastCrash = -1f;
    public const string Briefing = "Crouch fully under sturdy cover at 0 s.";

    float elapsed;

    public void PlayCrash()
    {
        if (sfx && crashClip && Time.unscaledTime - lastCrash > .4f)
        { lastCrash = Time.unscaledTime; sfx.PlayOneShot(crashClip); }
    }

    public void KillPlayer(string reason)
    {
        if (!isDead && CurrentPhase == Phase.Shaking)
        {
            isDead = true;
            End(false, reason);
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
        UpdateObjective();

        var ambience = new GameObject("Earthquake ambience");
        ambience.transform.SetParent(transform);
        rumble = ambience.AddComponent<AudioSource>();
        rumble.playOnAwake = false;
        rumble.clip = GameAudio.MakeRumble();
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

        elapsed = Mathf.Min(elapsed + Time.deltaTime, quakeEnd);

        if (CurrentPhase == Phase.Calm && elapsed >= quakeStart) BeginQuake();

        if (CurrentPhase == Phase.Ended) return;
        shelter.Evaluate(player);
        if (CurrentPhase == Phase.Shaking)
        {
            CheckRules();
            if (CurrentPhase == Phase.Ended) return;
        }

        float curveTime = Mathf.Lerp(10f, 34f, Mathf.InverseLerp(quakeStart, quakeEnd, elapsed));
        float intensity = CurrentPhase == Phase.Shaking ? Mathf.Clamp01(intensityCurve.Evaluate(curveTime)) : 0f;

        // Start sensory effects at onset; movement remains responsive throughout the quake.
        float sensoryIntensity = CurrentPhase == Phase.Shaking ?
            Mathf.Max(intensity,.05f*Mathf.Clamp01((quakeEnd-elapsed)/2f)) : 0f;
        shake.intensity = sensoryIntensity;
        rumbleGain.SetLevel(sensoryIntensity);
        UpdateObjective();
        if (hazards) hazards.Tick(elapsed, Time.deltaTime);

    }

    void UpdateObjective()
    {
        if (CurrentPhase == Phase.Calm)
        {
            int remaining = Mathf.CeilToInt(quakeStart - elapsed);
            shelter.objectiveMessage = levelTitle + "  ·  Shaking in " + remaining + " s\n" + orientationTip;
            shelter.safeMessage = shelter.objectiveMessage;
        }
        else
        {
            int remaining = Mathf.CeilToInt(quakeEnd - elapsed);
            shelter.objectiveMessage = remaining + " s left  ·  Earthquake\n" + coverInstruction;
            shelter.safeMessage = remaining + " s left  ·  In cover\nStay crouched until the countdown ends.";
        }
        shelter.Refresh();
    }

    void BeginQuake()
    {
        CurrentPhase = Phase.Shaking;
        shelter.listening = true;
        rumble.Play();
    }

    void CheckRules()
    {
        if (CurrentPhase != Phase.Shaking || elapsed < quakeEnd) return;
        // Refresh the authoritative crouching/full-body check at the deadline itself.
        Physics.SyncTransforms();
        shelter.Evaluate(player);
        bool win = !isDead && shelter.IsSafe;
        End(win, win ? "" : "You weren’t under cover when time ran out.");
    }

    void End(bool win, string reason)
    {
        if (CurrentPhase == Phase.Ended) return;
        CurrentPhase = Phase.Ended;
        if (hud) hud.SetActive(false);

        if (hazards) hazards.CancelPending();
        else foreach (var prop in FindObjectsByType<FallingProp>(FindObjectsSortMode.None)) prop.ClearWarning();
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

        if (resultTitle)
        {
            resultTitle.text = win ? "You survived" : "You didn’t survive";
            resultTitle.color = win ? new Color(.75f,.9f,.82f) : new Color(.93f,.76f,.72f);
        }
        if (resultBody) resultBody.text = win ? "You were in cover when the shaking stopped." : reason;
        if (nextLevelButton) nextLevelButton.gameObject.SetActive(win && GameFlow.HasNextLevel);
        if (resultPanel)
        {
            resultPanel.transform.SetAsLastSibling();
            resultPanel.SetActive(true);
        }
    }

    public static AudioClip MakeRumble() => GameAudio.MakeRumble();
}
