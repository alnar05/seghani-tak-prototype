// Copy to Assets/RoundRunner.cs only in /tmp/seghani-level1-check.
// Automated 8x Play Mode state/physics checks, not a visual gameplay or listening test.
using UnityEngine;using UnityEngine.SceneManagement;using System.Collections;using System.Reflection;
public class RoundRunner:MonoBehaviour {
 int seed=1701;bool failed;float deadline;int sceneGeneration;float maxPenetration;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(!Application.dataPath.StartsWith("/tmp/seghani-level1-check/"))return;if(!FindFirstObjectByType<RoundRunner>())new GameObject("AutomatedRoundRunner").AddComponent<RoundRunner>();}
 void Awake(){Physics.simulationMode=SimulationMode.FixedUpdate;DontDestroyOnLoad(gameObject);deadline=Time.realtimeSinceStartup+100;Application.logMessageReceived+=Log;SceneManager.sceneLoaded+=Loaded;}
 void Loaded(Scene scene,LoadSceneMode mode){sceneGeneration++;var builder=FindFirstObjectByType<SeghaniTakSceneBuilder>();if(builder)builder.level.seed=seed;}
 void Log(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error){failed=true;Debug.unityLogger.logEnabled=false;System.IO.File.AppendAllText("/tmp/round-test-failure.txt",message+"\n"+stack);UnityEditor.EditorApplication.Exit(1);}}
 void Update(){
 foreach(var prop in FindObjectsByType<FallingProp>(FindObjectsSortMode.None))if(prop.HasReleased)
 foreach(var c in prop.GetComponents<Collider>())
 foreach(var other in Physics.OverlapBox(c.bounds.center,c.bounds.extents,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
 if(other.attachedRigidbody!=c.attachedRigidbody && (other.name=="Floor"||other.name.Contains("Passage_")||other.name.StartsWith("Outer_")))
 if(Physics.ComputePenetration(c,c.transform.position,c.transform.rotation,other,other.transform.position,other.transform.rotation,out _,out float depth))maxPenetration=Mathf.Max(maxPenetration,depth);
 if(Time.realtimeSinceStartup>deadline&&!failed)throw new System.Exception("ROUND TEST TIMEOUT");}
 IEnumerator Start(){
 yield return null;yield return null;
 foreach(int selectedSeed in new[]{1701,42}){
 seed=selectedSeed;
 int generation=sceneGeneration;GameFlow.PlayLevel1();while(sceneGeneration==generation)yield return null;yield return null;
 var d=FindFirstObjectByType<EarthquakeDirector>();var s=d.hazards;var pause=FindFirstObjectByType<PauseMenu>();
 Require(d.RoundTime<.3f,"Timeline not reset");
 // Remove input from this automated test; shelter the stationary test player to observe the whole round.
 d.player.enabled=false;var cc=d.player.GetComponent<CharacterController>();cc.enabled=false;
 d.player.transform.position=GameObject.Find("Desk").transform.position+Vector3.up*.03f;
 cc.height=.9f;cc.center=Vector3.up*.45f;cc.enabled=true;
 typeof(FirstPersonCrouchController).GetField("<IsCrouching>k__BackingField",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(d.player,true);
 Time.timeScale=8;
 while(d.CurrentPhase==EarthquakeDirector.Phase.Calm){foreach(var p in s.props){Require(!p.Committed&&!p.IsWarning&&!p.HasReleased,"Premature hazard during calm");Require(p.GetComponent<Rigidbody>().isKinematic,"Calm body moved");}yield return null;}
 Debug.Log("PASS CALM no warning/motion seed="+seed+" onset="+d.RoundTime);
 var rumble=GameObject.Find("Earthquake ambience").GetComponent<AudioSource>();
 Require(rumble.clip&&rumble.isPlaying,"Onset ambience not started");Require(d.shake.intensity>0,"Onset shake not started");Require(d.shelter.objectiveMessage.Contains("remaining"),"Onset countdown missing");
 FallingProp active=null;
 while(!active){foreach(var p in s.props)if(p.IsWarning)active=p;yield return null;}
 float warningAt=d.RoundTime;Require(warningAt>=d.quakeStart+s.configuration.openingDelayAfterOnset,"Warning before scheduled onset offset");
 yield return CheckPause(d,pause,active,"warning");
 while(!active.HasReleased)yield return null;
 Require(d.RoundTime-warningAt>=s.configuration.openingWarningSeconds-.4f,"Warning shortened");
 yield return null;
 yield return CheckPause(d,pause,active,"fall");
 while(!active.HasImpacted)yield return null;
 yield return CheckPause(d,pause,active,"impact");
 while(!active.Settled)yield return null;
 Require(active.HasImpacted,"No physical impact callback");Require(Mathf.Abs(active.RestingBounds.min.y)<.04f,"Resting shelf hovering");
 yield return CheckPause(d,pause,active,"settled");
 Debug.Log("PASS ROUND STAGES seed="+seed+" shelf="+active.name+" impact="+active.HasImpacted+" floor="+active.RestingBounds.min.y);
 while(d.CurrentPhase!=EarthquakeDirector.Phase.Ended)yield return null;
 Require(!d.isDead,"Sheltered player unexpectedly lost");Debug.Log("PASS full survival round seed="+seed+" ended="+d.RoundTime);
 generation=sceneGeneration;GameFlow.Retry();while(sceneGeneration==generation)yield return null;yield return null;
 var retry=FindFirstObjectByType<EarthquakeDirector>();Require(retry.RoundTime<.3f,"Retry clock stale");foreach(var p in retry.hazards.props)Require(!p.Committed&&!p.HasReleased,"Retry physics stale");
 generation=sceneGeneration;GameFlow.MainMenu();while(sceneGeneration==generation)yield return null;yield return null;
 Require(!FindFirstObjectByType<EarthquakeDirector>()&&!GameFlow.Playing,"Main Menu retained round");
 Debug.Log("PASS retry and Main Menu reset seed="+seed);
 }
 Require(maxPenetration<.03f,"Excessive boundary penetration: "+maxPenetration);
 Debug.Log("PASS max boundary penetration="+maxPenetration);
 Debug.Log("PASS ALL automated Play Mode round sequences (headless, not visual gameplay)");UnityEditor.EditorApplication.Exit(0);
 }
 IEnumerator CheckPause(EarthquakeDirector d,PauseMenu pause,FallingProp prop,string stage){
 float clock=d.RoundTime;var position=prop.transform.position;var rotation=prop.transform.rotation;
 var visual=prop.warningVisual?prop.warningVisual.localRotation:Quaternion.identity;
 if(stage=="fall")Require(!prop.Settled,"Fall checkpoint missed moving stage");
 pause.PauseGame();yield return new WaitForSecondsRealtime(.15f);
 Require(Mathf.Abs(d.RoundTime-clock)<.0001f,"Clock advanced during "+stage+" pause");
 Require(Vector3.Distance(position,prop.transform.position)<.001f&&Quaternion.Angle(rotation,prop.transform.rotation)<.01f,"Physics moved during "+stage+" pause");
 Require(!prop.warningVisual||Quaternion.Angle(visual,prop.warningVisual.localRotation)<.001f,"Warning animation moved while paused");
 pause.ResumeGame();Time.timeScale=8;Debug.Log("PASS pause/resume "+stage+" seed="+seed);
 }
 static void Require(bool value,string message){if(!value)throw new System.Exception(message);}
}
