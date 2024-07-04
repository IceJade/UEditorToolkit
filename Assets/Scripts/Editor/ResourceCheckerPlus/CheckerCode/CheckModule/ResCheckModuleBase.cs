/********************************************************************
	created:	16:9:2017   13:54
	filename: 	ResCheckModuleBase.cs
	author:		zhangjian_dev
	
	purpose:	检查模块基类，每个检查模块下包含若干检查器，通过配置文件反射进行初始化
                
*********************************************************************/
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

namespace ResourceCheckerPlus
{
    /// <summary>
    /// 检查模块基类
    /// </summary>
    public class ResCheckModuleBase
    {
        public enum ReferenceCheckType
        {
            Prefab,
            Scene,
            Material,
            MixCheck
        }

        public GUIContent checkModeName = null;
        //用于检查某个文件夹下所有的功能
        public Object objInputSlot = null;
        public List<ObjectChecker> checkerList = new List<ObjectChecker>();
        public List<ObjectChecker> activeCheckerList = null;
        public string currentModuleTip = "";
        public SelfObjectChecker sideBarObjList = new SelfObjectChecker();
        public int[] activeCheckerConfig = null;
        public Rect SideBarRect;
        public Rect MainRect;
        public CheckModuleConfig checkModuleCfg = null;
        public List<string> checkRecord = new List<string>();
        public bool showCheckerSelecter = true;
        public int currentActiveChecker = 0;
        public string[] checkerListNames = null;

        public void CreateChecker()
        {
            foreach (var v in checkModuleCfg.checkerCfgs)
            {
                ObjectChecker.CreateChecker(this, v);
            }
        }

        public void ShowRefCheckItem(bool refObj, bool detailRef, bool activeInRef)
        {
            foreach (var v in checkerList)
            {
                v.refItem.show = refObj;
                v.totalRefItem.show = detailRef;
                v.activeItem.show = activeInRef;
            }
            sideBarObjList.ShowDetailRefRootButton(detailRef);
        }

        public virtual ObjectChecker CurrentActiveChecker()
        {
            if (activeCheckerList != null && currentActiveChecker < activeCheckerList.Count)
            {
                return activeCheckerList[currentActiveChecker];
            }
            return null;
        }

        public void SetCurrentActiveChecker(ObjectChecker checker)
        {
            if (!activeCheckerList.Contains(checker))
                return;
            currentActiveChecker = activeCheckerList.IndexOf(checker);
        }

        public virtual void ShowCurrentCheckDetail()
        {
            var checker = CurrentActiveChecker();
            if (checker != null)
            {
                var showChecker = checker.subChecker != null ? checker.subChecker : checker;
                showChecker.ShowCheckerTitle();
            }
            else
            {
                GUILayout.Label("当前无可用检查类别，请从右侧下拉列表中选择需要检查的资源类型");
            }
            if (showCheckerSelecter == true)
            {
                ShowCheckerSelecter();
            }
            if (checker != null)
            {
                var showChecker = checker.subChecker != null ? checker.subChecker : checker;
                showChecker.ShowCheckResult();
                if (Event.current.button == 1 && MainRect.Contains(Event.current.mousePosition))
                {
                    showChecker.OnContexMenu();
                }
            }
        }

        public void SetCheckerEnable<T>(bool enable) where T : ObjectChecker
        {
            ObjectChecker checker = GetChecker<T>();
            if (checker != null)
            {
                checker.checkerEnabled = enable;
                RefreshCheckerConfig(checker);
            }
        }

        public T GetChecker<T>() where T : ObjectChecker
        {
            foreach (var v in checkerList)
            {
                if (v is T)
                    return v as T;
            }
            return null;
        }

        public void ShowCheckerSelecter()
        {
            if (checkerListNames == null || activeCheckerList == null)
                return;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("检查类型", GUILayout.Width(70)))
            {
                GenericMenu menu = new GenericMenu();
                foreach (var c in checkerList.Where(x => !x.isSpecialChecker))
                {
                    //这个地方不能用delegate，Unity5.5版本没有问题，但在Unity5.3版本测试时下拉菜单传入的checker一直是for循环的最后一个
                    menu.AddItem(new GUIContent(c.checkerName), c.checkerEnabled, new GenericMenu.MenuFunction2(OnCheckerItemSelected), c);
                }
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("全部开启"), false, new GenericMenu.MenuFunction2(SetAllCheckerEnable), true);
                menu.AddItem(new GUIContent("全部关闭"), false, new GenericMenu.MenuFunction2(SetAllCheckerEnable), false);
                menu.AddSeparator(string.Empty);
                //special的不受全部开关控制
                foreach (var c in checkerList.Where(x => x.isSpecialChecker))
                {
                    menu.AddItem(new GUIContent(c.checkerName), c.checkerEnabled, new GenericMenu.MenuFunction2(OnCheckerItemSelected), c);
                }
                menu.ShowAsContext();
            }
            currentActiveChecker = GUILayout.Toolbar(currentActiveChecker, checkerListNames, GUILayout.Width(MainRect.width - 80));
            GUILayout.EndHorizontal();
        }

        private void OnCheckerItemSelected(object obj)
        {
            var checker = obj as ObjectChecker;
            if (checker == null)
                return;
            checker.checkerEnabled = !checker.checkerEnabled;
            RefreshCheckerConfig(checker);
        }

        public void SetAllCheckerEnable(object enable)
        {
            var enabled = (bool)enable;
            foreach (var v in checkerList.Where(x => !enabled || !x.isSpecialChecker))
            {
                v.checkerEnabled = enabled;
                RefreshCheckerConfig(v);
            }
        }

        public virtual void ShowCurrentSideBar()
        {
            ShowCommonSideBarContent();
            if (GUILayout.Button("其他功能"))
            {
                ShowOptionList();
            }
            ShowResourceAutoCheckResult();
            sideBarObjList.ShowCheckResult();
        }

        private void ShowResourceAutoCheckResult()
        {
            var config = CheckerConfigManager.commonConfing;
            if (!config.enableAutoResourceCheck)
                return;
            var warningList = activeCheckerList.Where(x => x.IsWarningChecker()).ToList();
            if (warningList.Count == 0)
                return;
            GUILayout.Label("不符合资源规范的资源种类：", GUILayout.Width(config.sideBarWidth));
            foreach(var checker in warningList)
            {
                GUI.color = config.errorItemColor;
                if (GUILayout.Button(checker.checkerName, GUILayout.Width(config.sideBarWidth)))
                {
                    SetCurrentActiveChecker(checker);
                }
                GUI.color = CheckerConfigManager.defaultTextColor;
            }
        }

        private void ShowOptionList()
        {
            var genericMenu = new GenericMenu();
            genericMenu.AddItem(new GUIContent("导出全部激活列表内容"), false, new GenericMenu.MenuFunction(ExportAllActiveCheckerResult));
            genericMenu.ShowAsContext();
        }

        public virtual void ShowCommonSideBarContent()
        {
            var cfg = CheckerConfigManager.commonConfing;
            if (cfg.inputType == CheckInputMode.DragMode)
            {
                ShowObjectDragSlot();
            }
            if (GUILayout.Button("检查资源"))
            {
                DoCheckResourceOption(null);
            }
        }

        public void InitCheckModule(CheckModuleConfig cfg)
        {
            checkerList.Clear();
            sideBarObjList.Clear();
            checkModuleCfg = cfg;
            if (cfg == null)
                return;
            CreateChecker();
            SetCheckerConfig();
            LoadCheckRecord();
            RefreshCheckerConfig();
        }

        public virtual void SetCheckerConfig() { }

        public void RefreshCheckerConfig(ObjectChecker checker = null)
        {
            if (checkerList != null)
            {
                activeCheckerList = checkerList.Where(x => x.checkerEnabled).ToList();
                checkerListNames = activeCheckerList.Select(x => x.checkerName).ToArray();
                if (checker != null && checker.checkerEnabled)
                {
                    currentActiveChecker = activeCheckerList.IndexOf(checker);
                    checker.Clear();
                }
                else
                {
                    currentActiveChecker = 0;
                }
            }
        }

        public virtual void Clear(bool releaseMemory = false)
        {
            //全清
            checkerList.ForEach(x => x.Clear());
            ClearSideBarList();
            ClearResourceTag();
            if (releaseMemory == true)
            {
                ReleaseMemory();
            }
        }

        /// <summary>
        /// 内存释放，防止连续过多次不同的资源检查造成内存累积，导致编辑器崩溃
        /// </summary>
        public void ReleaseMemory()
        {
#if UNITY_5_6_OR_NEWER
            var currentMemory = Profiler.GetTotalUnusedReservedMemoryLong() + Profiler.GetTotalAllocatedMemoryLong();
#else
            var currentMemory = Profiler.GetTotalUnusedReservedMemory() + Profiler.GetTotalAllocatedMemory();
#endif
            var maxMemory = (long)(CheckerConfigManager.commonConfing.maxMemoryCache * 1024 * 1024 * 1024);
            if (currentMemory > maxMemory)
            {
                Debug.Log("Resource Checker Plus：Release Memory，Current Memory : " + EditorUtility.FormatBytes(currentMemory));
                Resources.UnloadUnusedAssets();
            }
        }

        private void TestDebug(string name, int bytes)
        {
            Debug.Log(name + " " + EditorUtility.FormatBytes(bytes) + " " + bytes);
        }

        public virtual void DoCheckResourceOption(Object[] objects)
        {
            Clear(true);
            CheckResource(objects);
            PostCheckResource();
            Refresh();
        }

        public virtual void Refresh()
        {
            checkerList.ForEach(x => x.RefreshCheckResult());
        }

        public virtual void OnSceneDraw(SceneView sceneView)
        {
            var currentChecker = CurrentActiveChecker();
            if (currentChecker != null)
                currentChecker.OnSceneDraw(sceneView);
        }

        public void PrepareCheck()
        {
            activeCheckerList.ForEach(x => x.PrepareCheck());
        }

        public virtual void CheckResource(Object[] resources) { }

        public Object[] GetAllObjectInSelection(Object[] resources)
        {
            Object[] objects = null;
            if (resources != null)
                objects = resources;
            else if (CheckerConfigManager.commonConfing.inputType == CheckInputMode.SelectMode)
                objects = Selection.objects;
            else if (CheckerConfigManager.commonConfing.inputType == CheckInputMode.WholeProject)
                objects = new Object[] { AssetDatabase.LoadAssetAtPath<Object>("Assets") };
            else
                objects = new Object[] { objInputSlot };
            SetupResourceTag(objects);
            return objects;
        }

        protected void ShowObjectDragSlot()
        {
            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            objInputSlot = EditorGUILayout.ObjectField(objInputSlot, typeof(UnityEngine.Object), true);
            if (EditorGUI.EndChangeCheck())
            {
                RecordCheckPath(objInputSlot);
            }
            ShowCheckRecord();
            GUILayout.EndHorizontal();
        }

        protected void ShowCheckRecord()
        {
            if (checkRecord.Count == 0)
                return;
            if (GUILayout.Button("常用路径"))
            {
                var genericMenu = new GenericMenu();
                for (int i = checkRecord.Count - 1; i >= 0; i--)
                {
                    var v = checkRecord[i];
                    if (v == null)
                        continue;
                    string path = v.Replace('/', '.');
                    genericMenu.AddItem(new GUIContent(path), false, new GenericMenu.MenuFunction2(this.OnCheckRecordSelect), v);
                }
                genericMenu.ShowAsContext();
            }
        }

        private void OnCheckRecordSelect(object o)
        {
            var path = o as string;
            objInputSlot = AssetDatabase.LoadAssetAtPath<Object>(path);
            RecordCheckPath(objInputSlot);
        }

        private void RecordCheckPath(Object obj)
        {
            if (obj == null)
                return;
            var path = AssetDatabase.GetAssetPath(obj);
            if (checkRecord.Contains(path))
            {
                checkRecord.Remove(path);
            }
            checkRecord.Add(path);
            while (checkRecord.Count > CheckerConfigManager.commonConfing.maxCheckRecordCount)
            {
                checkRecord.RemoveAt(0);
            }
            checkModuleCfg.checkRecord = checkRecord.ToArray();
        }

        private void LoadCheckRecord()
        {
            checkRecord.Clear();
            checkRecord.AddRange(checkModuleCfg.checkRecord);
        }

        public void AddObjectToSideBarList(List<Object> objects, bool clear = true)
        {
            sideBarObjList.AddObjectToSelfObjectChecker(objects, clear);
        }

        public void ClearSideBarList()
        {
            sideBarObjList.ClearSelfObjectList();
        }

        public void SelectAll()
        {
            sideBarObjList.SelectAll();
        }

        public void OnSideBarMouseMenu()
        {
            sideBarObjList.OnContexMenu();
        }

        public void ExportAllActiveCheckerResult()
        {
            var path = ResourceCheckerHelper.GenericExportFolderName();
            Directory.CreateDirectory(path);
            activeCheckerList.ForEach(x => x.ExportCheckResult(path));
            AssetDatabase.Refresh();
        }

        public static void CreateCheckModule(ResourceCheckerPlus root, CheckModuleConfig cfg)
        {
            var type = ResourceCheckerAssemblyHelper.GetResourceCheckerType(cfg.CheckModuleClassName);
            if (type == null)
                return;
            var checkModule = System.Activator.CreateInstance(type) as ResCheckModuleBase;
            checkModule.InitCheckModule(cfg);
            checkModule.checkModeName = new GUIContent(cfg.CheckModuleTitleName, cfg.CheckModuleDescription);
            root.resCheckModeList.Add(checkModule);
        }

        public void RegistCustomCheckObjectDetailRefDelegate<T>(CustomCheckObjectDetailRefDelegate delFunc) where T : ObjectChecker
        {
            var checker = GetChecker<T>();
            if (checker == null)
                return;
            checker.RegistCustomCheckObjectDetailRefDelegate(delFunc);
        }

        public void PostInitChecker()
        {
            checkerList.ForEach(x => x.PostInitChecker());
        }

        public void PostCheckResource()
        {
            activeCheckerList.ForEach(x => x.PostCheckResource());
        }

        public void SetupResourceTag(Object[] objects)
        {
            //暂时不支持同时选择多个属于不同tag的资源进行检查，可能比较慢
            var tags = ResourceTagManager.GenerateResourceTags(objects);
            activeCheckerList.ForEach(x => x.SetResourceTag(tags));
        }

        public void ClearResourceTag()
        {
            activeCheckerList.ForEach(x => x.ClearResourceTag());
        }
    }
}
