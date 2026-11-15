// Copy to Assets/Editor/Level1RenderChecks.cs in /tmp/seghani-level1-check.
// Unity -batchmode -projectPath ... -executeMethod Level1RenderChecks.Run -quit
// Rendered native-physics endpoints, not a recording or human playtest.
using UnityEngine;using System;using System.Reflection;using System.IO;
public static class Level1RenderChecks
{
    static BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    public static void Run()
    {
        if(!Application.dataPath.StartsWith("/tmp/seghani-level1-check/"))throw new Exception("Disposable project only");
        typeof(GameFlow).GetField("<Playing>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,true);
        Physics.simulationMode=SimulationMode.Script;
        foreach(int side in new[]{-1,1})
        {
            foreach(var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))UnityEngine.Object.DestroyImmediate(go);
            var builder=new GameObject("Builder").AddComponent<SeghaniTakSceneBuilder>();typeof(SeghaniTakSceneBuilder).GetMethod("Awake",F).Invoke(builder,null);
            var scheduler=UnityEngine.Object.FindFirstObjectByType<HazardScheduler>();var director=scheduler.director;
            typeof(EarthquakeDirector).GetField("<CurrentPhase>k__BackingField",F).SetValue(director,EarthquakeDirector.Phase.Shaking);
            foreach(var p in scheduler.props)typeof(FallingProp).GetMethod("Awake",F).Invoke(p,null);
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.gameObject.SetActive(false);
            var camera=new GameObject("Validation observer").AddComponent<Camera>();camera.fieldOfView=68;camera.nearClipPlane=.05f;
            camera.transform.position=new Vector3(2.15f,1.45f,side*1.65f);camera.transform.LookAt(scheduler.props[side<0?7:8].RestingBounds.center);
            Capture(camera,"fixture-mounted-"+(side<0?"south":"north"));
            scheduler.props[side<0?2:6].Trigger(director);
            for(int i=0;i<500;i++)Physics.Simulate(.02f);
            var fixture=scheduler.props[side<0?7:8];fixture.Trigger(director);scheduler.props[9].Trigger(director);
            for(int i=0;i<500;i++)Physics.Simulate(.02f);
            camera.transform.position=new Vector3(-1.5f,1.65f,side*1.4f);camera.transform.LookAt(scheduler.props[side<0?2:6].RestingBounds.center);
            Capture(camera,"bookshelf-rest-"+(side<0?"south":"north"));
            camera.transform.position=new Vector3(2.15f,1.45f,side*1.65f);camera.transform.LookAt(fixture.RestingBounds.center);
            Capture(camera,"fixture-floor-"+(side<0?"south":"north"));
            camera.transform.position=new Vector3(2.1f,1.5f,.2f);camera.transform.LookAt(scheduler.props[9].RestingBounds.center);
            Capture(camera,"approach-clearance-"+(side<0?"south":"north"));
            Debug.Log("PASS rendered floor fixture and shelter gap side="+side);
        }
    }
    static void Capture(Camera camera,string name)
    {
        var target=new RenderTexture(1280,720,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
        var read=new Texture2D(1280,720,TextureFormat.RGB24,false);read.ReadPixels(new Rect(0,0,1280,720),0,0);read.Apply();
        Directory.CreateDirectory("/tmp/seghani-captures");File.WriteAllBytes("/tmp/seghani-captures/"+name+".png",read.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(read);target.Release();UnityEngine.Object.DestroyImmediate(target);
    }
}
