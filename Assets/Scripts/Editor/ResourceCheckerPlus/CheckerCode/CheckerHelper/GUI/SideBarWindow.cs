/*****************************************************************************************************************
	created:	17:5:2018   16:14
	filename: 	SideBarWindow.cs
	author:		zhangjian
	
	purpose:	侧边栏窗体
******************************************************************************************************************/
using UnityEditor;
using UnityEngine;

namespace ResourceCheckerPlus
{
    public class SideBarWindow : EditorWindow
    {
        public static int spriteBarWidth = 4;
        private int sideBarWidth = 250;

        void OnGUI()
        {
            Rect rect = position;
            Rect rectLeft = new Rect(0, 0, sideBarWidth, rect.height);
            Rect rectMid = new Rect(sideBarWidth, 0, spriteBarWidth, rect.height);
            Rect rectRight = new Rect(sideBarWidth + spriteBarWidth, 0, rect.width - sideBarWidth - spriteBarWidth, rect.height);

            SetMessageRect(rectLeft, rectRight);

            GUILayout.BeginArea(rectLeft);
            ShowLeftSide();
            GUILayout.EndArea();

            //分割线
            GUILayout.BeginArea(rectMid);
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

        public virtual void ShowLeftSide() { }

        public virtual void ShowRightSide() { }

        public virtual void SetMessageRect(Rect sideBar, Rect mainWindow) { }
    }
}

