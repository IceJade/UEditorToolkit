using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace ResourceCheckerPlus
{
    public class ListFilter : FilterItem
    {
        public ListFilter(ObjectChecker c) : base(c)
        {
            
        }

        public List<ObjectDetail> CheckDetailFilter(List<ObjectDetail> inList, bool onlyWarningItem)
        {
            var list = inList;
            if (filterType == FilterType.AndFilter)
                list = AndDetailFiter(inList);
            else if (filterType == FilterType.OrFilter)
                list = OrDetailFilter(inList);
            if (onlyWarningItem)
                list = list.Where(x => x.warningLevel > ResourceWarningLevel.Normal).ToList();
            return list; 
        }

        private List<ObjectDetail> AndDetailFiter(List<ObjectDetail> inList)
        {
            List<ObjectDetail> tempList = null;
            tempList = CustomDoFilter(inList);
            if (nextFilterNode != null)
            {
                return (nextFilterNode as ListFilter).AndDetailFiter(tempList);
            }
            else
            {
                return tempList;
            }
        }

        private List<ObjectDetail> OrDetailFilter(List<ObjectDetail> inList)
        {
            List<ObjectDetail> tempList = null;
            tempList = CustomDoFilter(inList);
            if (nextFilterNode != null)
            {
                List<ObjectDetail> childList = (nextFilterNode as ListFilter).OrDetailFilter(inList);
                foreach (var v in childList)
                {
                    if (!tempList.Contains(v))
                        tempList.Add(v);
                }
            }
            return tempList;
        }

        public virtual List<ObjectDetail> CustomDoFilter(List<ObjectDetail> inList)
        {
            var currentCheckItem = predefineFilterCheckItem == null ? filterArray[currentFilterIndex] : predefineFilterCheckItem;
            if (currentCheckItem == null || string.IsNullOrEmpty(currentFilterStr))
            {
                return filterType == FilterType.AndFilter || parentFilterNode == null ? inList.Where(x => true).ToList() : new List<ObjectDetail>(0);
            }
            else
            {
                return inList.Where(x => DoDetailFilter(x.GetCheckValue(currentCheckItem), currentCheckItem, currentFilterStr, positive) == true).ToList();
            }
        }

        public override void Clear(bool clearChildren)
        {
            base.Clear(clearChildren);
            currentFilterIndex = 0;
        }

        public override void CustomShowFilter()
        {
            GUILayout.BeginHorizontal();
            if (parentFilterNode == null)
            {
                if (GUILayout.Button("检查属性", GUILayout.Width(70)))
                {
                    CheckItemConfigWindow.Init(checker);
                }
            }
            else
            {
                GUILayout.Space(77);
            }
            EditorGUI.BeginChangeCheck();
            currentFilterStr = GUILayout.TextField(currentFilterStr, new GUIStyle("SearchTextField"), GUILayout.Width(300));
            if (EditorGUI.EndChangeCheck())
            {
                checker.RefreshCheckResult();
            }

            EditorGUI.BeginChangeCheck();
            currentFilterIndex = EditorGUILayout.Popup(currentFilterIndex, filterTypeArray, GUILayout.Width(150));
            if (EditorGUI.EndChangeCheck())
            {
                currentFilterStr = "";
                checker.RefreshCheckResult();
            }

            EditorGUI.BeginChangeCheck();
            positive = GUILayout.Toggle(positive, positive ? "正向" : "反向", GUILayout.Width(40));
            if (EditorGUI.EndChangeCheck())
            {
                checker.RefreshCheckResult();
            }

            if (parentFilterNode == null)
            {
                if (GUILayout.Button("增加筛选", GUILayout.Width(60)))
                {
                    AddFilterNode(new ListFilter(checker));
                }
                EditorGUI.BeginChangeCheck();
                filterType = (FilterType)EditorGUILayout.EnumPopup(filterType, GUILayout.Width(100));
                if (EditorGUI.EndChangeCheck())
                {
                    checker.RefreshCheckResult();
                }
                checker.ShowOptionButton();
            }
            else
            {
                if (GUILayout.Button("删除筛选", GUILayout.Width(60)))
                {
                    RemoveFilterNode();
                    checker.RefreshCheckResult();
                }
            }
            GUILayout.EndHorizontal();
        }

        //不用反射获取原生风格的搜索框了，在多个检查器切换时，数据切换了，显示得不对，怀疑是static实现？
        //public string SearchField(string value, params GUILayoutOption[] options)
        //{
        //    MethodInfo info = typeof(EditorGUILayout).GetMethod("ToolbarSearchField", BindingFlags.NonPublic | BindingFlags.Static, null, new System.Type[] { typeof(string), typeof(GUILayoutOption[]) }, null);
        //    if (info != null)
        //    {
        //        value = (string)info.Invoke(null, new object[] { value, options });
        //    }
        //    return value;
        //}
    }
}
