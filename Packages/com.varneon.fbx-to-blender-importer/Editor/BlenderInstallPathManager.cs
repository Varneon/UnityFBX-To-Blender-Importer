
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Varneon.BlenderFBXImporter
{
    /// <summary>
    /// Utility class for managing Blender install path
    /// </summary>
    internal static class BlenderInstallPathManager
    {
        private static string Extension => Application.platform == RuntimePlatform.WindowsEditor ? "exe" : string.Empty;
        private static string Executable => Application.platform == RuntimePlatform.WindowsEditor ? "blender.exe" : Application.platform == RuntimePlatform.OSXEditor ? "Blender" : "blender";
        private static string DefaultBlenderPath => Application.platform == RuntimePlatform.OSXEditor ? "/Applications/Blender.app/Contents/MacOS/Blender" : null;
        private const string BlenderInstallPathKey = "Varneon/BlenderFBXImporter/BlenderInstallPath";

        private static string BlenderInstallPath;

        static BlenderInstallPathManager()
        {
            GetBlenderInstallPath();
        }

        internal static string GetBlenderInstallPath(bool openExplorerIfInvalid = true)
        {
            return BlenderInstallPath = EditorPrefs.HasKey(BlenderInstallPathKey) ? EditorPrefs.GetString(BlenderInstallPathKey) : ValidateBlenderInstallPath(DefaultBlenderPath) ? DefaultBlenderPath : openExplorerIfInvalid ? SetBlenderExecutablePath() : null;
        }

        internal static bool ValidateBlenderInstallPath(string path)
        {
            if (!IsBlenderInstallPathValid(path))
            {
                return false;
            }

            return true;
        }

        private static bool IsBlenderInstallPathValid(string path)
        {
            return !string.IsNullOrEmpty(path) && File.Exists(path) && Path.GetFileName(path) == Executable;
        }

        internal static string SetBlenderExecutablePath()
        {
            string path = EditorUtility.OpenFilePanel("Select Blender Executable", string.Empty, Extension);

            // Account for MacOS bundles
            if (Application.platform == RuntimePlatform.OSXEditor && !string.IsNullOrEmpty(path) && Path.GetFileName(path) == "Blender.app") {
                path = Path.Combine(path, "Contents/MacOS/Blender");
            }

            if (IsBlenderInstallPathValid(path))
            {
                EditorPrefs.SetString(BlenderInstallPathKey, path);
            }
            else
            {
                return GetBlenderInstallPath(false);
            }

            return BlenderInstallPath = path;
        }

        internal static void DrawInstallPathField()
        {
            using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(BlenderInstallPath, EditorStyles.wordWrappedLabel);

                if (GUILayout.Button("Change", GUILayout.Width(60)))
                {
                    SetBlenderExecutablePath();
                }
            }
        }
    }
}
