using UnityEngine;
using UnityEngine.UI;

// Per-source headroom and unscaled fades keep pauses/results quiet without hard cuts.
public class GameAudio : MonoBehaviour
{
    public enum Bus { Earthquake, Warning, Impact, Feedback }
    public const string MasterKey = "audio.master", QuakeKey = "audio.earthquake";
    public static float Read(string key, float fallback) => Mathf.Clamp01(PlayerPrefs.GetFloat(key, fallback));
    public static float Master => Read(MasterKey, .8f);
    public static float Earthquake => Read(QuakeKey, .8f);
    public static bool Muted => PlayerPrefs.GetInt("audio.mute", 0) != 0;
    public static float Shake => Read("camera.shake", 1f);
    public static string Key(Bus bus) => bus == Bus.Earthquake ? QuakeKey : "audio." + bus.ToString().ToLowerInvariant();
    public static float Category(Bus bus) => Read(Key(bus), .8f);
    public static void ResetAudio()
    {
        PlayerPrefs.SetFloat(MasterKey, .8f);
        foreach (Bus bus in System.Enum.GetValues(typeof(Bus))) PlayerPrefs.SetFloat(Key(bus), .8f);
        PlayerPrefs.SetInt("audio.mute", 0); PlayerPrefs.Save();
    }
    public bool preview;
    AudioSource source;
    Bus bus;
    float level = 1f;
    public static GameAudio Bind(AudioSource source, Bus bus)
    {
        var gain = source.gameObject.AddComponent<GameAudio>();
        gain.source = source; gain.bus = bus;
        source.volume = 0f;
        source.ignoreListenerPause = true;
        source.mute = false;
        source.outputAudioMixerGroup = null; // Direct listener route; category gain is managed here.
        source.dopplerLevel = 0f;
        gain.Apply(true);
        return gain;
    }
    public void SetLevel(float value) => level = Mathf.Clamp01(value);
    public static void FadeOut(AudioSource source)
    {
        if (!source) return;
        foreach (var gain in source.GetComponents<GameAudio>())
            if (gain.source == source) gain.level = 0;
    }
    void Update() => Apply(false);
    void Apply(bool immediate)
    {
        float cap = bus == Bus.Earthquake ? .45f : bus == Bus.Warning ? .5f : bus == Bus.Impact ? .28f : .2f;
        float target = Muted || (AudioListener.pause && !preview) ? 0 : cap * Master * level * Category(bus);
        source.volume = immediate ? target : Mathf.MoveTowards(source.volume, target, Time.unscaledDeltaTime * 2f);
    }
    public static void LogDiagnostics()
    {
        int listeners = 0;
        foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            if (listener.isActiveAndEnabled) listeners++;
        Debug.Log("Audio: listeners=" + listeners + " listenerVolume=" + AudioListener.volume +
            " paused=" + AudioListener.pause + " muted=" + Muted + " master=" + Master +
            " outputRate=" + AudioSettings.outputSampleRate);
        foreach (var audio in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            Debug.Log("Audio source " + audio.gameObject.name + " clip=" + (audio.clip ? audio.clip.name : "one-shot/none") +
                " playing=" + audio.isPlaying + " volume=" + audio.volume + " mute=" + audio.mute +
                " mixer=" + (audio.outputAudioMixerGroup ? audio.outputAudioMixerGroup.name : "direct") +
                " ignorePause=" + audio.ignoreListenerPause);
    }
    public static Slider Slider(Transform parent, Font font, string label, string key, float value, float y)
    {
        var caption = SeghaniTakSceneBuilder.MakeText(parent, label, font, 26, TextAnchor.MiddleCenter,
            new Vector2(.5f, .5f), new Vector2(0, y + 25), new Vector2(900, 40));
        var track = new GameObject(label + " Slider", typeof(RectTransform), typeof(Image), typeof(Slider));
        track.transform.SetParent(parent, false);
        var rect = track.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(0, y - 10); rect.sizeDelta = new Vector2(500, 24);
        track.GetComponent<Image>().color = new Color(.25f,.25f,.3f);
        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(track.transform, false);
        handle.GetComponent<Image>().color = Color.white;
        var handleRect = handle.GetComponent<RectTransform>(); handleRect.sizeDelta = new Vector2(24, 30);
        var slider = track.GetComponent<Slider>(); slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>(); slider.value = value;
        caption.text = label + ": " + Mathf.RoundToInt(value * 100) + "%";
        slider.onValueChanged.AddListener(v => { PlayerPrefs.SetFloat(key, v); PlayerPrefs.Save();
            caption.text = label + ": " + Mathf.RoundToInt(v * 100) + "%"; });
        return slider;
    }
}
