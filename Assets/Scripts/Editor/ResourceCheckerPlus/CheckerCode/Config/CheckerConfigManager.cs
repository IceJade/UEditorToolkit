using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ResourceCheckerPlus
{
    public class CheckerConfigManager
    {
        public static string configRootPath = "Assets/EngineEffects/Editor/ResourceCheckerPlus/CheckerConfig";
        public static string commonConfigName = "/CommonConfig.asset";
        public static string initConfigName = "/InitConfig.asset";
        public static string resourceRuleConfigPath = "/ResourceRuleConfig";
        public static string checkerCfgPath = "/CheckerCfg";
        public static string checkModuleCfgPath = "/CheckModuleCfg";
        public static string defaultExportResultPath = "Assets";

        public static CheckerCommonConfig commonConfing = null;
        public static Dictionary<string, CheckerConfig> checkerConfigDic = new Dictionary<string, CheckerConfig>();
        public static List<CheckModuleConfig> checkModuleConfigList = new List<CheckModuleConfig>();

        public static Color defaultTextColor;
        public static Color defaultBackgroundColor;

        public void InitConfig()
        {
            configRootPath = GetResourceCheckerPlusConfigRootPath();
            defaultTextColor = GUI.color;
            defaultBackgroundColor = GUI.backgroundColor;
            InitCheckModuleConfig();
            InitCheckerCommonConfig();
        }

        public void InitCheckerCommonConfig()
        {
            if (commonConfing == null)
            {
                string path = configRootPath + commonConfigName;
                commonConfing = AssetDatabase.LoadAssetAtPath<CheckerCommonConfig>(path);
                if (commonConfing == null)
                {
                    commonConfing = ScriptableObject.CreateInstance<CheckerCommonConfig>();
                    AssetDatabase.CreateAsset(commonConfing, path);
                }
            }
        }

        public CheckerConfig GetCheckerConfig(string checkerName)
        {
            CheckerConfig cfg = null;
            checkerConfigDic.TryGetValue(checkerName, out cfg);
            if (cfg == null)
            {
                string path = configRootPath + checkerCfgPath + "/" + checkerName + ".asset";
                cfg = AssetDatabase.LoadAssetAtPath<CheckerConfig>(path);
                if (cfg == null)
                {
                    cfg = ScriptableObject.CreateInstance<CheckerConfig>();
                    AssetDatabase.CreateAsset(cfg, path);
                }
            }
            return cfg;
        }

        public void InitCheckModuleConfig()
        {
            checkModuleConfigList.Clear();
            var configPath = configRootPath + checkModuleCfgPath;
            var guids = AssetDatabase.FindAssets("t:ScriptableObject", new string[] { configPath });
            if (guids == null)
                return;
            foreach (var v in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(v);
                var cfg = AssetDatabase.LoadAssetAtPath<CheckModuleConfig>(path);
                if (cfg == null)
                    continue;
                checkModuleConfigList.Add(cfg);
            }
        }

        public void SaveCheckerConfig()
        {
            if (commonConfing != null)
            {
                EditorUtility.SetDirty(commonConfing);
            }
            checkModuleConfigList.ForEach(x => EditorUtility.SetDirty(x));
            var v = checkerConfigDic.GetEnumerator();
            while (v.MoveNext())
            {
                EditorUtility.SetDirty(v.Current.Value);
            }
            AssetDatabase.SaveAssets();
        }

        public void ClearConfig()
        {
            commonConfing = null;
            //checkerConfigDic.Clear();
            checkModuleConfigList.Clear();
        }

        public static string GetResourceCheckerPlusConfigRootPath()
        {
            //直接取当前脚本路径，然后稍加修改得到配置路径
            //使Resource Checker Plus可以任意放置路径，但是配置文件和本文件的位置不能动
            var frame = new System.Diagnostics.StackTrace(true).GetFrame(0);
            var configRootPath = frame.GetFileName();
            configRootPath = System.IO.Path.GetDirectoryName(configRootPath);

            configRootPath = configRootPath.Replace('\\', '/');
            configRootPath = configRootPath.Remove(0, configRootPath.IndexOf("/Assets") + 1);
            configRootPath = configRootPath.Replace("CheckerCode/Config", "CheckerConfig");
            
            return configRootPath;
        }
    }
}

