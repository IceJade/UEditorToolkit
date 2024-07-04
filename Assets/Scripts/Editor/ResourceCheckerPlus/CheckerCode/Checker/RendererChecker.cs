using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ResourceCheckerPlus
{
    public class RendererChecker : ObjectChecker
    {
        public class RendererDetail : ObjectDetail
        {
            public static string unityEngineHeadStr = "UnityEngine.";
            public RendererDetail(Object obj, RendererChecker checker) : base(obj, checker)
            {

            }

            public override void InitDetailCheckObject(Object obj)
            {
                var checker = currentChecker as RendererChecker;
                var render = obj as Renderer;
                var serializeObj = new SerializedObject(render);
                var scaleInLightMap = serializeObj.FindProperty("m_ScaleInLightmap").floatValue;
                AddOrSetCheckValue(checker.enabled, render.enabled);
                AddOrSetCheckValue(checker.lightMapIndex, render.lightmapIndex);
                AddOrSetCheckValue(checker.lightMapScale, scaleInLightMap);
                AddOrSetCheckValue(checker.materialCount, render.sharedMaterials.Length);
                AddOrSetCheckValue(checker.rendererType, render.GetType().ToString().TrimStart(unityEngineHeadStr.ToCharArray()));
                AddOrSetCheckValue(checker.castShadows, render.shadowCastingMode.ToString());
                AddOrSetCheckValue(checker.receiveShadows, render.receiveShadows.ToString());
                AddOrSetCheckValue(checker.reflectionProbs, render.reflectionProbeUsage.ToString());
            }
        }

        CheckItem enabled;
        CheckItem lightMapIndex;
        CheckItem lightMapScale;
        CheckItem materialCount;
        CheckItem rendererType;
        CheckItem castShadows;
        CheckItem receiveShadows;
        CheckItem reflectionProbs;

        public override void InitChecker()
        {
            checkerName = "Renderer";
            isSpecialChecker = true;
            enabled = new CheckItem(this, "激活");
            lightMapIndex = new CheckItem(this, "LightMapIndex", CheckType.Int);
            lightMapScale = new CheckItem(this, "LightMapScale", CheckType.Float);
            rendererType = new CheckItem(this, "类型");
            materialCount = new CheckItem(this, "材质数", CheckType.Int);
            castShadows = new CheckItem(this, "CastShadow");
            receiveShadows = new CheckItem(this, "ReceiveShadow");
            reflectionProbs = new CheckItem(this, "ReflectionPorbs");
        }

        public override ObjectDetail AddObjectDetail(object obj, Object refObj, Object detailRefObj)
        {
            var renderer = obj as Renderer;
            if (renderer == null)
                return null;
            var detail = new RendererDetail(renderer, this);
            detail.AddObjectReference(refObj, detailRefObj);
            return detail;
        }

        public override void AddObjectDetailRef(GameObject rootObj)
        {
            var renderers = rootObj.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                AddObjectWithRef(r, r.gameObject, rootObj);
            }
        }
    }

}
