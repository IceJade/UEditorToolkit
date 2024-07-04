using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace ResourceCheckerPlus
{
    public class RefObjectChecker : ObjectChecker
    {
        public class RefObjectDetail : ObjectDetail
        {
            public RefObjectDetail(Object obj, RefObjectChecker checker) : base(obj, checker)
            {
                AddOrSetCheckValue(checker.refType, GetObjectType(obj));
                AddOrSetCheckValue(checker.isEmptyFolder, IsEmptyFolder(obj).ToString());
            }

            private string GetObjectType(Object obj)
            {
                string type = obj == null ? "Null" : obj.GetType().ToString();
                if (type.Contains(unityEngineHead))
                    type = type.TrimStart(unityEngineHead.ToCharArray());
                return type;
            }

            private string unityEngineHead = "UnityEngine.";

            private bool IsEmptyFolder(Object obj)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                return ResourceCheckerHelper.IsEmptyFolder(path);
            }
        }

        CheckItem refType;
        CheckItem isEmptyFolder;
        CheckItem trashAsset;

        public override void InitChecker()
        {
            checkerName = "RefObj";
            isSpecialChecker = true;
            refType = new CheckItem(this, "类型");
            isEmptyFolder = new CheckItem(this, "空文件夹");
            trashAsset = new CheckItem(this, "无引用资源");
        }

        public override ObjectDetail AddObjectDetail(object obj, Object refObj, Object detailRefObj)
        {
            var unityObject = obj as Object;
            ObjectDetail objectDetail = null;
            //先查缓存
            foreach (var detail in CheckList)
            {
                if (detail.checkObject == unityObject)
                    objectDetail = detail;
            }
            if (objectDetail == null)
            {
                objectDetail = new RefObjectDetail(unityObject, this);
            }
            if (refObj != null)
            {
                objectDetail.referenceObjectList.Add(refObj);
            }
            var isTrashAsset = objectDetail.referenceObjectList.Count == 0 || objectDetail.GetCheckValue(isEmptyFolder).ToString() == "true";
            objectDetail.AddOrSetCheckValue(trashAsset, isTrashAsset.ToString());
            return objectDetail;
        }

        public override void AddObjectDetailRef(GameObject rootObj)
        {
            var objs = EditorUtility.CollectDependencies(new Object[] { rootObj });
            foreach (var o in objs)
            {
                AddObjectWithRef(o, null, rootObj);
            }
        }

        public override void ShowOptionButton()
        {
            base.ShowOptionButton();

            if (GUILayout.Button("删除无引用资源", GUILayout.Width(100)))
            {
                this.DeleteFileOfZeroReference();
            }

            if (GUILayout.Button("设置Bundle", GUILayout.Width(80)))
            {
                SetAssetBundleTool.Init(FilterList);
            }
        }

        private void DeleteFileOfZeroReference()
        {
            if(null == FilterList || FilterList.Count <= 0)
            {
                EditorUtility.DisplayDialog("提示", "没有选中的文件, 请确认！", "OK");
                return;
            }

            int count = 0;
            for (int i = 0; i < FilterList.Count; i++)
            {
                var detail = FilterList[i];
                if(detail.referenceObjectList.Count <= 0 
                    && File.Exists(detail.assetPath))
                {
                    count++;
                    File.Delete(detail.assetPath);
                }
            }

            string message = string.Format("处理完毕,共删除{0}个文件.", count);
            EditorUtility.DisplayDialog("提示", message, "OK");
        }
    }
}