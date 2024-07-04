using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
#pragma warning disable 0618
namespace ResourceCheckerPlus
{
    public class AnimClipChecker : ObjectChecker
    {
        public class AnimClipDetail : ObjectDetail
        {
            public AnimClipDetail(Object obj, AnimClipChecker checker) : base(obj, checker)
            {

            }

            public override void InitDetailCheckObject(Object obj)
            {
                var clip = obj as AnimationClip;
                var checker = currentChecker as AnimClipChecker;
                AddOrSetCheckValue(checker.animLength, clip.length);
                AddOrSetCheckValue(checker.animWrapMode, clip.wrapMode.ToString());
                AddOrSetCheckValue(checker.animLoop, clip.isLooping.ToString());
                AddOrSetCheckValue(checker.animFrameRate, clip.frameRate);
                AddOrSetCheckValue(checker.animLegacy, clip.legacy.ToString());
                AddOrSetCheckValue(checker.animSize, ResourceCheckerAnimclipHelper.GetAnimSize(clip));
            }
        }

        CheckItem animSize;
        CheckItem animLength;
        CheckItem animWrapMode;
        CheckItem animLoop;
        CheckItem animFrameRate;
        CheckItem animLegacy;

        private bool compressTime = false;

        public override void InitChecker()
        {
            checkerName = "AnimClip";
            checkerFilter = "t:AnimationClip";
            animSize = new CheckItem(this, "大小", CheckType.FormatSize);
            animLength = new CheckItem(this, "时间", CheckType.Float);
            animWrapMode = new CheckItem(this, "WrapMode");
            animLoop = new CheckItem(this, "Looping");
            animFrameRate = new CheckItem(this, "FrameRate", CheckType.Float);
            animLegacy = new CheckItem(this, "IsLegacy");
        }

        public override ObjectDetail AddObjectDetail(object obj, Object refObj, Object detailRefObj)
        {
            var animClip = obj as AnimationClip;
            if (animClip == null)
                return null;
            ObjectDetail detail = null;
            foreach (var v in CheckList)
            {
                if (v.checkObject == animClip)
                    detail = v;
            }
            if (detail == null)
            {
                detail = new AnimClipDetail(animClip, this);
            }
            detail.AddObjectReference(refObj, detailRefObj);
            return detail;
        }

        public override List<Object> GetAllDirectCheckObjectFromInput(Object[] selection, string filter)
        {
            return GetAllObjectFromInput<AnimationClip>(selection, filter);
        }

        public override void AddObjectDetailRef(GameObject rootObj)
        {
            AddAnimDetail<Animation>(rootObj);
            AddAnimDetail<Animator>(rootObj);
        }

        private void AddAnimDetail<T>(GameObject rootObj) where T : Component
        {
            var coms = rootObj.GetComponentsInChildren<T>(true);
            foreach (var anim in coms)
            {
                var dependency = EditorUtility.CollectDependencies(new Object[] { anim.gameObject });
                foreach (var clip in dependency)
                {
                    AddObjectWithRef(clip, anim.gameObject, rootObj);
                }
            }
        }

        public override void ShowOptionButton()
        {
            base.ShowOptionButton();
            compressTime = GUILayout.Toggle(compressTime, "压缩时间项", GUILayout.Width(130));
            if (GUILayout.Button("动画浮点精度压缩"))
            {
                ReduceAnimationClipFloat();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private void ReduceAnimationClipFloat()
        {
            int count = 0;
            foreach (var v in FilterList.Select(x => x.checkObject as AnimationClip))
            {
                count++;
                if (EditorUtility.DisplayCancelableProgressBar("压缩动画资源", "已完成: " + count + "/" + FilterList.Count, (float)count / FilterList.Count))
                    break;
                CompressAnimationClip(v, compressTime);
            }
            EditorUtility.ClearProgressBar();
        }

        public static void RemoveAnimationCurve(GameObject _obj, bool compressTime)
        {
            var tAnimationClipList = new List<AnimationClip>(AnimationUtility.GetAnimationClips(_obj));
            if (tAnimationClipList.Count == 0)
            {
                var tObjectList = UnityEngine.Object.FindObjectsOfType(typeof(AnimationClip)) as AnimationClip[];
                tAnimationClipList.AddRange(tObjectList);
            }

            foreach (AnimationClip animClip in tAnimationClipList)
            {
                foreach (EditorCurveBinding curveBinding in AnimationUtility.GetCurveBindings(animClip))
                {
                    var tName = curveBinding.propertyName.ToLower();
                    if (tName.Contains("scale"))
                    {
                        AnimationUtility.SetEditorCurve(animClip, curveBinding, null);
                    }
                }
                CompressAnimationClip(animClip, compressTime);
            }
        }

        //压缩精度
        public static void CompressAnimationClip(AnimationClip _clip, bool compressTime)
        {
            var tCurveArr = AnimationUtility.GetAllCurves(_clip);
            Keyframe tKey;
            Keyframe[] tKeyFrameArr;
            for (int i = 0; i < tCurveArr.Length; ++i)
            {
                var tCurveData = tCurveArr[i];
                if (tCurveData.curve == null || tCurveData.curve.keys == null)
                {
                    continue;
                }
                tKeyFrameArr = tCurveData.curve.keys;
                for (int j = 0; j < tKeyFrameArr.Length; j++)
                {
                    tKey = tKeyFrameArr[j];
                    if (compressTime)
                    {
                        tKey.time = float.Parse(tKey.time.ToString("f3")); ;
                    }
                    tKey.value = float.Parse(tKey.value.ToString("f3")); ;
                    tKey.inTangent = float.Parse(tKey.inTangent.ToString("f3"));
                    tKey.outTangent = float.Parse(tKey.outTangent.ToString("f3"));
                    tKey.inWeight = float.Parse(tKey.inWeight.ToString("f3"));
                    tKey.outWeight = float.Parse(tKey.outWeight.ToString("f3"));
                    tKeyFrameArr[j] = tKey;
                }
                tCurveData.curve.keys = tKeyFrameArr;
                _clip.SetCurve(tCurveData.path, tCurveData.type, tCurveData.propertyName, tCurveData.curve);
            }
        }
    }
}
