using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ResourceCheckerPlus
{
    public enum ResourceTagType
    {
        Common,
        InFolder,
        //ContainsStr,
    }

    [System.Serializable]
    public class ResourceTag
    {
        public string resourceTagName;
        public UnityEngine.Object resourceFolder;
        public ResourceTagType resourceTagType;
        public string tagGUIDs;
        public bool isSceneResourceTag;
    }
}

