using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ResourceCheckerPlus
{
    public class MaterialChecker : ObjectChecker
    {
        public class MaterialDetail : ObjectDetail
        {
            public MaterialDetail(Object obj, MaterialChecker checker) : base(obj, checker)
            {

            }

            public override void InitDetailCheckObject(Object obj)
            {
                var checker = currentChecker as MaterialChecker;
                var material = obj as Material;
                AddOrSetCheckValue(checker.matShaderName, material.shader == null ? "null" : material.shader.name);
                AddOrSetCheckValue(checker.matRenderQueue, material.renderQueue);
                AddOrSetCheckValue(checker.matPassCount, material.passCount);
                Texture tex = null;
                if (material.HasProperty("_MainTex") && material.mainTexture != null)
                {
                    tex = material.mainTexture;
                    AddOrSetCheckValue(checker.previewItem, tex);
                }
                keywords.AddRange(material.shaderKeywords);
                AddOrSetCheckValue(checker.matKeywords, keywords.Count);
                AddOrSetCheckValue(checker.matHasUnusedTexture, CheckHasUnusedTextureInMaterial(material));
            }

            public List<string> keywords = new List<string>();
            public bool showChildren = false;

            public int CheckHasUnusedTextureInMaterial(Material material)
            {
                var so = new SerializedObject(material);
                var properties = so.FindProperty("m_SavedProperties");
                var texEnvs = properties.FindPropertyRelative("m_TexEnvs");
                var count = 0;
                for (int i = 0; i < texEnvs.arraySize; i++)
                {
                    var name = texEnvs.GetArrayElementAtIndex(i).FindPropertyRelative("first").stringValue;
                    if (material.HasProperty(name) == false)
                    {
                        count++;
                        Debug.Log(material.name + " " + name);
                    }
                }
                return count;
            }

           

        }

        CheckItem matShaderName;
        CheckItem matRenderQueue;
        CheckItem matPassCount;
        CheckItem matKeywords;
        CheckItem matHasUnusedTexture;

        public string[] checkModStr = new string[] { "标准模式", "变体检查模式" };

       // private bool checkMaterialShaderKeywords = false;

        public override void InitChecker()
        {
            checkerName = "Material";
            checkerFilter = "t:Material";
            postfix = ".mat";
            enableReloadCheckItem = true;
            matShaderName = new CheckItem(this, "Shader");
            matRenderQueue = new CheckItem(this, "渲染队列", CheckType.Int);
            matPassCount = new CheckItem(this, "Pass数", CheckType.Int);
            matKeywords = new CheckItem(this, "Keyword", CheckType.Int, OnMaterialKeywordButtonClick);
            matHasUnusedTexture = new CheckItem(this, "废弃引用纹理数", CheckType.Int);

            //SetCheckMatKeyword();
        }

        public override ObjectDetail AddObjectDetail(object obj, Object refObj, Object detailRefObj)
        {
            var material = obj as Material;
            if (material == null)
                return null;
            ObjectDetail detail = null;
            foreach (var v in CheckList)
            {
                if (v.checkObject == material)
                    detail = v;
            }
            if (detail == null)
            {
                detail = new MaterialDetail(material, this);
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
                    AddObjectWithRef(mat, r.gameObject, rootObj);
                }
            }
        }

        public override void ShowOptionButton()
        {
            base.ShowOptionButton();
            //EditorGUI.BeginChangeCheck();
            //checkMaterialShaderKeywords = GUILayout.Toggle(checkMaterialShaderKeywords, "检查Shader Keyword", GUILayout.Width(150));
            //if (EditorGUI.EndChangeCheck())
            //{
            //    SetCheckMatKeyword();
            //}
            if (GUILayout.Button("检查Shader Keyword", GUILayout.Width(150)))
            {
                ShaderKeywordCheckerWindow.Init(this);
            }
            if (GUILayout.Button("清理材质废弃贴图", GUILayout.Width(150)))
            {
                int count = 0;
                foreach (var detail in FilterList)
                {
                    count++;
                    if (EditorUtility.DisplayCancelableProgressBar("清理废弃引用纹理", "已完成: " + count + "/" + FilterList.Count, (float)count / FilterList.Count))
                        break;
                    var mat = detail.checkObject as Material;
                    if (mat != null)
                        CleanUnusedTextureProperty(mat);
                }
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

        }

        private void SetCheckMatKeyword()
        {
            //matKeywords.show = checkMaterialShaderKeywords;
        }

        private void OnMaterialKeywordButtonClick(ObjectDetail detail)
        {
            var md = detail as MaterialDetail;
            md.showChildren = !md.showChildren;
        }

        public override void ShowChildDetail(ObjectDetail detail)
        {
            base.ShowChildDetail(detail);
            var md = detail as MaterialDetail;
            if (md.showChildren == false)
                return;
            foreach (var child in md.keywords)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(150);
                GUILayout.Label(child);
                GUILayout.EndHorizontal();
            }
        }

        public void CleanUnusedTextureProperty(Material material)
        {
            var so = new SerializedObject(material);
            var properties = so.FindProperty("m_SavedProperties");
            var texEnvs = properties.FindPropertyRelative("m_TexEnvs");
            for (int i = 0; i < texEnvs.arraySize; i++)
            {
                var name = texEnvs.GetArrayElementAtIndex(i).FindPropertyRelative("first").stringValue;
                if (material.HasProperty(name) == false)
                {
                    texEnvs.DeleteArrayElementAtIndex(i);
                    Debug.Log(material.name + " " + name);
                }
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(material);
        }


    }
}