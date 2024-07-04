using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Reflection;

namespace ResourceCheckerPlus
{
    public enum PropertyState
    {
        None,
        Missing,
        Null
    }

    /// <summary>
    /// 工具函数类
    /// </summary>
    public static class ResourceCheckerHelper
    {
        static ResourceCheckerHelper()
        {
            var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
            objectReferenceStringValueProperty = typeof(SerializedProperty).GetProperty("objectReferenceStringValue", bindingFlags);
        }

        static PropertyInfo objectReferenceStringValueProperty = null;

        public static bool isFolder(string path)
        {
            return Directory.Exists(path);
        }

        public static string GetRawAssetpath(string path)
        {
            var asset = "Asset";
            var head = Application.dataPath.TrimEnd(asset.ToCharArray());
            return head + path;
        }

        public static int GetRawAssetSize(string assetPath)
        {
            try
            {
                return (int)new FileInfo(GetRawAssetpath(assetPath)).Length;
            }
            catch(FileNotFoundException e)
            {
                //build in resource or null
                return 0;
            }
        }

        public static bool IsEmptyFolder(string path)
        {
            var rawpath = GetRawAssetpath(path);
            if (!isFolder(rawpath))
                return false;
            return Directory.GetDirectories(rawpath).Length == 0 && Directory.GetFiles(rawpath).Length == 0;
        }

        public static bool HasChineseCharInString(string str)
        {
            return Regex.IsMatch(str, @"[\u4e00-\u9fa5]");
        }

        private static string andSymbol = "&&";
        private static string unSymbol = "!";

        private static bool ComplexFilter(string filter, string str)
        {
            var detailFilter = filter.Split(andSymbol.ToCharArray());
            foreach (string detail in detailFilter)
            {
                if (!CheckFilterInternal(detail, str))
                    return false;
            }
            return true;
        }

        private static bool CheckFilterInternal(string filter, string str)
        {
            bool doesNotContain = filter.StartsWith(unSymbol);
            if (doesNotContain)
            {
                filter = filter.TrimStart(unSymbol.ToCharArray());
                return !str.Contains(filter);
            }
            else
            {
                return str.Contains(filter);
            }
        }

        public static string GetAssetPostfix(string assetPath)
        {
            return assetPath.Contains('.') ? assetPath.Substring(assetPath.LastIndexOf('.')) : "Unknown";
        }

        /// <summary>
        /// 无意中发现了一下，assetdatabase.GetAll查结尾的时候，能查到失效的prefab，而FindAllAssets是找不出来失效的prefab的
        /// </summary>
        public static void PrintAllInvalidPrefab()
        {
            var assetPath = AssetDatabase.GetAllAssetPaths();
            var prefabPath1 = assetPath.Where(x => x.Contains(".prefab")).ToArray();
            var guids = AssetDatabase.FindAssets("t:Prefab");
            var prefabPath2 = guids.Select(x => AssetDatabase.GUIDToAssetPath(x)).ToArray();
            var prefabPath3 = prefabPath1.Where(x => !prefabPath2.Contains(x)).ToArray();
            foreach (var v in prefabPath3)
            {
                UnityEngine.Debug.Log(v);
            }
        }

        private static string GenerateTimeString()
        {
            var dateString = System.DateTime.Now.Year.ToString() + "-" + System.DateTime.Now.Month.ToString() + "-" + System.DateTime.Now.Day.ToString();
            var timeString = System.DateTime.Now.ToLongTimeString().Replace(":", ".").Replace(" ", "");
            return dateString + "-" + timeString;
        }

        public static string GenericExportFolderName()
        {
            var folderString = CheckerConfigManager.commonConfing.checkResultExportPath;
            if (string.IsNullOrEmpty(folderString))
            {
                folderString = CheckerConfigManager.defaultExportResultPath;
            }
            var head = "Assets";
            folderString = folderString.TrimStart(head.ToCharArray());
            var timeString = GenerateTimeString();
            return Application.dataPath + folderString + "/ResourceCheckResult/" + timeString;
        }

        public static void OpenFolder(string folder)
        {
            folder = string.Format("\"{0}\"", folder);
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    Process.Start("Explorer.exe", folder.Replace('/', '\\'));
                    break;
                case RuntimePlatform.OSXEditor:
                    Process.Start("open", folder);
                    break;
                default:
                    throw new System.NotSupportedException(
                        string.Format("Opening folder on '{0}' platform is not supported.", Application.platform.ToString()));
            }
        }

        public static void BeginSelectableLine(bool selected, ResourceWarningLevel warningLevel = ResourceWarningLevel.Normal)
        {
            GUILayout.BeginHorizontal();
            var config = CheckerConfigManager.commonConfing;
            if (warningLevel == ResourceWarningLevel.Warning)
                GUI.color = config.warningItemColor;
            else if (warningLevel == ResourceWarningLevel.FatalError)
                GUI.color = config.errorItemColor;
            if (selected)
                GUI.color = config.selectItemColor;
        }

        public static void EndSelectableLine()
        {
            GUILayout.EndHorizontal();
            GUI.color = CheckerConfigManager.defaultTextColor;
        }

        public static Vector2 GetCurrentPopWindowPos()
        {
            var mousePos = Event.current.mousePosition;
            var destPosx = mousePos.x + ResourceCheckerPlus.instance.position.x + CheckerConfigManager.commonConfing.sideBarWidth;
            var destPosy = mousePos.y + ResourceCheckerPlus.instance.position.y + 100;
            return new Vector2(destPosx, destPosy);
        }

        public static bool IsAllObjectsInFolder(Object folder, Object[] objects)
        {
            if (folder == null || objects == null || objects.Length == 0)
                return false;
            var folderPath = AssetDatabase.GetAssetPath(folder);
            foreach(var o in objects)
            {
                var path = AssetDatabase.GetAssetPath(o);
                if (!path.StartsWith(folderPath))
                    return false;
            }
            return true;
        }

        public static string GetResourceCheckerRawPath()
        {
            var frame = new StackTrace(true).GetFrame(0);
            var configRootPath = frame.GetFileName();
            configRootPath = Path.GetDirectoryName(configRootPath);

            configRootPath = configRootPath.Replace('\\', '/');
            configRootPath = configRootPath.Replace("/CheckerCode/CheckerHelper", "");
            return configRootPath;
        }

        public static string GetResourceCheckerRootPath()
        {
            var path = GetResourceCheckerRawPath();
            path = path.Remove(0, path.IndexOf("/Assets") + 1);
            return path;
        }

        public static string GenerateGUID()
        {
#if UNITY_2017_3_OR_NEWER
            var guid = GUID.Generate().ToString();
#else
            var guid = GenerateTimeString();
#endif
            return guid;
        }

        //有问题，暂时不用
        public static void DeleteNullComponentsOnGameObject(GameObject go)
        {
            var so = new SerializedObject(go);
            var properties = so.FindProperty("m_Component");
            var components = go.GetComponents<Component>();
            var propertyIndex = 0;
            foreach (var com in components)
            {
                if (com == null)
                    properties.DeleteArrayElementAtIndex(propertyIndex);
                ++propertyIndex;
            }
            so.ApplyModifiedProperties();
            var root = GetPrefabRoot(go);
            EditorUtility.SetDirty(root);
            AssetDatabase.SaveAssets();
        }

        public static GameObject GetPrefabRoot(GameObject source)
        {
#if UNITY_2018_3_OR_NEWER
            return source.transform.root.gameObject;
#else
            return PrefabUtility.FindPrefabRoot(source);
#endif
        }

        public static void WriteFileLine(string content, string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            var file = new FileInfo(path);
            var writer = file.CreateText();
            writer.WriteLine(content);
            writer.Flush();
            writer.Dispose();
            writer.Close();

            AssetDatabase.Refresh();
        }

        public static PropertyState GetInvalidPropertyState(Object targetObject, string propertyName)
        {
            var state = PropertyState.None;
            var so = new SerializedObject(targetObject);
            var property = so.FindProperty(propertyName);
            if (property != null && property.propertyType == SerializedPropertyType.ObjectReference)
            {
                if(property.objectReferenceValue == null)
                {
                    state = PropertyState.Null;
                    if (IsMissingProperty(property))
                    {
                        state = PropertyState.Missing;
                    }
                }
            }
            return state;
        }

        public static PropertyState HasMissingProperty(Object targetObject)
        {
            var state = PropertyState.None;
            var so = new SerializedObject(targetObject);
            var it = so.GetIterator();
            while(it.NextVisible(true))
            {
                if (it.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (it.objectReferenceValue == null )
                    {
                        state = PropertyState.Null;
                        if (IsMissingProperty(it))
                        {
                            state = PropertyState.Missing;
                            return state;
                        }
                    }
                }
            }
            return state;
        }

        private static bool IsMissingProperty(SerializedProperty property)
        {
            if (property.objectReferenceInstanceIDValue != 0)
            {
                return true;
            }
            else
            {
                //Unity2018.4.8f1上面的判断已经可以证明该对象missing了，但是在2018.3.6，3.13等版本测试发现IDValue在Missing时是0！！
                //故，通过反射查Missing字段苟一下
                if (objectReferenceStringValueProperty != null)
                {
                    var propertyValue = objectReferenceStringValueProperty.GetValue(property).ToString();
                    if (propertyValue.StartsWith("Missing"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static Color oriColor;

        public static void BeginLine(bool error = false)
        {
            var level = error ? ResourceWarningLevel.FatalError : ResourceWarningLevel.Normal;
            BeginLine(level);
        }

        public static void BeginLine(ResourceWarningLevel resourceWarningLevel = ResourceWarningLevel.Normal)
        {
            oriColor = GUI.color;
            if (resourceWarningLevel == ResourceWarningLevel.FatalError)
                GUI.color = Color.red;
            else if (resourceWarningLevel == ResourceWarningLevel.Warning)
                GUI.color = Color.yellow;
            GUILayout.BeginHorizontal();

        }

        public static void EndLine()
        {
            GUILayout.EndHorizontal();
            GUI.color = oriColor;
        }
    }
}