// Copy into Assets/Editor in /tmp/seghani-level1-check with the checks listed in Docs/Level1Validation.md.
// Unity -batchmode -nographics -projectPath ... -executeMethod Level1ValidationLauncher.Run
// No -quit: the Play Mode runner exits after its asynchronous checks.
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class Level1ValidationLauncher
{
    public static void Run()
    {
        if(!Application.dataPath.StartsWith("/tmp/seghani-level1-check/"))throw new System.Exception("Disposable validation project only");
        RepairCheck.Run();
        Verify.Run();
        FixtureSupportCheck.Run();
        EditorSceneManager.OpenScene("Assets/Scenes/room.unity");
        EditorApplication.isPlaying=true;
    }
}
