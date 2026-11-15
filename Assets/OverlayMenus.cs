using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

// Modal screens restore the exact caller state: gameplay, pause menu, or main menu.
public class OverlayMenus : MonoBehaviour
{
    static OverlayMenus instance;
    static int consumedFrame = -1;
    public static bool BlocksPause => (instance && instance.open) || consumedFrame == Time.frameCount;
    GameObject screen, caller, hud;
    bool savedHud;
    GameObject previousSelection;
    Font font;
    float savedTime;
    bool savedAudio, savedCursor;
    CursorLockMode savedLock;
    bool open;
    readonly List<Selectable> navigation = new List<Selectable>();
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
        var pause = FindFirstObjectByType<PauseMenu>();
        hud = pause ? pause.hud : null;
        if (hud) { savedHud = hud.activeSelf; hud.SetActive(false); }
        Build();
    }

    void Build()
    {
        navigation.Clear();
        screen = SeghaniTakSceneBuilder.MakePanel(transform, settings ? "Settings" : "How to play", new Color(.06f,.08f,.11f,.99f));
        TextAt(settings ? "SETTINGS" : "HOW TO PLAY", 440, 52, 100);
        if (settings)
        {
            AddSlider("Master Volume", GameAudio.MasterKey, GameAudio.Master, 150);
            AddSlider("Camera Shake", "camera.shake", GameAudio.Shake, -10);
        }
        else
        {
            HelpCard("Goal", "SURVIVE", EarthquakeDirector.Briefing, -420, 170);
            HelpCard("Movement", "W A S D  /  ARROWS", "Move • Mouse to look • Esc to pause", 420, 170);
            HelpCard("Take Cover", "C  /  CTRL", "Hold to crouch under the sturdy desk.\nStay under it for protection.", -420, -100);
            HelpCard("Watch for Hazards", "!  →  WAIT / MOVE", "Listen for creaking and watch for movement.\nStop or retreat before an object falls.\nCheck the debris before continuing.", 420, -100);
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
    void HelpCard(string title, string icon, string body, float x, float y)
    {
        var card = SeghaniTakSceneBuilder.MakePanel(screen.transform, title, new Color(.12f,.18f,.2f));
        var rect = card.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
        rect.sizeDelta = new Vector2(780,240); rect.anchoredPosition = new Vector2(x,y);
        var heading = SeghaniTakSceneBuilder.MakeText(card.transform,"Heading",font,32,TextAnchor.MiddleLeft,
            new Vector2(.5f,.5f),new Vector2(0,82),new Vector2(700,45));
        heading.text = title; heading.fontStyle = FontStyle.Bold;
        if (title == "Take Cover")
        {
            IconBar(card.transform,new Vector2(285,91),new Vector2(100,9));
            IconBar(card.transform,new Vector2(244,64),new Vector2(8,48));
            IconBar(card.transform,new Vector2(326,64),new Vector2(8,48));
            IconBar(card.transform,new Vector2(281,55),new Vector2(32,20));
        }
        else if (title == "Watch for Hazards")
        {
            var sign=SeghaniTakSceneBuilder.MakeText(card.transform,"Warning sign",font,50,TextAnchor.MiddleCenter,
                new Vector2(.5f,.5f),new Vector2(300,77),new Vector2(70,65));
            sign.text="!"; sign.color=new Color(1f,.8f,.35f);
        }
        var keys = SeghaniTakSceneBuilder.MakeText(card.transform,"Control icon",font,26,TextAnchor.MiddleLeft,
            new Vector2(.5f,.5f),new Vector2(0,33),new Vector2(700,45));
        keys.text=icon; keys.color=new Color(.7f,.9f,.77f);
        var text = SeghaniTakSceneBuilder.MakeText(card.transform,"Instruction",font,27,TextAnchor.UpperLeft,
            new Vector2(.5f,.5f),new Vector2(0,-48),new Vector2(700,100));
        text.text=body; text.horizontalOverflow=HorizontalWrapMode.Wrap;
    }

    void IconBar(Transform parent, Vector2 position, Vector2 size)
    {
        var bar = new GameObject("Shelter illustration",typeof(RectTransform),typeof(Image));
        bar.transform.SetParent(parent,false);
        var image=bar.GetComponent<Image>(); image.color=new Color(.7f,.9f,.77f); image.raycastTarget=false;
        var rect=bar.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);
        rect.anchoredPosition=position;rect.sizeDelta=size;
    }

    public void Close()
    {
        if (!open) return;
        consumedFrame = Time.frameCount;
        screen.SetActive(false); Destroy(screen); open = false;
        if (caller) caller.SetActive(true);
        if (hud) hud.SetActive(savedHud);
        Time.timeScale = savedTime; AudioListener.pause = savedAudio;
        Cursor.lockState = savedLock; Cursor.visible = savedCursor;
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(previousSelection);
    }
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
