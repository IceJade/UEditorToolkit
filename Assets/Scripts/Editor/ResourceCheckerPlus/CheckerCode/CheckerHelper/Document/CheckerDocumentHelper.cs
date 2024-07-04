using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using UnityEditor;

namespace ResourceCheckerPlus
{
    /// <summary>
    /// Resource Checker Plus内置文档工具
    /// </summary>
    public class CheckerDocumentHelper
    {
        [System.Serializable]
        private class DocumentDetail
        {
            public string content = null;
            public string texture = null;
            [System.NonSerialized]
            public Texture2D textureAsset = null;
        }

        [System.Serializable]
        private class DocumentStruct
        {
            public string title = null;
            public List<DocumentDetail> documentDetails = new List<DocumentDetail>();
            [System.NonSerialized]
            public bool show = false;
        }

        [System.Serializable]
        private class DocumentStructWrap
        {
            public List<DocumentStruct> documentStructs = null;
        }

        private const string documentFolder = "/CheckerDocument/";
        private const string updateNoteName = "Resource Checker Plus更新日志.txt";
        private const string userGuideInfo = "UserGuide/GuideInfo.json";
        private const string documentName = "test.jpg";
        private const string documentTextureRoot = "UserGuide/";


        private static List<string> updateNotes = new List<string>();
        private static List<DocumentStruct> documentStructs = new List<DocumentStruct>();

        private static List<DocumentStruct> normalDoc = new List<DocumentStruct>();
        private static List<DocumentStruct> advancedDoc = new List<DocumentStruct>();

        private static Vector2 scrollPos = Vector2.zero;
        
        public static void LoadDocuments()
        {
            LoadUserGuide();
            LoadUpdateNotes();
        }

        public static void ReleaseDocuments()
        {
            documentStructs.Clear();
            updateNotes.Clear();
            normalDoc.Clear();
            advancedDoc.Clear();
        }

        private static void LoadUserGuide()
        {
            documentStructs.Clear();
            normalDoc.Clear();
            advancedDoc.Clear();
            var fullpath = ResourceCheckerHelper.GetResourceCheckerRootPath();
            fullpath = fullpath + documentFolder + userGuideInfo;
            var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(fullpath);
            var json = textAsset.text;
            var wrap = JsonUtility.FromJson<DocumentStructWrap>(json);
            foreach(var v in wrap.documentStructs)
            {
                documentStructs.Add(v);
                if (v.title.EndsWith("-进阶功能"))
                    advancedDoc.Add(v);
                else
                    normalDoc.Add(v);
            }
        }

        private static void LoadUpdateNotes()
        {
            updateNotes.Clear();
            var fullpath = ResourceCheckerHelper.GetResourceCheckerRawPath();
            fullpath = fullpath + documentFolder + updateNoteName;
            var reader = new StreamReader(fullpath, Encoding.ASCII);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                updateNotes.Add(line);
            }
            reader.Close();
        }

        //private static void TestSave()
        //{
        //    var fullpath = ResourceCheckerHelper.GetResourceCheckerRawPath();
        //    fullpath = fullpath + documentFolder + userGuideInfo;
        //    var test = new DocumentStructWrap();
        //    var document = new DocumentStruct();
        //    test.documentStructs = new List<DocumentStruct>();
        //    test.documentStructs.Add(document);
        //    var detail = new DocumentDetail();
        //    document.title = "test";
        //    document.documentDetails.Add(detail);
           
        //    detail.content = "ddd";
        //    detail.texture = "ttt";
        //    var json = JsonUtility.ToJson(test);
        //    ResourceCheckerHelper.WriteFileLine(json, fullpath); 
        //}

        private static string[] toolBarStr = new string[] { "操作文档", "更新日志", "关于工具" };
        private static int currentSelectToolBar = 0;

        public static void DrawDocuments()
        {
            GUILayout.BeginHorizontal();
            var cfg = CheckerConfigManager.commonConfing;
            if (cfg == null)
                return;
            cfg.showDocument = GUILayout.Toggle(cfg.showDocument, "显示说明", GUILayout.Width(70));
            if (cfg.showDocument == false)
            {
                GUILayout.EndHorizontal();
                return;
            }
            currentSelectToolBar = GUILayout.Toolbar(currentSelectToolBar, toolBarStr);
            GUILayout.EndHorizontal();

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            switch (currentSelectToolBar)
            {
                case 0:
                    DrawUserGuide();
                    break;
                case 1:
                    DrawUpdateNotes();
                    break;
                case 2:
                    DrawAboutNotes();
                    break;
            }
            GUILayout.EndScrollView();
        }

        private static void DrawUpdateNotes()
        {
            foreach (var line in updateNotes)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(line);
                GUILayout.EndHorizontal();
            }
        }

        private static DocumentStruct currentShowDocument = null;

        private static void DrawUserGuide()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Height(40));
            GUILayout.EndHorizontal();

            var count = Mathf.Max(normalDoc.Count, advancedDoc.Count);
            for(int i = 0; i < count; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(50);
                var doc1 = DrawDocument(normalDoc, i);
                GUILayout.Space(200);
                var doc2 = DrawDocument(advancedDoc, i);
                GUILayout.EndHorizontal();
                ShowCurrentDocument(doc1, doc2);
            }
        }

        private static DocumentStruct DrawDocument(List<DocumentStruct> doc, int index)
        {
            if (index >= doc.Count)
            {
                GUILayout.Space(400);
                return null;
            }

            var cur = doc[index];
            if (GUILayout.Button(cur.title, GUILayout.Width(400), GUILayout.Height(40)))
            {
                OnDocumentButtonClick(cur);
            }
            return cur;
        }

        private static void OnDocumentButtonClick(DocumentStruct document)
        {
            currentShowDocument = document;
            currentShowDocument.show = !currentShowDocument.show;
            foreach(var doc in documentStructs)
            {
                if (doc == document)
                    continue;
                doc.show = false;
            }
            if (currentShowDocument.show)
            {
                LoadDocumentTexture(document);
            }
        }

        private static void LoadDocumentTexture(DocumentStruct document)
        {
            var pathHead = ResourceCheckerHelper.GetResourceCheckerRootPath(); 
            foreach (var detail in document.documentDetails)
            {
                if (string.IsNullOrEmpty(detail.texture))
                    continue;
                var path = pathHead + documentFolder + documentTextureRoot + detail.texture;
                detail.textureAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
        }

        private static void ShowCurrentDocument(DocumentStruct doc1, DocumentStruct doc2)
        {
            if (currentShowDocument == null || (currentShowDocument != doc1 && currentShowDocument != doc2))
                return;
            if (currentShowDocument.show == true)
            {
                foreach (var detail in currentShowDocument.documentDetails)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("");
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(50);
                    GUILayout.Label(detail.content);
                    GUILayout.EndHorizontal();

                    if (detail.textureAsset != null)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(50);
                        GUILayout.Box(detail.textureAsset);
                        GUILayout.EndHorizontal();
                    }
                }
            }
        }

        private static void DrawAboutNotes()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Height(40));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(200);
            GUILayout.Label("Resource Checker Plus 资源检查及处理工具集 2.0");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(200);
            GUILayout.Label("改进建议或使用过程中的问题欢迎联系: zhoumf1214@163.com or 278000247@qq.com");
            GUILayout.EndHorizontal();
        }
    }

}
