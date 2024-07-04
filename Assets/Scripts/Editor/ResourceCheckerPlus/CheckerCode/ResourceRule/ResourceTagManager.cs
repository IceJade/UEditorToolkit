using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace ResourceCheckerPlus
{ 
    public class ResourceTagManager
    {
        private static List<ResourceTag> resourceTags = new List<ResourceTag>();
        private static Dictionary<string, ResourceTag> resourceTagDic = new Dictionary<string, ResourceTag>();
        private static ResourceTag deleteTag = null;
        private static ResourceTagConfig tagConfig = null;

        private static int ruleTagButtonWidth = 120;
        private static int ruleTagButtonHeight = 80;
        public  static  bool showResourceTag = false;

        private static ResourceTag currentResourceTag = null;

        public static ResourceTag GetCurrentResourceTag()
        {
            return currentResourceTag;
        }

        public static ResourceTag GetResrouceTag(string guid)
        {
            ResourceTag tag = null;
            resourceTagDic.TryGetValue(guid, out tag);
            return tag;
        }

        public static void LoadResourceTag()
        {
            resourceTags.Clear();
            resourceTagDic.Clear();
            var tagPath = CheckerConfigManager.configRootPath + CheckerConfigManager.resourceRuleConfigPath + "/ResourceTag.asset";
            tagConfig = AssetDatabase.LoadAssetAtPath<ResourceTagConfig>(tagPath);
            if (tagConfig == null)
            {
                tagConfig = ScriptableObject.CreateInstance<ResourceTagConfig>();
                AssetDatabase.CreateAsset(tagConfig, tagPath);
            }
            if (tagConfig.resourceTags != null)
            {
                resourceTags.AddRange(tagConfig.resourceTags);
                resourceTags.ForEach(x => resourceTagDic.Add(x.tagGUIDs, x));
            }
        }

        public static void SaveResourceTag()
        {
            if (tagConfig == null)
                return;
            tagConfig.resourceTags = resourceTags.ToArray();
            EditorUtility.SetDirty(tagConfig);
            AssetDatabase.SaveAssets();
        }

        public static void ShowResourceTags()
        {
            if (currentResourceTag == null)
            {
                currentResourceTag = GetFirstTag();
            }

            foreach(var tag in resourceTags)
            {
                ResourceCheckerHelper.BeginSelectableLine(currentResourceTag == tag);
                if (GUILayout.Button(tag.resourceTagName, GUILayout.Width(ruleTagButtonWidth), GUILayout.Height(ruleTagButtonHeight)))
                {
                    currentResourceTag = tag;
                }
                ResourceCheckerHelper.EndSelectableLine();
            }
            if (showResourceTag)
                GUI.color = CheckerConfigManager.commonConfing.warningItemColor;
            if (GUILayout.Button("编辑资源Tag", GUILayout.Width(ruleTagButtonWidth)))
            {
                showResourceTag = !showResourceTag;
            }
            GUI.color = CheckerConfigManager.defaultTextColor;
        }

        public static void ShowDetailResourceTags()
        {
            EditorGUI.BeginDisabledGroup(!CheckerConfigManager.commonConfing.enableEditResourceRule);
            foreach(var tag in resourceTags)
            {
                GUILayout.BeginHorizontal();
                tag.resourceTagName = GUILayout.TextField(tag.resourceTagName, GUILayout.Width(150));
                tag.resourceTagType = (ResourceTagType)EditorGUILayout.EnumPopup(tag.resourceTagType, GUILayout.Width(100));
                tag.resourceFolder = EditorGUILayout.ObjectField("路径", tag.resourceFolder, typeof(Object), false, GUILayout.Width(300));
                tag.isSceneResourceTag = GUILayout.Toggle(tag.isSceneResourceTag, "场景资源", GUILayout.Width(150));
                if (GUILayout.Button("删除", GUILayout.Width(100)))
                {
                    deleteTag = tag;
                    //关于RuleGroup没删除
                    ResourceRuleManager.DeleteResourceRuleByTag(deleteTag);
                }
                GUILayout.EndHorizontal();
            }
            if (deleteTag != null)
            {
                resourceTags.Remove(deleteTag);
                resourceTagDic.Remove(deleteTag.tagGUIDs);
                currentResourceTag = GetFirstTag();
                deleteTag = null;
            }
            if (GUILayout.Button("增加资源Tag", GUILayout.Width(200)))
            {
                var tag = new ResourceTag();
                tag.resourceTagName = "未命名资源标签";
                tag.tagGUIDs = ResourceCheckerHelper.GenerateGUID();
                resourceTags.Add(tag);
            }
            EditorGUI.EndDisabledGroup();
        }

        static private ResourceTag GetFirstTag()
        {
            if (resourceTags.Count > 0)
                return resourceTags[0];
            return null;
        }

        public static List<ResourceTag> GenerateResourceTags(Object[] objects)
        {
            var tags = new List<ResourceTag>();
            var currentModule = ResourceCheckerPlus.instance.CurrentCheckModule();
            //反向检查的时候不开
            if (currentModule is ReverseRefCheckModule)
                return tags;
            foreach (var tag in resourceTags)
            {
                if (tag.resourceTagType == ResourceTagType.Common)
                    tags.Add(tag);
                if (currentModule is SceneResCheckModule)
                {
                    if (tag.isSceneResourceTag)
                        tags.Add(tag);
                }
                else
                {
                    if (tag.resourceTagType == ResourceTagType.InFolder && ResourceCheckerHelper.IsAllObjectsInFolder(tag.resourceFolder, objects))
                        tags.Add(tag);
                }
            }
            return tags;
        }
    }
}

