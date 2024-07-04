using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;

namespace ResourceCheckerPlus
{
    /// <summary>
    /// Shader相关处理工具函数
    /// </summary>
    public static class ResourceCheckerShaderHelper
    {
#if UNITY_2018_1_OR_NEWER
        private static string[] keywordSeparator = new string[] { " " };
        private static string[] lineSeparator = new string[] { "\r\n", "\n" };

        private static List<KeyWordTypeStruct> keyWordTypeStructs = new List<KeyWordTypeStruct>();

        private struct KeyWordTypeStruct
        {
            public string keywordString;
            public ShaderKeywordType shaderKeywordType;

            public KeyWordTypeStruct(string str, ShaderKeywordType type)
            {
                keywordString = str;
                shaderKeywordType = type;
            }
        }
        
        static ResourceCheckerShaderHelper()
        {
            keyWordTypeStructs.Add(new KeyWordTypeStruct("#pragma multi_compile ", ShaderKeywordType.MultiCompile));
            keyWordTypeStructs.Add(new KeyWordTypeStruct("#pragma shader_feature ", ShaderKeywordType.ShaderFeature));
            keyWordTypeStructs.Add(new KeyWordTypeStruct("#pragma multi_compile_local ", ShaderKeywordType.LocalMultiCompile));
            keyWordTypeStructs.Add(new KeyWordTypeStruct("#pragma shader_feature_local ", ShaderKeywordType.LocalShaderFeature));
            InitShaderHelperMethod();
        }

        public static string ClearComment(string input)
        {
            input = Regex.Replace(input, @"/\*[\s\S]*?\*/", "", RegexOptions.IgnoreCase);
            input = Regex.Replace(input, @"^\s*//[\s\S]*?$", "", RegexOptions.Multiline);
            input = Regex.Replace(input, @"^\s*$\n", "", RegexOptions.Multiline);
            input = Regex.Replace(input, @"^\s*//[\s\S]*", "", RegexOptions.Multiline);
            return input;
        }

        public static ShaderDetailParam GetShaderDetailParams(Shader shader, bool checkOnlyMainSubShader = true)
        {
            var shaderDetail = new ShaderDetailParam();

            var shaderData = ShaderUtil.GetShaderData(shader);
            if (checkOnlyMainSubShader == true)
            {
                GetSubShaderKeywords(shaderData.ActiveSubshader, shaderDetail);
            }
            else
            {
                for (int i = 0; i < shaderData.SubshaderCount; i++)
                {
                    var subShader = shaderData.GetSubshader(i);
                    GetSubShaderKeywords(subShader, shaderDetail);
                }
            }
           
            shaderDetail.shaderKeyWords.Distinct();
            return shaderDetail;
        }

        private static void GetSubShaderKeywords(ShaderData.Subshader subShader, ShaderDetailParam detail)
        {
            for (int j = 0; j < subShader.PassCount; j++)
            {
                var pass = subShader.GetPass(j);
                var shaderPassSource = ClearComment(pass.SourceCode);
                var lines = shaderPassSource.Split(lineSeparator, System.StringSplitOptions.None);

                foreach (var shaderCodeLine in lines)
                {
                    foreach (var v in keyWordTypeStructs)
                    {
                        var index = shaderCodeLine.IndexOf(v.keywordString);
                        if (index < 0)
                            continue;
                        var headLength = index + v.keywordString.Length;
                        var keywordLine = shaderCodeLine.Substring(headLength, shaderCodeLine.Length - headLength);
                        var keywords = keywordLine.Split(keywordSeparator, System.StringSplitOptions.RemoveEmptyEntries);

                        detail.AddKeywords(keywords, v.shaderKeywordType);
                    }
                }
            }
        }

        public static void GetShaderVarients(Shader shader)
        {
            if (GetShaderVariantEntriesMethod == null)
                return;
            int[] keywordAllCombinePassType = null;
            string[] keywordAllCombine = null;
            var args = new object[] { shader, new ShaderVariantCollection(), keywordAllCombinePassType, keywordAllCombine };
            GetShaderVariantEntriesMethod.Invoke(null, args);
            keywordAllCombinePassType = args[2] as int[];
            keywordAllCombine = args[3] as string[];
            var test = "";
            foreach (var v in keywordAllCombine)
                test = v;
            var test2 = 1;
            foreach (var v in keywordAllCombinePassType)
                test2 = v;
            var count = GetShaderVariantCount(shader);
            Debug.Log(shader.name + " keywordAllCombine: " + keywordAllCombine.Length + " keywordAllCombinePassType: " + keywordAllCombinePassType.Length + " Direct Count: " + count);
        }

        public static ulong GetShaderVariantCount(Shader shader)
        {
            if (GetVariantCountMethod != null)
            {
                return (ulong)GetVariantCountMethod.Invoke(null, new object[] { shader, false });
            }
            return 0;
        }

        public static bool IsSurfaceShader(Shader shader)
        {
            if (HasSurfaceShadersMethod != null)
            {
                return (bool)HasSurfaceShadersMethod.Invoke(null, new object[] { shader });
            }
            return false;
        }

        public static int GetShaderLOD(Shader shader)
        {
            if (LODMethod != null)
            {
                return (int)LODMethod.Invoke(null, new object[] { shader });
            }
            return -1;
        }

        public static bool HasShadowCasterPass(Shader shader)
        {
            if (CastShadowMethod != null)
            {
                return (bool)CastShadowMethod.Invoke(null, new object[] { shader });
            }
            return false;
        }

        public static bool IgnoreProjector(Shader shader)
        {
            if (IgnoreProjectorMethod != null)
            {
                return (bool)IgnoreProjectorMethod.Invoke(null, new object[] { shader });
            }
            return false;
        }

        //public static string GetDependencyShaderName(Shader shader)
        //{
        //    if (GetDependencyMethod != null)
        //    {
        //        return (string)GetDependencyMethod.Invoke(null, new object[] { shader, shader.name });
        //    }
        //    return "";
        //}

        private static MethodInfo HasSurfaceShadersMethod = null;
        private static MethodInfo GetVariantCountMethod = null;
        private static MethodInfo GetShaderVariantEntriesMethod = null;
        private static MethodInfo CastShadowMethod = null;
        private static MethodInfo LODMethod = null;
        private static MethodInfo IgnoreProjectorMethod = null;
        //private static MethodInfo GetDependencyMethod = null;

        private static void InitShaderHelperMethod()
        {
            HasSurfaceShadersMethod = typeof(ShaderUtil).GetMethod("HasSurfaceShaders", BindingFlags.NonPublic | BindingFlags.Static);
            GetVariantCountMethod = typeof(ShaderUtil).GetMethod("GetVariantCount", BindingFlags.NonPublic | BindingFlags.Static);
            GetShaderVariantEntriesMethod = typeof(ShaderUtil).GetMethod("GetShaderVariantEntries", BindingFlags.NonPublic | BindingFlags.Static);
            CastShadowMethod = typeof(ShaderUtil).GetMethod("HasShadowCasterPass", BindingFlags.NonPublic | BindingFlags.Static);
            LODMethod = typeof(ShaderUtil).GetMethod("GetLOD", BindingFlags.NonPublic | BindingFlags.Static);
            IgnoreProjectorMethod = typeof(ShaderUtil).GetMethod("DoesIgnoreProjector", BindingFlags.NonPublic | BindingFlags.Static);
            //GetDependencyMethod = typeof(ShaderUtil).GetMethod("GetDependency", BindingFlags.NonPublic | BindingFlags.Static);
        }
#endif
    }
}


