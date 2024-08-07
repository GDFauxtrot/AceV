#if UNITY_EDITOR || DEVELOPMENT_BUILD

using System.IO;
using UnityEngine;

public class LogAnywhere : MonoBehaviour
{
    string filename = "";
    void OnEnable() { Application.logMessageReceived += Log;  }
    void OnDisable() { Application.logMessageReceived -= Log; }

    public static string location = Application.dataPath;

    public void Log(string logString, string stackTrace, LogType type)
    {
        if (filename == "")
        {
            // string d = System.Environment.GetFolderPath(
            //   System.Environment.SpecialFolder.Desktop) + "/YOUR_LOGS";
            // System.IO.Directory.CreateDirectory(d);
            filename = Path.Combine(location, "/output.log");
        }

        try {
            System.IO.File.AppendAllText(filename, logString + "\n");
        }
        catch { }
    }
}

#endif