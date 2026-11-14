using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;

// Modal screens restore the exact caller state: gameplay, pause menu, or main menu.
public class OverlayMenus : MonoBehaviour
{
    static OverlayMenus instance;
    static int consumedFrame = -1;
    public static bool BlocksPause => (instance && instance.open) || consumedFrame == Time.frameCount;
    GameObject screen, caller;
    GameObject previousSelection;
    Font font;
    float savedTime;
    bool savedAudio, savedCursor;
    CursorLockMode savedLock;
    bool open;
    readonly List<Selectable> navigation = new List<Selectable>();
    AudioSource previewSource;
    GameAudio.Bus previewBus;
    bool settings;

    public void Initialize(Font uiFont) { instance = this; font = uiFont; }
    public static void Help(GameObject caller = null)
    {
        var director = FindFirstObjectByType<EarthquakeDirector>();
        if (director && director.CurrentPhase == EarthquakeDirector.Phase.Ended) return;
        if (instance) instance.Show(false, caller);
    }
    public static void Settings(GameObject caller) { if (instance) instance.Show(true, caller); }

    void Show(bool isSettings, GameObject from)
    {
        if (open) return;
        consumedFrame = Time.frameCount;
        open = true; settings = isSettings; caller = from;
        savedTime = Time.timeScale; savedAudio = AudioListener.pause;
        savedLock = Cursor.lockState; savedCursor = Cursor.visible;
        previousSelection = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        Time.timeScale = 0f; AudioListener.pause = true;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        if (caller) caller.SetActive(false);
        Build();
    }

    void Build()
    {
        navigation.Clear();
        screen = SeghaniTakSceneBuilder.MakePanel(transform, settings ? "Settings" : "How to play", new Color(.06f,.08f,.11f,.99f));
        TextAt(settings ? "SETTINGS" : "HOW TO PLAY", 440, 52, 100);
        if (settings)
        {
            AddSlider("Master volume", GameAudio.MasterKey, GameAudio.Master, 330);
            int row = 0;
            foreach (GameAudio.Bus bus in System.Enum.GetValues(typeof(GameAudio.Bus)))
                AddSlider(bus + " volume", GameAudio.Key(bus), GameAudio.Category(bus), 250 - 80 * row++);
            AddSlider("Camera Shake (0 = off)", "camera.shake", GameAudio.Shake, -70);
            AddButton(GameAudio.Muted ? "Unmute" : "Mute", -170, -260, () => {
                PlayerPrefs.SetInt("audio.mute", GameAudio.Muted ? 0 : 1); PlayerPrefs.Save(); Rebuild(); });
            AddButton("Preview: " + previewBus, -170, 260, () => {
                previewBus = (GameAudio.Bus)(((int)previewBus + 1) % 4); Rebuild(); });
            AddButton("Play preview", -270, -260, Preview);
            AddButton("Reset audio", -270, 260, () => { GameAudio.ResetAudio(); Rebuild(); });
            TextAt("Previews use your current volumes and mute setting.", -345, 24, 50);
        }
        else
        {
            TextAt("Survive until the earthquake stops", 320, 36, 70);
            var player = FindFirstObjectByType<FirstPersonCrouchController>();
            bool jumping = player ? player.allowJump : new Level1Configuration().allowJump;
            TextAt(EarthquakeDirector.Briefing + "\n\n" + Level1Configuration.MovementHint + (jumping ? "   Space: jump" : "") + "\nC / Ctrl: crouch   Esc: pause   H: instructions\n\nWatch pulsing floor warnings and listen for furniture creaks.\nMove away before it falls. Fallen furniture can change your route.\n\nYou win by staying alive. Leaving cover is allowed.\nOnly dangerous physical impacts cause injury.\n\nUse arrow keys / Tab to select; Enter / Space to activate.", 30, 30, 500);
        }
        AddButton("Back", -400, 0, Close);
        for (int i = 0; i < navigation.Count; i++)
        {
            var nav = new Navigation { mode = Navigation.Mode.Explicit,
                selectOnUp = navigation[(i + navigation.Count - 1) % navigation.Count],
                selectOnDown = navigation[(i + 1) % navigation.Count] };
            navigation[i].navigation = nav;
        }
        navigation[0].Select();
    }

    void TextAt(string value, float y, int size, float height)
    {
        var text = SeghaniTakSceneBuilder.MakeText(screen.transform, "Text", font, size, TextAnchor.MiddleCenter,
            new Vector2(.5f,.5f), new Vector2(0,y), new Vector2(1550,height));
        text.horizontalOverflow = HorizontalWrapMode.Wrap; text.raycastTarget = false; text.text = value;
    }
    void AddSlider(string label, string key, float value, float y) =>
        navigation.Add(GameAudio.Slider(screen.transform, font, label, key, value, y));
    void AddButton(string label, float y, float x, UnityEngine.Events.UnityAction action)
    {
        var button = SeghaniTakSceneBuilder.MakeButton(screen.transform, label, font, new Vector2(x,y));
        button.onClick.AddListener(action); navigation.Add(button);
    }
    void Rebuild()
    {
        int index = navigation.FindIndex(s => s.gameObject == EventSystem.current.currentSelectedGameObject);
        screen.SetActive(false); Destroy(screen); Build();
        if (index >= 0 && index < navigation.Count) navigation[index].Select();
    }

    void Preview()
    {
        if (!previewSource)
        {
            var go = new GameObject("Settings audio preview"); go.transform.SetParent(transform);
            previewSource = go.AddComponent<AudioSource>();
        }
        // Separate source per category avoids retaining another category's gain component.
        foreach (var gain in previewSource.GetComponents<GameAudio>()) { gain.enabled = false; Destroy(gain); }
        var controller = GameAudio.Bind(previewSource, previewBus); controller.preview = true;
        previewSource.spatialBlend = 0f;
        previewSource.clip = previewBus == GameAudio.Bus.Earthquake ? EarthquakeDirector.MakeRumble() :
            SafeFeedback.MakeTone("Preview " + previewBus,
                previewBus == GameAudio.Bus.Warning ? new[] {260f,340f} :
                previewBus == GameAudio.Bus.Impact ? new[] {70f,110f,45f} : new[] {523.25f,659.25f,783.99f},
                .08f, .8f, .4f, 5f);
        previewSource.Play();
        GameAudio.LogDiagnostics();
    }
    public void Close()
    {
        if (!open) return;
        consumedFrame = Time.frameCount;
        if (previewSource) { GameAudio.FadeOut(previewSource); StartCoroutine(RetirePreview(previewSource.gameObject)); previewSource = null; }
        screen.SetActive(false); Destroy(screen); open = false;
        if (caller) caller.SetActive(true);
        Time.timeScale = savedTime; AudioListener.pause = savedAudio;
        Cursor.lockState = savedLock; Cursor.visible = savedCursor;
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(previousSelection);
    }
    IEnumerator RetirePreview(GameObject go)
    { yield return new WaitForSecondsRealtime(.25f); if (go) Destroy(go); }

    void Update()
    {
        if (open)
        {
            if (consumedFrame == Time.frameCount) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                var selected = EventSystem.current.currentSelectedGameObject;
                int index = navigation.FindIndex(s => s.gameObject == selected);
                int direction = Input.GetKey(KeyCode.LeftShift) ? -1 : 1;
                navigation[(index + direction + navigation.Count) % navigation.Count].Select();
            }
        }
        else if (GameFlow.Playing && Input.GetKeyDown(KeyCode.H))
        {
            var director = FindFirstObjectByType<EarthquakeDirector>();
            if (director && director.CurrentPhase != EarthquakeDirector.Phase.Ended)
            {
                var pause = FindFirstObjectByType<PauseMenu>();
                Help(pause && pause.IsPaused ? pause.panel : null);
            }
        }
    }
    void OnDestroy() { if (instance == this) instance = null; }
}
