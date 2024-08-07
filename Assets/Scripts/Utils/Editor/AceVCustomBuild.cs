#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.IO;

public class AceVCustomBuild 
{
    private static string[] levelsToBuild = new[]
    {
        "Assets/Scenes/MainScene.unity"
    };


    // Unity menu items
    [MenuItem("AceV/Build/Build Windows (Debug)")]
    public static void BuildWindowsDebug()
    {
        BuildWindows(true, true);
    }
    [MenuItem("AceV/Build/Build Windows (Dev)")]
    public static void BuildWindowsDev()
    {
        BuildWindows(true, false);
    }
    [MenuItem("AceV/Build/Build Windows (Release)")]
    public static void BuildWindowsRelease()
    {
        BuildWindows(false, false);
    }
    [MenuItem("AceV/Build/Build Mac (Debug)")]
    public static void BuildMacDebug()
    {
        BuildMac(true, true);
    }
    [MenuItem("AceV/Build/Build Mac (Dev)")]
    public static void BuildMacDev()
    {
        BuildMac(true, false);
    }
    [MenuItem("AceV/Build/Build Mac (Release)")]
    public static void BuildMacRelease()
    {
        BuildMac(false, false);
    }


    // Build functions
    private static void BuildWindows(bool isDevelopmentBuild, bool waitForDebugger)
    {
        // Get filename.
        string path = EditorUtility.SaveFolderPanel("Choose Build Folder", Application.dataPath, "");
        string buildFileName = $"AceV{(isDevelopmentBuild ? (waitForDebugger ? "-Debug" : "-Dev") : "-Release")}";
        string buildFileNameExt = buildFileName + ".exe";
        string buildPath = Path.Combine(path, buildFileNameExt);

        // Build player.
        BuildOptions windowsBuildOptions = BuildOptions.None;
        if (isDevelopmentBuild)
        {
            windowsBuildOptions = BuildOptions.Development | BuildOptions.AllowDebugging;
            EditorUserBuildSettings.waitForManagedDebugger = waitForDebugger;
        }
        BuildPipeline.BuildPlayer(levelsToBuild, buildPath, BuildTarget.StandaloneWindows64, windowsBuildOptions);

        // Copy internal "Assets" folder into the final build folder (ie. the actual game content!)
        string buildAssetsFolder = Path.Combine(Application.dataPath, "Assets");
        string buildAssetsDest = Path.Combine(Path.GetDirectoryName(buildPath), $"{buildFileName}_Data", "Assets");
        if (Directory.Exists(buildAssetsDest))
        {
            FileUtil.DeleteFileOrDirectory(buildAssetsDest);
        }
        string[] assetFiles = Directory.GetFiles(buildAssetsFolder, "*.*", SearchOption.AllDirectories);
        foreach (string f in assetFiles)
        {
            // THE MAIN REASON THIS BUILD FUNCTION EXISTS - ignore .meta files when copying
            if (Path.GetExtension(f) == ".meta")
            {
                continue;
            }

            string file = f.Remove(0,buildAssetsFolder.Length+1);
            string src = Path.Combine(buildAssetsFolder, file);
            string dst = Path.Combine(buildAssetsDest, file);

            // Create directory to file if it doesn't exist already
            if (!Directory.Exists(Path.GetDirectoryName(dst)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dst));
            }
            File.Copy(src, dst);
        }

        // Run the game (Process class from System.Diagnostics).
        // Process proc = new Process();
        // proc.StartInfo.FileName = path + "/BuiltGame.exe";
        // proc.Start();
    }


    private static void BuildMac(bool isDevelopmentBuild, bool waitForDebugger)
    {
        // Get filename.
        string path = EditorUtility.SaveFolderPanel("Choose Build Folder", Application.dataPath, "");
        string buildFileName = $"AceV{(isDevelopmentBuild ? (waitForDebugger ? "-Debug" : "-Dev") : "-Release")}";
        string buildFileNameExt = buildFileName + ".app";
        string buildPath = Path.Combine(path, buildFileNameExt);

        // Build player.
        BuildOptions macBuildOptions = BuildOptions.None;
        if (isDevelopmentBuild)
        {
            macBuildOptions = BuildOptions.Development | BuildOptions.AllowDebugging;
            EditorUserBuildSettings.waitForManagedDebugger = waitForDebugger;
        }
        BuildPipeline.BuildPlayer(levelsToBuild, buildPath, BuildTarget.StandaloneOSX, macBuildOptions);

        // Copy internal "Assets" folder into the final build folder (ie. the actual game content!)
        string buildAssetsFolder = Path.Combine(Application.dataPath, "Assets");
        string buildAssetsDest = Path.Combine(buildPath, "Contents", "Assets");
        if (Directory.Exists(buildAssetsDest))
        {
            FileUtil.DeleteFileOrDirectory(buildAssetsDest);
        }
        string[] assetFiles = Directory.GetFiles(buildAssetsFolder, "*.*", SearchOption.AllDirectories);
        foreach (string f in assetFiles)
        {
            // THE MAIN REASON THIS BUILD FUNCTION EXISTS - ignore .meta files when copying
            if (Path.GetExtension(f) == ".meta")
            {
                continue;
            }

            string file = f.Remove(0,buildAssetsFolder.Length+1);
            string src = Path.Combine(buildAssetsFolder, file);
            string dst = Path.Combine(buildAssetsDest, file);

            // Create directory to file if it doesn't exist already
            if (!Directory.Exists(Path.GetDirectoryName(dst)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dst));
            }
            File.Copy(src, dst);
        }

        // Run the game (Process class from System.Diagnostics).
        // Process proc = new Process();
        // proc.StartInfo.FileName = path + "/BuiltGame.exe";
        // proc.Start();
    }
}

#endif