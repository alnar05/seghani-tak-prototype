// Copy to Assets/Editor/LaunchRoundChecks.cs in the disposable validation project.
// Launch with -executeMethod LaunchRoundChecks.Run (without -quit); runner exits the Editor.
using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
public static class LaunchRoundChecks { public static void Run(){if(!Application.dataPath.StartsWith("/tmp/seghani-level1-check/"))throw new System.Exception("Disposable validation project only");EditorSceneManager.OpenScene("Assets/Scenes/room.unity");EditorApplication.isPlaying=true;} }
