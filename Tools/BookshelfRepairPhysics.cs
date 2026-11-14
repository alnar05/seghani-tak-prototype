// Copy to Assets/Editor/RepairCheck.cs only in the disposable validation project.
using UnityEngine;using System;using System.Reflection;
public static class RepairCheck {
 static BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 public static void Run(){
 if(!Application.dataPath.StartsWith("/tmp/seghani-level1-check/"))throw new Exception("Disposable validation project only");
 typeof(GameFlow).GetField("<Playing>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,true);
 Physics.simulationMode=SimulationMode.Script;Time.timeScale=1;
 foreach(int index in new[]{2,6}){
 foreach(var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))if(go)UnityEngine.Object.DestroyImmediate(go);
 var b=new GameObject("Builder").AddComponent<SeghaniTakSceneBuilder>();typeof(SeghaniTakSceneBuilder).GetMethod("Awake",F).Invoke(b,null);
 var s=UnityEngine.Object.FindFirstObjectByType<HazardScheduler>();var d=s.director;
 typeof(EarthquakeDirector).GetField("<CurrentPhase>k__BackingField",F).SetValue(d,EarthquakeDirector.Phase.Shaking);
 foreach(var p in s.props)typeof(FallingProp).GetMethod("Awake",F).Invoke(p,null);
 var p0=s.props[index];var rb=p0.GetComponent<Rigidbody>();
 Require(p0.GetComponents<Joint>().Length==0,"Joint retained");
 foreach(var c in p0.GetComponents<Collider>())foreach(var other in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))if(other!=c&&!other.isTrigger&&other.attachedRigidbody!=rb){if(Physics.ComputePenetration(c,c.transform.position,c.transform.rotation,other,other.transform.position,other.transform.rotation,out _,out float depth)&&depth>.002f)throw new Exception("INITIAL OVERLAP "+p0.name+" / "+other.name+" depth="+depth);}
 p0.Trigger(d);float maxSpeed=0,maxStep=0,minBottom=99;var previous=p0.transform.position;
 for(int i=0;i<500;i++){Physics.Simulate(.02f);maxSpeed=Mathf.Max(maxSpeed,rb.velocity.magnitude);maxStep=Mathf.Max(maxStep,Vector3.Distance(previous,p0.transform.position));previous=p0.transform.position;minBottom=Mathf.Min(minBottom,p0.RestingBounds.min.y);}
 Debug.Log("REPAIR "+p0.name+" bounds="+p0.RestingBounds+" rotation="+p0.transform.eulerAngles+" minY="+p0.RestingBounds.min.y+" maxSpeed="+maxSpeed+" maxStep="+maxStep+" minBottom="+minBottom+" settled="+p0.Settled);
 Require(p0.Settled,"Not settled");Require(Mathf.Abs(p0.RestingBounds.min.y)<.04f,"Hover/floor penetration");Require(minBottom>-.05f,"Deep penetration");Require(maxStep<.2f&&maxSpeed<8f,"Explosive/snap motion");
 Require(b.level.BlockedRoutes(p0.RestingBounds)==(index==2?1:2),"Wrong route occupied");
 Debug.Log("PASS isolated grounded rigid assembly "+p0.name);
 }
 }
 static void Require(bool v,string m){if(!v)throw new Exception(m);}
}
