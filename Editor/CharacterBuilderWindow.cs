using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class CharacterBuilderWindow : EditorWindow
{
    private BuildTarget buildTarget = BuildTarget.StandaloneWindows64;
    private GameObject character;
    
    private Vector2 scrollPosition = Vector2.zero;

    private class ModelInfo
    {
        public string message;
        public MessageType type;
        public ModelInfo(string message, MessageType type)
        {
            this.message = message;
            this.type = type;
        }
    }
    private List<ModelInfo> infoList = new();
    
    [MenuItem("BonV/Character Model Setup")]
    public static void OpenCharacterWindow()
    {
        var window = (CharacterBuilderWindow)GetWindow(typeof(CharacterBuilderWindow),true,"Character Model Setup");
        window.minSize = new Vector2(400, 200);
        window.Show();
    }
    
    private void OnGUI()
    {
        var headerStyle = new GUIStyle(EditorStyles.largeLabel)
        {
            fontStyle = FontStyle.Bold
        };
        GUILayout.Label("Character Model Builder Window", headerStyle);
        GUILayout.Space(10);
        
        // Character GameObject field
        GUILayout.BeginHorizontal();
        GUILayout.Label("Character GameObject:", GUILayout.Width(120));
        character = (GameObject)EditorGUILayout.ObjectField(character, typeof(GameObject), true);
        GUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        // Build Target dropdown
        GUILayout.BeginHorizontal();
        GUILayout.Label("Build Target:", GUILayout.Width(120));
        buildTarget = (BuildTarget)EditorGUILayout.EnumPopup(buildTarget);
        GUILayout.EndHorizontal();
        
        GUILayout.Space(20);
        GUILayout.Label("Character Validation", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
        // Show validation messages
        if (!character)
        {
            EditorGUILayout.HelpBox("Please select an Character GameObject to build.", MessageType.Warning);
        }
        else 
        {
            bool isValidCharacter = ValidateCharacter();
            if (!isValidCharacter)
            {
                
                GUILayout.Space(5);
                // Scrollable info box that stretches to bottom
                
                foreach (var info in infoList)
                {
                    EditorGUILayout.HelpBox(info.message, info.type);
                    GUILayout.Space(2);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Character is valid.", MessageType.Info);
            }
        }
        EditorGUILayout.EndScrollView();
        // Build button at the bottom
        GUILayout.FlexibleSpace();
        GUI.enabled = ValidateCharacter();
        if (GUILayout.Button("Build Character", GUILayout.Height(30)))
        {
            BuildCharacter();
        }
        GUI.enabled = true;
    }

    bool ValidateCharacter()
    {
        if (!character) return false;
        infoList.Clear();
        bool allowBuild = true;
        if(!character.GetComponent<Animator>())
        {
            infoList.Add(new ModelInfo("Character GameObject must have an Animator component.", MessageType.Error));
            allowBuild = false;
        }

        if (character.transform.localPosition != Vector3.zero)
        {
            infoList.Add(new ModelInfo("Character GameObject will reset to origin.", MessageType.Warning));
        }
        
        return allowBuild;
    }
    void BuildCharacter()
    {
            
            string fullpath = EditorUtility.SaveFilePanel("Export Character Bundle", ".", character.name, "vsm");

            if (string.IsNullOrEmpty(fullpath))
                return;

            string filename = Path.GetFileName(fullpath);
            bool complete = false;
            string prefabPath = $"Assets/VSM.prefab";
            try {
                AssetDatabase.DeleteAsset(prefabPath);
                if (File.Exists(prefabPath))
                    File.Delete(prefabPath);

                PrefabUtility.SaveAsPrefabAsset(character, prefabPath, out var succeededPack);
                if (!succeededPack) {
                    Debug.Log("Prefab creation failed");
                    return;
                }

                AssetBundleBuild bundleBuild = new AssetBundleBuild();
                AssetDatabase.RemoveUnusedAssetBundleNames();
                bundleBuild.assetBundleName = filename;
                bundleBuild.assetNames = new[] { prefabPath };
                bundleBuild.addressableNames = new[] { "VSM" };

                BuildAssetBundleOptions options = BuildAssetBundleOptions.ForceRebuildAssetBundle | BuildAssetBundleOptions.StrictMode;
                // if (character.GetComponentsInChildren<UnityEngine.Video.VideoPlayer>(true).Length > 0) {
                //     Debug.Log("VideoPlayer detected, using uncompressed asset bundle.");
                //     options = options | BuildAssetBundleOptions.UncompressedAssetBundle;
                // }
                BuildPipeline.BuildAssetBundles(Application.temporaryCachePath , new[] { bundleBuild }, options, buildTarget);
                if (File.Exists(fullpath))
                    File.Delete(fullpath);
                File.Move(Application.temporaryCachePath + "/" + filename, fullpath);

                EditorUtility.DisplayDialog("Export", "Export complete!", "OK");
                complete = true;
            }
            finally
            {
                try {
                    AssetDatabase.DeleteAsset(prefabPath);
                    if (File.Exists(prefabPath))
                        File.Delete(prefabPath);
                } 
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to delete prefab at {prefabPath}: {ex.Message}");
                }

                if (!complete)
                    EditorUtility.DisplayDialog("Export", "Export failed! See the console for details.", "OK");
            }
    }
}
