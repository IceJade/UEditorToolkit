using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceCheckerPlus
{
    [System.Serializable]
    public enum ShaderKeywordType
    {
        MultiCompile,
        ShaderFeature,
        LocalMultiCompile,
        LocalShaderFeature,
    }

    [System.Serializable]
    public class ShaderKeyWordSet
    {
        public List<string> shaderKeyWords = new List<string>();
        public ShaderKeywordType keywordType;

        public bool Equal(string[] keywords)
        {
            if (shaderKeyWords.Count != keywords.Length)
                return false;
            for (int i = 0; i < shaderKeyWords.Count; i++)
            {
                if (shaderKeyWords[i] != keywords[i])
                    return false;
            }
            return true;
        }
    }

    [System.Serializable]
    public class ShaderDetailParam
    {
        public List<ShaderKeyWordSet> shaderKeyWordSets = new List<ShaderKeyWordSet>();
        public List<string> shaderKeyWords = new List<string>();
        public int shaderVariantCount = 1;

        public void AddKeywords(string[] keywords, ShaderKeywordType type)
        {
            ShaderKeyWordSet keyWordSet = null;
            foreach (var v in shaderKeyWordSets)
            {
                if (v.Equal(keywords))
                    keyWordSet = v;
            }
            if (keyWordSet != null)
                return;
            keyWordSet = new ShaderKeyWordSet();
            keyWordSet.shaderKeyWords.AddRange(keywords);
            keyWordSet.keywordType = type;
            shaderKeyWordSets.Add(keyWordSet);
            shaderKeyWords.AddRange(keywords);
            shaderVariantCount *= keywords.Length;
        }
    }
}
