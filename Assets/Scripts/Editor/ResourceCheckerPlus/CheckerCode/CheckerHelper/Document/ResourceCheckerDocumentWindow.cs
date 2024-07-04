using UnityEngine;
using UnityEditor;

namespace ResourceCheckerPlus
{
    public class ResourceCheckerDocumentWindow : EditorWindow
    {
        public static void Init()
        {
            var window = GetWindow<ResourceCheckerDocumentWindow>();
            var pos = ResourceCheckerHelper.GetCurrentPopWindowPos();
            window.position = new Rect(pos, new Vector2(1200, 800));
        }

        private void OnGUI()
        {
            CheckerDocumentHelper.DrawDocuments();
        }
    }
}

