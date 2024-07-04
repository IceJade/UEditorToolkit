using System;
namespace ResourceCheckerPlus
{
    public enum ResourceWarningLevel
    {
        Normal,                  //正常
        Warning,                 //警告，不符合要求，建议修改
        FatalError,              //严重问题，一定要修改
    }  

    [Serializable]
    public class ResourceRule 
    {  
        public ResourceRuleCheckResult result = new ResourceRuleCheckResult();
        public FilterItem.FilterType filterType;
        public FilterItemCfg[] filterItems;
        public string resourceTagGUID;
    }
}


