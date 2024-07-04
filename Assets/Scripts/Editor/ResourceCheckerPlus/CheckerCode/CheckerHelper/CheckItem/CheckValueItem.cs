using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace ResourceCheckerPlus
{
    [System.Serializable]
    public class ResourceRuleCheckResult
    {
        public ResourceWarningLevel warningLevel = ResourceWarningLevel.Normal;
        public string resCheckResultTips = "";

        //通过检测的资源结果
        public static ResourceRuleCheckResult passResult = new ResourceRuleCheckResult();
    }

    public class CheckValueItem
    {
        public object ValueItem
        {
            get { return _valueItem; }
            set
            {
                _valueItem = value;
                GenerateContent();
            }
        }
     
        public GUIContent uicontent;
        public ResourceWarningLevel warningLevel = ResourceWarningLevel.Normal;
        public CheckItem checkItem;
        public ObjectDetail checkDetail;

        private object _valueItem;

        public CheckValueItem(CheckItem item, ObjectDetail detail)
        {
            checkItem = item;
            checkDetail = detail;
        }

        private void GenerateContent()
        {
            string valueStr = ValueItem == null ? "" : GenShowStr();
            if (uicontent == null)
                uicontent = new GUIContent();
            uicontent.text = valueStr;
        }

        public void SetCheckResult(ResourceRuleCheckResult result)
        {
            if (result.warningLevel == ResourceWarningLevel.Normal)
                return;
            if (result.warningLevel > warningLevel)
                warningLevel = result.warningLevel;

            uicontent.tooltip += result.resCheckResultTips;
        }

        public void ClearCheckResult()
        {
            warningLevel = ResourceWarningLevel.Normal;
            uicontent.tooltip = "";
        }

        private string GenShowStr()
        {
            if (checkItem.type == CheckType.FormatSize)
            {
                return EditorUtility.FormatBytes((int)ValueItem);
            }
            else
            {
                return ValueItem.ToString();
            }
        }

        public void ShowCheckValue()
        {
            Color oriColor = GUI.color;
            if (warningLevel == ResourceWarningLevel.Warning)
                GUI.color = CheckerConfigManager.commonConfing.warningItemColor;
            else if (warningLevel == ResourceWarningLevel.FatalError)
                GUI.color = CheckerConfigManager.commonConfing.errorItemColor;

            if (checkItem.type == CheckType.Texture)
            {
                Texture tex = ValueItem as Texture;
                if (tex == null)
                    GUILayout.Box("null", GUILayout.Width(checkItem.width), GUILayout.Height(checkItem.width));
                else
                    GUILayout.Box(tex, GUILayout.Width(checkItem.width), GUILayout.Height(checkItem.width));
            }
            else
            {
                if (checkItem.type == CheckType.List)
                {
                    List<Object> list = ValueItem as List<Object>;
                    uicontent.text = list.Count.ToString();
                }
                else if (checkItem.type == CheckType.ListShowFirstItem)
                {
                    List<Object> list = ValueItem as List<Object>;
                    uicontent.text = list.Count == 0 ? "Null": list[0].ToString();
                }
                if (checkItem.clickOption == null)
                {
                    GUILayout.Label(uicontent, GUILayout.Width(checkItem.width));
                }
                else
                {
                    if (GUILayout.Button(uicontent, GUILayout.Width(checkItem.width)))
                    {
                        checkDetail.currentChecker.SelectObjectDetail(checkDetail);
                        checkItem.clickOption(checkDetail);
                    }
                }
            }

            GUI.color = oriColor;
        }
    }
}
