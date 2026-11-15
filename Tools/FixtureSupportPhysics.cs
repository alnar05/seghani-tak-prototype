// Copy to Assets/Editor/FixtureSupportCheck.cs in /tmp/seghani-level1-check.
using UnityEngine;using System;using System.Reflection;
public static class FixtureSupportCheck
{
    static BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    public static void Run()
    {
        if(!Application.dataPath.StartsWith("/tmp/seghani-level1-check/"))throw new Exception("Disposable project only");
        typeof(GameFlow).GetField("<Playing>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,true);
        Physics.simulationMode=SimulationMode.Script;
        foreach(int side in new[]{-1,1})foreach(bool shelfFirst in new[]{false,true})
        {
            foreach(var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))UnityEngine.Object.DestroyImmediate(go);
            var builder=new GameObject("Builder").AddComponent<SeghaniTakSceneBuilder>();typeof(SeghaniTakSceneBuilder).GetMethod("Awake",F).Invoke(builder,null);
            var s=UnityEngine.Object.FindFirstObjectByType<HazardScheduler>();var d=s.director;
            typeof(EarthquakeDirector).GetField("<CurrentPhase>k__BackingField",F).SetValue(d,EarthquakeDirector.Phase.Shaking);
            foreach(var p in s.props)typeof(FallingProp).GetMethod("Awake",F).Invoke(p,null);
            var shelf=s.props[side<0?2:6];var light=s.props[side<0?7:8];
            if(shelfFirst){shelf.Trigger(d);for(int n=0;n<500;n++)Physics.Simulate(.02f);}
            light.Trigger(d);s.props[9].Trigger(d);for(int n=0;n<500;n++)Physics.Simulate(.02f);
            foreach(var p in new[]{light,s.props[9]})
            {
                if(!p.Settled||Mathf.Abs(p.RestingBounds.min.y)>.035f||p.RestingBounds.max.y>.3f)throw new Exception("Fixture not stepable on floor: "+p.name+" "+p.RestingBounds);
                bool floor=false;
                foreach(var c in Physics.OverlapBox(p.RestingBounds.center,p.RestingBounds.extents+Vector3.one*.025f))
                    if(c.attachedRigidbody!=p.GetComponent<Rigidbody>())
                    {Debug.Log("FIXTURE support="+c.name+" prop="+p.name+" bounds="+p.RestingBounds);if(c.name=="Floor")floor=true;else throw new Exception("Unintended fixture support "+c.name);}
                if(!floor)throw new Exception("No floor contact "+p.name);
            }
            Debug.Log("PASS fixture floor contact side="+side+" shelfFirst="+shelfFirst);
        }
    }
}
