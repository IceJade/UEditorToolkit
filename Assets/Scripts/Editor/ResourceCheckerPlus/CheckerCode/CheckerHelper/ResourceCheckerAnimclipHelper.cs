using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.IO;

/// <summary>
/// 更新动画工具内容2020.3
/// 1.用骨骼动画压缩的方式压缩在unity内手K动画出问题
/// 2.补充部分动画相关属性反射
/// </summary>
namespace ResourceCheckerPlus
{
    public class ResourceCheckerAnimclipHelper
    {
        static MethodInfo getAnimationClipStats;
        static FieldInfo sizeInfo;

        static ResourceCheckerAnimclipHelper()
        {
            var asm = Assembly.GetAssembly(typeof(Editor));
            getAnimationClipStats = typeof(AnimationUtility).GetMethod("GetAnimationClipStats", BindingFlags.Static | BindingFlags.NonPublic);
            var aniclipstats = asm.GetType("UnityEditor.AnimationClipStats");
            sizeInfo = aniclipstats.GetField("size", BindingFlags.Public | BindingFlags.Instance);
        }

        private static object[] animObject = new object[1];

        public static int GetAnimSize(AnimationClip clip)
        {
            animObject[0] = clip;
            var stats = getAnimationClipStats.Invoke(null, animObject);
            return (int)sizeInfo.GetValue(stats);
        }


    }

}

