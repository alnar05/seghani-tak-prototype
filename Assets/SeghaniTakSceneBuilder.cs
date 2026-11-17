using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class SeghaniTakSceneBuilder : MonoBehaviour
{
    public Level1Configuration level = new Level1Configuration();

    [Header("Room")]
    public float roomWidth = 10f;
    public float roomDepth = 10f;
    public float roomHeight = 3f;
    public float wallThickness = 0.2f;

    [Header("Materials")]
    public Material baseMaterial;

    Transform root;
    Text objectiveText, stanceText, resultTitle, resultBody, interactionText;
    Image vignette;
    GameObject resultPanel, pausePanel, hudRoot;
    Button nextButton;

    FirstPersonCrouchController playerCtrl;
    Camera playerCam;
    CameraShake shake;
    SafeZoneTrigger safeZone;
    SafeFeedback safeFeedback;
    EarthquakeDirector director;
    readonly Dictionary<Color, Material> paints = new Dictionary<Color, Material>();
    readonly List<Material> runtimeMaterials = new List<Material>();

    void Awake()
    {
        bool menuBackdrop = !GameFlow.Playing;
        int index = menuBackdrop ? 0 : GameFlow.SelectedLevelIndex;
        if (index != 0 || level == null) level = LevelCatalog.Create(index);
        roomWidth = level.roomSize.x;
        roomDepth = level.roomSize.z;
        roomHeight = level.roomSize.y;
        foreach (var cam in Camera.allCameras) Destroy(cam.gameObject);

        root = new GameObject(LevelCatalog.Names[index]).transform;
        BuildLayout(index);
        if (menuBackdrop)
        {
            var view = new GameObject("Menu room camera").AddComponent<Camera>();
            view.transform.position = new Vector3(5.45f, 2.27f, -3.11f);
            view.transform.LookAt(new Vector3(1.96f, 1.05f, 1.18f));
            view.fieldOfView = 69f;
            gameObject.AddComponent<GameFlow>();
            return;
        }
        BuildPauseController();
        BuildUI();
        BuildPlayer();
        safeZone = new GameObject("Shelter Check").AddComponent<SafeZoneTrigger>();
        safeZone.volumes = shelters.ToArray();
        safeZone.objectiveText = objectiveText;
        BuildFeedback();
        BuildDirector(index);
        pause.director = director;
        pause.panel = pausePanel;
        pause.hud = hudRoot;
        if (houseInteractions) houseInteractions.Initialize(playerCtrl, director, interactionText);
    }

    void OnDestroy()
    {
        foreach (var material in runtimeMaterials) Destroy(material);
        if (floorGrain) Destroy(floorGrain);
    }

    void BuildPlayer()
    {
        var player = new GameObject("Player");
        player.transform.position = level.spawn + Vector3.up * .06f;
        player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        var cc = player.AddComponent<CharacterController>();
        cc.radius = 0.3f;
        cc.height = 1.8f;
        cc.stepOffset = 0.3f;
        cc.skinWidth = 0.03f;

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body_Capsule";
        DestroyImmediate(body.GetComponent<Collider>());
        body.transform.SetParent(player.transform, false);
        body.GetComponent<Renderer>().enabled = false;

        var pivot = new GameObject("CameraPivot");
        pivot.transform.SetParent(player.transform, false);

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(pivot.transform, false);
        var cam = camGO.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f;
        cam.fieldOfView = 75f;
        camGO.AddComponent<AudioListener>();
        shake = camGO.AddComponent<CameraShake>();

        var fpc = player.AddComponent<FirstPersonCrouchController>();
        fpc.cameraPivot = pivot.transform;
        fpc.bodyVisual = body.transform;
        fpc.allowJump = level.allowJump; fpc.walkSpeed = level.walkSpeed; fpc.crouchSpeed = level.crouchSpeed;
        playerCtrl = fpc;
        playerCam = cam;
    }

    void BuildFeedback()
    {
        var fb = new GameObject("Feedback").AddComponent<SafeFeedback>();
        safeFeedback = fb;
        fb.objectiveText = objectiveText;
        fb.stanceText = stanceText;
        fb.vignette = vignette;
        fb.zone = safeZone;
        fb.player = playerCtrl;
        fb.cam = playerCam;
    }

    void BuildDirector(int index)
    {
        var go = new GameObject("EarthquakeDirector");
        director = go.AddComponent<EarthquakeDirector>();
        director.feedback = safeFeedback;
        director.hud = hudRoot;
        director.player = playerCtrl;
        director.shake = shake;
        director.shelter = safeZone;
        director.resultPanel = resultPanel;
        director.resultTitle = resultTitle;
        director.resultBody = resultBody;
        director.nextLevelButton = nextButton;
        director.levelTitle = LevelCatalog.Names[index];
        director.orientationTip = level.orientationTip;
        director.coverInstruction = level.coverInstruction;

        director.quakeStart = level.explorationSeconds;
        director.quakeEnd = level.explorationSeconds + level.shakingSeconds;
        var hazards = go.AddComponent<HazardScheduler>();
        hazards.director = director;
        director.hazards = hazards;
        hazards.configuration = level;
        hazards.props = props.ToArray();
        hazards.cues = cues;
        hazards.floorHeights = index < 3 ? new[] { 0f } : new[] { 0f, Level1Configuration.UpperFloor };
    }

    PauseMenu pause;

    void BuildPauseController()
    {
        pause = new GameObject("PauseMenu").AddComponent<PauseMenu>();

        if (EventSystem.current == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("UI_Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<GraphicRaycaster>();
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Transform ct = canvasGO.transform;
        hudRoot = new GameObject("Gameplay HUD",typeof(RectTransform));
        hudRoot.transform.SetParent(ct,false);
        var hudRect = hudRoot.GetComponent<RectTransform>();
        hudRect.anchorMin=Vector2.zero; hudRect.anchorMax=Vector2.one;
        hudRect.offsetMin=hudRect.offsetMax=Vector2.zero;
        Transform hud = hudRoot.transform;

        Sprite vig = MakeVignetteSprite();
        vignette = MakeFullscreenImage(hud, "SafeVignette", vig, new Color(0.3f, 1f, 0.45f, 0f));

        var objectiveCard = MakePanel(hud, "Objective card", new Color(.033f, .052f, .056f, .83f));
        var objectiveRect = objectiveCard.GetComponent<RectTransform>();
        objectiveRect.anchorMin = objectiveRect.anchorMax = objectiveRect.pivot = new Vector2(.5f, 1f);
        objectiveRect.anchoredPosition = new Vector2(0f, -27f);
        objectiveRect.sizeDelta = new Vector2(850f, 110f);
        objectiveCard.GetComponent<Image>().raycastTarget = false;
        objectiveText = MakeText(objectiveCard.transform, "ObjectiveText", font, 27, TextAnchor.MiddleCenter,
            new Vector2(.5f, .5f), Vector2.zero, new Vector2(810f, 100f));
        objectiveText.horizontalOverflow = HorizontalWrapMode.Wrap;

        stanceText = MakeText(hud, "StanceText", font, 20, TextAnchor.LowerCenter,
            new Vector2(.5f, 0f), new Vector2(0f, 26f), new Vector2(820f, 42f));
        if (houseInteractions)
        {
            interactionText = MakeText(hud, "Nearby interaction", font, 24, TextAnchor.LowerCenter,
                new Vector2(.5f, 0f), new Vector2(0f, 74f), new Vector2(900f, 40f));
            interactionText.color = MenuTheme.Accent;
        }
        var helpHint = MakeText(hud, "Help shortcut", font, 21, TextAnchor.UpperRight,
            new Vector2(1f, 1f), new Vector2(-26f, -22f), new Vector2(270f, 48f));
        helpHint.text = "H  How to Play";
        helpHint.color = MenuTheme.Muted;

        resultPanel = MakePanel(ct, "ResultPanel", MenuTheme.Backdrop);
        var resultCard = MenuTheme.Card(resultPanel.transform, "ResultCard", Vector2.zero,
            new Vector2(892f, 548f), MenuTheme.Surface);
        resultTitle = MenuTheme.Label(resultCard.transform, font, "", 57, TextAnchor.MiddleCenter,
            new Vector2(0f, 166f), new Vector2(780f, 86f), MenuTheme.Text, true);
        resultBody = MenuTheme.Label(resultCard.transform, font, "", 27, TextAnchor.MiddleCenter,
            new Vector2(0f, 91f), new Vector2(744f, 78f), MenuTheme.Muted);

        nextButton = MenuTheme.Button(resultCard.transform, font, "Next Level", new Vector2(0f, -5f),
            new Vector2(440f, 65f), GameFlow.NextLevel, true);
        MenuTheme.Button(resultCard.transform, font, "Retry Level", new Vector2(0f, -90f),
            new Vector2(440f, 65f), GameFlow.Retry);
        MenuTheme.Button(resultCard.transform, font, "Main Menu", new Vector2(0f, -175f),
            new Vector2(440f, 65f), GameFlow.MainMenu);
        resultPanel.SetActive(false);

        pausePanel = MakePanel(ct, "PausePanel", MenuTheme.Backdrop);
        var pauseCard = MenuTheme.Card(pausePanel.transform, "Pause card", Vector2.zero,
            new Vector2(690f, 740f), MenuTheme.Surface);
        MenuTheme.Label(pauseCard.transform, font, "Paused", 64, TextAnchor.MiddleCenter,
            new Vector2(0f, 259f), new Vector2(630f, 106f), MenuTheme.Text, true);
        MenuTheme.Button(pauseCard.transform, font, "Resume", new Vector2(0f, 141f),
            new Vector2(480f, 70f), pause.ResumeGame, true);
        MenuTheme.Button(pauseCard.transform, font, "How to Play", new Vector2(0f, 50f),
            new Vector2(480f, 70f), () => OverlayMenus.Help(pausePanel));
        MenuTheme.Button(pauseCard.transform, font, "Settings", new Vector2(0f, -41f),
            new Vector2(480f, 70f), () => OverlayMenus.Settings(pausePanel));
        MenuTheme.Button(pauseCard.transform, font, "Restart Level", new Vector2(0f, -132f),
            new Vector2(480f, 70f), pause.RestartLevel);
        MenuTheme.Button(pauseCard.transform, font, "Main Menu", new Vector2(0f, -223f),
            new Vector2(480f, 70f), GameFlow.MainMenu);
        MenuTheme.Label(pauseCard.transform, font, MenuTheme.NavigationHint, 20, TextAnchor.MiddleCenter,
            new Vector2(0f, -304f), new Vector2(600f, 40f), MenuTheme.Muted);
        var overlay = canvasGO.AddComponent<OverlayMenus>();
        overlay.Initialize(font);
        pausePanel.AddComponent<MenuNavigation>();
        resultPanel.AddComponent<MenuNavigation>();
        pausePanel.SetActive(false);
    }

    public static GameObject MakePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    public static Button MakeButton(Transform parent, string label, Font font, Vector2 pos)
    {
        var go = new GameObject(label + "Button");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = Color.white;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = new Color(.16f,.23f,.25f,.97f);
        colors.highlightedColor = new Color(.25f,.5f,.4f);
        colors.selectedColor = new Color(.25f,.5f,.4f);
        colors.pressedColor = new Color(0.3f, 0.55f, 0.35f);
        btn.colors = colors;

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(480f, 80f);

        Text t = MakeText(go.transform, "Label", font, 32, TextAnchor.MiddleCenter,
                          new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480f, 80f));
        t.text = label;
        t.raycastTarget = false;
        return btn;
    }

    static Image MakeFullscreenImage(Transform parent, string name, Sprite sprite, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return img;
    }

    public static Text MakeText(Transform parent, string name, Font font, int size, TextAnchor align,
                         Vector2 anchor, Vector2 pos, Vector2 dims)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        go.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);

        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = dims;
        return t;
    }

    static Sprite MakeVignetteSprite()
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x / (size - 1f) * 2f - 1f;
                float dy = y / (size - 1f) * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.SmoothStep(0.35f, 1.35f, d)));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    GameObject Shape(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(root, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        Tint(go, color);
        return go;
    }

    GameObject Box(string name, Vector3 pos, Vector3 scale, Color color) =>
        Shape(PrimitiveType.Cube, name, pos, scale, color);

    void Tint(GameObject go, Color c)
    {
        if (!paints.TryGetValue(c, out var material))
        {
            material = baseMaterial ? new Material(baseMaterial) : new Material(Shader.Find("Standard"));
            material.SetColor("_Color", c);
            material.SetFloat("_Glossiness", .18f);
            paints.Add(c, material);
            runtimeMaterials.Add(material);
        }
        go.GetComponent<Renderer>().sharedMaterial = material;
    }
}
