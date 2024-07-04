using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ResourceCheckerPlus
{
    [System.Serializable]
    public class ResourceRuleGroup
    {
        public string checkItemName;
        public ResourceRule[] resourceRules;

        [System.NonSerialized]
        public List<DetailFilter> detailFilterList = new List<DetailFilter>();      //资源规则筛选
        private ObjectChecker currentChecker = null;
        [System.NonSerialized]
        public CheckItem currentCheckItem = null;

        private List<DetailFilter> realCheckFilter = new List<DetailFilter>();

        public void SaveResourceRuleGroup()
        {
            List<ResourceRule> ruleList = new List<ResourceRule>();
            foreach (var filter in detailFilterList)
            {
                var rules = filter.GenerateResourceRule();
                ruleList.Add(rules);
            }
            resourceRules = ruleList.ToArray();
            checkItemName = currentCheckItem.title;
        }

        public void AutoCheckResource(ObjectDetail detail)
        {
            realCheckFilter.ForEach(x => x.AutoCheckResource(detail, currentCheckItem));
        }

        public void Init(ObjectChecker checker, CheckItem checkItem)
        {
            detailFilterList.Clear();
            currentChecker = checker;
            currentCheckItem = checkItem;
            if (resourceRules == null)
                return;
            foreach(var rule in resourceRules)
            {
                var item = new DetailFilter(currentChecker);
                item.CreateFromResourceRule(rule);
                detailFilterList.Add(item);
            } 
        }

        public DetailFilter GetFirstFilter()
        {
            return detailFilterList.Count > 0 ? detailFilterList[0] : null;
        }

        public void SetAutoCheckConfig(List<ResourceTag> tags)
        {
            var guids = tags.Select(x => x.tagGUIDs).ToList();
            realCheckFilter = detailFilterList.Where(x => guids.Contains(x.resourceRule.resourceTagGUID)).ToList();
        }

        public void ClearResourceRuleGroup()
        {
            realCheckFilter.Clear();
        }
    }
}
