using System.Collections.Generic;
using UnityEngine;

namespace ResourceCheckerPlus
{
    public class ResourceCondition 
    {
        public CheckType checkType;
        public bool positiveType;
        public string filterString;

        public bool CheckDetailFilterInternal(object value, CheckItem item, string filter, bool positive)
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
                case CheckType.Custom:
                    {
                        return item.customFilter(value);
                    }
                default:
                    return true;
            }
        }
    }

   
}

