using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace ResourceCheckerPlus
{
    public class GameObjectChecker : ObjectChecker
    {
        public class GameObjectDetail : ObjectDetail
        {
            public GameObjectDetail(Object obj, GameObjectChecker checker) : base(obj, checker)
            {

            }

            public override void InitDetailCheckObject(Object obj)
            {
                var go = obj as GameObject;
                var checker = currentChecker as GameObjectChecker;
                var isStatic = go.isStatic;
                var flag = GameObjectUtility.GetStaticEditorFlags(go);
                var batchStatic = (flag & StaticEditorFlags.BatchingStatic) == StaticEditorFlags.BatchingStatic;
                var lightmapStatic = (flag & StaticEditorFlags.ContributeGI) == StaticEditorFlags.ContributeGI;
                var navigationStatic = (flag & StaticEditorFlags.NavigationStatic) == StaticEditorFlags.NavigationStatic;
                var rootPrefab = ResourceCheckerHelper.GetPrefabRoot(go);
                var rootPrefabName = rootPrefab == null ? "null" : rootPrefab.name;
                AddOrSetCheckValue(checker.goPrefabRoot, rootPrefabName);
                AddOrSetCheckValue(checker.goTag, go.tag);
                AddOrSetCheckValue(checker.goLayer, LayerMask.LayerToName(go.layer));
                AddOrSetCheckValue(checker.isStatic, isStatic.ToString());
                AddOrSetCheckValue(checker.batchStatic, batchStatic.ToString());
                AddOrSetCheckValue(checker.lightmapStatic, lightmapStatic.ToString());
                AddOrSetCheckValue(checker.navigaionStatic, navigationStatic.ToString());
                AddOrSetCheckValue(checker.staticFlag, (int)flag);
                CheckIsRefObjectActive(go);
            }
        }

        CheckItem goPrefabRoot;
        CheckItem goTag;
        CheckItem goLayer;
        CheckItem isStatic;
        CheckItem batchStatic;
        CheckItem lightmapStatic;
        CheckItem navigaionStatic;
        CheckItem staticFlag;

        public override void InitChecker()
        {
            checkerName = "GameObject";
            checkerFilter = "t:Prefab";
            enableReloadCheckItem = true;
            refItem.show = false;
            totalRefItem.show = false;
            goPrefabRoot = new CheckItem(this, "PrefabRoot", CheckType.String, OnPrefabRootButtonClick);
            goTag = new CheckItem(this, "Tag");
            goLayer = new CheckItem(this, "Layer");
            isStatic = new CheckItem(this, "IsStatic");
            batchStatic = new CheckItem(this, "BactcStatic");
            lightmapStatic = new CheckItem(this, "LightMapStatic");
            navigaionStatic = new CheckItem(this, "NavigationStatic");
            staticFlag = new CheckItem(this, "StaticFlag", CheckType.Int);
        }

        public override void AddObjectDetail(Object rootObj)
        {
            if (rootObj is GameObject)
            {
                new GameObjectDetail(rootObj, this);
            }
        }

        public override ObjectDetail AddObjectDetail(object obj, Object refObj, Object detailRefObj)
        {
            var unityObject = obj as Object;
            var detail = new GameObjectDetail(unityObject, this);
            detail.AddObjectReference(refObj, detailRefObj);
            return detail;
        }

        public override void AddObjectDetailRef(GameObject rootObj)
        {
            var gos = rootObj.GetComponentsInChildren<Transform>(true).Select(x => x.gameObject).ToArray();
            foreach (var go in gos)
            {
                AddObjectDetail(go);
            }
        }

        private void OnPrefabRootButtonClick(ObjectDetail detail)
        {
            var go = detail.checkObject as GameObject;
            var rootPrefab = ResourceCheckerHelper.GetPrefabRoot(go);
            Selection.activeGameObject = rootPrefab;
        }
    }
}
