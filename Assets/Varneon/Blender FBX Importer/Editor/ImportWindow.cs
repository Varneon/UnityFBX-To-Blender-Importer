
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using DragAndDropState = Varneon.BlenderFBXImporter.DragAndDropHandler.DragAndDropState;

namespace Varneon.BlenderFBXImporter
{
    /// <summary>
    /// Import configuration window
    /// </summary>
    internal class ImportWindow : EditorWindow
    {
        List<FBXAsset> models = new List<FBXAsset>();

        ImportParameters importParameters = new ImportParameters(1f, true);

        Vector2 scrollPos = Vector2.zero;

        float previewSize = 50;

        const string MenuPath = "Assets/Import FBX to Blender";

        static readonly Vector2 MinWindowSize = new Vector2(512, 512);

        DragAndDropState dragAndDropState;

        Action handleDragAndDrop;

        Func<string, bool> TryAddModelPathFunc;

        private struct FBXAsset
        {
            internal string Path;
            internal string RelativePath;
            internal GUIContent Content;
            internal Object Asset;
            internal int ID;
            internal PreviewStatus Status;

            internal enum PreviewStatus
            {
                None,
                Loading,
                Retrying,
                Loaded,
                Failed
            }

            internal FBXAsset(string path)
            {
                Path = path;
                RelativePath = Path;
                string appPath = Application.dataPath.Replace('/', '\\');
                RelativePath = RelativePath.Replace(appPath.Substring(0, appPath.IndexOf("Assets")), string.Empty);
                Asset = AssetDatabase.LoadAssetAtPath(RelativePath, typeof(Object));
                Content = new GUIContent(Path, AssetPreview.GetAssetPreview(Asset));
                ID = Asset.GetInstanceID();
                Status = PreviewStatus.Loading;
            }

            internal bool IsPreviewLoading()
            {
                return AssetPreview.IsLoadingAssetPreview(ID);
            }

            internal void ReloadAssetPreview()
            {
                Status = (Content.image = AssetPreview.GetAssetPreview(Asset)) != null ? PreviewStatus.Loaded : Status == PreviewStatus.Retrying ? PreviewStatus.Failed : PreviewStatus.Failed;
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool DoesSelectionContainFBXAssets()
        {
            return GetSelectedFBXAssets().Length > 0;
        }

        [MenuItem(MenuPath, false)]
        private static void OpenImportPrompt()
        {
            ImportWindow window = GetWindow<ImportWindow>();
            window.titleContent = new GUIContent("Blender FBX Importer", Resources.Load<Texture2D>("Icon_BlenderFBXImporter"));
            window.minSize = MinWindowSize;
            window.Show();
        }

        private void OnEnable()
        {
            models.AddRange(GetSelectedFBXAssetPaths().Select(c => new FBXAsset(c)));

            TryAddModelPathFunc = path => TryAddModelPath(path);

            handleDragAndDrop = DragAndDropHandler.HandleFileDragAndDrop(dragAndDropState, TryAddModelPathFunc);
        }

        private void OnGUI()
        {
            using (var scope = new GUILayout.ScrollViewScope(scrollPos))
            {
                scrollPos = scope.scrollPosition;

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Model Paths:");

                    previewSize = EditorGUILayout.Slider("Preview Size:", previewSize, 32, 128, GUILayout.ExpandWidth(false));
                }

                for(int i = 0; i < models.Count; i++)
                {
                    FBXAsset fbx = models[i];

                    if((fbx.Status == FBXAsset.PreviewStatus.Loading || fbx.Status == FBXAsset.PreviewStatus.Retrying) && !fbx.IsPreviewLoading())
                    {
                        fbx.ReloadAssetPreview();

                        models[i] = fbx;
                    }

                    using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        GUILayout.Label(fbx.Content?.image, new GUILayoutOption[] { GUILayout.Width(previewSize), GUILayout.Height(previewSize) });

                        GUILayout.Label(fbx.Content.text, EditorStyles.wordWrappedLabel);

                        if (GUILayout.Button("Copy", GUILayout.Width(50)))
                        {
                            EditorGUIUtility.systemCopyBuffer = Path.GetFullPath(fbx.Path);
                        }
                        else if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            models.Remove(fbx);

                            break;
                        }
                    }
                }

                GUILayout.FlexibleSpace();
            }

            if (GUILayout.Button("Browse", GUILayout.Height(32)))
            {
                string path = EditorUtility.OpenFilePanelWithFilters("Select FBX Asset", "Assets", new string[] { "FBX Files", "fbx" });

                if (!string.IsNullOrEmpty(path))
                {
                    TryAddModelPath(path);
                }
            }

            EditorGUILayout.HelpBox("Hint: You can drag and drop FBX model files to this window to add them", MessageType.Info, true);

            GUILayout.Space(20);

            GUILayout.Label("Blender Executable Path:");

            BlenderInstallPathManager.DrawInstallPathField();

            GUILayout.Space(20);

            GUILayout.Label("Import Parameters:");

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                importParameters.DrawFields();
            }

            GUILayout.Space(20);

            GUILayout.Label("Actions:");

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Import FBX To Blender", GUILayout.Height(32)))
                {
                    ImportFBXsToBlender();

                    Close();

                    return;
                }
                else if (GUILayout.Button("Copy FBX Import Python Code", GUILayout.Height(32)))
                {
                    CopyBlenderFBXImportCode();
                }
            }

            handleDragAndDrop();
        }

        private bool TryAddModelPath(string path)
        {
            path = Path.GetFullPath(path);

            if (models.Where(c => c.Path.Equals(path)).Count() == 0)
            {
                models.Add(new FBXAsset(path));

                return true;
            }

            return false;
        }

        private static Object[] GetSelectedFBXAssets()
        {
            return Selection.GetFiltered(typeof(Object), SelectionMode.Assets).Where(c => Path.GetExtension(AssetDatabase.GetAssetPath(c)).ToLower() == ".fbx").ToArray();
        }

        private static string[] GetSelectedFBXAssetPaths()
        {
            return GetSelectedFBXAssets().Select(c => Path.GetFullPath(AssetDatabase.GetAssetPath(c))).ToArray();
        }

        private void ImportFBXsToBlender()
        {
            Importer.ImportFBXsToBlender(models.Select(c => c.Path).ToArray(), importParameters);
        }

        private void CopyBlenderFBXImportCode()
        {
            EditorGUIUtility.systemCopyBuffer = PythonOperatorGenerator.GetBlenderFBXImportPythonScript(models.Select(c => c.Path).ToArray(), false, importParameters);
        }
    }
}
