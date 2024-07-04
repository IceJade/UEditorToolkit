// Resource Checker原始版本，貌似...除了名字（额，的前半部分）...其他都不一样了....
// https://github.com/handcircus/Unity-Resource-Checker
// Resource Checker Plus1.8.0
// author: 引擎部 zhangjian_dev，有改进建议欢迎联系

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace ResourceCheckerPlus
{
    public enum CheckInputMode
    {
        DragMode,
        SelectMode,
        WholeProject,
        //ResourcesFolder,
    }

    public enum BatchOptionSelection
    {
        AllInFilterList,
        CurrentSelect,
    }

    public class ResourceCheckerPlus : SideBarWindow
    {
        private int currentActiveCheckModule = 0;
        public GUIContent[] checkModeListNames = null;
        public List<ResCheckModuleBase> resCheckModeList = new List<ResCheckModuleBase>();
        public CheckerConfigManager configManager = new CheckerConfigManager();
        public static ResourceCheckerPlus instance = null;

        [MenuItem("Window/Resource Checker Plus &R")]
        public static void Init()
        {
            //getwindow(xx, true) 可以使之有边框，并且不会被挡住，不过不能拖到unity主窗体里
            var window = (ResourceCheckerPlus)GetWindow(typeof(ResourceCheckerPlus));
            window.minSize = new Vector2(800, 600);
            instance = window;
            window.titleContent = new GUIContent("ResourceChecker Plus 2.0", "Resource Checker Plus 资源检查及处理工具集2.0版本，改进建议或使用过程中的问题欢迎联系 Terry");
        }

        void OnEnable()
        {
            ObjectChecker.allCheckerDic.Clear();
            instance = this;
            configManager.InitConfig();
            SetSideBarWide(CheckerConfigManager.commonConfing.sideBarWidth); 
            InitCheckerModule();
           
            ResourceTagManager.LoadResourceTag();
            CheckerDocumentHelper.LoadDocuments();
            PostInitChecker();
            SceneView.onSceneGUIDelegate += OnSceneDraw;
        }

        private void OnDisable()
        {
            SceneView.onSceneGUIDelegate -= OnSceneDraw;
        }

        void OnDestroy()
        {
            CheckerDocumentHelper.ReleaseDocuments();
            configManager.SaveCheckerConfig();
            ClearCheckModule();
            configManager.ClearConfig();
            instance = null;
        }

        public ResCheckModuleBase CurrentCheckModule()
        {
            if (currentActiveCheckModule < resCheckModeList.Count)
                return resCheckModeList[currentActiveCheckModule];
            return null;
        }

        public override void SetMessageRect(Rect sideBar, Rect mainWindow)
        {
            var module = CurrentCheckModule();
            if (module != null)
            {
                module.SideBarRect = sideBar;
                module.MainRect = mainWindow;
            }
        }

        public override void ShowLeftSide()
        {
            if (checkModeListNames == null)
                return;
            currentActiveCheckModule = GUILayout.Toolbar(currentActiveCheckModule, checkModeListNames);
            GUILayout.BeginVertical();
            var module = CurrentCheckModule();
            if (module != null)
            {
                module.ShowCurrentSideBar();
            }
            GUILayout.EndVertical();
        }

        public override void ShowRightSide()
        {
            if (checkModeListNames == null)
                return;
            var module = CurrentCheckModule();
            if (module != null)
            {
                module.ShowCurrentCheckDetail();
            }
        }

        public void InitCheckerModule()
        {
            //初始化检查模式
            resCheckModeList.Clear();
            foreach (var v in CheckerConfigManager.checkModuleConfigList)
            {
                ResCheckModuleBase.CreateCheckModule(this, v);
            }
            resCheckModeList.Sort(delegate (ResCheckModuleBase module1, ResCheckModuleBase module2) { return module1.checkModuleCfg.checkModuleOrder - module2.checkModuleCfg.checkModuleOrder; });
            checkModeListNames = resCheckModeList.Select(x => x.checkModeName).ToArray();
        }

        public void ClearCheckModule()
        {
            resCheckModeList.ForEach(x => x.Clear());
            Resources.UnloadUnusedAssets();
        }

        public void SetCurrentActiveCheckModule<T>() where T : ResCheckModuleBase
        {
            foreach(var module in resCheckModeList)
            {
                if (module is T)
                    currentActiveCheckModule = resCheckModeList.IndexOf(module);
            }
        }

        public void CheckResource()
        {
            OnEnable();
            var selectObjects = Selection.objects;
            var checkObjects = ObjectChecker.GetAllObjectFromInput<Object>(selectObjects, "t:Object");
            SetCurrentActiveCheckModule<MixResCheckModule>();
            var currentModule = CurrentCheckModule();
            currentModule.DoCheckResourceOption(checkObjects.ToArray());
        }

        public void PostInitChecker()
        {
            resCheckModeList.ForEach(x => x.PostInitChecker());
        }

        public void RegistCustomCheckObjectDetailRefDelegate<T>(CustomCheckObjectDetailRefDelegate delFunc) where T : ObjectChecker
        {
            resCheckModeList.ForEach(x => x.RegistCustomCheckObjectDetailRefDelegate<T>(delFunc));
        }

        public void OnSceneDraw(SceneView sceneView)
        {
            var checkMode = CurrentCheckModule();
            checkMode.OnSceneDraw(sceneView);
        }
    }
}

