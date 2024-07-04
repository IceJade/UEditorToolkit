using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;

namespace ResourceCheckerPlus
{
    public class CustomFunctionEditor : SideBarWindow
    {
        private static CustomFunctionChecker checker = null;

        public static void Init(ObjectChecker oriChecker)
        {
            checker = new CustomFunctionChecker();

            var window = GetWindow<CustomFunctionEditor>();
            checker.currentWindow = window;
            checker.currentWindow.titleContent = new GUIContent("特殊功能检查", "进阶检查功能");
            checker.InitComponentDetailChecker(oriChecker);
            checker.checkModule = oriChecker.checkModule;
            checker.pathItem.show = !(checker.checkModule is SceneResCheckModule);
            window.SetSideBarWide(CheckerConfigManager.commonConfing.sideBarWidth);
        }

        public override void ShowLeftSide()
        {
            if (checker == null || ResourceCheckerPlus.instance == null)
                Close();
            if (checker != null && ResourceCheckerPlus.instance != null)
            {
                checker.ShowSideBar();
            }
        }

        public override void ShowRightSide()
        {
            if (checker != null && ResourceCheckerPlus.instance != null)
            {
                checker.ShowCheckerTitle();
                checker.ShowCheckResult();
            }
        }
    }

    public class CustomFunctionChecker : ObjectChecker
    {
        public class CustomFunctionDetail : ObjectDetail
        {
            public CustomFunctionDetail(Object obj, CustomFunctionChecker checker) : base(obj, checker)
            {

            }

            public override void InitDetailCheckObject(Object obj)
            {
                var checker = currentChecker as CustomFunctionChecker;
            }

            public List<ObjectDetail> subDetailList = new List<ObjectDetail>();
            public bool showChildren = false;
        }

        private Vector2 scrollPos = Vector2.zero;
        private ObjectChecker sourceChecker = null;
        public CheckItem sameNameCount;

        public override void InitChecker()
        {
            checkerName = "CustomFunctionChecker";
            activeItem.show = false;
            refItem.show = false;
            memorySizeItem.show = false;
            totalRefItem.show = false;
            pathItem.show = false;
            sameNameCount = new CheckItem(this, "资源名称个数", CheckType.Int, OnSameNameButtonClick);
        }

        public void AddCustomCheckDetail(ObjectDetail objectDetail)
        {
            var detail = new CustomFunctionDetail(objectDetail.checkObject, this);
        }

        public void AddCustomCheckDetailCheckSameName(ObjectDetail objectDetail)
        {
            CustomFunctionDetail detail = null;
            foreach(var v in CheckList)
            {
                if (v.assetName == objectDetail.assetName)
                    detail = v as CustomFunctionDetail;
            }
            if (detail == null)
            {
                detail = new CustomFunctionDetail(objectDetail.checkObject, this);
            }

            detail.subDetailList.Add(objectDetail);
            detail.AddOrSetCheckValue(sameNameCount, detail.subDetailList.Count);
        }

        public void DoCheckOpection()
        {
            Clear();
            ClearPredefineFilter();
            foreach (var v in sourceChecker.FilterList)
            {
                AddCustomCheckDetail(v);
            }
            RefreshCheckResult();
        }

        public void InitComponentDetailChecker(ObjectChecker oriChecker)
        {
            sourceChecker = oriChecker;
            DoCheckOpection();
        }

        public void SetupPredefineFilter()
        {
            predefineFilterGroups.Clear();
            nullFilterGroup.filterGroupName = "显示全部";
            predefineFilterGroups.Add(nullFilterGroup);
            var filter = new FilterItemCfgGroup();
            filter.filterGroupName = "仅显示重复资源";
            filter.filterType = FilterItem.FilterType.AndFilter;
            var cfg = new FilterItemCfg();
            cfg.checkItemName = "资源名称个数";
            cfg.filterString = "2";
            cfg.positive = true;
            filter.filterItems = new FilterItemCfg[] { cfg };
            predefineFilterGroups.Add(filter);
            filterNames = predefineFilterGroups.Select(x => x.filterGroupName).ToArray();
            currentSelectPredefineFilter = 0;
        }

        public void ClearPredefineFilter()
        {
            predefineFilterGroups.Clear();
        }

        private void OnSameNameButtonClick(ObjectDetail detail)
        {
            var customDetail = detail as CustomFunctionDetail;
            customDetail.showChildren = !customDetail.showChildren;
        }

        public override void ShowChildDetail(ObjectDetail detail)
        {
            var customDetail = detail as CustomFunctionDetail;
            if (customDetail.showChildren == true)
            {
                foreach (var child in customDetail.subDetailList)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(65);
                    if (GUILayout.Button(child.assetPath, GUILayout.Width(400)))
                    {
                        SelectObject(child.checkObject);
                    }
                    if (sourceChecker.refItem.show == true)
                    {
                        if (GUILayout.Button(child.referenceObjectList.Count.ToString(), GUILayout.Width(50)))
                        {
                            SelectObjects(child.referenceObjectList);
                            checkModule.AddObjectToSideBarList(child.referenceObjectList);
                            ResourceCheckerPlus.instance.Repaint();
                        }
                    }
                    if (sourceChecker.totalRefItem.show == true)
                    {
                        if (GUILayout.Button(child.detailReferenceList.Count.ToString(), GUILayout.Width(50)))
                        {
                            SelectObjects(child.detailReferenceList);
                            checkModule.AddObjectToSideBarList(child.detailReferenceList);
                            ResourceCheckerPlus.instance.Repaint();
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }

        public void ShowSideBar()
        {
            if (GUILayout.Button("恢复默认"))
            {
                DoCheckOpection();
            }

            if (GUILayout.Button("重名资源检查"))
            {
                CheckSameNameFile();
            }
        }

        public void CheckSameNameFile()
        {
            Clear();
            SetupPredefineFilter();
            foreach (var v in sourceChecker.FilterList)
            {
                AddCustomCheckDetailCheckSameName(v);
            }
            RefreshCheckResult();
        }
    }
}