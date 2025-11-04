using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.Video;

namespace BonV.Editor
{
    public class LoadTestMaterials : EditorWindow
    {
        private int cameraCount = 0;
        bool loadedByButton = false;

        private void OnEnable()
        {
            // Subscribe to scene and hierarchy changes
            EditorSceneManager.sceneOpened += OnSceneChanged;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.playModeStateChanged += ModeChanged;

            OnSceneCheck();
        }

        private void OnDisable()
        {
            // Unsubscribe when window is closed
            EditorSceneManager.sceneOpened -= OnSceneChanged;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        public void LoadTestScene()
        {
            EditorSceneManager.OpenScene("Assets/TestMaterials.unity", OpenSceneMode.Additive);
            EditorApplication.EnterPlaymode();

            loadedByButton = true;
        }

        private void ModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode && loadedByButton)
            {
                Scene scene = SceneManager.GetSceneByPath("Assets/TestMaterials.unity");
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [MenuItem("BonV/Scene Setup")]
        public static void ShowWindow()
        {
            // Create and show the window
            GetWindow<LoadTestMaterials>("Scene Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Scene check:", EditorStyles.boldLabel);
            SceneCountWindow();
            CameraStatusWindow();
            PlayStatusWindow();
            GUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(!SceneCheck());
            if (GUILayout.Button("Export Scene Bundle"))
            {
                ExportSceneBundle();
            }

            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace(); // Pushes content to the top
            GUILayout.Label("Render checks:", EditorStyles.boldLabel);
            if (SceneManager.GetSceneByPath("Assets/TestMaterials.unity").IsValid())
            {
                if (GUILayout.Button("Unload Test Materials"))
                {
                    EditorSceneManager.CloseScene(SceneManager.GetSceneByPath("Assets/TestMaterials.unity"), true);
                }
            }
            else
            {
                if (GUILayout.Button("Edit Test Material Scene"))
                {
                    EditorSceneManager.OpenScene("Assets/TestMaterials.unity", OpenSceneMode.Additive);
                }
            }
        }

        void SceneCountWindow()
        {
            MessageType type = MessageType.Info;
            if (EditorSceneManager.loadedRootSceneCount > 1)
            {
                type = MessageType.Error;
            }

            EditorGUILayout.HelpBox($"Scene number count : {EditorSceneManager.loadedRootSceneCount}", type);
        }

        void PlayStatusWindow()
        {
            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox($"You can't build while in Play Mode", MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox("Not in Play Mode. (OK)", MessageType.Info);
            }
        }

        void CameraStatusWindow()
        {
            // if (cameraCount > 0)
            // {
            //     EditorGUILayout.HelpBox($"The active scene contains {cameraCount} camera(s). This may overwrite the main camera in the build.", MessageType.Warning);
            // }
            // else
            // {
            //     EditorGUILayout.HelpBox("No cameras found in the active scene. (OK)", MessageType.Info);
            // }
        }

        private static void ExportSceneBundle()
        {
            string scenePath = SceneManager.GetActiveScene().path;
            string fullpath =
                EditorUtility.SaveFilePanel("Export Scene File", ".", SceneManager.GetActiveScene().name, "vss");

            if (string.IsNullOrEmpty(fullpath))
            {
                Debug.Log("Build scene file cancelled.");
                return;
            }

            bool complete = false;

            try
            {
                AssetBundleBuild build = new AssetBundleBuild
                {
                    assetBundleName = SceneManager.GetActiveScene().name,
                    assetNames = new string[] { scenePath }
                };


                BuildAssetBundleOptions options = BuildAssetBundleOptions.ForceRebuildAssetBundle |
                                                  BuildAssetBundleOptions.StrictMode;
                VideoPlayer[] videoPlayers =
                    UnityEngine.Object.FindObjectsByType<VideoPlayer>(FindObjectsSortMode.None);
                if (videoPlayers.Length > 0)
                {
                    Debug.Log("VideoPlayer detected, using uncompressed asset bundle.");
                    options |= BuildAssetBundleOptions.UncompressedAssetBundle;
                }

                BuildPipeline.BuildAssetBundles(Application.temporaryCachePath, new AssetBundleBuild[] { build },
                    options, BuildTarget.StandaloneWindows);

                if (File.Exists(fullpath))
                    File.Delete(fullpath);
                File.Move(Application.temporaryCachePath + "/" + SceneManager.GetActiveScene().name, fullpath);

                EditorUtility.DisplayDialog("Export", "Export complete!", "OK");
                complete = true;
            }
            finally
            {
                try
                {
                }
                catch
                {
                }

                if (!complete)
                    EditorUtility.DisplayDialog("Export", "Export failed! See the console for details.", "OK");
            }
        }

        private void OnSceneCheck()
        {
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None); // Finds all active cameras
            cameraCount = cameras.Length;

            //Debug.Log($"[Camera Checker] Found {cameraCount} camera(s) in the active scene.");
            Repaint(); // Refresh the editor window UI
        }

        private void OnSceneChanged(Scene scene, OpenSceneMode mode)
        {
            OnSceneCheck();
        }

        private void OnHierarchyChanged()
        {
            OnSceneCheck();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.ExitingPlayMode)
            {
                OnSceneCheck();
            }
        }

        bool SceneCheck()
        {
            bool allowBuild = true;
            // if (cameraCount > 0 || EditorApplication.isPlaying)
            // {
            //     allowBuild = false;
            // }
            // else
            // {
            //     allowBuild = true;
            // }
            return allowBuild;
        }
    }
}