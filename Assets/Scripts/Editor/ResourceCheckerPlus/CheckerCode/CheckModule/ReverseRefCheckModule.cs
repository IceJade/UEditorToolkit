using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ResourceCheckerPlus
{
    /// <summary>
    /// 资源在全工程反向引用查找
    /// </summary>
    public class ReverseRefCheckModule : ResCheckModuleBase
    {
        public enum ReverseCheckMod
        {
            MainlyObjectSelf,   //主要检查资源自身依赖了哪些资源，显示引用计数
            MainlyReference,    //主要检查依赖的资源
        }
        private GUIContent reverseCheckModeContent = new GUIContent("检查模式");
        private ReverseCheckMod reverseCheckMode = ReverseCheckMod.MainlyObjectSelf;
        private RefObjectChecker refObjectChecker = new RefObjectChecker();
        private bool enableCheckPrefab = true;
        private bool enableCheckScene = true;
        private bool enableCheckMaterial = false;

        private bool checkTotalProjectReference = true;
        private Object reverseCheckRangeFolder = null;

        public override void SetCheckerConfig()
        {
            refObjectChecker.checkModule = this;
            ShowRefCheckItem(true, false, false);
        }

        public override void ShowCommonSideBarContent()
        {
            checkTotalProjectReference = GUILayout.Toggle(checkTotalProjectReference, "检查全工程范围内引用");
            if (!checkTotalProjectReference)
            {
                reverseCheckRangeFolder = EditorGUILayout.ObjectField("检查路径范围内的引用", reverseCheckRangeFolder, typeof(Object), false);
            }
            reverseCheckMode = (ReverseCheckMod)EditorGUILayout.EnumPopup(reverseCheckModeContent, reverseCheckMode);
            if (reverseCheckMode == ReverseCheckMod.MainlyObjectSelf)
            {
                enableCheckPrefab = GUILayout.Toggle(enableCheckPrefab, "Prefab");
                enableCheckScene = GUILayout.Toggle(enableCheckScene, "Scene");
                enableCheckMaterial = GUILayout.Toggle(enableCheckMaterial, "Material");
            }

            base.ShowCommonSideBarContent();
        }

        public override void CheckResource(Object[] resources)
        {
            var checkObjects = GetAllObjectInSelection(resources);
            if (reverseCheckMode == ReverseCheckMod.MainlyObjectSelf)
                CheckMainlyObjectSelf(checkObjects);
            else
                CheckMainlyReference(checkObjects);
        }

        public override void Refresh()
        {
            base.Refresh();
            refObjectChecker.RefreshCheckResult();
        }

        public override void Clear(bool releaseMemory = false)
        {
            base.Clear(releaseMemory);
            refObjectChecker.Clear();
        }

        private Object GetCheckRangeFolder()
        {
            return checkTotalProjectReference ? null : reverseCheckRangeFolder;
        }

        private void CheckMainlyObjectSelf(Object[] selection)
        {
            var rangeFolder = GetCheckRangeFolder();
            if (enableCheckPrefab)
                refObjectChecker.ReverseResCheckMainlyObjectSelf(selection, ".prefab", rangeFolder);
            if (enableCheckScene)
                refObjectChecker.ReverseResCheckMainlyObjectSelf(selection, ".unity", rangeFolder);
            if (enableCheckMaterial)
                refObjectChecker.ReverseResCheckMainlyObjectSelf(selection, ".mat", rangeFolder);
        }

        private void CheckMainlyReference(Object[] selection)
        {
            var rangeFolder = GetCheckRangeFolder();
            foreach (var v in activeCheckerList)
            {
                v.ReverseResCheckMainlyReference(selection, rangeFolder);
            }
        }

        public override void ShowCurrentCheckDetail()
        {
            if (reverseCheckMode == ReverseCheckMod.MainlyObjectSelf)
                ShowMainlyObjectSelfCheckDetail();
            else
                base.ShowCurrentCheckDetail();
        }

        private void ShowMainlyObjectSelfCheckDetail()
        {
            refObjectChecker.ShowCheckerTitle();
            refObjectChecker.ShowCheckResult();
        }
    }
}
