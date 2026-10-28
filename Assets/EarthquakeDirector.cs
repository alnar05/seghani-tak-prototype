using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EarthquakeDirector : MonoBehaviour
{
    public enum Phase { Calm, Shaking, Ended }
    public Phase CurrentPhase { get; private set; } = Phase.Calm;

    public float quakeStart = 10f;
    public float quakeEnd = 38f;
    public float earlyCoverDeadline = 18f;

    public FirstPersonCrouchController player;
    public CameraShake shake;
    public SafeZoneTrigger shelter;
    public Text bannerText;
    public GameObject resultPanel;
    public Text resultTitle, resultBody;

    [HideInInspector] public bool doorwayHit;
    [HideInInspector] public bool isDead;

    class Timed { public float time; public Action action; public bool done; }

    readonly List<Timed> timeline = new List<Timed>();

    AnimationCurve intensityCurve;
    AudioSource rumble, sfx;
    AudioClip crashClip;

    float elapsed, coverTime = -1f, exposedSeconds;
    float bannerTimer, bannerAge, messageLockUntil;
    bool hasReachedCover;
    Color bannerColor = Color.white;

    static readonly Color Orange = new Color(1f, 0.6f, 0.2f);
    static readonly Color Red = new Color(1f, 0.35f, 0.3f);

    public void Schedule(float time, Action action) => timeline.Add(new Timed { time = time, action = action });

    public void ShowMessage(string text, float seconds, Color color)
    {
        messageLockUntil = elapsed + seconds;
        SetBanner(text, seconds, color);
    }

    public void PlayCrash()
    {
        if (sfx && crashClip) sfx.PlayOneShot(crashClip);
    }

    public void KillPlayer(string reason)
    {
        if (!isDead && CurrentPhase != Phase.Ended)
        {
            isDead = true;
            End(false, reason, "Get under the desk to physically shield yourself from falling objects.");
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
        shelter.objectiveMessage = "Look around the room.";
        shelter.safeMessage = "Safe! Hold on until the shaking stops.";
        shelter.Refresh();

        rumble = gameObject.AddComponent<AudioSource>();
        rumble.clip = MakeRumble();
        rumble.loop = true;
        rumble.volume = 0f;
        rumble.spatialBlend = 0f;

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.spatialBlend = 0f;
        crashClip = SafeFeedback.MakeTone("Crash", new[] { 70f, 110f, 45f }, 0.015f, 0.6f, 0.5f, 7f);

        if (bannerText) bannerText.color = new Color(1, 1, 1, 0);
    }

    void Update()
    {
        if (CurrentPhase == Phase.Ended)
        {
            if (Input.GetKeyDown(KeyCode.R)) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        elapsed += Time.deltaTime;

        if (CurrentPhase == Phase.Calm && elapsed >= quakeStart) BeginQuake();

        float intensity = Mathf.Clamp01(intensityCurve.Evaluate(elapsed));

        shake.intensity = intensity;
        rumble.volume = intensity * 0.9f;
        player.speedMultiplier = Mathf.Lerp(1f, 0.6f, intensity);
        player.swayStrength = shelter.PlayerInside ? 0f : intensity;

        foreach (var e in timeline)
            if (!e.done && elapsed >= e.time) { e.done = true; e.action(); }

        if (CurrentPhase == Phase.Shaking)
        {
            ApplyHazards(intensity);
            CheckRules();
        }

        UpdateBanner();
    }

    void BeginQuake()
    {
        CurrentPhase = Phase.Shaking;
        shelter.listening = true;
        shelter.objectiveMessage = "Objective: Go under the desk.";
        shelter.Refresh();
        rumble.Play();
        ShowMessage("EARTHQUAKE!  FIND COVER!", 2.5f, Orange);
    }

    void ApplyHazards(float intensity)
    {
        if (!shelter.IsSafe && intensity > 0.02f)
        {
            exposedSeconds += Time.deltaTime;
            if (elapsed >= messageLockUntil && elapsed - quakeStart > 2.5f)
                SetBanner("GET UNDER THE DESK!", 0.5f, Red);
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

        shelter.listening = false;
        shake.intensity = 0f;
        rumble.Stop();
        player.speedMultiplier = 1f;
        player.swayStrength = 0f;
        player.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        bool early = win && hasReachedCover && coverTime <= earlyCoverDeadline;
        int stars = (win ? 1 : 0) + (early ? 1 : 0);

        var sb = new StringBuilder();
        if (!win) sb.AppendLine(reason).AppendLine();
        sb.AppendLine("[" + (win ? "x" : " ") + "]  Survive the earthquake");
        sb.AppendLine("[" + (early ? "x" : " ") + "]  Take cover before the peak");
        sb.AppendLine().AppendLine("Stars: " + stars + " / 2").AppendLine();
        sb.AppendLine("Time to cover: " + (hasReachedCover ? (coverTime - quakeStart).ToString("0.0") + " s after the quake began" : "never"));
        sb.AppendLine("Time exposed: " + exposedSeconds.ToString("0.0") + " s").AppendLine();

        sb.AppendLine("Drop, cover, and hold on.");
        if (win) sb.AppendLine("The desk protected you.");
        else sb.AppendLine(loseTip);
        if (doorwayHit) sb.AppendLine("Doorways and windows are not safer. Stay away from them.");
        sb.AppendLine().Append("Press R to retry");

        if (resultTitle)
        {
            resultTitle.text = win ? "YOU SURVIVED" : "NOT THIS TIME";
            resultTitle.color = win ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.45f, 0.4f);
        }
        if (resultBody) resultBody.text = sb.ToString();
        if (resultPanel) resultPanel.SetActive(true);
        if (bannerText) bannerText.color = new Color(1, 1, 1, 0);
    }

    void SetBanner(string text, float seconds, Color color)
    {
        if (!bannerText) return;
        bool same = bannerTimer > 0f && bannerText.text == text;
        bannerText.text = text;
        bannerColor = color;
        bannerTimer = seconds;
        if (!same) bannerAge = 0f;
    }

    void UpdateBanner()
    {
        if (!bannerText) return;
        Color c = bannerColor;
        if (bannerTimer > 0f)
        {
            bannerTimer -= Time.deltaTime;
            bannerAge += Time.deltaTime;
            c.a = Mathf.Min(Mathf.Clamp01(bannerAge / 0.15f), Mathf.Clamp01(bannerTimer / 0.4f));
        }
        else c.a = 0f;
        bannerText.color = c;
    }

    static AudioClip MakeRumble()
    {
        const int sr = 22050;
        int n = sr * 2;
        var d = new float[n];
        var rnd = new System.Random(7);
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
            lp += (noise - lp) * 0.02f;
            float t = i / (float)sr;
            float v = lp * 4f + Mathf.Sin(2f * Mathf.PI * 40f * t) * 0.35f + Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.2f;
            d[i] = Mathf.Clamp(v, -1f, 1f);
        }
        var clip = AudioClip.Create("Rumble", n, 1, sr, false);
        clip.SetData(d, 0);
        return clip;
    }
}