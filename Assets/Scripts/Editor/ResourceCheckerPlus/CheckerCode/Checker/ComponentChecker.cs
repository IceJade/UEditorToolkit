using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace ResourceCheckerPlus
{
    public class ComponentChecker : ObjectChecker
    {
        public class ComponentDetail : ObjectDetail
        {
            public ComponentDetail(Object obj, ComponentChecker checker) : base(obj, checker)
            {

            }

            public override void InitDetailCheckObject(Object obj)
            {
                var checker = currentChecker as ComponentChecker;
                if (obj != null)
                {
                    assetName = obj is MonoScript ? obj.name : obj.GetType().ToString();
                }
                else
                {
                    assetName = "MissingComponent";
                }
                AddOrSetCheckValue(checker.nameItem, assetName);
                AddOrSetCheckValue(checker.totalRefItem, detailReferenceList);
                var path = buildInType;
                if (obj is MonoScript)
                    path = AssetDatabase.GetAssetPath(obj);
                else if (obj is MonoBehaviour)
                    path = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(obj as MonoBehaviour));
                AddOrSetCheckValue(checker.pathItem, path);
                AddOrSetCheckValue(checker.comEnabledItem, "true");
            }

            public bool allRefIsEnabled = true;
            public Dictionary<Component, Object> componentList = new Dictionary<Component, Object>();
        }

        public CheckItem comEnabledItem;
        public CheckItem detailItem;

        public CheckItem particleSystemCount;

        public override void InitChecker()
        {
            checkerName = "Component";
            checkerFilter = "t:Script";
            comEnabledItem = new CheckItem(this, "Enabled");
            detailItem = new CheckItem(this, "组件属性", CheckType.String, OnComponentDetailButtonClick);

            particleSystemCount = new CheckItem(this, "粒子组件数", CheckType.Int, null, null, ItemFlag.SceneCheckInfo);
        }

        private bool IsComponentEnabled(Object obj)
        {
            Component com = obj as Component;
            if (com == null)
                return true;
            PropertyInfo info = com.GetType().GetProperty("enabled");
            if (info == null)
                return true;
            return (bool)info.GetValue(com, null);
        }

        public override ObjectDetail AddObjectDetail(object obj, Object refObj, Object detailRefObj)
        {
            var com = obj as Component;
            if (com == null)
            {
                var script = obj as MonoScript;
                if (script == null)
                    return null;
            }
            ComponentDetail detail = null;
            foreach (var d in CheckList)
            {
                if (d == null)
                    continue;
                if (d.checkObject is Component)
                {
                    if (d.checkObject.GetType() == obj.GetType())
                        detail = d as ComponentDetail;
                }
                else if (d.checkObject is MonoScript)
                {
                    if (d.checkObject == com)
                        detail = d as ComponentDetail;
                }
            }
            if (detail == null)
            {
                detail = new ComponentDetail(com, this);
            }
            detail.AddObjectReference(refObj, detailRefObj);
            detail.allRefIsEnabled &= IsComponentEnabled(com);
            detail.AddOrSetCheckValue(comEnabledItem, detail.allRefIsEnabled.ToString());
            detail.AddOrSetCheckValue(detailItem, "详细属性");
            if (com != null && !detail.componentList.ContainsKey(com))
            {
                detail.componentList.Add(com, refObj);
            }
            return detail;
        }

        public override void AddObjectDetailRef(GameObject rootObj)
        {
            AddObjectDetailRefInternal(rootObj, rootObj);
        }

        private void OnComponentDetailButtonClick(ObjectDetail detail)
        {
            var comDetail = detail as ComponentDetail;
            if (comDetail != null)
            {
                ComponentFiledEditor.Init(comDetail);
            }
        }

        private void AddObjectDetailRefInternal(GameObject rootObj, GameObject checkObject)
        {
            var rootTran = checkObject.transform;
            var coms = rootTran.GetComponents<Component>();
            foreach (var com in coms)
            {
                if (com != null)
                    AddObjectWithRef(com, checkObject, rootObj);
                else
                    AddNullComponentDetail(rootObj, checkObject);
            }
            for (int i = 0; i < rootTran.childCount; i++)
            {
                AddObjectDetailRefInternal(rootObj, rootTran.GetChild(i).gameObject);
            }
        }

        private void AddNullComponentDetail(GameObject rootGo, GameObject refGo)
        {
            ComponentDetail detail = null;
            foreach (var v in CheckList)
            {
                if (v.checkObject == null)
                    detail = v as ComponentDetail;
            }
            if (detail == null)
            {
                detail = new ComponentDetail(null, this);
                detail.warningLevel = ResourceWarningLevel.FatalError;
                detail.resourceWarningTips = "空组件对象";
                detail.flag |= ObjectDetailFlag.Warning;
            }
            detail.AddObjectReference(rootGo, refGo);
        }

        //Component默认开启详细检查
        public override void CheckPrefabReference(Object root, bool checkDetailRef, bool checkRefPrefabReference)
        {
            var go = root as GameObject;
            if (go != null)
            {
                AddObjectDetailRef(go);
            }
        }

        public override void ShowCheckerFliter()
        {
            totalRefItem.show = true;
            base.ShowCheckerFliter();
        }

        public override void CheckDetailSummary()
        {
            base.CheckDetailSummary();
            var detail = GetCheckDetail(nameItem, "UnityEngine.ParticleSystem") as ComponentDetail;
            checkResultDic[particleSystemCount] = detail == null ? 0 : detail.referenceObjectList.Count;
        }

        //public void DeleteAllNullComponents()
        //{
        //    ComponentDetail nullDetail = null;
        //    foreach (var v in CheckList)
        //    {
        //        if (v.checkObject == null)
        //            nullDetail = v as ComponentDetail;
        //    }
        //    if (nullDetail == null)
        //        return;
        //    foreach (var detailObj in nullDetail.totalRef)
        //    {
        //        var go = detailObj as GameObject;
        //        ResourceCheckerHelper.DeleteNullComponentsOnGameObject(go);
        //        Debug.Log("删除" + go.name + "对象上的空脚本");
        //    }

        //    AssetDatabase.Refresh();
        //}
    }
}
