using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ResourceCheckerPlus
{
    public class SceneChecker : ObjectChecker
    {
        public class SceneDetail : ObjectDetail
        {
            public SceneDetail(Object obj, ObjectChecker checker) : base(obj, checker)
            {

            }

            public override void InitDetailCheckObject(Object obj)
            {
                foreach (var c in currentChecker.checkModule.activeCheckerList)
                {
                    foreach (var summary in c.checkResultDic)
                    {
                        if ((summary.Key.itemFlag & ItemFlag.SceneCheckInfo) == ItemFlag.SceneCheckInfo)
                        {
                            var checkItem = currentChecker.CopyOrGetCheckItemByTitle(summary.Key);
                            AddOrSetCheckValue(checkItem, summary.Value);
                        }
                    }
                }
            }
        }

        public override void InitChecker()
        {
            checkerName = "Scene";
            checkerFilter = "t:Scene";
            postfix = ".unity";
        }

        public override ObjectDetail AddObjectDetail(object obj, Object refObj, Object detailRefObj)
        {
            var unityObject = obj as Object;
            ObjectDetail detail = null;
            foreach (var v in CheckList)
            {
                if (v.checkObject == unityObject)
                    detail = v;
            }
            if (detail == null)
            {
                detail = new SceneDetail(unityObject, this);
            }
            detail.AddObjectReference(refObj, detailRefObj);
            return detail;
        }
    }
}
