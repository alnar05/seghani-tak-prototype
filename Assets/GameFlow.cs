using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Shared navigation; room construction and hazard authoring belong to the level.
public class GameFlow : MonoBehaviour
{
    public static bool Playing { get; private set; }

    static string selectedScene = "room";

    static void Load(bool playing, string scene)
    {
        Playing = playing;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        AudioListener.volume = 1f;
        Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !playing;
        SceneManager.LoadScene(scene);
    }

    public static void StartLevel(string scene)
    {
        selectedScene = scene;
        Load(true, scene);
    }
    public static void PlayLevel1() => StartLevel("room");
    public static void Retry() => Load(true, selectedScene);
    public static void MainMenu() => Load(false, "room");
    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        AudioListener.volume = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        var canvas = new GameObject("MainMenuCanvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.gameObject.AddComponent<GraphicRaycaster>();
        var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        if (!EventSystem.current)
        {
            var events = new GameObject("EventSystem");
            events.AddComponent<EventSystem>();
            events.AddComponent<StandaloneInputModule>();
        }
        Font font = Font.CreateDynamicFontFromOSFont("Arial", 36);
        var menu = SeghaniTakSceneBuilder.MakePanel(canvas.transform, "Main Menu", new Color(.025f,.045f,.05f,.78f));
        var select = SeghaniTakSceneBuilder.MakePanel(canvas.transform, "Level Select", new Color(.025f,.045f,.05f,.78f));
        Title(menu.transform, font, "EARTHQUAKE SURVIVAL");
        Title(select.transform, font, "LEVEL SELECT");
        var goal = SeghaniTakSceneBuilder.MakeText(menu.transform, "Goal", font, 30, TextAnchor.MiddleCenter,
            new Vector2(.5f,.5f), new Vector2(0,200), new Vector2(1400,60));
        goal.text = "Goal: be alive and crouched fully under the desk when the countdown ends.";
        var overlay = canvas.gameObject.AddComponent<OverlayMenus>();
        overlay.Initialize(font);
        Button(menu.transform, font, "Play", 100).onClick.AddListener(PlayLevel1);
        Button(menu.transform, font, "Level Select", -10).onClick.AddListener(() => { menu.SetActive(false); select.SetActive(true); Focus(select); });
        Button(menu.transform, font, "Settings", -120).onClick.AddListener(() => OverlayMenus.Settings(menu));
        Button(menu.transform, font, "How to play", -230).onClick.AddListener(() => OverlayMenus.Help(menu));
        Button(menu.transform, font, "Quit", -340).onClick.AddListener(Quit);
        Button(select.transform, font, "Level 1", 100).onClick.AddListener(PlayLevel1);
        Button(select.transform, font, "Level 2 - Unavailable", -10).interactable = false;
        Button(select.transform, font, "Level 3 - Unavailable", -120).interactable = false;
        Button(select.transform, font, "Back", -230).onClick.AddListener(() => { select.SetActive(false); menu.SetActive(true); Focus(menu); });

        menu.AddComponent<MenuNavigation>();
        select.AddComponent<MenuNavigation>();
        levelSelect = select; mainPanel = menu;
        select.SetActive(false);
        EventSystem.current.SetSelectedGameObject(menu.GetComponentInChildren<Button>().gameObject);
    }

    GameObject levelSelect, mainPanel;
    void Update()
    {
        if (!OverlayMenus.BlocksPause && Input.GetKeyDown(KeyCode.Escape) && levelSelect.activeSelf)
        { levelSelect.SetActive(false); mainPanel.SetActive(true); Focus(mainPanel); }
    }

    static void Focus(GameObject panel) => panel.GetComponentInChildren<Button>().Select();

    static Button Button(Transform parent, Font font, string label, float y) =>
        SeghaniTakSceneBuilder.MakeButton(parent, label, font, new Vector2(0, y));

    static void Title(Transform parent, Font font, string title)
    {
        var text = SeghaniTakSceneBuilder.MakeText(parent, "Title", font, 64, TextAnchor.MiddleCenter,
            new Vector2(.5f, .5f), new Vector2(0, 300), new Vector2(1600, 120));
        text.text = title;
    }
}
