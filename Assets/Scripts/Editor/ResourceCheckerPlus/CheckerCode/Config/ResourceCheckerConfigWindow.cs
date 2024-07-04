using UnityEngine;
using UnityEditor;

namespace ResourceCheckerPlus
{
    /// <summary>
    /// 配置窗口类
    /// </summary>
    public class ResourceCheckerConfigWindow : EditorWindow
    {
        public static string[] checkBatchOptionStr = new string[] { "处理列表中全部内容", "仅处理列表中选中的内容" };
        public static void Init()
        {
            var window = GetWindow<ResourceCheckerConfigWindow>();
            var pos = ResourceCheckerHelper.GetCurrentPopWindowPos();
            window.position = new Rect(pos, new Vector2(500, 400));
            exportResultPath = AssetDatabase.LoadAssetAtPath<Object>(CheckerConfigManager.commonConfing.checkResultExportPath);
        }

        private static Object exportResultPath = null;

        void OnGUI()
        {
            if (ResourceCheckerPlus.instance == null)
            {
                Close();
                return;
            }
            var config = CheckerConfigManager.commonConfing;
            if (config != null)
            {
                config.selectItemColor = EditorGUILayout.ColorField("选中条目高亮颜色", config.selectItemColor);
                config.warningItemColor = EditorGUILayout.ColorField("警告条目高亮颜色", config.warningItemColor);
                config.errorItemColor = EditorGUILayout.ColorField("错误条目高亮显色", config.errorItemColor);
                config.sideBarWidth = EditorGUILayout.IntSlider("侧边栏宽度", config.sideBarWidth, 180, 300);
                config.clearFilterOnReCheck = GUILayout.Toggle(config.clearFilterOnReCheck, "重新检查时清除筛选条件", GUILayout.Width(300));
                config.autoFilterOnSideBarButtonClick = GUILayout.Toggle(config.autoFilterOnSideBarButtonClick, "点击侧边栏对象自动进行引用筛选", GUILayout.Width(300));
                config.enableAutoResourceCheck = GUILayout.Toggle(config.enableAutoResourceCheck, "开启资源规范检查", GUILayout.Width(300));
                config.enableEditResourceRule = GUILayout.Toggle(config.enableEditResourceRule, "是否可编辑资源规则", GUILayout.Width(300));
                config.batchOptionType = (BatchOptionSelection)EditorGUILayout.Popup("批量处理功能处理范围", (int)config.batchOptionType, checkBatchOptionStr);
                config.maxCheckRecordCount = EditorGUILayout.IntSlider("常用查询记录最大值", config.maxCheckRecordCount, 5, 20);
                config.maxMemoryCache = EditorGUILayout.Slider("内存最大缓存值(GB)", config.maxMemoryCache, 0.5f, 16.0f);
                EditorGUI.BeginChangeCheck();
                exportResultPath = EditorGUILayout.ObjectField("导出检查结果Excel路径", exportResultPath, typeof(Object), false);
                if (EditorGUI.EndChangeCheck())
                {
                    config.checkResultExportPath = AssetDatabase.GetAssetPath(exportResultPath);
                    if (!ResourceCheckerHelper.isFolder(config.checkResultExportPath))
                    {
                        config.checkResultExportPath = "";
                        exportResultPath = null;
                    }
                }

                if (GUILayout.Button("资源规则设置", GUILayout.Width(300)))
                {
                    ResourceRuleConfigWindow.Init();
                }

                if (GUILayout.Button("刷新检查配置", GUILayout.Width(300)))
                {
                    ObjectChecker.SerilizeAllCheckItemConfig();
                }
            }
        }
    }
}
