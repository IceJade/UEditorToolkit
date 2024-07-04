using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ResourceCheckerPlus
{
    public class BuildAssetBundleTool : CheckerPluginEditor
    {
        public Object assetBundleDestFolderName = null;

        private BuildAssetBundleOptions buildOption = BuildAssetBundleOptions.None;

        public static void Init(List<Object> objects)
        {
            GetWindow(typeof(BuildAssetBundleTool));
            objectList = objects;
        }

        public void OnGUI()
        {
            string destPath = AssetDatabase.GetAssetPath(assetBundleDestFolderName);
            assetBundleDestFolderName = EditorGUILayout.ObjectField("DestFolder", assetBundleDestFolderName, typeof(Object), false) as Object;
#if UNITY_2017_3_OR_NEWER
            buildOption = (BuildAssetBundleOptions)EditorGUILayout.EnumFlagsField("BuildOption", buildOption);
#else
            buildOption = (BuildAssetBundleOptions)EditorGUILayout.EnumMaskField("BuildOption", buildOption);
#endif
            if (GUILayout.Button("Build"))
            {
                List<AssetBundleBuild> buildMap = new List<AssetBundleBuild>();
                foreach(var obj in objectList)
                {
                    string assetPath = AssetDatabase.GetAssetPath(obj);
                    AssetBundleBuild buildinfo = new AssetBundleBuild();
                    buildinfo.assetBundleName = GetAssetName(assetPath) + ".assetbundle";
                    buildinfo.assetNames = new string[] { assetPath };
                    buildMap.Add(buildinfo);
                }
                BuildPipeline.BuildAssetBundles(destPath, buildMap.ToArray(), buildOption, EditorUserBuildSettings.activeBuildTarget);
                AssetDatabase.Refresh();

                ResourceCheckerHelper.OpenFolder(destPath);
            }
        }

        private string GetAssetName(string path)
        {
            int index = path.LastIndexOf('/');
            if (index == -1)
            {
                return path;
            }
            path = path.Substring(index + 1);
            int lastIndex = path.LastIndexOf('.');
            if (lastIndex == -1)
            {
                return path;
            }
            return path.Substring(0, lastIndex);
        }
    }

}
