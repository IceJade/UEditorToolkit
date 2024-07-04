using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace ResourceCheckerPlus
{

    public class ShaderProperty
    {
        public string name;
        public string description;
        public string type;
    }

    /// <summary>
    /// Shader类型检查
    /// </summary>
    public class ShaderChecker : ObjectChecker
    {
        public class ShaderDetail : ObjectDetail
        {
            public ShaderDetail(Object obj, ShaderChecker checker) : base(obj, checker)
            {

            }

            public override void InitDetailCheckObject(Object obj)
            {
                var shader = obj as Shader;
                var checker = currentChecker as ShaderChecker;
                AddOrSetCheckValue(checker.shaderMaxLod, shader.maximumLOD);
                AddOrSetCheckValue(checker.shaderRenderQueue, shader.renderQueue);

                var propertyCount = ShaderUtil.GetPropertyCount(shader);
                for (int i = 0; i < propertyCount; i++)
                {
                    GetShaderProperty(shader, i);
                }
                AddOrSetCheckValue(checker.shaderPropertyCount, propertyCount);
                AddOrSetCheckValue(checker.systemVarientCount, (int)ResourceCheckerShaderHelper.GetShaderVariantCount(shader));
                AddOrSetCheckValue(checker.shaderIsSurface, ResourceCheckerShaderHelper.IsSurfaceShader(shader).ToString());
                AddOrSetCheckValue(checker.shaderLOD, ResourceCheckerShaderHelper.GetShaderLOD(shader));
                AddOrSetCheckValue(checker.igonreProjector, ResourceCheckerShaderHelper.IgnoreProjector(shader).ToString());
                AddOrSetCheckValue(checker.castShadow, ResourceCheckerShaderHelper.HasShadowCasterPass(shader).ToString());
                //AddOrSetCheckValue(checker.dependencyShader, ResourceCheckerShaderHelper.GetDependencyShaderName(shader));
#if UNITY_2018_1_OR_NEWER
                var shaderData = ShaderUtil.GetShaderData(shader);
                if (shaderData != null)
                {
                    AddOrSetCheckValue(checker.subShaderCount, shaderData.SubshaderCount);
                    AddOrSetCheckValue(checker.currentSubShaderIndex, shaderData.ActiveSubshaderIndex);
                    var currentSubShader = shaderData.ActiveSubshader;
                    if (currentSubShader != null)
                    {
                        AddOrSetCheckValue(checker.currentSubShaderPassCount, currentSubShader.PassCount);
                    }
                }
                detailParam = ResourceCheckerShaderHelper.GetShaderDetailParams(shader, checker.checkShaderKeywordOnlyCurrentSubShader);
                AddOrSetCheckValue(checker.shaderKeyWordSetCount, detailParam.shaderKeyWordSets.Count);
                AddOrSetCheckValue(checker.shaderKeyWordsCount, detailParam.shaderKeyWords.Count);
                AddOrSetCheckValue(checker.customVarientCount, detailParam.shaderVariantCount);
#endif
            }

            private void GetShaderProperty(Shader shader, int index)
            {
                var property = new ShaderProperty();
                property.name = ShaderUtil.GetPropertyName(shader, index);
                property.description = ShaderUtil.GetPropertyDescription(shader, index);
                property.type = ShaderUtil.GetPropertyType(shader, index).ToString();
                propertyList.Add(property);
            }

            public List<ShaderProperty> propertyList = new List<ShaderProperty>();
            public bool showShaderProperty = false;
            public ShaderDetailParam detailParam = null;

            public bool showKeywordSets = false;
        }

        CheckItem shaderMaxLod;
        CheckItem shaderRenderQueue;
        CheckItem shaderPropertyCount;
#if UNITY_2018_1_OR_NEWER
        CheckItem subShaderCount;
        CheckItem currentSubShaderIndex;
        CheckItem currentSubShaderPassCount;
        CheckItem shaderKeyWordSetCount;
        CheckItem shaderKeyWordsCount;
        CheckItem customVarientCount;
#endif
        CheckItem systemVarientCount;
        CheckItem shaderIsSurface;
        CheckItem igonreProjector;
        CheckItem shaderLOD;
        CheckItem castShadow;
        //CheckItem dependencyShader;

        public bool checkShaderKeywordOnlyCurrentSubShader = true;

        public override void InitChecker()
        {
            checkerName = "Shader";
            checkerFilter = "t:Shader";
            enableReloadCheckItem = true;
            shaderPropertyCount = new CheckItem(this, "PropertyCount", CheckType.Int, OnButtonShowPropertyClick);

            shaderRenderQueue = new CheckItem(this, "RenderQueue", CheckType.Int);
            systemVarientCount = new CheckItem(this, "Total Varient Count", CheckType.Int);
#if UNITY_2018_1_OR_NEWER
            shaderKeyWordSetCount = new CheckItem(this, "KeywordSet Count", CheckType.Int, OnButtonShowKeywordSetClick);
            shaderKeyWordsCount = new CheckItem(this, "KeyWord Count", CheckType.Int);
            customVarientCount = new CheckItem(this, "Variant Count", CheckType.Int);
            subShaderCount = new CheckItem(this, "SubShader Count", CheckType.Int);
            currentSubShaderIndex = new CheckItem(this, "Current SubShader", CheckType.Int);
            currentSubShaderPassCount = new CheckItem(this, "Pass Count", CheckType.Int);
#endif
            shaderIsSurface = new CheckItem(this, "IsSurface");
            shaderLOD = new CheckItem(this, "LOD", CheckType.Int);
            shaderMaxLod = new CheckItem(this, "MaximumLOD", CheckType.Int);
            igonreProjector = new CheckItem(this, "Igonre Projector");
            castShadow = new CheckItem(this, "Cast Shadow");
            //dependencyShader = new CheckItem(this, "Dependency Shader");
        }

        public override ObjectDetail AddObjectDetail(object obj, Object refObj, Object detailRefObj)
        {
            var shader = obj as Shader;
            if (shader == null)
                return null;
            ObjectDetail detail = null;
            foreach (var v in CheckList)
            {
                if (v.checkObject == shader)
                    detail = v;
            }
            if (detail == null)
            {
                detail = new ShaderDetail(shader, this);
            }
            detail.AddObjectReference(refObj, detailRefObj);
            return detail;
        }

        public override void AddObjectDetailRef(GameObject rootObj)
        {
            var renderers = rootObj.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                foreach (var mat in r.sharedMaterials)
                {
                    if (mat != null && mat.shader != null)
                    {
                        AddObjectWithRef(mat.shader, r.gameObject, rootObj);
                    }
                }
            }
        }

        // TODO:增加一种通用的显示子项的方法
        public override void ShowChildDetail(ObjectDetail detail)
        {
            ShaderDetail shaderDetail = detail as ShaderDetail;
            if (shaderDetail.showShaderProperty)
            {
                for (int i = 0; i < shaderDetail.propertyList.Count; i++)
                {
                    ShaderProperty property = shaderDetail.propertyList[i];
                    if (property == null)
                        continue;
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(720);
                    GUILayout.Label(property.name, GUILayout.Width(100));
                    GUILayout.Label(property.type, GUILayout.Width(100));
                    GUILayout.Label(property.description, GUILayout.Width(100));
                    GUILayout.EndHorizontal();
                }
            }
            if (shaderDetail.showKeywordSets)
            {
                var keywordSets = shaderDetail.detailParam.shaderKeyWordSets;
                for (int i = 0; i < keywordSets.Count; i++)
                {
                    var keywordSet = keywordSets[i];
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(200);
                    GUILayout.Label(keywordSet.keywordType.ToString(), GUILayout.Width(200));
                    foreach (var key in keywordSet.shaderKeyWords)
                    {
                        GUILayout.Label(key, GUILayout.Width(200));
                        GUILayout.Space(20);
                    }
                    GUILayout.EndHorizontal();
                }
            }

        }

        private void OnButtonShowPropertyClick(ObjectDetail detail)
        {
            var sd = detail as ShaderDetail;
            sd.showShaderProperty = !sd.showShaderProperty;
        }

        private void OnButtonShowKeywordSetClick(ObjectDetail detail)
        {
            var sd = detail as ShaderDetail;
            sd.showKeywordSets = !sd.showKeywordSets;
        }

        public override void ShowOptionButton()
        {
            checkShaderKeywordOnlyCurrentSubShader = GUILayout.Toggle(checkShaderKeywordOnlyCurrentSubShader, "仅检查激活SubShader的Keyword");

            if (GUILayout.Button("Test"))
            {
                foreach(var d in FilterList)
                {
                    var shader = d.checkObject as Shader;
                    ResourceCheckerShaderHelper.GetShaderVarients(shader);
                }
            }

            if (GUILayout.Button("Test2"))
            {
                foreach (var d in FilterList)
                {
                    var shader = d.checkObject as Shader;
                    var count = ResourceCheckerShaderHelper.GetShaderVariantCount(shader);
                    Debug.Log(count);
                }

            }
        }
    }
}
