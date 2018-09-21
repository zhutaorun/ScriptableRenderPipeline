using System;
using System.IO;
using System.Text.RegularExpressions;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    internal static class HDBakingUtilities
    {
        const string k_HDProbeAssetFormat = "ReflectionProbe-{0}.exr";
        static readonly Regex k_HDProbeAssetRegex = new Regex(@"(?<type>ReflectionProbe|PlanarProbe)-(?<index>\d+)\.exr");

        public enum SceneObjectCategory
        {
            ReflectionProbe
        }

        public static string HDProbeAssetPattern(ProbeSettings.ProbeType type)
        {
            return string.Format("{0}-*.exr", type);
        }

        public static string GetBakedTextureDirectory(SceneManagement.Scene scene)
        {
            var scenePath = scene.path;
            var cacheDirectoryName = Path.GetFileNameWithoutExtension(scenePath);
            var cacheDirectory = Path.Combine(Path.GetDirectoryName(scenePath), cacheDirectoryName);
            return cacheDirectory;
        }

        public static string GetBakedTextureFilePath(HDProbe probe)
        {
            return GetBakedTextureFilePath(
                probe.settings.type,
                SceneObjectIDMap.GetOrCreateSceneObjectID(
                    probe.gameObject, SceneObjectCategory.ReflectionProbe
                ),
                probe.gameObject.scene
            );
        }

        public static bool TryParseBakedProbeAssetFileName(
            string filename,
            out ProbeSettings.ProbeType type,
            out int index
        )
        {
            var match = k_HDProbeAssetRegex.Match(filename);
            if (!match.Success)
            {
                type = default(ProbeSettings.ProbeType);
                index = 0;
                return false;
            }

            type = (ProbeSettings.ProbeType)Enum.Parse(typeof(ProbeSettings.ProbeType), match.Groups["type"].Value);
            index = int.Parse(match.Groups["index"].Value);
            return true;
        }

        public static string GetBakedTextureFilePath(
            ProbeSettings.ProbeType probeType,
            int index,
            SceneManagement.Scene scene
        )
        {
            var cacheDirectory = GetBakedTextureDirectory(scene);
            var format = string.Empty;
            switch (probeType)
            {
                case ProbeSettings.ProbeType.ReflectionProbe:
                    format = k_HDProbeAssetFormat;
                    break;
                default:
                    throw new ArgumentException(string.Format("{0} is not handled.", probeType));
            }
            var targetFile = Path.Combine(
                cacheDirectory,
                string.Format(format, index)
            );
            return targetFile;
        }

        public static void CreateParentDirectoryIfMissing(string path)
        {
            var fileInfo = new FileInfo(path);
            if (!fileInfo.Directory.Exists)
                fileInfo.Directory.Create();
        }
    }
}
