using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace ResourceCheckerPlus
{
    /// <summary>
    /// 引用资源检查功能
    /// </summary>
    public class ReferenceResCheckModule : ResCheckModuleBase
    {
        private GUIContent checkPrefabDetailRefContent = new GUIContent("检查资源被Prefab下子节点的引用", "开启该选项后，在检查Prefab引用的资源时，会将资源具体被哪些子物体引用了也统计出来（切换该属性需要重新检查）");
        private GUIContent checkSceneDetailRefContent = new GUIContent("检查目录下场景详细引用", "开启该选项后，检查场景时会进入每个场景进行场景检查，可以统计一些信息，但时间会较慢");
        private GUIContent batchCheckSceneInfoContent = new GUIContent("统计场景信息", "统计检查的场景的贴图资源占用，网格顶点数，面数等信息");
        private GUIContent checkRefPrefabDetailReferenceContent = new GUIContent("检查嵌套引用prefab引用的资源", "开启该选项后，检查Prefab时，会检查Prefab引用的Prefab引用的资源");
        private static string[] referenceCheckTypeContent = new string[] { "检查Prefab的引用", "检查Scene的引用", "检查Material的引用", "检查全部引用及资源" };
        public bool checkPrefabDetailRef = false;
        public bool checkSceneDetailRef = false;
        public bool batchCheckSceneInfo = false;
        public bool checkRefPrefabDetailReference = false;
        public ReferenceCheckType currentCheckType = ReferenceCheckType.Prefab;
        private SceneChecker sceneChecker = null;

        public override void SetCheckerConfig()
        {
            ShowRefCheckItem(true, checkPrefabDetailRef, checkPrefabDetailRef);
            var cfg = new CheckerCfg("SceneChecker");
            sceneChecker = ObjectChecker.CreateChecker(this, cfg) as SceneChecker;
            sceneChecker.refItem.show = false;
            sceneChecker.totalRefItem.show = false;
            sceneChecker.activeItem.show = false;
            checkerList.Remove(sceneChecker);
        }

        public override void ShowCommonSideBarContent()
        {
            currentCheckType = (ReferenceCheckType)EditorGUILayout.Popup("引用资源检查类型", (int)currentCheckType, referenceCheckTypeContent);
            if (currentCheckType == ReferenceCheckType.Prefab)
            {
                EditorGUI.BeginChangeCheck();
                checkPrefabDetailRef = GUILayout.Toggle(checkPrefabDetailRef, checkPrefabDetailRefContent);
                if (EditorGUI.EndChangeCheck())
                {
                    ShowRefCheckItem(true, checkPrefabDetailRef, checkPrefabDetailRef);
                }
                if(checkPrefabDetailRef)
                {
                    checkRefPrefabDetailReference = GUILayout.Toggle(checkRefPrefabDetailReference, checkRefPrefabDetailReferenceContent);
                }
            }
            else if (currentCheckType == ReferenceCheckType.Scene)
            {
                batchCheckSceneInfo = GUILayout.Toggle(batchCheckSceneInfo, batchCheckSceneInfoContent);
                showCheckerSelecter = !batchCheckSceneInfo;
                if (!batchCheckSceneInfo)
                {
                    checkSceneDetailRef = GUILayout.Toggle(checkSceneDetailRef, checkSceneDetailRefContent);
                }
            }
            base.ShowCommonSideBarContent();
        }

        public override void CheckResource(Object[] resources)
        {
            switch(currentCheckType)
            {
                case ReferenceCheckType.Prefab:
                    CheckPrefabReference(resources);
                    break;
                case ReferenceCheckType.Scene:
                    CheckSceneReference(resources);
                    break;
                case ReferenceCheckType.Material:
                    CheckMaterialReference(resources);
                    break;
                case ReferenceCheckType.MixCheck:
                    CheckMixReference(resources);
                    break;
            }
        }

        public void CheckPrefabReference(Object[] resources)
        {
            var selection = GetAllObjectInSelection(resources);
            activeCheckerList.ForEach(x => x.ReferenceResCheck(selection, "t:Prefab", checkPrefabDetailRef, checkRefPrefabDetailReference));
        }

        public void CheckSceneReference(Object[] resources)
        {
            var selection = GetAllObjectInSelection(resources);
            if (batchCheckSceneInfo)
            {
                BatchCheckSceneInfo(resources);
            }
            else
            {
                if (checkSceneDetailRef)
                    CheckSceneDetailRef(resources);
                else
                    activeCheckerList.ForEach(x => x.ReferenceResCheck(selection, "t:Scene", checkPrefabDetailRef, checkRefPrefabDetailReference));
            }
        }

        public void CheckMaterialReference(Object[] resources)
        {
            var selection = GetAllObjectInSelection(resources);
            activeCheckerList.ForEach(x => x.ReferenceResCheck(selection, "t:Material", checkPrefabDetailRef, checkRefPrefabDetailReference));
        }

        public void CheckMixReference(Object[] resources)
        {
            var selection = GetAllObjectInSelection(resources);
            var checkObjects = ObjectChecker.GetAllObjectFromInput<Object>(selection, "t:Object");
            foreach (var v in checkObjects)
            {
                activeCheckerList.ForEach(x => x.MixCheckDirectAndRefRes(v, checkPrefabDetailRef));
            }
        }

        //不用GetDependency的方式，而是打开每个场景进行遍历
        public void CheckSceneDetailRef(Object[] resources)
        {
            var selection = GetAllObjectInSelection(resources);
            var checkObjects = ObjectChecker.GetAllObjectFromInput<Object>(selection, "t:Scene");
            for (int i = 0; i < checkObjects.Count; i++)
            {
                var scene = checkObjects[i];
                if (EditorUtility.DisplayCancelableProgressBar("正在检查" + scene.name + "场景资源", "已完成：" + i + "/" + checkObjects.Count, (float)i / checkObjects.Count))
                    break;
                var path = AssetDatabase.GetAssetPath(scene);
                EditorSceneManager.OpenScene(path);
                CheckCurrentSceneTotalRes(scene);
            }
            EditorUtility.ClearProgressBar();
        }

        public override ObjectChecker CurrentActiveChecker()
        {
            if (batchCheckSceneInfo == true)
                return sceneChecker;
            return base.CurrentActiveChecker();
        }

        public void BatchCheckSceneInfo(Object[] resources)
        {
            sceneChecker.Clear();
            SetCheckerEnable<TextureChecker>(true);
            SetCheckerEnable<MeshChecker>(true);
            SetCheckerEnable<ComponentChecker>(true);
            var selection = GetAllObjectInSelection(resources);
            var checkObjects = ObjectChecker.GetAllObjectFromInput<Object>(selection, "t:Scene");
            for (int i = 0; i < checkObjects.Count; i++)
            {
                var scene = checkObjects[i];
                if (EditorUtility.DisplayCancelableProgressBar("正在检查" + scene.name + "场景资源", "已完成：" + i + "/" + checkObjects.Count, (float)i / checkObjects.Count))
                    break;
                var path = AssetDatabase.GetAssetPath(scene);
                EditorSceneManager.OpenScene(path);
                Clear();
                CheckCurrentSceneTotalRes(scene);
                Refresh();
                sceneChecker.AddObjectDetail(scene, null, null);
            }
            EditorUtility.ClearProgressBar();
            Clear();
            Refresh();
            sceneChecker.RefreshCheckResult();
        }

        private Object currentCheckScene = null;

        public Object GetCurrentCheckScene()
        {
            return currentCheckScene;
        }

        void CheckCurrentSceneTotalRes(Object rootScene)
        {
            PrepareCheck();
            currentCheckScene = rootScene;
            Scene scene = SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();
            foreach (var go in rootObjects)
            {
                if (go == null)
                    continue;
                activeCheckerList.ForEach(x =>
                {
                    x.AddObjectDetailRefWrap(go, false);
                });
            }
            //加入天空盒的资源
            var skyMat = RenderSettings.skybox;
            var skyTex = EditorUtility.CollectDependencies(new Object[] { skyMat });
            //场景LightMap资源
            var lightMapData = LightmapSettings.lightmaps;
            activeCheckerList.ForEach(x =>
            {
                foreach (var obj in skyTex)
                {
                    x.AddObjectDetail(obj, null, null);
                }
                if (lightMapData != null)
                {
                    foreach (var data in lightMapData)
                    {
#if UNITY_2017_3_OR_NEWER
                        if (data.shadowMask != null)
                            x.AddObjectDetail(data.shadowMask, null, null);
                        if (data.lightmapColor != null)
                            x.AddObjectDetail(data.lightmapColor, null, null);
                        if (data.lightmapDir != null)
                            x.AddObjectDetail(data.lightmapDir, null, null);
#elif UNITY_5_5_OR_NEWER
                        if (data.lightmapLight != null)
                            x.AddObjectDetail(data.lightmapLight, null, null);
                        if (data.lightmapDir != null)
                            x.AddObjectDetail(data.lightmapDir, null, null);
#else
                        if (data.lightmapFar != null)
                            x.AddObjectDetail(data.lightmapFar, null, null);
                        if (data.lightmapNear != null)
                            x.AddObjectDetail(data.lightmapNear, null, null);
#endif
                    }
                }
            });
        }
    }
}
