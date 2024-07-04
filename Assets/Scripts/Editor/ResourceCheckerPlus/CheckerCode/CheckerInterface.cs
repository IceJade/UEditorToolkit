//Resource Checker Plus接口文件
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ResourceCheckerPlus
{
    public class CheckerInterface
    {
        public static void ExportCheckResult()
        {
            ResourceCheckerPlus.instance.CurrentCheckModule().ExportAllActiveCheckerResult();
        }

        [MenuItem("Assets/检查资源规范")]
        public static void CheckResource()
        {
            ResourceCheckerPlus.Init();
            ResourceCheckerPlus.instance.CheckResource();
        }
    }
}


