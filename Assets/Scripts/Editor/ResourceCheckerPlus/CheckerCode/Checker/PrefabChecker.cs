using UnityEngine;
using UnityEditor;

namespace ResourceCheckerPlus
{
    public class PrefabChecker : ObjectChecker
    {
        public class PrefabDetail : ObjectDetail
        {
            public PrefabDetail(Object obj, PrefabChecker checker) : base(obj, checker)
            {
#if UNITY_2018_3_OR_NEWER
                var go = obj as GameObject;
                if (go != null)
                {

                    AddOrSetCheckValue(checker.prefabAssetTyepItem, PrefabUtility.GetPrefabAssetType(go).ToString());
                    //var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                    //if (root != null)
                    //    AddOrSetCheckValue(checker.rootPrefabItem, root.ToString());
                }
#endif
                AddOrSetCheckValue(checker.totalRefItem, detailReferenceList);
            }
        }

#if UNITY_2018_3_OR_NEWER
        CheckItem prefabAssetTyepItem;
        //CheckItem rootPrefabItem;
#endif
 
        public override void InitChecker()
        {
            checkerName = "Prefab";
            checkerFilter = "t:Prefab";
            postfix = ".prefab";
#if UNITY_2018_3_OR_NEWER
            prefabAssetTyepItem = new CheckItem(this, "PrefabAssetType");
            //rootPrefabItem = new CheckItem(this, "Nearest Root");
#endif

        }

        public override ObjectDetail AddObjectDetail(object obj, Object refObj, Object detailRefObj)
        {
            var go = obj as GameObject;
            if (go == null)
                return null;
            var prefab = ResourceCheckerHelper.GetPrefabRoot(go);
            //剔除prefab自身
            if (checkModule is ReferenceResCheckModule && prefab == refObj)
                return null;
            PrefabDetail detail = null;
            foreach (var d in CheckList)
            {
                if (d.checkObject == prefab)
                    detail = d as PrefabDetail;
            }
            if (detail == null)
            {
                detail = new PrefabDetail(prefab, this);
            }
            detail.AddObjectReference(refObj, go);
            return detail;
        }

        public override void AddObjectDetailRef(GameObject rootObj)
        {
            var dependencies = EditorUtility.CollectDependencies(new Object[] { rootObj });
            foreach(var dep in dependencies)
            {
                AddObjectDetail(dep, rootObj, rootObj);
            }
        }
    }
}