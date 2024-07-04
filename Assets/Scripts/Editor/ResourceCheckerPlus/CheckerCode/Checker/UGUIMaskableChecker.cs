using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ResourceCheckerPlus
{
    public class UGUIMaskableChecker : ObjectChecker
    {
        public CheckItem rayCastHit;
        public CheckItem type;

        private Color textColor = Color.green;
        private Color imageColor = Color.red;
        private bool showDisableMask = false;

        public class MaskDetail : ObjectDetail
        {
            public MaskDetail(Object obj, UGUIMaskableChecker checker) : base(obj, checker)
            {

            }

            public override void InitDetailCheckObject(Object obj)
            {
                var maskObj = obj as MaskableGraphic;
                var checker = currentChecker as UGUIMaskableChecker;
                AddOrSetCheckValue(checker.rayCastHit, maskObj.raycastTarget.ToString());
                AddOrSetCheckValue(checker.type, maskObj.GetType().ToString());
            }
        }

        public override void InitChecker()
        {
            checkerName = "MaskChecker";
            activeItem.show = false;
            refItem.show = false;
            memorySizeItem.show = false;
            totalRefItem.show = false;
            pathItem.show = false;
            isSpecialChecker = true;
            rayCastHit = new CheckItem(this, "是否开启了Raycast");
            type = new CheckItem(this, "类型");
        }

        private Vector3[] maskCorners = new Vector3[4];

        public override void OnSceneDraw(SceneView sceneView)
        {
            base.OnSceneDraw(sceneView);
            foreach(var obj in FilterList)
            {
                var md = obj as MaskDetail;
                var mask = md.checkObject as MaskableGraphic;
                if (mask == null)
                    continue;
                if (mask.raycastTarget)
                {
                    if (showDisableMask == false && mask.gameObject.activeInHierarchy == false)
                        continue;
                    var rt = mask.transform as RectTransform;
                    rt.GetWorldCorners(maskCorners);
                    for(var i = 0; i < 4; i++)
                    {
                        Handles.color = mask is Image ? imageColor : textColor;
                        Handles.DrawLine(maskCorners[i], maskCorners[(i + 1) % 4]);
                    }
                }
            }

        }

        public override void AddObjectDetailRef(GameObject rootObj)
        {
            var coms = rootObj.GetComponentsInChildren<MaskableGraphic>(true);
            foreach (var mask in coms)
            {
                AddObjectWithRef(mask, mask.gameObject, rootObj);
            }
        }

        public override ObjectDetail AddObjectDetail(object obj, Object refObj, Object detailRefObj)
        {
            var maskObj = obj as MaskableGraphic;
            if (maskObj == null)
                return null;
            MaskDetail detail = null;
            foreach(var v in CheckList)
            {
                if (v.checkObject == maskObj)
                    detail = v as MaskDetail;
            }
            if (detail == null)
            {
                detail = new MaskDetail(maskObj, this);
            }
            detail.AddObjectReference(refObj, detailRefObj);
            return detail;
        }

        public override void ShowOptionButton()
        {
            base.ShowOptionButton();
            EditorGUI.BeginChangeCheck();
            textColor = EditorGUILayout.ColorField("Text颜色", textColor, GUILayout.Width(200));
            imageColor = EditorGUILayout.ColorField("Image颜色", imageColor, GUILayout.Width(200));
            showDisableMask = GUILayout.Toggle( showDisableMask, "显示非激活节点", GUILayout.Width(120));
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
        }

        public override void RefreshCheckResult()
        {
            base.RefreshCheckResult();
            SceneView.RepaintAll();
        }


    }
}

