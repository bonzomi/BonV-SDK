using UnityEditor;
using System;
using UnityEngine;
using System.IO;

public class BuildVSM
{
    [MenuItem("BonV/Model Setup")]
    public static void ExportAvatarBundle()
        {
            GameObject obj = Selection.activeGameObject;
            
            string fullpath = EditorUtility.SaveFilePanel("Export Avatar Bundle", ".", obj.name, "vsm");

            if (fullpath == null || fullpath == "")
                return;

            var animator = obj.GetComponent<Animator>();
            if (animator != null && animator.avatar != null) {
                string avatarPath = "Assets/GeneratedAvatar.asset";
                AssetDatabase.CreateAsset(UnityEngine.Object.Instantiate(animator.avatar), avatarPath);
                AssetDatabase.SaveAssets();
            }

            
            string filename = Path.GetFileName(fullpath);
            bool complete = false;
            string prefabPath = $"Assets/VSM.prefab";
            try {
                AssetDatabase.DeleteAsset(prefabPath);
                if (File.Exists(prefabPath))
                    File.Delete(prefabPath);

                bool succeededPack = false;
                PrefabUtility.SaveAsPrefabAsset(obj, prefabPath, out succeededPack);
                if (!succeededPack) {
                    Debug.Log("Prefab creation failed");
                    return;
                }

                AssetBundleBuild bundleBuild = new AssetBundleBuild();
                AssetDatabase.RemoveUnusedAssetBundleNames();
                bundleBuild.assetBundleName = filename;
                bundleBuild.assetNames = new string[] { prefabPath,"Assets/GeneratedAvatar.asset"};
                bundleBuild.addressableNames = new string[] { "VSM" };

                BuildAssetBundleOptions options = BuildAssetBundleOptions.ForceRebuildAssetBundle | BuildAssetBundleOptions.DeterministicAssetBundle | BuildAssetBundleOptions.StrictMode;
                // if (obj.GetComponentsInChildren<UnityEngine.Video.VideoPlayer>(true).Length > 0) {
                //     Debug.Log("VideoPlayer detected, using uncompressed asset bundle.");
                //     options = options | BuildAssetBundleOptions.UncompressedAssetBundle;
                // }
                BuildPipeline.BuildAssetBundles(Application.temporaryCachePath , new AssetBundleBuild[] { bundleBuild }, options, BuildTarget.StandaloneWindows);
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
                } catch {}

                if (!complete)
                    EditorUtility.DisplayDialog("Export", "Export failed! See the console for details.", "OK");
            }
        }
}