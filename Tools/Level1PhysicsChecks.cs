// Headless geometry/physics checks, NOT an interactive difficulty test.
// Copy to Assets/Editor/Verify.cs in /tmp/seghani-level1-check (disposable copy).
// Run Unity -batchmode -nographics -projectPath /tmp/seghani-level1-check -executeMethod Verify.Run -quit.
// This test rebuilds scene objects. It is intentionally outside Assets in the real project.
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
public static class Verify {
 static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Invoke(object target,string method) {target.GetType().GetMethod(method,flags).Invoke(target,null);}
 static void Require(bool value,string message) {if(!value) throw new Exception(message);}
 public static void Run() {
  Require(Application.dataPath.StartsWith("/tmp/seghani-level1-check/"),"Run only in the disposable validation project");
  typeof(GameFlow).GetField("<Playing>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,true);
  Physics.simulationMode=SimulationMode.Script; Time.timeScale=1;
  foreach(int seed in new[]{1701,7,42}) {
   foreach(var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)) if(go) UnityEngine.Object.DestroyImmediate(go);
   var builder=new GameObject("Builder").AddComponent<SeghaniTakSceneBuilder>(); Invoke(builder,"Awake");
   var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonCrouchController>();
   var cc=player.GetComponent<CharacterController>();
   var scheduler=UnityEngine.Object.FindFirstObjectByType<HazardScheduler>(); var config=builder.level;
   var zone=UnityEngine.Object.FindFirstObjectByType<SafeZoneTrigger>(); Invoke(zone,"Awake");
   Require(!player.allowJump,"Level1 jump enabled");
   foreach(var prop in scheduler.props)Invoke(prop,"Awake");
   foreach(var desk in GameObject.Find("Desk").GetComponentsInChildren<BoxCollider>()) {
    var wall=GameObject.Find("Outer_East").GetComponent<BoxCollider>(); Physics.SyncTransforms();
    Require(!Physics.ComputePenetration(desk,desk.transform.position,desk.transform.rotation,wall,wall.transform.position,wall.transform.rotation,out _,out _),"Desk/wall penetration");
    Require(wall.bounds.min.x-desk.bounds.max.x>=.25f,"Desk lacks setback");
   }
   Navigate(config.Routes[0],config,player,cc,true);Navigate(config.Routes[1],config,player,cc,true);
   typeof(FirstPersonCrouchController).GetField("<IsCrouching>k__BackingField",flags).SetValue(player,true);
   zone.Evaluate(player);Require(zone.IsSafe,"Crouched capsule not safe at desk");
   Debug.Log("PASS initial routes and desk: seed="+seed);
   var director=scheduler.director;
   typeof(EarthquakeDirector).GetField("<CurrentPhase>k__BackingField",flags).SetValue(director,EarthquakeDirector.Phase.Shaking);
   int route=new System.Random(seed).Next(2);var falling=scheduler.props[route==0?2:6];
   falling.Trigger(director);for(int i=0;i<800;i++)Physics.Simulate(.02f);
   Debug.Log("Actual shelf seed="+seed+" route="+route+" bounds="+falling.RestingBounds+" rotation="+falling.transform.eulerAngles);
   Require(falling.Settled,"Shelf never settled");
   Require(!Navigate(config.Routes[route],config,player,cc,false),"Fallen bookcase bypassed without jumping");
   Navigate(config.Routes[1-route],config,player,cc,true);
   var light=scheduler.props[route==0?8:7]; light.Trigger(director);for(int i=0;i<600;i++)Physics.Simulate(.02f);
   Navigate(config.Routes[1-route],config,player,cc,true);
   Debug.Log("PASS physical obstruction, no-jump alternate and restart rebuild: seed="+seed);
  }
 }
 static bool Navigate(Vector3[] route,Level1Configuration config,FirstPersonCrouchController player,CharacterController cc,bool required) {
  cc.enabled=false;player.transform.position=route[0]+Vector3.up*.03f;cc.enabled=true;
  foreach(var point in route) {
   for(int step=0;step<600;step++) {
    float height=player.transform.position.x>config.CrouchApproachX?player.crouchHeight:player.standHeight;
    cc.height=height;cc.center=Vector3.up*height/2;
    var delta=point-player.transform.position;delta.y=0;if(delta.magnitude<.03f)break;
    cc.Move(Vector3.ClampMagnitude(delta,.025f)+Vector3.down*.005f);
   }
   var diff=point-player.transform.position;diff.y=0;
   if(diff.magnitude>.1f) {if(required)throw new Exception("No-jump route blocked: "+player.transform.position+" -> "+point);return false;}
  }
  return true;
 }
}