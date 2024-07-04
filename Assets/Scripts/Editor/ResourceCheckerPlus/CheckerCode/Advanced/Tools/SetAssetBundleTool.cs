using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace ResourceCheckerPlus
{
    public class SetAssetBundleTool : CheckerPluginEditor
    {
        private static string assetBundleName = "";
        private static string assetBundleVariant = "bundle";

        public static List<ObjectDetail> objectDetailList = null;

        private int referenceCountMin = 2;
        private int referenceCountMax = 10;

        public static void Init(List<ObjectDetail> objectList)
        {
            var window = GetWindow<SetAssetBundleTool>();
            var pos = ResourceCheckerHelper.GetCurrentPopWindowPos();
            window.position = new Rect(pos, new Vector2(800, 300));

            objectDetailList = objectList;

            // 获得默认的AssetBundle名称
            GetDefaultAssetBundleName();
        }

        public void OnGUI()
        {
            this.referenceCountMin = EditorGUILayout.IntSlider("引用计数最小值", this.referenceCountMin, 2, 300);
            this.referenceCountMax = EditorGUILayout.IntSlider("引用计数最大值", this.referenceCountMax, 2, 300);

            var window = GetWindow<SetAssetBundleTool>();
            float textFieldWidth = window.position.width - 100;

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("AssetBundle名称:");
                assetBundleName = GUILayout.TextField(assetBundleName, GUILayout.Width(textFieldWidth));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("AssetBundle变体名称:");
                assetBundleVariant = GUILayout.TextField(assetBundleVariant, GUILayout.Width(textFieldWidth));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("====================================说明====================================");
            EditorGUILayout.LabelField("a.设置的引用计数为闭区间;");
            EditorGUILayout.LabelField("b.AssetBundle名称会转换成小写字母, 并且设置为全路径, 防止AssetBundle名称重复。");
            EditorGUILayout.LabelField(string.Format("c.选择第3和第4个按钮时, assetBundleName固定为:{0}, X为引用数量。", assetBundleName));
            EditorGUILayout.LabelField("============================================================================");
            EditorGUILayout.Space();

            if (GUILayout.Button("1.统计引用计数相同的资源数量"))
            {
                this.StatisticsResourceReferenceCount();
            }

            if (GUILayout.Button("2.对在指定引用计数范围内的所有文件设置同一个AssetBundle"))
            {
                this.SetAssetBundleInfoWithReferenceRange();
            }

            if (GUILayout.Button("3.对在指定引用计数范围内引用计数相同的资源单独设置成同一个AssetBundle"))
            {
                this.SetAssetBundleInfoWithReferenceCount(false);
            }

            if (GUILayout.Button("4.对所有引用计数(大于等于2)相同的资源单独设置成同一个AssetBundle"))
            {
                this.SetAssetBundleInfoWithReferenceCount(true);
            }

            if (GUILayout.Button("5.对在指定引用计数范围内的资源按照其文件名设置AssetBundle"))
            {
                this.SetAssetBundleInfoWithFileName();
            }

            if (GUILayout.Button("6.对所有文件设置相同的AssetBundle"))
            {
                this.SetAssetBundleInfo();
            }
        }

        private void StatisticsResourceReferenceCount()
        {
            if (!this.CheckConfig())
                return;

            Dictionary<int, int> referenceDic = new Dictionary<int, int>();
            foreach (var item in objectDetailList)
            {
                // 过滤不在设置的引用计数区间内的文件
                int referenceCount = item.referenceObjectList.Count;
                if (referenceCount < this.referenceCountMin || referenceCount > this.referenceCountMax)
                    continue;

                if (referenceDic.ContainsKey(referenceCount))
                    referenceDic[referenceCount]++;
                else
                    referenceDic.Add(referenceCount, 1);
            }

            ShowReferenceCountTool.Init(referenceDic);
        }

        private void SetAssetBundleInfo()
        {
            if (!this.CheckConfig())
                return;

            foreach(var item in objectDetailList)
            {
                var importer = AssetImporter.GetAtPath(item.assetPath);
                if (null == importer)
                    return;

                if (importer.assetBundleName != assetBundleName)
                    importer.assetBundleName = assetBundleName;

                if (importer.assetBundleVariant != assetBundleVariant)
                    importer.assetBundleVariant = assetBundleVariant;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("提示", "处理完毕", "OK");
        }

        private void SetAssetBundleInfoWithReferenceRange()
        {
            if (!this.CheckConfig())
                return;

            foreach (var item in objectDetailList)
            {
                // 过滤不在设置的引用计数区间内的文件
                int referenceCount = item.referenceObjectList.Count;
                if (referenceCount < this.referenceCountMin || referenceCount > this.referenceCountMax)
                    continue;

                var importer = AssetImporter.GetAtPath(item.assetPath);
                if (null == importer)
                    return;

                if (importer.assetBundleName != assetBundleName)
                    importer.assetBundleName = assetBundleName;

                if (importer.assetBundleVariant != assetBundleVariant)
                    importer.assetBundleVariant = assetBundleVariant;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("提示", "处理完毕", "OK");
        }

        private void SetAssetBundleInfoWithReferenceCount(bool dumpAll = true)
        {
            if (!this.CheckConfig())
                return;

            foreach (var item in objectDetailList)
            {
                int referenceCount = item.referenceObjectList.Count;
                if (referenceCount <= 1)
                    continue;

                if (!dumpAll && (referenceCount < this.referenceCountMin || referenceCount > this.referenceCountMax))
                    continue;

                var importer = AssetImporter.GetAtPath(item.assetPath);
                if (null == importer)
                    return;

                if (importer.assetBundleName != assetBundleName)
                    importer.assetBundleName = assetBundleName;

                if (importer.assetBundleVariant != assetBundleVariant)
                    importer.assetBundleVariant = assetBundleVariant;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("提示", "处理完毕", "OK");
        }

        private void SetAssetBundleInfoWithFileName()
        {
            if (!this.CheckConfig())
                return;

            foreach (var item in objectDetailList)
            {
                // 引用计数小于等于1的不设置bundle
                int referenceCount = item.referenceObjectList.Count;
                if (referenceCount <= 1)
                    continue;

                if (referenceCount < this.referenceCountMin || referenceCount > this.referenceCountMax)
                    continue;

                var importer = AssetImporter.GetAtPath(item.assetPath);
                if (null == importer)
                    return;

                // 获得不包含扩展名的文件名
                string bundleName = Path.Combine(Path.GetDirectoryName(item.assetPath), Path.GetFileNameWithoutExtension(item.assetPath));

                if (importer.assetBundleName != bundleName)
                    importer.assetBundleName = bundleName;

                if (importer.assetBundleVariant != assetBundleVariant)
                    importer.assetBundleVariant = assetBundleVariant;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("提示", "处理完毕", "OK");
        }

        private bool CheckConfig()
        {
            if(null == objectDetailList || objectDetailList.Count <= 0)
            {
                EditorUtility.DisplayDialog("提示", "没有选中的文件, 请检查！", "OK");
                return false;
            }

            if(string.IsNullOrEmpty(assetBundleName.Trim()))
            {
                EditorUtility.DisplayDialog("提示", "请设置AssetBundle名称！", "OK");
                return false;
            }

            if (string.IsNullOrEmpty(assetBundleVariant.Trim()))
            {
                EditorUtility.DisplayDialog("提示", "请设置AssetBundle的变体名称！", "OK");
                return false;
            }

            if(this.referenceCountMax < this.referenceCountMin)
            {
                EditorUtility.DisplayDialog("提示", "最大引用计数不能小于最小引用计数！", "OK");
                return false;
            }

            return true;
        }

        private static void GetDefaultAssetBundleName()
        {
            if (null != objectDetailList && objectDetailList.Count > 0)
            {
                string path = Path.GetDirectoryName(objectDetailList[0].assetPath);
                if (Directory.Exists(path))
                    assetBundleName = path.Replace("\\","/").ToLowerInvariant();
            }
        }
    }

}
