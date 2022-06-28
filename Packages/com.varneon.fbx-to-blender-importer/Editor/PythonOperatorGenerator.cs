
using System.Collections.Generic;

namespace Varneon.BlenderFBXImporter
{
    /// <summary>
    /// Utility class for generating Python operators for Blender's Python API
    /// </summary>
    public static class PythonOperatorGenerator
    {
        private const string
            PythonScriptWrapper = "import bpy; {0}",
            NewEmptySceneOperator = "bpy.ops.scene.new(type='EMPTY');",
            FBXImportOperator = "bpy.ops.import_scene.fbx( {0} );",
            FilePathParameterWrapper = "filepath = '{0}'";

        /// <summary>
        /// Gets formatted Python script for importing FBX in Blender
        /// </summary>
        /// <param name="path"></param>
        /// <param name="importInNewScene"></param>
        /// <param name="importParameters"></param>
        public static string GetBlenderFBXImportPythonScript(string path, bool importInNewScene, ImportParameters importParameters = null)
        {
            return GetBlenderFBXImportPythonScript(new string[] { path }, importInNewScene, importParameters);
        }

        public static string GetBlenderFBXImportPythonScript(string[] paths, bool importInNewScene, ImportParameters importParameters = null)
        {
            List<string> operators = new List<string>();

            if (importInNewScene) { operators.Add(NewEmptySceneOperator); }

            foreach(string path in paths)
            {
                if (!path.ToLower().EndsWith(".fbx")) { continue; }

                operators.Add(string.Format(FBXImportOperator, $"{string.Format(FilePathParameterWrapper, path.Replace(@"'", @"\'").Replace(@"\", "/"))}{(importParameters != null ? $", {importParameters.GetImportOperatorParameters()}" : string.Empty)}"));
            }

            return string.Format(PythonScriptWrapper, string.Join(" ", operators));
        }
    }
}
