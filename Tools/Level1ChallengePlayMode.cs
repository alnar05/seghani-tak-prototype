// Copy to Assets/ChallengeRunner.cs in /tmp/seghani-level1-check only.
// Run using BookshelfSequenceLauncher with graphics enabled for rendered captures.
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Reflection;
using System.IO;
public class ChallengeRunner : MonoBehaviour
{
    static BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    int seed=1701; int generation; float deadline;
    string output="/tmp/seghani-captures";
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){if(Application.dataPath.StartsWith("/tmp/seghani-level1-check/")&&!FindFirstObjectByType<ChallengeRunner>())new GameObject("ChallengeRunner").AddComponent<ChallengeRunner>();}
    void Awake(){Physics.simulationMode=SimulationMode.FixedUpdate;DontDestroyOnLoad(gameObject);deadline=Time.realtimeSinceStartup+240;Time.maximumDeltaTime=.05f;Directory.CreateDirectory(output);SceneManager.sceneLoaded+=Loaded;Application.logMessageReceived+=Log;}
    void Loaded(Scene scene,LoadSceneMode mode){generation++;var b=FindFirstObjectByType<SeghaniTakSceneBuilder>();if(b)b.level.seed=seed;}
    void Log(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error){File.AppendAllText("/tmp/challenge-failure.txt",message+"\n"+stack+"\n");UnityEditor.EditorApplication.Exit(1);}}
    void Update(){if(Time.realtimeSinceStartup>deadline)throw new System.Exception("Challenge timeout");}
    IEnumerator Load(bool playing){int g=generation;if(playing)GameFlow.PlayLevel1();else GameFlow.MainMenu();while(generation==g)yield return null;yield return null;yield return null;}
    IEnumerator Start()
    {
        yield return null;yield return null;
        yield return Load(false);
        Capture("main-1920",1920,1080);
        var main=GameObject.Find("Main Menu");CheckNavigation(main);
        OverlayMenus.Help(main);yield return null;Capture("help-1280",1280,720);FindFirstObjectByType<OverlayMenus>().Close();
        OverlayMenus.Settings(main);yield return null;CheckSettings();Capture("settings-1024",1024,768);FindFirstObjectByType<OverlayMenus>().Close();
        if(!System.Array.Exists(System.Environment.GetCommandLineArgs(),a=>a=="-routesOnly"))
        foreach(int selectedSeed in new[]{1701,42,7})
        {
            seed=selectedSeed;yield return Load(true);
            var d=FindFirstObjectByType<EarthquakeDirector>();var s=d.hazards;
            d.player.enabled=false;
            var cc=d.player.GetComponent<CharacterController>();
            cc.enabled=false;d.player.transform.position=GameObject.Find("Desk").transform.position+Vector3.up*.03f;
            cc.height=.9f;cc.center=Vector3.up*.45f;cc.enabled=true;
            typeof(FirstPersonCrouchController).GetField("<IsCrouching>k__BackingField",F).SetValue(d.player,true);
            Time.timeScale=4;
            while(d.CurrentPhase==EarthquakeDirector.Phase.Calm){foreach(var p in s.props)Require(!p.Committed,"Warning before onset");yield return null;}
            while(!s.props[2].IsWarning&&!s.props[6].IsWarning)yield return null;
            var shelf=s.props[2].IsWarning?s.props[2]:s.props[6];
            Require(d.RoundTime>=d.quakeStart+s.configuration.openingDelayAfterOnset,"Early warning");
            var pause=FindFirstObjectByType<PauseMenu>();pause.PauseGame();float clock=d.RoundTime;
            OverlayMenus.Settings(pause.panel);yield return null;CheckSettings();
            Require(!pause.panel.activeSelf,"Pause overlapped Settings");
            FindFirstObjectByType<OverlayMenus>().Close();yield return null;
            Require(pause.IsPaused&&Time.timeScale==0&&AudioListener.pause&&pause.panel.activeSelf,"Settings resumed paused game");
            Require(!pause.hud.activeSelf,"Pause HUD overlap");
            Capture("pause-"+seed,1366,768);
            CheckNavigation(pause.panel);
            Require(d.RoundTime==clock,"Paused clock advanced");pause.ResumeGame();Time.timeScale=4;
            while(!shelf.HasReleased)yield return null;
            while(!shelf.Settled)yield return null;
            Require(Mathf.Abs(shelf.RestingBounds.min.y)<.04f,"Shelf not grounded");
            var view=Camera.main;view.transform.parent.position=new Vector3(-1.5f,1.65f,shelf.transform.position.z<0?-1.35f:1.35f);
            view.transform.parent.rotation=Quaternion.LookRotation(Vector3.right);
            d.feedback.vignette.enabled=false;
            Capture("settled-"+seed,1280,720);
            // Isolate the cosmetic camera effects, retaining the parent look rotation.
            var look=view.transform.parent.localRotation;
            d.shake.intensity=1;d.shake.Kick(1);PlayerPrefs.SetFloat("camera.shake",0);GameAudio.ApplySettings();yield return null;
            Require(view.transform.localPosition==Vector3.zero&&Quaternion.Angle(view.transform.localRotation,Quaternion.identity)<.001f,"Zero camera effects remain");
            Require(Quaternion.Angle(look,view.transform.parent.localRotation)<.001f,"Shake altered mouse-look pivot");
            Require(Mathf.Abs(view.fieldOfView-75)<.001f,"Zero FOV effect remains");
            typeof(SafeFeedback).GetField("fovKick",F).SetValue(d.feedback,6f);
            PlayerPrefs.SetFloat("camera.shake",1);GameAudio.ApplySettings();
            float offset=0,rotation=0;
            for(int sample=0;sample<10;sample++)
            {
                yield return null;
                offset=Mathf.Max(offset,view.transform.localPosition.sqrMagnitude);
                var q=view.transform.localRotation;
                rotation=Mathf.Max(rotation,q.x*q.x+q.y*q.y+q.z*q.z);
            }
            Require(offset>1e-12f&&rotation>1e-12f,"Maximum shake has no transform motion: offset="+offset+" rotation="+rotation);
            Require(view.fieldOfView<75,"Maximum setting has no FOV response");
            PlayerPrefs.SetFloat(GameAudio.MasterKey,0);GameAudio.ApplySettings();foreach(var a in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))Require(a.volume==0,"Master zero leaves audio");
            PlayerPrefs.SetFloat(GameAudio.MasterKey,.8f);GameAudio.ApplySettings();
            while(d.CurrentPhase!=EarthquakeDirector.Phase.Ended)yield return null;
            Require(!d.isDead,"Protected player died");
            Debug.Log("PASS shelter, shelf physics, Settings pause, camera extremes seed="+seed+" opening="+shelf.name);
            int g=generation;GameFlow.Retry();while(generation==g)yield return null;yield return null;
            Require(FindFirstObjectByType<EarthquakeDirector>().RoundTime<.3f,"Restart clock stale");
        }
        // Scripted constant-speed route strategies. Physical controller contacts are kept active.
        foreach(int selectedSeed in new[]{1701,42})foreach(int route in new[]{0,1})foreach(bool attentive in new[]{false,true})
        {
            seed=selectedSeed;yield return Load(true);
            var d=FindFirstObjectByType<EarthquakeDirector>();var s=d.hazards;var player=d.player;player.enabled=false;player.gameObject.AddComponent<ScriptedContactRelay>();
            var cc=player.GetComponent<CharacterController>();var path=s.configuration.Routes[route];int waypoint=1;float stopUntil=0;var settledCaptured=new System.Collections.Generic.HashSet<FallingProp>();var traced=new System.Collections.Generic.HashSet<FallingProp>();
            // Attentive driver switches away from the preselected initial threat before entering a passage.
            if(attentive){int threatened=new System.Random(seed).Next(2);path=s.configuration.Routes[1-threatened];}
            Time.timeScale=1;
            bool warningCapture=false;
            while(d.RoundTime<13&&d.CurrentPhase!=EarthquakeDirector.Phase.Ended)
            {
                if(!warningCapture&&s.props[route==0?7:8].IsWarning){warningCapture=true;Capture("warning-"+seed+"-"+route+"-"+attentive,1280,720);}
                foreach(var p in s.props)if(p.HasReleased&&traced.Add(p))Debug.Log("RELEASE "+p.label+" player="+player.transform.position+" time="+d.RoundTime);
                foreach(var p in s.props)if(p.HasReleased&&!p.routeObstacle&&p.Settled&&settledCaptured.Add(p))CaptureFixture(p,"fixture-"+seed+"-"+route+"-"+attentive+"-"+p.name);
                bool wait=false;
                if(attentive)foreach(var p in s.props)
                {
                    if(!p.Committed||p.routeObstacle||p.Settled)continue;
                    if(p.predictedArea.max.x<player.transform.position.x)continue;
                    if(Vector3.Distance(p.predictedArea.ClosestPoint(player.transform.position),player.transform.position)<2.4f)wait=true;
                }
                if(wait)stopUntil=d.RoundTime+.15f;
                float h=player.transform.position.x>s.configuration.CrouchApproachX?.9f:1.8f;cc.height=h;cc.center=Vector3.up*h/2;
                typeof(FirstPersonCrouchController).GetField("<IsCrouching>k__BackingField",F).SetValue(player,h<1);
                if(waypoint<path.Length&&d.RoundTime>=stopUntil)
                {
                    var delta=path[waypoint]-player.transform.position;delta.y=0;
                    cc.Move(Vector3.ClampMagnitude(delta,(h<1?player.crouchSpeed:player.walkSpeed)*Time.deltaTime)+Vector3.down*.02f);
                    if(delta.magnitude<.1f)waypoint++;
                }
                yield return null;
            }
            Debug.Log("STRATEGY seed="+seed+" route="+route+" attentive="+attentive+" dead="+d.isDead+" waypoint="+waypoint+" position="+player.transform.position);
            if(!attentive)Require(d.isDead||waypoint<path.Length,"Blind run still bypasses all decisions");
            if(attentive&&waypoint<path.Length)Debug.Log("BLOCKED DRIVER height="+cc.height+" center="+cc.center+" step="+cc.stepOffset+" crouchX="+s.configuration.CrouchApproachX+" contacts="+player.GetComponent<ScriptedContactRelay>().contacts);
            if(attentive)Require(!d.isDead&&waypoint>=path.Length,"Attentive strategy failed");
        }
        yield return Load(false);Require(!FindFirstObjectByType<EarthquakeDirector>(),"Menu retained round");
        PlayerPrefs.Save();Debug.Log("PASS challenge integration and transitions; rendered captures in "+output);
        UnityEditor.EditorApplication.Exit(0);
    }
    void CheckSettings()
    {
        var panel=GameObject.Find("Settings");var sliders=panel.GetComponentsInChildren<Slider>();
        Require(sliders.Length==2,"Settings sliders");Require(panel.GetComponentsInChildren<Button>().Length==1,"Settings buttons");
        float master=sliders[0].value,shake=sliders[1].value;
        sliders[0].value=0;Require(GameAudio.Master==0,"Master slider not applied");
        sliders[0].value=1;Require(GameAudio.Master==1,"Master slider max not applied");
        sliders[1].value=0;Require(GameAudio.Shake==0,"Shake slider not applied");
        sliders[1].value=1;Require(GameAudio.Shake==1,"Shake slider max not applied");
        sliders[0].value=master;sliders[1].value=shake;
    }
    void CheckNavigation(GameObject panel)
    {
        foreach(var control in panel.GetComponentsInChildren<Selectable>())if(control.IsInteractable())
        {
            Require(control.navigation.selectOnDown&&control.navigation.selectOnUp,"Broken keyboard navigation");
            Require(control.colors.normalColor!=control.colors.selectedColor,"No selected state");
        }
    }
    void CaptureFixture(FallingProp fixture,string name)
    {
        var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;
        var d=FindFirstObjectByType<EarthquakeDirector>();bool active=d.hud.activeSelf;d.hud.SetActive(false);
        camera.transform.position=new Vector3(2.15f,1.45f,fixture.transform.position.z);
        camera.transform.LookAt(fixture.RestingBounds.center);
        Capture(name,1280,720);
        camera.transform.position=position;camera.transform.rotation=rotation;d.hud.SetActive(active);
    }
    void Capture(string name,int width,int height)
    {
        if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
        var camera=Camera.main;if(!camera)camera=FindFirstObjectByType<Camera>();
        var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        var texture=new RenderTexture(width,height,24);camera.targetTexture=texture;
        foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.5f;}
        Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=texture;
        var read=new Texture2D(width,height,TextureFormat.RGB24,false);read.ReadPixels(new Rect(0,0,width,height),0,0);read.Apply();
        File.WriteAllBytes(output+"/"+name+".png",read.EncodeToPNG());
        foreach(var canvas in canvases)canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        camera.targetTexture=null;RenderTexture.active=null;Destroy(read);texture.Release();Destroy(texture);
    }
    static void Require(bool ok,string message){if(!ok)throw new System.Exception(message);}
}

// The scripted driver owns movement/crouch instead of hardware input. Keep the normal
// controller contact path alive, forwarding only actual Unity controller collisions.
public class ScriptedContactRelay : MonoBehaviour
{
    public string contacts="";
    System.Collections.Generic.HashSet<string> names=new System.Collections.Generic.HashSet<string>();
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if(transform.position.x>4&&names.Add(hit.collider.name))contacts+=hit.collider.name+" point="+hit.point+" normal="+hit.normal+"; ";
        var prop=hit.collider.GetComponentInParent<FallingProp>();
        if(prop)prop.HitController(hit);
    }
}
