using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ResourceCheckerPlus
{
    public class ShaderKeywordChecker : ObjectChecker
    {
        public class ShaderKeywordDetail : ObjectDetail
        {
            public ShaderKeywordDetail(Object obj, ShaderKeywordChecker checker) : base(obj, checker)
            {

            }

            public override void InitDetailCheckObject(Object obj)
            {
                
            }

            public void InitShaderKeywordDetail(string keyword)
            {
                assetName = keyword;
                specialCheckObject = keyword;
                AddOrSetCheckValue(currentChecker.nameItem, keyword);
            }

            public void AddReferenceMaterial(Material mat)
            {
                if (materialList.Contains(mat))
                    return;
                materialList.Add(mat);
                var keywordChecker = currentChecker as ShaderKeywordChecker;
                AddOrSetCheckValue(keywordChecker.matName, materialList.Count);
            }

            public void AddReferenceShader(Shader shader)
            {
                if (shaderList.Contains(shader))
                    return;
                shaderList.Add(shader);
                var keywordChecker = currentChecker as ShaderKeywordChecker;
                AddOrSetCheckValue(keywordChecker.shaderName, shaderList.Count);
            }

            public List<Object> materialList = new List<Object>();
            public List<Object> shaderList = new List<Object>();
        }

        CheckItem shaderName;
        CheckItem matName;

        // private bool checkMaterialShaderKeywords = false;

        public override void InitChecker()
        {
            checkerName = "ShaderKeywordChecker";

            enableReloadCheckItem = true;
            shaderName = new CheckItem(this, "Shader", CheckType.Int, OnShaderButtonClick);
            matName = new CheckItem(this, "Material", CheckType.Int, OnMaterialButtonClick);

            pathItem.show = false;
            chineseCharItem.show = false;
            spaceCharItem.show = false;
            memorySizeItem.show = false;
            activeItem.show = false;

            //SetCheckMatKeyword();
        }

        public void InitShaderKeywordChecker(MaterialChecker materialChecker)
        {
            checkModule = materialChecker.checkModule;

            foreach(var d in materialChecker.FilterList)
            {
                var md = d as MaterialChecker.MaterialDetail;
                var mat = md.checkObject as Material;
                if (mat == null)
                    continue; //暂时未考虑material == null的情况

                foreach(var keyword in mat.shaderKeywords)
                {
                    AddObjectDetail(keyword, md);
                }
            }
        }

        public ShaderKeywordDetail AddObjectDetail(string keyword, MaterialChecker.MaterialDetail materialDetail)
        {
            ShaderKeywordDetail detail = null;
            foreach(var v in CheckList)
            {
                if (v.specialCheckObject != null && v.specialCheckObject.ToString() == keyword)
                    detail = v as ShaderKeywordDetail;
            }
            if (detail == null)
            {
                detail = new ShaderKeywordDetail(null, this);
                detail.InitShaderKeywordDetail(keyword);
            }
            foreach(var refObj in materialDetail.referenceObjectList)
            {
                detail.AddRootObjectReference(refObj);
            }
            foreach(var detailRef in materialDetail.detailReferenceList)
            {
                detail.AddDetailObjectReference(detailRef);
            }
            var mat = materialDetail.checkObject as Material;
            detail.AddReferenceMaterial(mat);
            detail.AddReferenceShader(mat.shader);
            return detail;
        }

        public override void AddObjectDetailRef(GameObject rootObj)
        {
            var renderers = rootObj.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                foreach (var mat in r.sharedMaterials)
                {
                    AddObjectWithRef(mat, r.gameObject, rootObj);
                }
            }
        }

        private void OnMaterialButtonClick(ObjectDetail detail)
        {
            var skd = detail as ShaderKeywordDetail;
            if (checkModule != null)
            {
                checkModule.AddObjectToSideBarList(skd.materialList);
            }
            SelectObjects(skd.materialList);
            ResourceCheckerPlus.instance.Repaint();
        }

        private void OnShaderButtonClick(ObjectDetail detail)
        {
            var skd = detail as ShaderKeywordDetail;
            if (checkModule != null)
            {
                checkModule.AddObjectToSideBarList(skd.shaderList);
            }
            SelectObjects(skd.shaderList);
            ResourceCheckerPlus.instance.Repaint();
        }
    }

    public class ShaderKeywordCheckerWindow : SideBarWindow
    {
        private static ShaderKeywordChecker checker = null;

        public static void Init(MaterialChecker materialChecker)
        {
            checker = new ShaderKeywordChecker();

            var window = GetWindow<ShaderKeywordCheckerWindow>();
            checker.currentWindow = window;
            checker.currentWindow.titleContent = new GUIContent("Shader变体检查工具", "检查材质或者Shader中使用的变体及其引用关系");
            checker.InitShaderKeywordChecker(materialChecker);
            checker.RefreshCheckResult();
            window.SetSideBarWide(1);
        }

        //public override void ShowLeftSide()
        //{
        //    if (checker == null || ResourceCheckerPlus.instance == null)
        //        Close();
        //    if (checker != null && ResourceCheckerPlus.instance != null)
        //    {
        //        checker.ShowSideBar();
        //    }
        //}

        public override void ShowRightSide()
        {
            if (checker != null && ResourceCheckerPlus.instance != null)
            {
                checker.ShowCheckerTitle();
                checker.ShowCheckResult();
            }
        }
    }
}