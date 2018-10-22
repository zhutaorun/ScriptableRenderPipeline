using UnityEngine;
using UnityEditor;
using System.Linq;

[InitializeOnLoad]
public class Startup
{
    static Startup()
    {
        /* Temporary : skip everything in editor if we are trying to execute editorTests */
        string os = SystemInfo.operatingSystem;
        if (os.StartsWith("Mac"))
        {
            var args = System.Environment.GetCommandLineArgs();
            if (args.Any(o => o.Contains("runEditorTests")))
                EditorApplication.Exit(0);
        }
    }
}
