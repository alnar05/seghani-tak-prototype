using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OverlayMenus : MonoBehaviour
{
    static OverlayMenus instance;
    static int consumedFrame = -1;
    public static bool BlocksPause => (instance && instance.screen) || consumedFrame == Time.frameCount;

    GameObject screen, caller, hud, previousSelection;
    Font font;
    float savedTime;
    bool savedAudioPause, savedCursorVisible, savedHud;
    CursorLockMode savedCursorLock;

    public void Initialize(Font uiFont)
    {
        instance = this;
        font = uiFont;
    }

    public static void Help(GameObject caller = null)
    {
        var director = FindFirstObjectByType<EarthquakeDirector>();
        if (instance && (!director || director.CurrentPhase != EarthquakeDirector.Phase.Ended))
            instance.Show(false, caller);
    }

    public static void Settings(GameObject caller)
    {
        if (instance) instance.Show(true, caller);
    }

    void Show(bool settings, GameObject from)
    {
        if (screen || consumedFrame == Time.frameCount) return;

        consumedFrame = Time.frameCount;
        caller = from;
        previousSelection = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        savedTime = Time.timeScale;
        savedAudioPause = AudioListener.pause;
        savedCursorLock = Cursor.lockState;
        savedCursorVisible = Cursor.visible;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (caller) caller.SetActive(false);
        var pause = FindFirstObjectByType<PauseMenu>();
        hud = pause ? pause.hud : null;
        savedHud = hud && hud.activeSelf;
        if (hud) hud.SetActive(false);

        string title = settings ? "Settings" : "How to Play";
        screen = SeghaniTakSceneBuilder.MakePanel(transform, title + " Overlay", MenuTheme.Backdrop);
        var card = MenuTheme.Card(screen.transform, title, Vector2.zero,
            settings ? new Vector2(850f, 648f) : new Vector2(1400f, 976f), MenuTheme.Surface);
        var parent = card.transform;
        MenuTheme.Label(parent, font, title, 56, TextAnchor.MiddleLeft,
            new Vector2(0f, settings ? 233f : 405f),
            new Vector2(settings ? 722f : 1272f, 96f), MenuTheme.Text, true);

        if (settings) BuildSettings(parent);
        else BuildHelp(parent);

        screen.AddComponent<MenuNavigation>();
    }

    void BuildSettings(Transform parent)
    {
        AddSlider(parent, "Master Volume", GameAudio.MasterKey, GameAudio.Master, 95f);
        AddSlider(parent, "Camera Shake", GameAudio.ShakeKey, GameAudio.Shake, -58f);
        AddBack(parent, -209f);
        MenuTheme.Label(parent, font, "↑ ↓ or Tab to select   ·   ← → to adjust   ·   Esc to go back", 22,
            TextAnchor.MiddleCenter, new Vector2(0f, -275f), new Vector2(730f, 44f), MenuTheme.Muted);
    }

    void AddSlider(Transform parent, string label, string key, float value, float y)
    {
        const float width = 722f;
        var track = MenuTheme.Card(parent, label + " Slider", new Vector2(0f, y),
            new Vector2(width, 120f), Color.white);
        var slider = track.AddComponent<Slider>();
        slider.targetGraphic = track.GetComponent<Image>();

        var colors = slider.colors;
        colors.normalColor = new Color(.13f, .21f, .23f);
        colors.selectedColor = new Color(.20f, .34f, .31f);
        colors.highlightedColor = colors.selectedColor;
        colors.pressedColor = colors.selectedColor;
        colors.fadeDuration = .12f;
        slider.colors = colors;

        MenuTheme.Label(track.transform, font, label, 28, TextAnchor.MiddleLeft,
            new Vector2(-104f, 29f), new Vector2(456f, 42f), MenuTheme.Text, true);
        var percent = MenuTheme.Label(track.transform, font, Percent(value), 26, TextAnchor.MiddleRight,
            new Vector2(267f, 29f), new Vector2(130f, 42f), MenuTheme.Accent, true);
        var rail = SliderPart(track.transform, "Rail", new Vector2(0f, -24f),
            new Vector2(width - 56f, 9f), new Color(.38f, .49f, .48f));
        var fill = SliderPart(rail, "Fill", Vector2.zero, Vector2.zero, MenuTheme.Accent);
        var handle = SliderPart(rail, "Handle", Vector2.zero, new Vector2(18f, 31f), MenuTheme.Text);

        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.value = value;
        slider.onValueChanged.AddListener(newValue =>
        {
            PlayerPrefs.SetFloat(key, newValue);
            percent.text = Percent(newValue);
            GameAudio.ApplySettings();
        });
    }

    static RectTransform SliderPart(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var part = MenuTheme.Card(parent, name, position, size, color);
        var image = part.GetComponent<Image>();
        image.raycastTarget = false;
        return image.rectTransform;
    }

    static string Percent(float value) => Mathf.RoundToInt(value * 100f) + "%";

    void BuildHelp(Transform parent)
    {
        HelpCard(parent, "Goal", "Orientation → Shaking",
            "Find sturdy cover and look over your routes during orientation. Survive the shaking crouched fully under a desk or table when the countdown reaches zero.",
            -326f, 211f);
        HelpCard(parent, "Movement", "WASD / Arrows  ·  Mouse",
            "Move with WASD or arrows and look with the mouse. Press E near a key or door, Esc to pause, or H for help.",
            326f, 211f);
        var cover = HelpCard(parent, "Cover", "Hold C / Ctrl",
            "Get your whole body under sturdy cover and watch the shelter indicator. You can leave cover and return before time runs out.",
            -326f, -37f);
        DrawCover(cover);
        HelpCard(parent, "Hazards", "Watch and listen",
            "Look for shifting objects and listen for creaking. Stop, back away, or change routes before something falls. A dangerous impact can end the round.",
            326f, -37f);
        var shortcut = MenuTheme.Card(parent, "Optional shortcut", new Vector2(0f, -229f),
            new Vector2(1272f, 118f), new Color(.105f, .163f, .177f));
        MenuTheme.Label(shortcut.transform, font, "Optional shortcut in Two-Floor House", 26, TextAnchor.MiddleLeft,
            new Vector2(0f, 31f), new Vector2(1216f, 36f), MenuTheme.Accent, true);
        MenuTheme.Label(shortcut.transform, font,
            "During orientation, press E near the entry-wall key to collect it and near the locked shortcut to open it. Take another route around the separate jammed upstairs door.",
            24, TextAnchor.UpperLeft, new Vector2(0f, -21f), new Vector2(1216f, 60f), MenuTheme.Muted);
        MenuTheme.Label(parent, font,
            "The countdown is a game rule. Indoors during a real earthquake, drop, cover, and hold on.",
            22, TextAnchor.MiddleCenter, new Vector2(0f, -325f), new Vector2(1272f, 44f), MenuTheme.Muted);

        AddBack(parent, -395f);
        MenuTheme.Label(parent, font, "Esc to go back", 22, TextAnchor.MiddleCenter,
            new Vector2(0f, -452f), new Vector2(650f, 38f), MenuTheme.Muted);
    }

    Transform HelpCard(Transform parent, string title, string cue, string body, float x, float y)
    {
        var card = MenuTheme.Card(parent, title, new Vector2(x, y), new Vector2(620f, 230f),
            new Color(.105f, .163f, .177f));
        MenuTheme.Label(card.transform, font, title, 31, TextAnchor.MiddleLeft,
            new Vector2(0f, 75f), new Vector2(564f, 47f), MenuTheme.Text, true);
        MenuTheme.Label(card.transform, font, cue, 24, TextAnchor.MiddleLeft,
            new Vector2(0f, 31f), new Vector2(564f, 42f), MenuTheme.Accent, true);
        MenuTheme.Label(card.transform, font, body, 25, TextAnchor.UpperLeft,
            new Vector2(0f, -49f), new Vector2(564f, 110f), MenuTheme.Muted);
        return card.transform;
    }

    static void DrawCover(Transform card)
    {
        MenuTheme.Card(card, "Tabletop", new Vector2(220f, 83f), new Vector2(118f, 7f), MenuTheme.Muted);
        MenuTheme.Card(card, "Left leg", new Vector2(172f, 56f), new Vector2(5f, 50f), MenuTheme.Muted);
        MenuTheme.Card(card, "Right leg", new Vector2(268f, 56f), new Vector2(5f, 50f), MenuTheme.Muted);
        MenuTheme.Card(card, "Head", new Vector2(233f, 61f), new Vector2(14f, 14f), MenuTheme.Accent);
        var back = MenuTheme.Card(card, "Crouched back", new Vector2(219f, 45f), new Vector2(34f, 9f), MenuTheme.Accent);
        back.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
        MenuTheme.Card(card, "Legs", new Vector2(224f, 34f), new Vector2(28f, 6f), MenuTheme.Accent);
    }

    void AddBack(Transform parent, float y)
    {
        MenuTheme.Button(parent, font, "Back", new Vector2(0f, y), new Vector2(250f, 64f), Close);
    }

    public void Close()
    {
        if (!screen) return;

        consumedFrame = Time.frameCount;
        PlayerPrefs.Save();
        screen.SetActive(false);
        Destroy(screen);
        screen = null;

        if (hud) hud.SetActive(savedHud);
        if (caller) caller.SetActive(true);
        if (previousSelection && previousSelection.activeInHierarchy && EventSystem.current)
            EventSystem.current.SetSelectedGameObject(previousSelection);

        Time.timeScale = savedTime;
        AudioListener.pause = savedAudioPause;
        Cursor.lockState = savedCursorLock;
        Cursor.visible = savedCursorVisible;
    }

    void Update()
    {
        if (consumedFrame == Time.frameCount) return;

        if (screen)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
        }
        else if (GameFlow.Playing && Input.GetKeyDown(KeyCode.H))
        {
            var pause = FindFirstObjectByType<PauseMenu>();
            Help(pause && pause.IsPaused ? pause.panel : null);
        }
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
