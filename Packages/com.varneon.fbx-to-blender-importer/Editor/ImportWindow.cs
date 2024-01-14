
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using DragAndDropState = Varneon.BlenderFBXImporter.DragAndDropHandler.DragAndDropState;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;

namespace Varneon.BlenderFBXImporter
{
    /// <summary>
    /// Import configuration window
    /// </summary>
    internal class ImportWindow : EditorWindow
    {
        [SerializeField]
        private Texture2D windowIcon;

        private List<FBXAsset> models = new List<FBXAsset>();

        private ImportParameters importParameters = new ImportParameters(1f, true);

        private Vector2 scrollPos = Vector2.zero;

        private float previewSize = 50;

        private const string MenuPath = "Assets/Import FBX to Blender";

        private static readonly Vector2 MinWindowSize = new Vector2(512, 512);

        private DragAndDropState dragAndDropState;

        private Action handleDragAndDrop;

        private Func<string, bool> TryAddModelPathFunc;

        private static string ApplicationDataPath;

        private static UnityEditor.PackageManager.PackageInfo[] LocalPackages;

        private struct FBXAsset
        {
            internal string Path;
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
                Asset = AssetDatabase.LoadAssetAtPath(path, typeof(Object));

                Path = System.IO.Path.GetFullPath(path);

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
            GetWindow<ImportWindow>();
        }

        private void OnEnable()
        {
            ApplicationDataPath = Application.dataPath.Replace('\\', '/');

            titleContent = new GUIContent("Blender FBX Importer", windowIcon);

            minSize = MinWindowSize;

            TryAddModelPathFunc = path => TryAddModelPath(path);

            handleDragAndDrop = DragAndDropHandler.HandleFileDragAndDrop(dragAndDropState, TryAddModelPathFunc);

            foreach(string path in GetSelectedFBXAssetPaths())
            {
                TryAddModelPath(path);
            }
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

            if (!models.Any(c => c.Path.Equals(path)) && TryConvertToRelativePath(path, out string relativePath))
            {
                models.Add(new FBXAsset(relativePath));

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

        /// <summary>
        /// Converts full path to one relative to the project
        /// </summary>
        /// <param name="path">Full path pointing inside the project</param>
        /// <returns>Path relative to the project</returns>
        private static bool TryConvertToRelativePath(string path, out string relativePath)
        {
            // If string is null or empty, throw an exception
            if (string.IsNullOrEmpty(path)) { throw new ArgumentException("Invalid path!", nameof(path)); }

            // If the directory is already valid, return original path
            if (AssetDatabase.IsValidFolder(Path.GetDirectoryName(path))) { relativePath = path; return true; }

            // Get the project's root directory (Trim 'Assets' from the end of the path)
            string projectDirectory = ApplicationDataPath.Substring(0, ApplicationDataPath.Length - 6);

            // Ensure that the path is the full path
            path = Path.GetFullPath(path);

            // Replace backslashes with forward slashes
            path = path.Replace('\\', '/');

            // If path doesn't point inside the project, scan all packages
            if (!path.StartsWith(projectDirectory))
            {
                if(LocalPackages == null)
                {
                    // Request all packages in offline mode
                    ListRequest request = Client.List(true, false);

                    // Wait until the request is completed
                    while (!request.IsCompleted) { }

                    if (request.Status == StatusCode.Success)
                    {
                        LocalPackages = request.Result.Where(p => p.source.Equals(PackageSource.Local)).ToArray();
                    }
                }

                // Try to find a package with same path as the one we are validating
                UnityEditor.PackageManager.PackageInfo info = LocalPackages.FirstOrDefault(p => path.StartsWith(p.resolvedPath.Replace('\\', '/')));

                // If a package with same path exists, return resolved path
                if (info != null)
                {
                    string resolvedPackagePath = info.resolvedPath.Replace('\\', '/');

                    relativePath = string.Concat("Packages/", info.name, path.Substring(resolvedPackagePath.Length));

                    return true;
                }

                relativePath = string.Empty;

                return false;
            }

            // Return a path relative to the project
            relativePath = path.Replace(projectDirectory, string.Empty);

            return true;
        }
    }
}
