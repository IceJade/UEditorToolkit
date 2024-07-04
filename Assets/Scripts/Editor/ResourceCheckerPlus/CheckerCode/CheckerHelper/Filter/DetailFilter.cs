using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ResourceCheckerPlus
{
    public class DetailFilter : FilterItem
    {
        public CheckItem currentFilterCheckItem = null;

        public ResourceRule resourceRule = null;

        public DetailFilter(ObjectChecker c) : base(c)
        {
            
        }

        public void AutoCheckResource(ObjectDetail detail, CheckItem curItem)
        {
            if (resourceRule == null)
                return;

            bool result = DoCheckFilterInternal(detail);
            var curValueItem = detail.checkMap[curItem.title];

            if (result)
            {
                curValueItem.SetCheckResult(resourceRule.result);
                detail.SetResourceCheckResult(resourceRule.result);
            } 
            else
            {
                curValueItem.SetCheckResult(ResourceRuleCheckResult.passResult);
            }
        }

        private bool DoCheckFilterInternal(ObjectDetail detail)
        {
            if (filterType == FilterType.AndFilter)
            {
                bool checkResult = true;
                DoAndCheckFilter(detail, ref checkResult);
                return checkResult;
            }
            else
            {
                bool checkResult = false;
                DoOrCheckFilter(detail, ref checkResult);
                return checkResult;
            }
        }

        public void DoAndCheckFilter(ObjectDetail detail, ref bool result)
        {
            var item = GetCurrentCheckItem();
            if (item == null)
                return;

            var obj = detail.GetCheckValue(item);
            if (obj != null)
            {
                result &= DoDetailFilter(obj, item, currentFilterStr, positive);
            }

            if (nextFilterNode != null)
            {
                (nextFilterNode as DetailFilter).DoAndCheckFilter(detail, ref result);
            }
        }

        public void DoOrCheckFilter(ObjectDetail detail, ref bool result)
        {
            var item = GetCurrentCheckItem();
            if (item == null)
                return;

            var obj = detail.GetCheckValue(item);
            if (obj != null)
            {
                result |= DoDetailFilter(detail.GetCheckValue(item), item, currentFilterStr, positive);
            }

            if (nextFilterNode != null)
            {
                (nextFilterNode as DetailFilter).DoOrCheckFilter(detail, ref result);
            }
        }

        public CheckItem GetCurrentCheckItem()
        {
            return filterArray[currentFilterIndex];
        }

        public ResourceRule GenerateResourceRule()
        {
            List<FilterItemCfg> cfgList = new List<FilterItemCfg>();
            SaveFilterAsCfg(cfgList);
            resourceRule.filterItems = cfgList.ToArray();
            return resourceRule;
        }

        public void SaveFilterAsCfg(List<FilterItemCfg> cfgList)
        {
            FilterItemCfg cfg = new FilterItemCfg();
            cfg.checkItemName = GetCurrentCheckItem().title;
            cfg.filterString = currentFilterStr;
            cfg.positive = positive;
            cfgList.Add(cfg);
            if (nextFilterNode != null)
            {
                 (nextFilterNode as DetailFilter).SaveFilterAsCfg(cfgList);
            }
        }

        public void CreateFilterFromCfg(DetailFilter item, ResourceRule ruleConfig, int index = 0)
        {
            FilterItemCfg cfg = null;
            if (index < ruleConfig.filterItems.Length)
                cfg = ruleConfig.filterItems[index];
            else
                return;
            DetailFilter newItem = new DetailFilter(checker);
            if (InitFilterByCfg(newItem, cfg))
                item.AddFilterNode(newItem);
            CreateFilterFromCfg(newItem, ruleConfig, ++index);
        }

        private bool InitFilterByCfg(DetailFilter detailFilter, FilterItemCfg cfg)
        {
            if (cfg == null)
                return false;

            SetFilterItem(detailFilter, cfg.checkItemName);
            detailFilter.currentFilterStr = cfg.filterString;
            detailFilter.positive = cfg.positive;
            return true;
        }

        public void SetFilterItem(DetailFilter detailFilter, string checkItemName)
        {
            CheckItem cItem = checker.GetCheckItemByCheckItemTitle(checkItemName);
            if (cItem != null)
            {
                detailFilter.currentFilterIndex = System.Array.IndexOf(filterArray, cItem);
            }
        }

        public void CreateFromResourceRule(ResourceRule rule)
        {
            resourceRule = rule;
            Clear(true);
            //首先设置自身，然后设置子节点
            if (rule != null && rule.filterItems != null && rule.filterItems.Length > 0)
            {
                var filterCfg = rule.filterItems[0];
                InitFilterByCfg(this, filterCfg);
                CreateFilterFromCfg(this, rule, 1);
            }
        }

        public override void CustomShowFilter()
        {
            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            currentFilterIndex = EditorGUILayout.Popup(currentFilterIndex, filterTypeArray, GUILayout.Width(150));
            if (EditorGUI.EndChangeCheck())
            {
                currentFilterStr = "";
                currentFilterCheckItem = filterArray[currentFilterIndex];
            }
            currentFilterStr = GUILayout.TextField(currentFilterStr, GUILayout.Width(300));

            EditorGUI.BeginChangeCheck();
            positive = GUILayout.Toggle(positive, positive ? "正向" : "反向", GUILayout.Width(40));
            if (EditorGUI.EndChangeCheck())
            {
                //checker.RefreshCheckResult();
            }

            if (parentFilterNode == null)
            {
                if (GUILayout.Button("增加检查条件", GUILayout.Width(100)))
                {
                    AddFilterNode(new DetailFilter(checker));
                }
                filterType = (FilterType)EditorGUILayout.EnumPopup(filterType, GUILayout.Width(100));
            }
            else
            {
                if (GUILayout.Button("删除检查条件", GUILayout.Width(100)))
                {
                    RemoveFilterNode();
                    //checker.RefreshCheckResult();
                }
            }
            GUILayout.EndHorizontal();
        }

        public void ShowResourceRuleGroup()
        {
            GUILayout.BeginHorizontal();


            GUILayout.EndHorizontal();
        }
    }
}
