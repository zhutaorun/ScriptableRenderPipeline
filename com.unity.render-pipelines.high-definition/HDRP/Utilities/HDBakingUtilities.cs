using System.IO;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    internal static class HDBakingUtilities
    {
        internal static string GetBakedTextureDirectory(SceneManagement.Scene scene)
        {
            var scenePath = scene.path;
            var cacheDirectoryName = Path.GetFileNameWithoutExtension(scenePath);
            var cacheDirectory = Path.Combine(Path.GetDirectoryName(scenePath), cacheDirectoryName);
            return cacheDirectory;
        }

        internal static string GetBakedTextureFilePath(HDProbe probe)
        {
            var cacheDirectory = GetBakedTextureDirectory(probe.gameObject.scene);
            var targetFile = Path.Combine(
                cacheDirectory,
                string.Format("ReflectionProbe-{0}.exr", probe.name)
            );
            return targetFile;
        }

        internal static void CreateParentDirectoryIfMissing(string path)
        {
            var fileInfo = new FileInfo(path);
            if (!fileInfo.Directory.Exists)
                fileInfo.Directory.Create();
        }
    }
}
