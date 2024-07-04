using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace ResourceCheckerPlus
{
    public class ResourceRuleManager
    {
        private static Dictionary<string, ResourceRuleConfig> resourceRuleMap = new Dictionary<string, ResourceRuleConfig>();

        public static ResourceRuleConfig GetCurrentCheckerResourceRule(string checkerName)
        {
            ResourceRuleConfig config = null;
            resourceRuleMap.TryGetValue(checkerName, out config);
            if (config == null)
            {
                var configPath = CheckerConfigManager.configRootPath + CheckerConfigManager.resourceRuleConfigPath;
                var rulePath = configPath + "/" + checkerName + ".asset";
                config = AssetDatabase.LoadAssetAtPath<ResourceRuleConfig>(rulePath);
                if (config == null)
                {
                    config = ScriptableObject.CreateInstance<ResourceRuleConfig>();
                    AssetDatabase.CreateAsset(config, rulePath);
                }
                resourceRuleMap.Add(checkerName, config);
            }
            return config;
        }

        public static void SaveResourceRule()
        {
            foreach (var rule in resourceRuleMap)
            {
                SaveResourceRuleConfig(rule.Key);
                EditorUtility.SetDirty(rule.Value);
            }
            AssetDatabase.SaveAssets();
        }

        private static void SaveResourceRuleConfig(string checkerName)
        {
            var ruleConfig = GetCurrentCheckerResourceRule(checkerName);
            if (ruleConfig == null)
                return;
            var checker = ObjectChecker.GetCheckerByName(checkerName);
            List<ResourceRuleGroup> ruleList = new List<ResourceRuleGroup>();
            foreach (var checkItem in checker.checkItemList)
            {
                var ruleGroup = checkItem.resourceRuleGroup;
                if (ruleGroup != null)
                {
                    ruleGroup.SaveResourceRuleGroup();
                    ruleList.Add(ruleGroup);
                }
            }
            ruleConfig.resourceRuleGroup = ruleList.ToArray();
        }

        public static void DeleteResourceRuleByTag(ResourceTag tag)
        {
            foreach (var rule in resourceRuleMap)
            {
                var checker = ObjectChecker.GetCheckerByName(rule.Key);
                foreach(var item in checker.checkItemList)
                {
                    var ruleGroup = item.resourceRuleGroup;
                    if (ruleGroup != null)
                    {
                        ruleGroup.detailFilterList = ruleGroup.detailFilterList.Where(x => x.resourceRule.resourceTagGUID != tag.tagGUIDs).ToList();
                        if (ruleGroup.detailFilterList.Count == 0)
                            item.resourceRuleGroup = null;
                    }
                }
            }
        }

    }
}
