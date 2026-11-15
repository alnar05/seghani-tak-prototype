using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameFlow : MonoBehaviour
{
    const string RoomScene = "room";

    public static bool Playing { get; private set; }
    public static int SelectedLevelIndex { get; private set; }
    public static bool HasNextLevel => SelectedLevelIndex + 1 < LevelCatalog.Count;

    static string selectedScene = RoomScene;
    static bool loading;

    GameObject levelSelect, mainPanel;
    Button levelSelectButton;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetNavigation()
    {
        Playing = false;
        SelectedLevelIndex = 0;
        selectedScene = RoomScene;
        loading = false;
        SceneManager.sceneLoaded -= RestoreAfterLoad;
    }

    static void Load(bool playing, string scene, int levelIndex)
    {
        if (loading || !Application.CanStreamedLevelBeLoaded(scene)) return;

        loading = true;
        Playing = playing;
        SelectedLevelIndex = levelIndex;
        if (playing) selectedScene = scene;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        SceneManager.sceneLoaded += RestoreAfterLoad;
        SceneManager.LoadScene(scene);
    }

    static void RestoreAfterLoad(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= RestoreAfterLoad;
        loading = false;
        RestoreInteraction();
    }

    static void RestoreInteraction()
    {
        Time.timeScale = 1f;
        GameAudio.ApplyMasterVolume();
        AudioListener.pause = false;
        Cursor.lockState = Playing ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !Playing;
    }

    public static void StartLevel(int index)
    {
        if (index >= 0 && index < LevelCatalog.Count) Load(true, RoomScene, index);
    }

    public static void StartLevel(string scene) => Load(true, scene, 0);
    public static void PlayLevel1() => StartLevel(0);
    public static void Retry() => Load(true, selectedScene, SelectedLevelIndex);
    public static void MainMenu() => Load(false, RoomScene, 0);

    public static void NextLevel()
    {
        if (HasNextLevel) StartLevel(SelectedLevelIndex + 1);
    }

    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Start()
    {
        RestoreInteraction();
        var canvas = new GameObject("MainMenuCanvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.gameObject.AddComponent<GraphicRaycaster>();

        var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        if (!EventSystem.current)
        {
            var events = new GameObject("EventSystem");
            events.AddComponent<EventSystem>();
            events.AddComponent<StandaloneInputModule>();
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var overlay = canvas.gameObject.AddComponent<OverlayMenus>();
        overlay.Initialize(font);

        mainPanel = MakeSidePanel(canvas.transform, "Main Menu", new Vector2(680f, 866f));
        levelSelect = MakeSidePanel(canvas.transform, "Level Select", new Vector2(770f, 930f));

        BuildMainMenu(font);
        BuildLevelSelect(font);

        levelSelect.SetActive(false);
        mainPanel.AddComponent<MenuNavigation>();
        levelSelect.AddComponent<MenuNavigation>();
    }

    static GameObject MakeSidePanel(Transform canvas, string name, Vector2 size)
    {
        var panel = MenuTheme.Card(canvas, name, Vector2.zero, size, MenuTheme.Surface);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, .5f);
        rect.anchoredPosition = new Vector2(size.x * .5f + 76f, 0f);

        var accent = MenuTheme.Card(panel.transform, "Top rule", new Vector2(0f, size.y * .5f - 2f),
            new Vector2(size.x, 4f), MenuTheme.Accent);
        accent.GetComponent<Image>().raycastTarget = false;
        return panel;
    }

    void BuildMainMenu(Font font)
    {
        var parent = mainPanel.transform;
        MenuTheme.Label(parent, font, "Seghani Tak", 76, TextAnchor.MiddleLeft,
            new Vector2(0f, 324f), new Vector2(568f, 108f), MenuTheme.Text, true);
        MenuTheme.Label(parent, font, "Indoor earthquake survival", 30, TextAnchor.MiddleLeft,
            new Vector2(0f, 252f), new Vector2(568f, 52f), MenuTheme.Accent);
        MenuTheme.Label(parent, font, "Be alive and crouched fully under sturdy cover when the countdown ends.", 27,
            TextAnchor.UpperLeft, new Vector2(0f, 178f), new Vector2(568f, 82f), MenuTheme.Muted);

        MainButton(font, "Play", 66f, PlayLevel1, true);
        levelSelectButton = MainButton(font, "Level Select", -18f, () => ShowLevelSelect(true));
        MainButton(font, "Settings", -102f, () => OverlayMenus.Settings(mainPanel));
        MainButton(font, "How to Play", -186f, () => OverlayMenus.Help(mainPanel));
        MainButton(font, "Quit", -270f, Quit);

        MenuTheme.Label(parent, font, MenuTheme.NavigationHint, 22, TextAnchor.MiddleLeft,
            new Vector2(0f, -371f), new Vector2(568f, 50f), MenuTheme.Muted);
    }

    Button MainButton(Font font, string label, float y, UnityEngine.Events.UnityAction action, bool primary = false)
    {
        var button = MenuTheme.Button(mainPanel.transform, font, label, new Vector2(0f, y),
            new Vector2(568f, 70f), action, primary);
        button.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
        return button;
    }

    void BuildLevelSelect(Font font)
    {
        var parent = levelSelect.transform;
        MenuTheme.Label(parent, font, "Level Select", 54, TextAnchor.MiddleLeft,
            new Vector2(0f, 366f), new Vector2(658f, 92f), MenuTheme.Text, true);

        for (int i = 0; i < LevelCatalog.Count; i++)
        {
            int index = i;
            var row = MenuTheme.Button(parent, font, LevelCatalog.Names[i], new Vector2(0f, 262f - 115f * i),
                new Vector2(658f, 104f), () => StartLevel(index));
            var label = row.GetComponentInChildren<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            MenuTheme.Place(label.rectTransform, new Vector2(28f, 26f), new Vector2(558f, 37f));
            MenuTheme.Label(row.transform, font, (i + 1).ToString("00"), 24, TextAnchor.MiddleCenter,
                new Vector2(-286f, 26f), new Vector2(60f, 37f), MenuTheme.Accent, true);
            MenuTheme.Label(row.transform, font, LevelCatalog.Descriptions[i], 22, TextAnchor.UpperLeft,
                new Vector2(28f, -21f), new Vector2(558f, 48f), MenuTheme.Muted);
        }

        MenuTheme.Button(parent, font, "Back", new Vector2(-236f, -292f),
            new Vector2(186f, 68f), () => ShowLevelSelect(false));
        MenuTheme.Label(parent, font, "Esc to go back", 22, TextAnchor.MiddleLeft,
            new Vector2(44f, -292f), new Vector2(350f, 48f), MenuTheme.Muted);
    }

    void ShowLevelSelect(bool show)
    {
        if (show)
        {
            mainPanel.SetActive(false);
            levelSelect.SetActive(true);
        }
        else
        {
            levelSelect.SetActive(false);
            mainPanel.SetActive(true);
            levelSelectButton.Select();
        }
    }

    void Update()
    {
        if (levelSelect.activeSelf && !OverlayMenus.BlocksPause && Input.GetKeyDown(KeyCode.Escape))
            ShowLevelSelect(false);
    }
}
