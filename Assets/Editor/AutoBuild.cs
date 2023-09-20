using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class AutoBuild : MonoBehaviour
{
    public static void PerformSwitchAndroid()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
    }
    [MenuItem("Auto Build/Android/Android Cheat", false, 0)]
    public static void AutoBuildAndroid()
    {
        List<string> sceneArrays = new List<string>();
        int count = EditorBuildSettings.scenes.Length;
        for (int i = 0; i < count; i++)
        {
            string path = EditorBuildSettings.scenes[i].path;
            if (EditorBuildSettings.scenes[i].enabled)
            {
                sceneArrays.Add(path);
            }
        }

        string outFolder = "./_build/android";
        string productName = PlayerSettings.productName;
        CreateFolder(outFolder);
        // Create the build options
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = sceneArrays.ToArray();
        buildPlayerOptions.locationPathName = outFolder + "/" + productName+".apk";
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;
        // Build the project
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
    private static void CreateFolder(string path)
    {
        Directory.CreateDirectory(path);
    }
}
