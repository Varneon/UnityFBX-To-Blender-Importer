
using UnityEditor;
using UnityEngine;

namespace Varneon.BlenderFBXImporter
{
    /// <summary>
    /// Settings for importing FBX models into Blender
    /// </summary>
    public class ImportParameters
    {
        // Blender Python API documentation for Import Scene Operators: https://docs.blender.org/api/current/bpy.ops.import_scene.html

        /// <summary>
        /// Scale
        /// </summary>
        public float Scale { get; private set; }

        /// <summary>
        /// Apply Transform, Bake space transform into object data, avoids getting unwanted rotations to objects when target space is not aligned with Blender’s space (WARNING! experimental option, use at own risks, known broken with armatures/animations)
        /// </summary>
        public bool ApplyTransform { get; private set; }

        public ImportParameters(float scale = 1f, bool applyTransforms = true)
        {
            Scale = scale;
            ApplyTransform = applyTransforms;
        }

        /// <summary>
        /// Draw editor window fields for editing the values
        /// </summary>
        public void DrawFields()
        {
            Scale = EditorGUILayout.FloatField("Scale", Mathf.Clamp(Scale, 0.001f, 1000f));

            ApplyTransform = EditorGUILayout.Toggle("Apply Transform", ApplyTransform);
        }

        /// <summary>
        /// Get import settings as formatted parameters for import operator in Python script
        /// </summary>
        /// <returns>Formatted import operator parameters</returns>
        public string GetImportOperatorParameters()
        {
            return $"global_scale = {Scale}, bake_space_transform = {ApplyTransform}";
        }
    }
}
