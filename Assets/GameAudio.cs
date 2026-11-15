using UnityEngine;
using UnityEngine.UI;

// Per-source headroom and unscaled fades keep pauses/results quiet without hard cuts.
public class GameAudio : MonoBehaviour
{
    public enum Bus { Earthquake, Warning, Impact, Feedback }
    public const string MasterKey = "audio.master", QuakeKey = "audio.earthquake";
    public static float Read(string key, float fallback) => Mathf.Clamp01(PlayerPrefs.GetFloat(key, fallback));
    public static float Master => Read(MasterKey, .8f);
    public static float Shake => Read("camera.shake", 1f);
    // Category balance is internal; legacy mute/category preferences no longer override Master.
    public static float Category(Bus bus) => .8f;
    public static void ApplySettings()
    {
        foreach (var gain in FindObjectsByType<GameAudio>(FindObjectsSortMode.None)) gain.Apply(true);
        foreach (var shake in FindObjectsByType<CameraShake>(FindObjectsSortMode.None)) shake.ApplySettings();
        foreach (var feedback in FindObjectsByType<SafeFeedback>(FindObjectsSortMode.None)) feedback.ApplyCameraSetting();
    }
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
        float target = AudioListener.pause ? 0 : cap * Master * level * Category(bus);
        source.volume = immediate ? target : Mathf.MoveTowards(source.volume, target, Time.unscaledDeltaTime * 2f);
    }
    public static Slider Slider(Transform parent, Font font, string label, string key, float value, float y)
    {
        var caption = SeghaniTakSceneBuilder.MakeText(parent, label, font, 26, TextAnchor.MiddleCenter,
            new Vector2(.5f, .5f), new Vector2(0, y + 25), new Vector2(900, 40));
        var track = new GameObject(label + " Slider", typeof(RectTransform), typeof(Image), typeof(Slider));
        track.transform.SetParent(parent, false);
        var rect = track.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(0, y - 10); rect.sizeDelta = new Vector2(650, 30);
        track.GetComponent<Image>().color = new Color(.25f,.25f,.3f);
        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(track.transform, false);
        handle.GetComponent<Image>().color = Color.white;
        var handleRect = handle.GetComponent<RectTransform>(); handleRect.sizeDelta = new Vector2(30, 42);
        var slider = track.GetComponent<Slider>(); slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>(); slider.value = value;
        var colors=slider.colors; colors.selectedColor=new Color(.55f,.8f,.7f); colors.highlightedColor=colors.selectedColor; slider.colors=colors;
        caption.text = label + ": " + Mathf.RoundToInt(value * 100) + "%";
        slider.onValueChanged.AddListener(v => { PlayerPrefs.SetFloat(key, v); PlayerPrefs.Save(); ApplySettings();
            caption.text = label + ": " + Mathf.RoundToInt(v * 100) + "%"; });
        return slider;
    }
}
