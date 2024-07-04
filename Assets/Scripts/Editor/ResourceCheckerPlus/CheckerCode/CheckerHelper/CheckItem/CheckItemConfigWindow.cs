/*****************************************************************************************************
	created:	24:11:2017   14:09
	filename: 	CheckItemConfigWindow.cs
	author:		zhangjian
	
	purpose:	检查项设置窗口，可以修改检查项目是否开启，排序类型，宽度等
******************************************************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace ResourceCheckerPlus
{
    public class CheckItemConfigWindow : EditorWindow
    {
        private Vector2 liftSideScrollPos = Vector2.zero;

        private static ObjectChecker currentChecker = null;

        public static void Init(ObjectChecker checker)
        {
            var window = GetWindow<CheckItemConfigWindow>();
            var pos = ResourceCheckerHelper.GetCurrentPopWindowPos();
            window.position = new Rect(pos, new Vector2(300, 600));
            currentChecker = checker;
        }

        public void OnGUI()
        {
            if (currentChecker == null || ResourceCheckerPlus.instance == null)
            {
                Close();
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label("检查属性", GUILayout.Width(200));
            GUILayout.Label("顺序序号", GUILayout.Width(30));
            GUILayout.EndHorizontal();

            liftSideScrollPos = EditorGUILayout.BeginScrollView(liftSideScrollPos);
            EditorGUI.BeginChangeCheck();
            foreach (var v in currentChecker.checkItemList)
            {
                GUILayout.BeginHorizontal();
                
                EditorGUI.BeginDisabledGroup((v.itemFlag & ItemFlag.NoCustomShow) != 0);
                v.show = GUILayout.Toggle(v.show, v.title, GUILayout.Width(220));
                EditorGUI.EndDisabledGroup();
                v.order = EditorGUILayout.IntField(v.order, GUILayout.Width(30));
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("全部开启", GUILayout.Width(70)))
            {
                currentChecker.SetAllCheckItemVisible(true);
            }

            if (GUILayout.Button("全部关闭", GUILayout.Width(70)))
            {
                currentChecker.SetAllCheckItemVisible(false);
            }

            GUILayout.Space(55);
            if (GUILayout.Button("刷新顺序", GUILayout.Width(70)))
            {
                currentChecker.RefreshCheckerItemConfig(true);
            }
            GUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                currentChecker.RefreshCheckerItemConfig(false);
            }
        }
    }
}
