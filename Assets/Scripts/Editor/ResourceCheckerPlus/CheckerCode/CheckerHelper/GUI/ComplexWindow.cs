/*****************************************************************************************************************
	created:	17:5:2018   16:14
	filename: 	SideBarWindow.cs
	author:		zhangjian
	
	purpose:	复杂窗体
******************************************************************************************************************/
using UnityEditor;
using UnityEngine;

namespace ResourceCheckerPlus
{
    public class ComplexWindow : EditorWindow
    {
        public static int spriteBarWidth = 4;
        private int sideBarWidth = 250;
        private int topHeight = 20;
        private int tagBarWidth = 120; 

        void OnGUI()
        {
            Rect rect = position;
            Rect rectTop = new Rect(0, 0, rect.width, topHeight);
            Rect rectTopSpit = new Rect(0, topHeight, rect.width, spriteBarWidth);
            Rect rectLeft = new Rect(tagBarWidth + spriteBarWidth, topHeight + spriteBarWidth, sideBarWidth, rect.height - topHeight);
            Rect rectMidSplit = new Rect(sideBarWidth + tagBarWidth + spriteBarWidth, topHeight, spriteBarWidth, rect.height);
            Rect rectRight = new Rect(tagBarWidth + sideBarWidth + spriteBarWidth + spriteBarWidth, topHeight + spriteBarWidth, rect.width - sideBarWidth - spriteBarWidth, rect.height - topHeight);
            Rect tagBar = new Rect(0, topHeight, tagBarWidth, rect.height - topHeight);
            Rect rectTagSplit = new Rect(tagBarWidth, topHeight, spriteBarWidth, rect.height - topHeight);

            SetMessageRect(rectLeft, rectRight);

            GUILayout.BeginArea(rectTop);
            ShowTopSide();
            GUILayout.EndArea();

            //分割线
            GUILayout.BeginArea(rectTopSpit);
            GUI.backgroundColor = Color.black;
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(spriteBarWidth));
            GUI.backgroundColor = CheckerConfigManager.defaultBackgroundColor;
            GUILayout.EndArea();

            GUILayout.BeginArea(tagBar);
            ShowTagBar();
            GUILayout.EndArea();

            //分割线
            GUILayout.BeginArea(rectTagSplit);
            GUI.backgroundColor = Color.black;
            GUILayout.Box("", GUILayout.ExpandHeight(true), GUILayout.Width(spriteBarWidth));
            GUI.backgroundColor = CheckerConfigManager.defaultBackgroundColor;
            GUILayout.EndArea();

            GUILayout.BeginArea(rectLeft);
            ShowLeftSide();
            GUILayout.EndArea();

            //分割线
            GUILayout.BeginArea(rectMidSplit);
            GUI.backgroundColor = Color.black;
            GUILayout.Box("", GUILayout.ExpandHeight(true), GUILayout.Width(spriteBarWidth));
            GUI.backgroundColor = CheckerConfigManager.defaultBackgroundColor;
            GUILayout.EndArea();

            GUILayout.BeginArea(rectRight);
            ShowRightSide();
            GUILayout.EndArea();
        }

        public void SetSideBarWide(int wide)
        {
            sideBarWidth = wide;
        }

        public virtual void ShowTagBar() { }

        public virtual void ShowLeftSide() { }

        public virtual void ShowRightSide() { }

        public virtual void ShowTopSide() { }

        public virtual void SetMessageRect(Rect sideBar, Rect mainWindow) { }
    }
}

