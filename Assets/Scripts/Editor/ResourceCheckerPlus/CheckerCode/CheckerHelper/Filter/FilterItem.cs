using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace ResourceCheckerPlus
{

    [System.Serializable]
    public class FilterItemCfg
    {
        public string checkItemName;
        public string filterString;
        public bool positive;
    }

    [System.Serializable]
    public class FilterItemCfgGroup
    {
        public FilterItem.FilterType filterType;
        public FilterItemCfg[] filterItems;
        public string filterGroupName;
    }

    /// <summary>
    /// 筛选器
    /// </summary>
    public class FilterItem
    {
        public enum FilterType
        {
            AndFilter,
            OrFilter,
        }

        public bool positive = true;
        public string currentFilterStr = "";
        //目前只支持And筛选或者Or筛选，混用暂时不支持
        public static FilterType filterType = FilterType.AndFilter;

        public ObjectChecker checker = null;
        //一种简单的职责链进行实现
        public FilterItem nextFilterNode = null;
        public FilterItem parentFilterNode = null;

        public string[] filterTypeArray = null;
        public CheckItem[] filterArray = null;
        protected int currentFilterIndex = 0;

        public CheckItem predefineFilterCheckItem = null;

        public FilterItem(ObjectChecker c)
        {
            checker = c;
            RefreshFilterItems();
        }

        public void RefreshFilterItems()
        {
            filterTypeArray = checker.checkItemList.Where(x => x.type != CheckType.Texture && x.show).Select(x => x.title).ToArray();
            filterArray = checker.checkItemList.Where(x => x.type != CheckType.Texture && x.show).ToArray();
        }

        public void ShowFilter()
        {
            CustomShowFilter();
            //职责链
            if (nextFilterNode != null)
            {
                nextFilterNode.ShowFilter();
            }
        }

        public bool DoDetailFilter(object value, CheckItem item, string filter, bool positive)
        {
            if (string.IsNullOrEmpty(filter))
                return true;
            switch (item.type)
            {
                case CheckType.String:
                    {
                        string str = value as string;
                        return positive ? str.ToLower().Contains(filter.ToLower()) : !str.ToLower().Contains(filter.ToLower());
                    }
                case CheckType.Int:
                case CheckType.FormatSize:
                    {
                        int num = 0;
                        int.TryParse(filter, out num);
                        return positive ? (int)value >= num : (int)value <= num;
                    }
                case CheckType.Float:
                    {
                        float num = 0;
                        float.TryParse(filter, out num);
                        return positive ? (float)value >= num : (float)value <= num;
                    }
                case CheckType.List:
                    {
                        int num = 0;
                        int.TryParse(filter, out num);
                        List<Object> list = value as List<Object>;
                        return positive ? list.Count >= num : list.Count <= num;
                    }
                case CheckType.ListShowFirstItem:
                    {
                        int num = 0;
                        int.TryParse(filter, out num);
                        List<Object> list = value as List<Object>;
                        string str = list.Count == 0 ? "Null" : list[0].ToString();
                        return positive ? str.ToLower().Contains(filter.ToLower()) : !str.ToLower().Contains(filter.ToLower());
                    }
                case CheckType.Custom:
                    {
                        return item.customFilter(value);
                    }
                default:
                    return true;
            }
        }

        public virtual void Clear(bool clearChildren)
        {
            currentFilterStr = "";
            positive = true;
            predefineFilterCheckItem = null;
            if (clearChildren)
            {
                nextFilterNode = null;
            }
        }

        public void AddFilterNode(FilterItem item)
        {
            if (nextFilterNode == null)
            {
                nextFilterNode = item;
                item.parentFilterNode = this;
            }
            else
            {
                nextFilterNode.AddFilterNode(item);
            }
        }

        public void RemoveFilterNode()
        {
            if (parentFilterNode != null)
                parentFilterNode.nextFilterNode = nextFilterNode;
            if (nextFilterNode != null)
                nextFilterNode.parentFilterNode = parentFilterNode;
        }

        public virtual void CustomShowFilter() { }

        public void SetFilterByConfig(FilterItemCfgGroup cfgGroup)
        {
            Clear(true);
            filterType = cfgGroup.filterType;
            foreach(var item in cfgGroup.filterItems)
            {
                var filter = new ListFilter(checker);
                filter.currentFilterStr = item.filterString;
                filter.predefineFilterCheckItem = filterArray.FirstOrDefault(x => x.title == item.checkItemName);
                filter.positive = item.positive;
                AddFilterNode(filter);
            }
        }
    }
}
