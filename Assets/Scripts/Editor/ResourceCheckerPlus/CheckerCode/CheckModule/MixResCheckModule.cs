using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ResourceCheckerPlus
{
    public class MixResCheckModule : ResCheckModuleBase
    {
        protected bool checkPrefabDetailReference = false;
        protected GUIContent checkPrefabDetailRefContent = new GUIContent("检查资源被Prefab下子节点的引用", "开启该选项后，在检查Prefab引用的资源时，会将资源具体被哪些子物体引用了也统计出来（可以检查空脚本以及Unity内置资源，切换该属性需要重新检查）");

        public override void CheckResource(Object[] objects)
        {
            var selectObjects = GetAllObjectInSelection(objects);

            var checkObjects = ObjectChecker.GetAllObjectFromInput<Object>(selectObjects, "t:Object").ToArray();

            for (int i = 0; i < checkObjects.Length; i++)
            {
                EditorUtility.DisplayProgressBar("正在检查资源", "已完成：" + i + "/" + checkObjects.Length, (float)i / checkObjects.Length);
                Object root = checkObjects[i];
                if (root == null)
                    continue;
                activeCheckerList.ForEach(x => x.MixCheckDirectAndRefRes(root, checkPrefabDetailReference));
            }
            EditorUtility.ClearProgressBar();
        }

        public override void ShowCommonSideBarContent()
        {
            checkPrefabDetailReference = GUILayout.Toggle(checkPrefabDetailReference, checkPrefabDetailRefContent);
            base.ShowCommonSideBarContent();
        }
    }
}
