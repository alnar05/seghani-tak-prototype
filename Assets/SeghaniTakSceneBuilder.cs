using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SeghaniTakSceneBuilder : MonoBehaviour
{
    [Header("Room")]
    public float roomWidth = 10f;
    public float roomDepth = 10f;
    public float roomHeight = 3f;
    public float wallThickness = 0.2f;

    [Header("Materials")]
    public Material baseMaterial;

    [Header("Desk")]
    public float deskTopHeight = 1.0f;

    const float DoorCenterX = 3f, DoorWidth = 1.2f, DoorHeight = 2.2f;

    Transform root;
    Text objectiveText, stanceText, bannerText, resultTitle, resultBody;
    Image vignette;
    GameObject resultPanel, pausePanel;
    Button resumeButton, restartButton, quitButton;

    FirstPersonCrouchController playerCtrl;
    Camera playerCam;
    CameraShake shake;
    SafeZoneTrigger safeZone;
    EarthquakeDirector director;
    FallingProp frameProp, lampProp, shelfProp, windowProp, tvProp;

    void Awake()
    {
        foreach (var cam in Camera.allCameras) Destroy(cam.gameObject);

        root = new GameObject("Level1_Generated").transform;
        BuildRoom();
        Transform desk = BuildDesk();
        BuildFurniture();
        BuildUI();
        BuildPlayer();
        BuildSafeZone(desk, objectiveText);
        BuildFeedback();
        BuildDirector();
        BuildPause(); // BuildDebrisSpawner removed from here
    }

    void BuildRoom()
    {
        float W = roomWidth, D = roomDepth, H = roomHeight, T = wallThickness;
        Color wall = new Color(0.82f, 0.78f, 0.70f);

        Box("Floor", new Vector3(0, -0.05f, 0), new Vector3(W + 2 * T, 0.1f, D + 2 * T), new Color(0.35f, 0.25f, 0.18f));
        Box("Ceiling", new Vector3(0, H + 0.05f, 0), new Vector3(W + 2 * T, 0.1f, D + 2 * T), new Color(0.9f, 0.9f, 0.9f));
        Box("Wall_Back", new Vector3(0, H / 2, -(D / 2 + T / 2)), new Vector3(W + 2 * T, H, T), wall);
        Box("Wall_Left", new Vector3(-(W / 2 + T / 2), H / 2, 0), new Vector3(T, H, D), wall);
        Box("Wall_Right", new Vector3(W / 2 + T / 2, H / 2, 0), new Vector3(T, H, D), wall);

        float zFront = D / 2 + T / 2;
        float gapL = DoorCenterX - DoorWidth / 2f;
        float gapR = DoorCenterX + DoorWidth / 2f;
        float edgeL = -(W / 2 + T), edgeR = W / 2 + T;

        Box("Wall_Front_L", new Vector3((edgeL + gapL) / 2, H / 2, zFront), new Vector3(gapL - edgeL, H, T), wall);
        Box("Wall_Front_R", new Vector3((gapR + edgeR) / 2, H / 2, zFront), new Vector3(edgeR - gapR, H, T), wall);
        float lintelH = H - DoorHeight;
        Box("Wall_Front_Lintel", new Vector3(DoorCenterX, DoorHeight + lintelH / 2, zFront), new Vector3(DoorWidth, lintelH, T), wall);

        var hinge = new GameObject("Door_Hinge").transform;
        hinge.SetParent(root, false);
        hinge.localPosition = new Vector3(gapL, 0f, zFront);
        hinge.localRotation = Quaternion.Euler(0f, 80f, 0f);
        var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.name = "Door";
        door.transform.SetParent(hinge, false);
        door.transform.localPosition = new Vector3(DoorWidth / 2f, DoorHeight / 2f, 0f);
        door.transform.localScale = new Vector3(DoorWidth, DoorHeight, 0.08f);
        Tint(door, new Color(0.45f, 0.28f, 0.12f));

        var lightGO = new GameObject("Room_Light");
        lightGO.transform.SetParent(root, false);
        lightGO.transform.localPosition = new Vector3(0, H - 0.4f, 0);
        var l = lightGO.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 14f;
        l.intensity = 1.4f;
    }

    Transform BuildDesk()
    {
        var desk = new GameObject("Desk").transform;
        desk.SetParent(root, false);

        Color wood = new Color(0.40f, 0.26f, 0.14f);
        float topThick = 0.1f;
        float sizeX = 2.0f, sizeZ = 1.0f, legT = 0.08f;

        var top = Box("Desk_Top", new Vector3(0, deskTopHeight + topThick / 2, 0), new Vector3(sizeX, topThick, sizeZ), wood);
        top.transform.SetParent(desk, true);

        float lx = sizeX / 2 - legT / 2, lz = sizeZ / 2 - legT / 2;
        int i = 0;
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
            {
                var leg = Box("Desk_Leg_" + (++i), new Vector3(sx * lx, deskTopHeight / 2, sz * lz),
                              new Vector3(legT, deskTopHeight, legT), wood);
                leg.transform.SetParent(desk, true);
            }
        return desk;
    }

    void BuildFurniture()
    {
        Color teal = new Color(0.25f, 0.38f, 0.42f);
        Color wood = new Color(0.55f, 0.40f, 0.25f);
        Color dark = new Color(0.30f, 0.20f, 0.12f);

        Box("Couch", new Vector3(-1.8f, 0.4f, -2f), new Vector3(2.2f, 0.8f, 0.9f), teal);
        Box("Couch_Back", new Vector3(-1.8f, 0.95f, -2.3f), new Vector3(2.2f, 0.5f, 0.3f), teal);
        Box("CoffeeTable", new Vector3(2.5f, 0.225f, -1.5f), new Vector3(1.0f, 0.45f, 0.6f), wood);
        Box("TV_Stand", new Vector3(-3.5f, 0.25f, 4f), new Vector3(1.4f, 0.5f, 0.5f), dark);

        var frame = Box("PictureFrame", new Vector3(1.5f, 1.7f, 4.97f), new Vector3(0.6f, 0.45f, 0.04f), new Color(0.85f, 0.72f, 0.3f));
        frameProp = MakeProp(frame, 1f, "Picture frame hit you", 0.5f,
                             new Vector3(1.5f, 1f, 4.2f), new Vector3(1.6f, 2.5f, 1.6f),
                             new Vector3(0f, 0f, -0.5f), Vector3.zero);

        var lamp = Shape(PrimitiveType.Sphere, "CeilingLamp", new Vector3(0.3f, 2.55f, 0f), Vector3.one * 0.4f, new Color(1f, 0.95f, 0.6f));
        lampProp = MakeProp(lamp, 3f, "Falling lamp", 0.6f,
                            new Vector3(0.3f, 1.5f, 0f), new Vector3(2.6f, 3f, 2.6f),
                            Vector3.zero, Vector3.zero);

        var shelf = Box("Bookshelf", new Vector3(4.79f, 0.9f, -1f), new Vector3(0.4f, 1.8f, 1.4f), dark);
        shelfProp = MakeProp(shelf, 50f, "Bookshelf toppled", 0.9f,
                             new Vector3(3.6f, 1f, -1f), new Vector3(2.0f, 2.5f, 1.6f),
                             new Vector3(-90f, 0f, 0f), new Vector3(0f, 0f, 60f));

        var pane = Box("WindowPane", new Vector3(-4.97f, 1.6f, 1.5f), new Vector3(0.04f, 1.2f, 2.4f), new Color(0.6f, 0.8f, 0.95f));
        windowProp = MakeProp(pane, 4f, "Shattering glass", 0.4f,
                              new Vector3(-4.25f, 1f, 1.5f), new Vector3(1.5f, 2.5f, 3f),
                              new Vector3(1.2f, 0f, 0f), Vector3.zero);

        var tv = Box("TV", new Vector3(-3.5f, 0.85f, 4f), new Vector3(1.1f, 0.7f, 0.1f), new Color(0.08f, 0.08f, 0.1f));
        tvProp = MakeProp(tv, 12f, "TV fell on you", 0.6f,
                          new Vector3(-3.5f, 1f, 3f), new Vector3(2f, 2.5f, 1.6f),
                          new Vector3(0f, 0f, -6f), new Vector3(-3f, 0f, 0f));
    }

    FallingProp MakeProp(GameObject go, float mass, string label, float delay,
                         Vector3 zoneCenter, Vector3 zoneSize, Vector3 impulse, Vector3 torque)
    {
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = mass;
        var fp = go.AddComponent<FallingProp>();
        fp.label = label;
        fp.impactDelay = delay;
        fp.hazardCenter = zoneCenter;
        fp.hazardSize = zoneSize;
        fp.pushImpulse = impulse;
        fp.torqueImpulse = torque;
        return fp;
    }

    void BuildPlayer()
    {
        var player = new GameObject("Player");
        player.transform.position = new Vector3(-3.5f, 0.1f, -3.5f);
        player.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

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
        playerCtrl = fpc;
        playerCam = cam;
    }

    void BuildSafeZone(Transform desk, Text objective)
    {
        var zone = new GameObject("SafeZone_UnderDesk");
        zone.transform.SetParent(desk, false);
        zone.transform.localPosition = new Vector3(0f, deskTopHeight / 2f, 0f);

        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(1.6f, deskTopHeight, 0.7f);

        var rb = zone.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        var sz = zone.AddComponent<SafeZoneTrigger>();
        sz.objectiveText = objective;
        safeZone = sz;
    }

    void BuildFeedback()
    {
        var fb = new GameObject("Feedback").AddComponent<SafeFeedback>();
        fb.objectiveText = objectiveText;
        fb.stanceText = stanceText;
        fb.vignette = vignette;
        fb.zone = safeZone;
        fb.player = playerCtrl;
        fb.cam = playerCam;
        
        var go = new GameObject("SafeBurst");
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = 0.9f;
        main.startSpeed = 2.2f;
        main.startSize = 0.07f;
        main.startColor = new Color(0.4f, 1f, 0.5f);
        main.gravityModifier = 0.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        go.GetComponent<ParticleSystemRenderer>().material = new Material(Shader.Find("Sprites/Default"));
        fb.burst = ps;
    }

    void BuildDirector()
    {
        var go = new GameObject("EarthquakeDirector");
        director = go.AddComponent<EarthquakeDirector>();
        director.player = playerCtrl;
        director.shake = shake;
        director.shelter = safeZone;
        director.bannerText = bannerText;
        director.resultPanel = resultPanel;
        director.resultTitle = resultTitle;
        director.resultBody = resultBody;

        director.Schedule(13f, () => frameProp.Trigger(director));
        director.Schedule(18f, () => director.ShowMessage("It's getting worse!  Get low!", 2.5f, new Color(1f, 0.6f, 0.2f)));
        director.Schedule(20f, () => lampProp.Trigger(director));
        director.Schedule(23f, () => shelfProp.Trigger(director));
        director.Schedule(25f, () => windowProp.Trigger(director));
        director.Schedule(27f, () => tvProp.Trigger(director));
        director.Schedule(30f, () => director.ShowMessage("Stay down until it stops.", 3f, Color.white));

        float zFront = roomDepth / 2 + wallThickness / 2;
        var plug = new GameObject("Doorway_Blocker");
        plug.transform.SetParent(root, false);
        plug.transform.localPosition = new Vector3(DoorCenterX, DoorHeight / 2f, zFront);
        plug.AddComponent<BoxCollider>().size = new Vector3(DoorWidth, DoorHeight, wallThickness);

        var trig = new GameObject("Doorway_Hazard");
        trig.transform.SetParent(root, false);
        trig.transform.localPosition = new Vector3(DoorCenterX + 0.1f, DoorHeight / 2f, roomDepth / 2 - 0.4f);
        var tc = trig.AddComponent<BoxCollider>();
        tc.isTrigger = true;
        tc.size = new Vector3(1.0f, DoorHeight, 0.8f);
        var trb = trig.AddComponent<Rigidbody>();
        trb.isKinematic = true;
        var dh = trig.AddComponent<DoorwayHazard>();
        dh.director = director;
        dh.returnPosition = new Vector3(DoorCenterX + 0.1f, 0.1f, roomDepth / 2 - 1.8f);
    }

    void BuildPause()
    {
        var pm = new GameObject("PauseMenu").AddComponent<PauseMenu>();
        pm.panel = pausePanel;
        pm.director = director;

        resumeButton.onClick.AddListener(pm.ResumeGame);
        restartButton.onClick.AddListener(pm.RestartLevel);
        quitButton.onClick.AddListener(pm.QuitGame);

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

        Font font = Font.CreateDynamicFontFromOSFont("Arial", 36);
        Transform ct = canvasGO.transform;

        Sprite vig = MakeVignetteSprite();
        vignette = MakeFullscreenImage(ct, "SafeVignette", vig, new Color(0.3f, 1f, 0.45f, 0f));

        objectiveText = MakeText(ct, "ObjectiveText", font, 44, TextAnchor.UpperCenter,
                                 new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1400, 80));
        objectiveText.fontStyle = FontStyle.Bold;

        bannerText = MakeText(ct, "BannerText", font, 56, TextAnchor.MiddleCenter,
                              new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(1600, 100));
        bannerText.fontStyle = FontStyle.Bold;
        bannerText.color = new Color(1, 1, 1, 0);

        stanceText = MakeText(ct, "StanceText", font, 30, TextAnchor.LowerCenter,
                              new Vector2(0.5f, 0f), new Vector2(0f, 25f), new Vector2(900, 50));

        Text hint = MakeText(ct, "ControlsHint", font, 24, TextAnchor.LowerLeft,
                             new Vector2(0f, 0f), new Vector2(30f, 25f), new Vector2(1400, 40));
        hint.text = "WASD: move   Space: jump   Mouse: look   C / Ctrl: crouch   Esc: pause";
        hint.color = new Color(1, 1, 1, 0.75f);

        resultPanel = MakePanel(ct, "ResultPanel", new Color(0, 0, 0, 0.78f));
        resultTitle = MakeText(resultPanel.transform, "ResultTitle", font, 72, TextAnchor.MiddleCenter,
                               new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(1600, 120));
        resultTitle.fontStyle = FontStyle.Bold;
        resultBody = MakeText(resultPanel.transform, "ResultBody", font, 32, TextAnchor.MiddleCenter,
                              new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), new Vector2(1600, 700));
        resultPanel.SetActive(false);

        pausePanel = MakePanel(ct, "PausePanel", new Color(0, 0, 0, 0.72f));
        Text pauseTitle = MakeText(pausePanel.transform, "PauseTitle", font, 80, TextAnchor.MiddleCenter,
                                   new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(1000, 120));
        pauseTitle.text = "PAUSED";
        pauseTitle.fontStyle = FontStyle.Bold;
        resumeButton = MakeButton(pausePanel.transform, "Resume", font, new Vector2(0f, 90f));
        restartButton = MakeButton(pausePanel.transform, "Restart", font, new Vector2(0f, -20f));
        quitButton = MakeButton(pausePanel.transform, "Quit", font, new Vector2(0f, -130f));
        pausePanel.SetActive(false);
    }

    static GameObject MakePanel(Transform parent, string name, Color color)
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

    static Button MakeButton(Transform parent, string label, Font font, Vector2 pos)
    {
        var go = new GameObject(label + "Button");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.18f, 0.95f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.45f, 0.75f, 0.5f);
        colors.pressedColor = new Color(0.3f, 0.55f, 0.35f);
        btn.colors = colors;

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(420f, 80f);

        Text t = MakeText(go.transform, "Label", font, 40, TextAnchor.MiddleCenter,
                          new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 80f));
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

    static Text MakeText(Transform parent, string name, Font font, int size, TextAnchor align,
                         Vector2 anchor, Vector2 pos, Vector2 dims)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
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
        var r = go.GetComponent<Renderer>();
        if (baseMaterial) r.sharedMaterial = new Material(baseMaterial);
        var m = r.material;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }
}