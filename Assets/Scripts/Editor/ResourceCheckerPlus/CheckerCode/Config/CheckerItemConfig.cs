using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceCheckerPlus
{
    [System.Serializable]
    public class CheckItemConfig
    {
        public string ItemTitle;
        public bool show = true;
        public int order = 0;

        public string ItemClassName = "";
    }
}
