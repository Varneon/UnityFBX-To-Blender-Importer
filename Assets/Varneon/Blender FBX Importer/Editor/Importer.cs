
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Varneon.BlenderFBXImporter
{
    /// <summary>
    /// Core Importer class for importing FBX models into Blender
    /// </summary>
    public static class Importer
    {
        private const string PythonExpressionArgumentWrapper = "--python-expr \"{0}\"";

        public static void ImportFBXToBlender(string path, ImportParameters importParameters = null)
        {
            ImportFBXsToBlender(new string[] { path }, importParameters);
        }

        public static void ImportFBXToBlender(Object fbxAsset, ImportParameters importParameters = null)
        {
            ImportFBXToBlender(AssetDatabase.GetAssetPath(fbxAsset), importParameters);
        }

        public static void ImportFBXsToBlender(Object[] fbxAssets, ImportParameters importParameters = null)
        {
            ImportFBXsToBlender(fbxAssets.Select(c => Path.GetFullPath(AssetDatabase.GetAssetPath(c))).ToArray(), importParameters);
        }

        public static void ImportFBXsToBlender(string[] paths, ImportParameters importParameters = null)
        {
            string executablePath = BlenderInstallPathManager.GetBlenderInstallPath();

            if (!BlenderInstallPathManager.ValidateBlenderInstallPath(executablePath))
            {
                throw new Exception("Invalid Blender executable path!");
            }

            try
            {
                System.Diagnostics.Process blender = new System.Diagnostics.Process();
                blender.StartInfo.FileName = executablePath;

                string args = string.Format(PythonExpressionArgumentWrapper, PythonOperatorGenerator.GetBlenderFBXImportPythonScript(paths, true, importParameters));

                blender.StartInfo.Arguments = args;

                blender.Start();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e.InnerException);
            }
        }
    }
}
