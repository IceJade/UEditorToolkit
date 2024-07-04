//侧边栏显示列表
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace ResourceCheckerPlus
{
    public class SelfObjectChecker : ObjectChecker
    {
        public class SelfObjectDetail : ObjectDetail
        {
            public SelfObjectDetail(Object obj, SelfObjectChecker checker) : base(obj, checker)
            {
                AddOrSetCheckValue(checker.prefabRootItem, "Root");
            }
        }

        public bool isLocked = false;
        CheckItem prefabRootItem;

        public override void InitChecker()
        {
            checkerName = "SelfObj";
            refItem.show = false;
            pathItem.show = false;
            totalRefItem.show = false;
            activeItem.show = false;
            nameItem.clickOption = OnRefButtonClick;
            nameItem.itemFlag |= ItemFlag.FixWidth;
            nameItem.width = CheckerConfigManager.commonConfing.sideBarWidth - 40;

            prefabRootItem = new CheckItem(this, "Root", CheckType.String, OnPrefabRootButtonClick);
            prefabRootItem.itemFlag |= ItemFlag.FixWidth;
            prefabRootItem.width = 38;
            prefabRootItem.show = false;
        }

        public override void AddObjectDetail(Object rootObj)
        {
            ObjectDetail detail = null;
            foreach (var v in CheckList)
            {
                if (v.checkObject == rootObj)
                    detail = v;
            }
            if (detail == null)
            {
                detail = new SelfObjectDetail(rootObj, this);
            }
        }

        public void OnRefButtonClick(ObjectDetail detail)
        {
            var curCheckModule = ResourceCheckerPlus.instance.CurrentCheckModule();
            if (!(curCheckModule is DirectResCheckModule) && CheckerConfigManager.commonConfing.autoFilterOnSideBarButtonClick)
            {
                var checker = ResourceCheckerPlus.instance.CurrentCheckModule().CurrentActiveChecker();
                if (checker is ParticleChecker || checker is GameObjectChecker)
                    return;
                var filter = new RefFilterItem(checker);
                checker.filterItem.Clear(true);
                filter.checkObjList = SelectList.Select(x => x.checkObject).ToList();
                checker.filterItem.AddFilterNode(filter);
                checker.RefreshCheckResult();
            }
        }

        public void OnPrefabRootButtonClick(ObjectDetail detail)
        {
            var go = detail.checkObject as GameObject;
            if (go == null)
                return;
            var root = ResourceCheckerHelper.GetPrefabRoot(go);
            Selection.activeGameObject = root;
        }

        public override void ShowCheckResult()
        {
            if (CheckList.Count > 0)
            {
                base.ShowCheckResult();
            }
        }

        public override void ShowCheckerSort()
        {
            viewListScrollPos.x = EditorGUILayout.BeginScrollView(new Vector2(viewListScrollPos.x, 0), GUILayout.Height(40)).x;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(previewItem.title, GUILayout.Width(previewItem.width)))
            {
            }
            if (GUILayout.Button(nameItem.title, GUILayout.Width(180)))
            {
                CheckDetailSort(nameItem, nameItem.sortSymbol);
                nameItem.sortSymbol = !nameItem.sortSymbol;
            }
            isLocked = EditorGUILayout.Toggle(lockButtonContent, isLocked, new GUIStyle("IN LockButton"), GUILayout.Width(20));
            GUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        private GUIContent lockButtonContent = new GUIContent("", "锁定侧边栏时，新加入内容时不会清空原有内容");

        public void AddObjectToSelfObjectChecker(List<Object> objects, bool clear)
        {
            if (!isLocked && clear)
            {
                Clear();
            }
            AddObjectDetailBatch(objects);
            RefreshCheckResult();
        }

        public void ClearSelfObjectList()
        {
            if (!isLocked)
            {
                Clear();
            }
            RefreshCheckResult();
        }

        public void ShowDetailRefRootButton(bool show)
        {
            prefabRootItem.show = show;
            nameItem.width = show ? CheckerConfigManager.commonConfing.sideBarWidth - 40 : CheckerConfigManager.commonConfing.sideBarWidth;
        }
    }
}