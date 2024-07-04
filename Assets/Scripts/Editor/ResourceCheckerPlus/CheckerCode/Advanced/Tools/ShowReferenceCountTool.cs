using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace ResourceCheckerPlus
{
    public class ShowReferenceCountTool : CheckerPluginEditor
    {
        public static Dictionary<int, int> referenceDic = null;

        public static void Init(Dictionary<int, int> references)
        {
            var window = GetWindow<ShowReferenceCountTool>();
            var pos = ResourceCheckerHelper.GetCurrentPopWindowPos();
            window.position = new Rect(pos, new Vector2(400, 800));

            referenceDic = references;
        }

        public void OnGUI()
        {
            EditorGUILayout.LabelField("引用计数\t数量");

            foreach(var item in referenceDic)
                EditorGUILayout.LabelField(string.Format("{0}\t{1}", item.Key, item.Value));
        }
    }

}
