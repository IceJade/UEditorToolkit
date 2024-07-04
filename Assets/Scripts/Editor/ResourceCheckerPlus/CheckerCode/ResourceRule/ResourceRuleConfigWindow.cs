/*****************************************************************************************************
	created:	11:06:2018   11:23
	filename: 	ResourceRuleConfigWindow.cs
	author:		zhangjian
	
	purpose:	资源规则设置窗口
******************************************************************************************************/
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ResourceCheckerPlus
{
    public class ResourceRuleConfigWindow : ComplexWindow
    {
        private Vector2 liftSideScrollPos = Vector2.zero;
        private Vector2 topRightScrollPos = Vector2.zero;
        private Vector2 bottomRightScrollPos = Vector2.zero;

        private CheckItem currentCheckItem = null;
        private DetailFilter currentDetailFilter = null;
        private DetailFilter deleteFilter = null;

        private static CheckerSelector checkerSelector = new CheckerSelector();

        public static ResourceRuleConfigWindow instance = null;

        public static void Init()
        {
            var window = GetWindow<ResourceRuleConfigWindow>();
            var pos = ResourceCheckerHelper.GetCurrentPopWindowPos();
            checkerSelector.RefreshChecker(ObjectChecker.allCheckerDic.Values.ToList());
            window.position = new Rect(pos, new Vector2(1200, 600));
            window.SetSideBarWide(CheckerConfigManager.commonConfing.sideBarWidth);
            instance = window;
        }

        public void SetCurrentChecker(ObjectChecker checker)
        {
            currentDetailFilter = null;
            currentCheckItem = null;
        }

        public override void ShowTopSide()
        {
            checkerSelector.DrawCheckerSelector(position.width);
        }

        public override void ShowTagBar()
        {
            ResourceTagManager.ShowResourceTags();
        }

        public override void ShowLeftSide()
        {
            var currentChecker = checkerSelector.GetCurrentActiveChecker();
            if (currentChecker == null || ResourceCheckerPlus.instance == null)
            {
                Close();
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label("检查属性", GUILayout.Width(220));
            //GUILayout.Label("规则数", GUILayout.Width(60));
            GUILayout.EndHorizontal();

            liftSideScrollPos = EditorGUILayout.BeginScrollView(liftSideScrollPos);

            foreach (var v in currentChecker.checkItemList)
            {
                ResourceCheckerHelper.BeginSelectableLine(currentCheckItem == v);

                if (GUILayout.Button(v.title, GUILayout.Width(220)))
                {
                    if (v.resourceRuleGroup != null)
                    {
                        v.resourceRuleGroup.currentCheckItem = v;
                    }
                    currentCheckItem = v;
                    currentDetailFilter = v.resourceRuleGroup == null ? null : v.resourceRuleGroup.GetFirstFilter();
                }

                //int ruleCount = v.resourceRuleGroup == null ? 0 : v.resourceRuleGroup.detailFilterList.Count;
                //GUILayout.Label(ruleCount.ToString(), GUILayout.Width(60));

                ResourceCheckerHelper.EndSelectableLine();
            }
            EditorGUILayout.EndScrollView();
        }

        public override void ShowRightSide()
        {
            if (ResourceTagManager.showResourceTag)
                ResourceTagManager.ShowDetailResourceTags();
            else
                ShowResourceRuleConfig();
        }
        private ObjectChecker currentChecker = null;
        private void ShowResourceRuleConfig()
        {
            var checker = checkerSelector.GetCurrentActiveChecker();
            if (checker == null)
                return;
            if (currentChecker != checker)
            {
                currentChecker = checker;
                SetCurrentChecker(currentChecker);
            }
            var currentResourceTag = ResourceTagManager.GetCurrentResourceTag();

            topRightScrollPos = EditorGUILayout.BeginScrollView(topRightScrollPos);
            if (currentCheckItem != null)
            {
                if (currentCheckItem.resourceRuleGroup != null)
                {
                    foreach (var filter in currentCheckItem.resourceRuleGroup.detailFilterList)
                    {
                        var rule = filter.resourceRule;
                        if (rule == null || rule.resourceTagGUID != currentResourceTag.tagGUIDs)
                            continue;

                        ResourceCheckerHelper.BeginSelectableLine(filter == currentDetailFilter, rule.result.warningLevel);
                        if (GUILayout.Button("选择", GUILayout.Width(50)))
                        {
                            currentDetailFilter = filter;
                        }
                        EditorGUI.BeginDisabledGroup(!CheckerConfigManager.commonConfing.enableEditResourceRule);
                        rule.result.resCheckResultTips = GUILayout.TextField(rule.result.resCheckResultTips, GUILayout.Width(200));
                        rule.result.warningLevel = (ResourceWarningLevel)EditorGUILayout.EnumPopup(rule.result.warningLevel, GUILayout.Width(100));
                        if (GUILayout.Button("删除", GUILayout.Width(50)))
                        {
                            deleteFilter = currentDetailFilter;
                        }
                        EditorGUI.EndDisabledGroup();
                        ResourceCheckerHelper.EndSelectableLine();
                    }
                    if (deleteFilter != null)
                    {
                        var filterList = currentCheckItem.resourceRuleGroup.detailFilterList;
                        filterList.Remove(deleteFilter);
                        currentDetailFilter = filterList.Count > 0 ? filterList[0] : null;
                        deleteFilter = null;
                    }
                }
                if (CheckerConfigManager.commonConfing.enableEditResourceRule)
                {
                    if (GUILayout.Button("增加资源规则"))
                    {
                        if (currentCheckItem.resourceRuleGroup == null)
                        {
                            currentCheckItem.resourceRuleGroup = new ResourceRuleGroup();
                            currentCheckItem.resourceRuleGroup.currentCheckItem = currentCheckItem;
                        }
                        var detailFilter = new DetailFilter(currentChecker);
                        detailFilter.SetFilterItem(detailFilter, currentCheckItem.title);
                        detailFilter.currentFilterCheckItem = currentCheckItem;
                        var rule = new ResourceRule();
                        rule.result.warningLevel = ResourceWarningLevel.Warning;
                        rule.result.resCheckResultTips = "请填写资源修改建议";
                        rule.resourceTagGUID = currentResourceTag.tagGUIDs;
                        detailFilter.resourceRule = rule;
                        currentCheckItem.resourceRuleGroup.detailFilterList.Add(detailFilter);
                        currentDetailFilter = detailFilter;
                    }
                }

            }
            EditorGUILayout.EndScrollView();

            bottomRightScrollPos = EditorGUILayout.BeginScrollView(bottomRightScrollPos);
            EditorGUI.BeginDisabledGroup(!CheckerConfigManager.commonConfing.enableEditResourceRule);
            if (currentDetailFilter != null)
                currentDetailFilter.ShowFilter();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndScrollView();

        }

        private void OnDestroy()
        {
            instance = null;
            if (!CheckerConfigManager.commonConfing.enableEditResourceRule)
                return;
            if (EditorUtility.DisplayDialog("提示", "是否保存当前资源规则", "保存", "取消"))
            {
                ResourceRuleManager.SaveResourceRule();
                ResourceTagManager.SaveResourceTag();
            }
        }
    }
}
