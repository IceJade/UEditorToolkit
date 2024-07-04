using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ResourceCheckerPlus
{
    public class CustomPreviewWindow : EditorWindow
    {
        private static Texture2D currentPreviewTexture = null;
        private static CustomPreviewWindow window = null;
        private static bool viewOriSize = true;

        private TexturePreviewType currentPreviewType = TexturePreviewType.Transparent;
        private string[] previewStr = new string[] { "半透模式", "RGB模式", "Alpha模式" };
        private enum TexturePreviewType
        {
            Transparent = 0,
            TextureRGB,
            TextureA,
        }

        public static void Init(string name)
        {
            if (window == null)
            {
                window = GetWindow<CustomPreviewWindow>();
                window.name = name;
                window.minSize = new Vector2(600, 600);
                var rect = new Rect(Event.current.mousePosition, new Vector2(600, 600));
                window.position = rect;
            }
        }

        public static void SetPreviewTexture(Texture2D tex)
        {
            Init("自定义预览");
            currentPreviewTexture = tex;
            window.Repaint();
            window.ShowPopup();
        }

        private void OnGUI()
        {
            if (currentPreviewTexture == null)
                return;
            GUILayout.BeginHorizontal();
            currentPreviewType = (TexturePreviewType)GUILayout.Toolbar((int)currentPreviewType, previewStr);
            viewOriSize = GUILayout.Toggle(viewOriSize, "显示原始大小");
            GUILayout.EndHorizontal();
            GUILayout.Label(currentPreviewTexture.name + " " + currentPreviewTexture.width + " x " + currentPreviewTexture.height);
            var rect = GetPreviewSize();
            switch(currentPreviewType)
            {
                case TexturePreviewType.Transparent:
                    EditorGUI.DrawTextureTransparent(rect, currentPreviewTexture);
                    break;
                case TexturePreviewType.TextureRGB:
                    EditorGUI.DrawPreviewTexture(rect, currentPreviewTexture);
                    break;
                case TexturePreviewType.TextureA:
                    EditorGUI.DrawTextureAlpha(rect, currentPreviewTexture);
                    break;
            }
        }

        private Rect GetPreviewSize()
        {
            var rect = window.position;
            rect.position = new Vector2(5, 40);
            rect.size -= new Vector2(50, 50);
            if (viewOriSize)
            {
                rect.width = Mathf.Min(currentPreviewTexture.width, rect.width);
                rect.height = Mathf.Min(currentPreviewTexture.height, rect.height);
            }
            return rect;
        }
    }

}
