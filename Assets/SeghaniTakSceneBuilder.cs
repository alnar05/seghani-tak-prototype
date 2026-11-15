using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SeghaniTakSceneBuilder : MonoBehaviour
{
    public Level1Configuration level = new Level1Configuration();

    [Header("Room")]
    public float roomWidth = 10f;
    public float roomDepth = 10f;
    public float roomHeight = 3f;
    public float wallThickness = 0.2f;

    [Header("Materials")]
    public Material baseMaterial;

    [Header("Desk")]
    public float deskTopHeight = 1.0f;


    Transform root;
    Text objectiveText, stanceText, resultTitle, resultBody;
    Image vignette;
    GameObject resultPanel, pausePanel, hudRoot;
    Button resumeButton, restartButton, quitButton;

    FirstPersonCrouchController playerCtrl;
    Camera playerCam;
    CameraShake shake;
    SafeZoneTrigger safeZone;
    SafeFeedback safeFeedback;
    EarthquakeDirector director;
    FallingProp frameProp, lampProp, shelfProp, windowProp, tvProp, spawnLightProp, northShelf, southCaution, northCaution, approachPanel;
    BoxCollider eastWall;

    void Awake()
    {
        bool menuBackdrop = !GameFlow.Playing;
        roomWidth = level.roomSize.x;
        roomDepth = level.roomSize.z;
        roomHeight = level.roomSize.y;
        foreach (var cam in Camera.allCameras) Destroy(cam.gameObject);

        root = new GameObject("Level1_Generated").transform;
        BuildRoom();
        Transform desk = BuildDesk();
        BuildFurniture();
        if (menuBackdrop)
        {
            // The menu shows the same room, furniture and shelter as the playable level.
            var view = new GameObject("Menu room camera").AddComponent<Camera>();
            view.transform.position = new Vector3(5.45f, 2.35f, -3.1f);
            view.transform.LookAt(new Vector3(1.1f, 1f, 1.3f));
            view.fieldOfView = 68f;
            gameObject.AddComponent<GameFlow>();
            return;
        }
        BuildUI();
        BuildPlayer();
        BuildSafeZone(desk, objectiveText);
        BuildFeedback();
        BuildDirector();
        BuildPause();
    }

    void BuildRoom()
    {
        float W = roomWidth, D = roomDepth, H = roomHeight, T = wallThickness;
        Color wall = new Color(.82f,.78f,.70f);
        Box("Floor", new Vector3(0,-.05f,0), new Vector3(W+2*T,.1f,D+2*T), new Color(.35f,.25f,.18f));
        Box("Ceiling", new Vector3(0,H+.05f,0), new Vector3(W+2*T,.1f,D+2*T), Color.gray);
        Box("Outer_South", new Vector3(0,H/2,-D/2-T/2), new Vector3(W+2*T,H,T), wall);
        Box("Outer_North", new Vector3(0,H/2,D/2+T/2), new Vector3(W+2*T,H,T), wall);
        Box("Outer_West", new Vector3(-W/2-T/2,H/2,0), new Vector3(T,H,D), wall);
        eastWall = Box("Outer_East", new Vector3(W/2+T/2,H/2,0), new Vector3(T,H,D), wall).GetComponent<BoxCollider>();
        // Two 1.8 m-wide, 2.3 m-high openings in each room/passsage partition.
        foreach (float x in new[] {-2f,2f})
        {
            Box("Partition_South", new Vector3(x,H/2,-3.025f), new Vector3(T,H,.95f), wall);
            Box("Partition_Center", new Vector3(x,H/2,0), new Vector3(T,H,1.5f), wall);
            Box("Partition_North", new Vector3(x,H/2,3.025f), new Vector3(T,H,.95f), wall);
            foreach (float z in new[] {-1.65f,1.65f})
                Box("Door_Lintel", new Vector3(x,(H+2.3f)/2,z), new Vector3(T,H-2.3f,1.8f),
                    x > 0 ? new Color(.35f,.55f,.4f) : wall);
        }
        Box("Passage_Divider", new Vector3(0,H/2,0), new Vector3(4,H,.8f), wall);
        foreach (float z in new[] {-2.8f,2.8f})
            Box("Passage_Outer", new Vector3(0,H/2,z), new Vector3(4,H,T), wall);
        // Seal unused strips: no narrow accidental paths behind the passage walls.
        foreach (float z in new[] {-3.2f,3.2f})
            Box("Passage_Void", new Vector3(0,H/2,z), new Vector3(4,H,.6f), wall);
        foreach (var position in new[] {new Vector3(-4,H-.4f,0), new Vector3(0,H-.4f,-1.65f),
            new Vector3(0,H-.4f,1.65f), new Vector3(4.8f,H-.4f,0)})
        {
            var lightGO = new GameObject("Room_Light"); lightGO.transform.SetParent(root,false);
            lightGO.transform.localPosition = position;
            var light = lightGO.AddComponent<Light>(); light.type = LightType.Point;
            light.range = 6f; light.intensity = 1.1f;
        }
    }

    GameObject DeskPart(Transform desk, string name, Vector3 position, Vector3 size, Color color)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube); part.name = name;
        part.transform.SetParent(desk,false); part.transform.localPosition = position;
        part.transform.localScale = size; Tint(part,color); return part;
    }

    Transform BuildDesk()
    {
        var desk = new GameObject("Desk").transform; desk.SetParent(root,false);
        const float width=2f, depth=1.2f, leg=.08f;
        var wood = new Color(.62f,.40f,.20f);
        DeskPart(desk,"Desk_Top",new Vector3(0,deskTopHeight+.05f,0),new Vector3(width,.1f,depth),wood);
        foreach (float x in new[] {-1f,1f}) foreach (float z in new[] {-1f,1f})
            DeskPart(desk,"Desk_Leg",new Vector3(x*(width-leg)/2,deskTopHeight/2,z*(depth-leg)/2),
                new Vector3(leg,deskTopHeight,leg),wood);
        Physics.SyncTransforms();
        // Derive placement from the actual generated east wall, including any configured room size.
        desk.position = new Vector3(eastWall.bounds.min.x-depth/2-.3f,0,0);
        desk.rotation = Quaternion.Euler(0,90,0);
        level.SetDeskPosition(desk.position,desk.position.x-depth/2);
        return desk;
    }

    void ValidateDesk(Transform desk)
    {
        Physics.SyncTransforms();
        foreach (var part in desk.GetComponentsInChildren<BoxCollider>())
        {
            if (part.bounds.max.x > eastWall.bounds.min.x-.25f)
                Debug.LogError("Desk part/trigger lacks wall clearance: " + part.name);
            if (Physics.ComputePenetration(part,part.transform.position,part.transform.rotation,
                eastWall,eastWall.transform.position,eastWall.transform.rotation,out _,out _))
                Debug.LogError("Desk intersects world-space wall: " + part.name);
        }
        var feet = desk.position + Vector3.up*.03f;
        foreach (var hit in Physics.OverlapCapsule(feet+Vector3.up*.3f,feet+Vector3.up*(playerCtrl.crouchHeight-.3f),
            .3f,~0,QueryTriggerInteraction.Ignore))
            if (hit.gameObject != playerCtrl.gameObject) Debug.LogError("Desk crouch access obstructed by " + hit.name);
    }

    void BuildFurniture()
    {
        Color wood = new Color(.55f,.40f,.25f), dark = new Color(.3f,.2f,.12f);
        Box("Couch", new Vector3(-3.4f,.4f,-3f), new Vector3(1.6f,.8f,.65f), new Color(.25f,.38f,.42f));
        Box("TV_Stand", new Vector3(-3.4f,.25f,2.9f), new Vector3(1.4f,.5f,.5f), dark);
        var entry = Box("EntryCeilingLight", new Vector3(-5,2.55f,-1.65f), new Vector3(.7f,.25f,.7f), Color.yellow);
        spawnLightProp = MakeProp(entry,5f,"Falling entry light",Vector3.zero,Vector3.zero);
        spawnLightProp.routeObstacle = false;
        entry.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
        var frame = Box("PictureFrame", new Vector3(-5.97f,1.7f,2.7f), new Vector3(.04f,.45f,.6f), wood);
        frameProp = MakeProp(frame,1f,"Picture frame",new Vector3(.5f,0,0),Vector3.zero);
        var lamp = Shape(PrimitiveType.Sphere,"CeilingLamp",new Vector3(3,2.55f,-2.7f),Vector3.one*.4f,Color.yellow);
        lampProp = MakeProp(lamp,3f,"Falling lamp",Vector3.zero,Vector3.zero);
        shelfProp = BuildBookcase(-1);
        northShelf = BuildBookcase(1);
        southCaution = BuildCautionLight(-1);
        northCaution = BuildCautionLight(1);
        approachPanel = BuildFixture("Shelter approach ceiling light", new Vector3(3.35f,2.75f,0), new Vector3(.7f,.16f,1.5f));
        var pane = Box("WindowPane",new Vector3(-5.97f,1.6f,-2.8f),new Vector3(.04f,1.2f,1.2f),new Color(.6f,.8f,.95f));
        windowProp = MakeProp(pane,4f,"Falling window pane",new Vector3(1.2f,0,0),Vector3.zero);
        var tv = Box("TV",new Vector3(-3.4f,.85f,2.9f),new Vector3(1.1f,.7f,.1f),new Color(.08f,.08f,.1f));
        tvProp = MakeProp(tv,12f,"Falling TV",new Vector3(0,0,6f),new Vector3(3f,0,0));
    }

    FallingProp BuildBookcase(int side)
    {
        var wood=new Color(.3f,.2f,.12f);
        string route=side<0?"South":"North";
        var shelf=new GameObject(route+"_Bookshelf"); shelf.transform.SetParent(root,false);
        shelf.transform.localPosition=new Vector3(1,.95f,side*2.325f);
        // One unscaled rigid body. Both visible sections move together; no static pedestal or joint.
        var lower=shelf.AddComponent<BoxCollider>(); lower.center=new Vector3(0,-.775f,0);
        lower.size=new Vector3(1.7f,.35f,.65f);
        var upper=shelf.AddComponent<BoxCollider>(); upper.center=new Vector3(0,.175f,0);
        upper.size=new Vector3(1.7f,1.55f,.65f);
        var model=new GameObject("Bookcase_Model").transform; model.SetParent(shelf.transform,false);
        DeskPart(model,"Cabinet_Base",new Vector3(0,-.775f,0),lower.size,wood);
        DeskPart(model,"Bookcase_Upper",new Vector3(0,.175f,0),upper.size,wood);
        foreach(var collider in model.GetComponentsInChildren<Collider>()) DestroyImmediate(collider);
        foreach(float y in new[]{-.4f,.2f,.7f}) for(int book=0;book<5;book++)
        {
            var spine=DeskPart(model,"Book_Front",new Vector3(-.7f+book*.3f,y,-side*.335f),
                new Vector3(.24f,.35f,.02f),new Color(.35f+book*.08f,.3f,.2f+book*.06f));
            DestroyImmediate(spine.GetComponent<Collider>());
        }
        var prop=MakeProp(shelf,50,route+" passage bookshelf",new Vector3(0,0,-side*35),new Vector3(-side*12,0,0));
        prop.pushPointLocal=new Vector3(0,.85f,0);
        prop.warningVisual=model;
        var body=shelf.GetComponent<Rigidbody>();
        body.interpolation=RigidbodyInterpolation.Interpolate;
        body.solverIterations=12; body.solverVelocityIterations=4;
        body.maxDepenetrationVelocity=2f;
        return prop;
    }

    FallingProp BuildCautionLight(int side) => BuildFixture(
        side < 0 ? "South passage ceiling light" : "North passage ceiling light",
        new Vector3(3.05f,2.75f,side*1.65f), new Vector3(1f,.16f,1.25f));

    FallingProp BuildFixture(string name, Vector3 position, Vector3 size)
    {
        // A single visible metal housing collider, clear of bookcases and door lintels.
        var housing = Box(name,position,size,new Color(.22f,.26f,.28f));
        var model = new GameObject("Light diffuser").transform;
        model.SetParent(housing.transform,false);
        var diffuser = DeskPart(model,"Warm diffuser",new Vector3(0,-.53f,0),new Vector3(.86f,.06f,.85f),new Color(1f,.91f,.67f));
        DestroyImmediate(diffuser.GetComponent<Collider>());
        var prop = MakeProp(housing,5,name,Vector3.zero,Vector3.zero);
        prop.routeObstacle = false;
        // A flat housing falls vertically and remains a stepable, physical floor object.
        housing.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
        prop.warningVisual = model;
        return prop;
    }

    FallingProp MakeProp(GameObject go, float mass, string label,
                         Vector3 impulse, Vector3 torque)
    {
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = mass;
        var fp = go.AddComponent<FallingProp>();
        fp.label = label;
        fp.pushImpulse = impulse;
        fp.torqueImpulse = torque;
        return fp;
    }

    void BuildPlayer()
    {
        var player = new GameObject("Player");
        player.transform.position = Level1Configuration.SpawnPosition + Vector3.up * .1f;
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

    void BuildSafeZone(Transform desk, Text objective)
    {
        var zone = new GameObject("SafeZone_UnderDesk");
        zone.transform.SetParent(desk, false);
        zone.transform.localPosition = new Vector3(0f, deskTopHeight / 2f, 0f);

        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(1.84f, deskTopHeight, 1.04f);

        var rb = zone.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        var sz = zone.AddComponent<SafeZoneTrigger>();
        sz.objectiveText = objective;
        safeZone = sz;
        ValidateDesk(desk);
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

    void BuildDirector()
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

        director.quakeStart = level.explorationSeconds;
        director.quakeEnd = level.explorationSeconds + level.shakingSeconds;
        var hazards = go.AddComponent<HazardScheduler>();
        hazards.director = director;
        director.hazards = hazards;
        hazards.configuration = level;
        hazards.props = new[] { frameProp, lampProp, shelfProp, windowProp, tvProp, spawnLightProp, northShelf, southCaution, northCaution, approachPanel };
        level.AuthorHazards(hazards.props);
        level.ValidateRoutes(playerCtrl);
    }

    void BuildPause()
    {
        var pm = new GameObject("PauseMenu").AddComponent<PauseMenu>();
        pm.panel = pausePanel;
        pm.hud = hudRoot;
        pm.director = director;

        resumeButton.onClick.AddListener(pm.ResumeGame);
        restartButton.onClick.AddListener(pm.RestartLevel);
        quitButton.onClick.AddListener(GameFlow.MainMenu);

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
        scaler.matchWidthOrHeight = .5f;

        Font font = Font.CreateDynamicFontFromOSFont("Arial", 36);
        Transform ct = canvasGO.transform;
        hudRoot = new GameObject("Gameplay HUD",typeof(RectTransform));
        hudRoot.transform.SetParent(ct,false);
        var hudRect = hudRoot.GetComponent<RectTransform>();
        hudRect.anchorMin=Vector2.zero; hudRect.anchorMax=Vector2.one;
        hudRect.offsetMin=hudRect.offsetMax=Vector2.zero;
        Transform hud = hudRoot.transform;

        Sprite vig = MakeVignetteSprite();
        vignette = MakeFullscreenImage(hud, "SafeVignette", vig, new Color(0.3f, 1f, 0.45f, 0f));

        objectiveText = MakeText(hud, "ObjectiveText", font, 28, TextAnchor.UpperCenter,
                                 new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1400, 120));
        objectiveText.fontStyle = FontStyle.Bold;
        objectiveText.horizontalOverflow = HorizontalWrapMode.Wrap;

        stanceText = MakeText(hud, "StanceText", font, 30, TextAnchor.LowerCenter,
                              new Vector2(0.5f, 0f), new Vector2(0f, 25f), new Vector2(900, 50));

        resultPanel = MakePanel(ct, "ResultPanel", new Color(.015f,.025f,.03f,.72f));
        var resultCard = MakePanel(resultPanel.transform, "ResultCard", new Color(.075f,.115f,.13f,.98f));
        var cardRect = resultCard.GetComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(.5f,.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(920,650);
        var border = resultCard.AddComponent<Outline>();
        border.effectColor = new Color(.3f,.42f,.43f,.5f);
        border.effectDistance = new Vector2(1,-1);

        resultTitle = MakeText(resultCard.transform, "ResultTitle", font, 56, TextAnchor.MiddleCenter,
                               new Vector2(.5f,.5f), new Vector2(0,185), new Vector2(800,90));
        resultTitle.fontStyle = FontStyle.Bold;
        resultBody = MakeText(resultCard.transform, "ResultBody", font, 30, TextAnchor.MiddleCenter,
                              new Vector2(.5f,.5f), new Vector2(0,65), new Vector2(760,130));
        resultBody.horizontalOverflow = HorizontalWrapMode.Wrap;
        resultBody.color = new Color(.78f,.83f,.84f);
        MakeButton(resultCard.transform, "Retry Level", font, new Vector2(0,-90)).onClick.AddListener(GameFlow.Retry);
        MakeButton(resultCard.transform, "Main Menu", font, new Vector2(0,-200)).onClick.AddListener(GameFlow.MainMenu);
        var resultHint = MakeText(resultCard.transform, "ResultControls", font, 22, TextAnchor.MiddleCenter,
            new Vector2(.5f,.5f), new Vector2(0,-280), new Vector2(800,40));
        resultHint.text = "R to retry  •  ↑ ↓ / Tab to select  •  Enter to confirm";
        resultHint.color = new Color(.6f,.69f,.71f);
        resultPanel.SetActive(false);

        pausePanel = MakePanel(ct, "PausePanel", new Color(0, 0, 0, 0.72f));
        Text pauseTitle = MakeText(pausePanel.transform, "PauseTitle", font, 80, TextAnchor.MiddleCenter,
                                   new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(1000, 120));
        pauseTitle.text = "PAUSED";
        pauseTitle.fontStyle = FontStyle.Bold;
        resumeButton = MakeButton(pausePanel.transform, "Resume", font, new Vector2(0f, 140f));
        restartButton = MakeButton(pausePanel.transform, "Restart Level", font, new Vector2(0f, -190f));
        quitButton = MakeButton(pausePanel.transform, "Main Menu", font, new Vector2(0f, -300f));
        
        Text hint = MakeText(pausePanel.transform, "ControlsHint", font, 24, TextAnchor.MiddleCenter,
                             new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1400, 40));
        hint.text = Level1Configuration.MovementHint + (level.allowJump ? "   Space: jump" : "") + "   C / Ctrl: crouch   Esc: pause";
        hint.color = new Color(1, 1, 1, 0.75f);
        
        MakeButton(pausePanel.transform, "Settings", font, new Vector2(0,-80)).onClick.AddListener(() => OverlayMenus.Settings(pausePanel));
        MakeButton(pausePanel.transform, "How to play", font, new Vector2(0,30)).onClick.AddListener(() => OverlayMenus.Help(pausePanel));
        var help = MakeButton(hud, "How to play (H)", font, Vector2.zero);
        var helpRect = help.GetComponent<RectTransform>();
        helpRect.anchorMin = helpRect.anchorMax = new Vector2(1,1);
        helpRect.anchoredPosition = new Vector2(-160,-200);
        helpRect.sizeDelta = new Vector2(280,60);
        var helpLabel = help.GetComponentInChildren<Text>();
        helpLabel.fontSize = 28; helpLabel.rectTransform.sizeDelta = new Vector2(280,60);
        help.onClick.AddListener(() => OverlayMenus.Help());
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
        var r = go.GetComponent<Renderer>();
        if (baseMaterial) r.sharedMaterial = new Material(baseMaterial);
        var m = r.material;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }
}
