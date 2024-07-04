// -------------------------------------------------------------------------------------------
// @说       明: 脚本处理工具
// @作       者: zhoumingfeng
// @版  本  号: V1.00
// @创建时间: 2021.1.8
// @详细描述: 
//    1.支持脚本替换: 
//       a.有继承关系的脚本替换不会丢失引用;
//       b.强制替换脚本,但是会造成引用丢失,一般用于没有继承关系的脚本;
// @修改记录:
// --------------------------------------------------------------------------------------------
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Reflection;

namespace UToolkits
{
    public class ScriptTool
    {
        //替换string对_fileID和GUID同时替换
        private static Dictionary<string, string> replaceStringPairs = new Dictionary<string, string>();
        private static List<long> oldFileIDs = new List<long>();
        static string oldScriptGUID;
        static long oldScriptFileID;
        static string newScriptGUID;
        static long newScriptFileID;
        private static List<Text> texts = new List<Text>();

        private static Regex rg_Number = new Regex("-?[0-9]+");
        private static Regex rg_FileID = new Regex(@"(?<=m_Script: {fileID:\s)-?[0-9]+");
        private static Regex rg_GUID = new Regex(@"(?<=guid:\s)[a-z0-9]{32,}(?=,)");

        #region 获取所有Text内容

        [MenuItem("Tools/ReplaceScript/获取所有Text内容")]
        static void GetAllTextContent()
        {
            List<string> executePaths = getExecutePaths();
            OnGetAllTextContent(executePaths);
        }

        static void OnGetAllTextContent(List<string> executePaths)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            List<string> allPrefabPaths = getAllPrefabsFromPaths(executePaths);
            int count = 0;
            foreach (string file in allPrefabPaths)
            {
                string path = getAssetPath(file);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Text[] comps_Old = prefab.GetComponentsInChildren<Text>(true);
                foreach (Text com_Old in comps_Old)
                {
                    count++;
                    Stack<string> parentNames = new Stack<string>();
                    string debugString = "控件_" + count + ":";
                    Transform point = com_Old.transform;
                    while (point.parent != null)
                    {
                        parentNames.Push(point.parent.name);
                        point = point.parent;
                    }
                    while (parentNames.Count != 0)
                    {
                        debugString += parentNames.Pop() + " > ";
                    }
                    debugString += "[" + com_Old.name + "] 内容: {" + com_Old.text + "}";
                    Debug.Log(debugString);
                }
            }
        }
        #endregion

        #region 替换Text组件为TextPlus(使用继承关系的脚本,不会丢失引用)

        [MenuItem("Tools/ReplaceScript/替换Text为TextPlus(不会丢失引用)")]
        public static void ReplaceScriptNoMissingReference()
        {
            List<string> executePaths = getExecutePaths();
            OnExecute(executePaths);
        }

        private static List<string> getExecutePaths()
        {
            List<string> executePaths = new List<string>();
            executePaths.Add("Assets/Prefabs/convert");
            return executePaths;

            /*
            Object[] arr = Selection.GetFiltered(typeof(Object), SelectionMode.TopLevel);
            if (arr == null || arr.Length == 0)
            {
                executePaths.Add("Assets/Prefabs/convert");
                return executePaths;
            }
            foreach (Object dir in arr)
            {
                executePaths.Add(AssetDatabase.GetAssetPath(dir));
            }
            return executePaths;
            */
        }

        static void OnExecute(List<string> executePaths)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            texts.Clear();
            oldFileIDs.Clear();
            replaceStringPairs.Clear();

            //获取新脚本GUID
            var newScriptFile = Directory.GetFiles(Application.dataPath + "/Scripts/UGUIPlus/Core/Scripts/", "TextPlus.cs", SearchOption.TopDirectoryOnly);
            newScriptGUID = AssetDatabase.AssetPathToGUID(getAssetPath(newScriptFile[0]));
            Debug.Log("newScriptGUID:" + newScriptGUID);

            //获取新脚本FileID
            string[] newComponentPrefabFile = Directory.GetFiles(Application.dataPath + "/Scripts/Editor/TempReplacePrefabs/", "TextPlusTest.prefab", SearchOption.TopDirectoryOnly);
            GameObject localizationTextPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(getAssetPath(newComponentPrefabFile[0]));
            long newComponentFileID = getFileID(localizationTextPrefab.GetComponent<TextPlus>());
            newScriptFileID = getScriptFileIDbyFileID(newComponentFileID, getAssetPath(newComponentPrefabFile[0]), newScriptGUID);
            Debug.Log("newScriptFileID:" + newScriptFileID);

            //获取老脚本FileID，GUID
            string[] oldComponentPrefabFile = Directory.GetFiles(Application.dataPath + "/Scripts/Editor/TempReplacePrefabs/", "TextTest.prefab", SearchOption.TopDirectoryOnly);
            GameObject textPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(getAssetPath(oldComponentPrefabFile[0]));
            long oldComponentFileID = getFileID(textPrefab.GetComponent<Text>());
            oldScriptGUID = getScriptGUIDbyFilePath(getAssetPath(oldComponentPrefabFile[0]));
            oldScriptFileID = getScriptFileIDbyFileID(oldComponentFileID, getAssetPath(oldComponentPrefabFile[0]), oldScriptGUID);

            Debug.Log("oldScriptFileID:" + oldScriptFileID);
            Debug.Log("oldScriptGUID:" + oldScriptGUID);

            List<string> allPrefabPaths = getAllPrefabsFromPaths(executePaths);
            Debug.Log("begin:replaceTextComponents,prefab num:" + allPrefabPaths.Count);

            foreach (string file in allPrefabPaths)
            {
                texts.Clear();
                oldFileIDs.Clear();
                replaceStringPairs.Clear();

                string path = getAssetPath(file);
                //Debug.Log("Prepare path:"+ path);
                getAllTextComponents(path);
                getReplaceStringPairs(path);
                updatePrefab(path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Replace Complete");
        }
        private static List<string> getAllPrefabsFromPaths(List<string> executePaths)
        {
            List<string> allPrefabPaths = new List<string>();
            foreach (string dir in executePaths)
            {
                string absolute_dir = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')) + "/" + dir;
                if (Directory.Exists(dir))
                {
                    string[] files = Directory.GetFiles(absolute_dir, "*.prefab", SearchOption.AllDirectories);
                    allPrefabPaths.AddRange(files);
                }
                if (Path.GetExtension(absolute_dir).Equals(".prefab"))
                {
                    allPrefabPaths.Add(absolute_dir);
                }
            }
            return allPrefabPaths;
        }
        private static string getScriptGUIDbyFilePath(string prefabPath)
        {
            Regex rg = new Regex(@"(?<=m_Script:\s{fileID:\s-?[0-9]+, guid:\s)[a-z0-9]{32,}(?=,)");
            using (StreamReader sr = new StreamReader(prefabPath))
            {
                int beginLineNumber = 3;
                for (int i = 0; i < beginLineNumber - 1; i++)
                {
                    sr.ReadLine();
                }
                string line;
                while (!string.IsNullOrEmpty(line = sr.ReadLine()))
                {
                    MatchCollection mc_Scripts = rg.Matches(line);
                    if (mc_Scripts.Count != 0)
                    {
                        return mc_Scripts[0].ToString();
                    }
                }
            }
            return "";
        }
        private static long getScriptFileIDbyFileID(long newComponentFileID, string prefabPath, string matchString)
        {
            using (StreamReader sr = new StreamReader(prefabPath))
            {
                int beginLineNumber = 3;
                for (int i = 0; i < beginLineNumber - 1; i++)
                {
                    sr.ReadLine();
                }
                string line;
                while (!string.IsNullOrEmpty(line = sr.ReadLine()))
                {
                    if (line.StartsWith("---"))
                    {
                        MatchCollection mc_ComponentFileID = rg_Number.Matches(line);
                        if (newComponentFileID == long.Parse(mc_ComponentFileID[1].ToString()))
                        {
                            long fileID = 0;
                            string guid = "";
                            while (!string.IsNullOrEmpty(line = sr.ReadLine()))
                            {
                                MatchCollection mc = rg_FileID.Matches(line);
                                MatchCollection mc_guid = rg_GUID.Matches(line);
                                if (mc.Count != 0 && long.Parse(mc[0].ToString()) != 0)
                                {
                                    if (mc_guid.Count != 0 && !string.IsNullOrEmpty(mc_guid[0].ToString()))
                                    {
                                        guid = mc_guid[0].ToString();
                                        if (guid == matchString)
                                        {
                                            fileID = long.Parse(mc[0].ToString());
                                            return fileID;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return 0;
        }
        private static void getAllTextComponents(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Text[] comps_Old = prefab.GetComponentsInChildren<Text>(true);
            foreach (Text com_Old in comps_Old)
            {
                if (com_Old is TextPlus)
                    continue;

                texts.Add(com_Old);
                long old_fileID = getFileID(com_Old);
                if (!oldFileIDs.Contains(old_fileID))
                {
                    oldFileIDs.Add(old_fileID);
                }
            }
        }
        private static void getReplaceStringPairs(string prefabPath)
        {
            using (StreamReader sr = new StreamReader(prefabPath))
            {
                int beginLineNumber = 3;
                for (int i = 0; i < beginLineNumber - 1; i++)
                {
                    sr.ReadLine();
                }

                string line;
                int index = 0;
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    index++;
                    if (line.StartsWith("---"))
                    {
                        MatchCollection mc_ComponentFileID = rg_Number.Matches(line);
                        long thisComID = long.Parse(mc_ComponentFileID[1].ToString());
                        if (oldFileIDs.Contains(thisComID))
                        {
                            long this_FileID = 0;
                            string this_GUID = "";
                            while (!string.IsNullOrEmpty(line = sr.ReadLine()))
                            {
                                index++;

                                if (line.StartsWith("---"))
                                {
                                    mc_ComponentFileID = rg_Number.Matches(line);
                                    thisComID = long.Parse(mc_ComponentFileID[1].ToString());
                                    if (!oldFileIDs.Contains(thisComID))
                                    {
                                        break;
                                    }
                                }

                                MatchCollection mc = rg_FileID.Matches(line);
                                MatchCollection mc_guid = rg_GUID.Matches(line);
                                if (mc.Count != 0 && long.Parse(mc[0].ToString()) != 0)
                                {
                                    if (mc_guid.Count != 0 && !string.IsNullOrEmpty(mc_guid[0].ToString()))
                                    {
                                        this_FileID = long.Parse(mc[0].ToString());
                                        this_GUID = mc_guid[0].ToString();
                                        if (oldScriptGUID == this_GUID || oldScriptFileID == this_FileID)
                                        {
                                            if (this_GUID != "" && this_FileID != 0)
                                            {
                                                string replace_old = "fileID: " + this_FileID + ", guid: " + this_GUID;
                                                string replace_new = "fileID: " + newScriptFileID + ", guid: " + newScriptGUID;
                                                if (!replaceStringPairs.ContainsKey(replace_old))
                                                {
                                                    replaceStringPairs.Add(replace_old, replace_new);
                                                }
                                                break;
                                            }
                                            else
                                            {
                                                Debug.LogError("this_GUID and this_FileID is null.");
                                            }

                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        private static void updatePrefab(string prefabPath)
        {
            string con;
            bool changed = false;
            using (FileStream fs = new FileStream(prefabPath, FileMode.Open, FileAccess.Read))
            {
                using (StreamReader sr = new StreamReader(fs))
                {
                    con = sr.ReadToEnd();

                    foreach (KeyValuePair<string, string> rsp in replaceStringPairs)
                    {
                        con = con.Replace(rsp.Key, rsp.Value);
                        Debug.Log("Find and Replace:" + rsp.Key + "->" + rsp.Value);
                        changed = true;
                    }

                    sr.Close();
                    sr.Dispose();
                }

                fs.Close();
                fs.Dispose();
            }
            if (changed)
            {
                using (StreamWriter sw = new StreamWriter(prefabPath, false))
                {
                    try
                    {
                        //sw.WriteLine(con);
                        sw.Write(con);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError(ex.ToString());
                    }
                    finally
                    {
                        sw.Close();
                        sw.Dispose();
                    }
                }
            }
        }

        private static string getAssetPath(string str)
        {
            var path = str.Replace(@"\", "/");
            path = path.Substring(path.IndexOf("Assets"));
            return path;
        }
        private static PropertyInfo inspectorMode = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
        private static long getFileID(UnityEngine.Object target)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            inspectorMode.SetValue(serializedObject, InspectorMode.Debug, null);
            SerializedProperty localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");
            return localIdProp.longValue;
        }
        #endregion

        #region 替换Text组件为TextPlus(不替换被引用的Text)

        [MenuItem("Tools/ReplaceScript/替换Text为TextPlus(被引用的Text不替换)")]
        public static void ReplaceScriptMissingReference()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/convert/MultiTextTest.prefab", typeof(GameObject)) as GameObject;
            if (null != prefab)
            {
                var newPrefab = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                PrefabUtility.UnpackPrefabInstance(newPrefab, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                Text[] texts = newPrefab.GetComponentsInChildren<Text>();
                if (null != texts)
                {
                    for (int i = 0; i < texts.Length; i++)
                    {
                        GameObject gameObject = texts[i].gameObject;
                        if (null != gameObject)
                        {
                            // 有引用关系的脚本忽略
                            if (IsBeReferenced(newPrefab, texts[i]))
                                continue;

                            GameObject.DestroyImmediate(texts[i]);
                            gameObject.AddComponent<TextPlus>();

                            string genPath = Path.Combine(Application.dataPath, "Prefabs", "gen");
                            if (!Directory.Exists(genPath))
                                Directory.CreateDirectory(genPath);

                            string genFile = Path.Combine(genPath, prefab.name + ".prefab");

                            PrefabUtility.SaveAsPrefabAsset(newPrefab, genFile);
                            PrefabUtility.UnloadPrefabContents(newPrefab);
                            GameObject.DestroyImmediate(newPrefab);
                        }
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// 是否被引用
        /// </summary>
        /// <param name="go"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        private static bool IsBeReferenced(GameObject go, Component component)
        {
            if (null == go)
                return false;

            MonoBehaviour[] scripts = go.GetComponentsInChildren<MonoBehaviour>(true);
            if (null != scripts)
            {
                string tips = string.Empty;
                for (int i = 0; i < scripts.Length; i++)
                {
                    MonoBehaviour mono = scripts[i];
                    if (null == mono)
                        continue;

                    if (IsUnityClass(mono))
                        continue;

                    SerializedObject tempObject = new SerializedObject(mono);
                    SerializedProperty temProperty = tempObject.GetIterator();
                    while (temProperty.NextVisible(true))
                    {
                        if (temProperty.propertyType == SerializedPropertyType.ObjectReference
                            && temProperty.objectReferenceInstanceIDValue == component.GetInstanceID())
                        {
                            return true;
                        }

                        /*
                        if (temProperty.propertyType == SerializedPropertyType.ObjectReference
                            && temProperty.objectReferenceValue == null
                            && temProperty.objectReferenceInstanceIDValue != 0)
                        {
                            tips += mono.GetType().ToString() + "| |" + temProperty.propertyPath + "引用丢失\t\n";
                        }
                        */
                    }
                }
            }

            return false;
        }

        private static bool IsUnityClass(MonoBehaviour mono)
        {
            if (mono is Text)
                return true;

            if (mono is Image)
                return true;

            return false;
        }

        #endregion

        #region 替换Text组件为TextPlus(没有继承关系,可用这个方法)

        [MenuItem("Tools/ReplaceScript/强制替换Text为TextPlus(会丢失引用)")]
        public static void ForceReplaceScript()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/convert/MultiTextTest.prefab", typeof(GameObject)) as GameObject;
            if (null != prefab)
            {
                var newPrefab = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                PrefabUtility.UnpackPrefabInstance(newPrefab, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                Text[] texts = newPrefab.GetComponentsInChildren<Text>();
                if (null != texts)
                {
                    for (int i = 0; i < texts.Length; i++)
                    {
                        GameObject gameObject = texts[i].gameObject;
                        if (null != gameObject)
                        {
                            GameObject.DestroyImmediate(texts[i]);
                            gameObject.AddComponent<TextPlus>();

                            string genPath = Path.Combine(Application.dataPath, "Prefabs", "gen");
                            if (!Directory.Exists(genPath))
                                Directory.CreateDirectory(genPath);

                            string genFile = Path.Combine(genPath, prefab.name + ".prefab");

                            PrefabUtility.SaveAsPrefabAsset(newPrefab, genFile);
                            PrefabUtility.UnloadPrefabContents(newPrefab);
                            GameObject.DestroyImmediate(newPrefab);
                        }
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        #endregion
    }
}