using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace ResourceCheckerPlus
{
    /// <summary>
    /// 场景检查功能
    /// </summary>
    public class SceneResCheckModule : ResCheckModuleBase
    {
        public bool completeRefCheck = false;
        private bool checkSceneObjectReferenceInScene = false;
        private bool ignoreSelectObjectSelfOnSceneRefCheck = true;
        private GameObject resourceCheckerTempObject = null;

        private GUIContent completeRefCheckContent = new GUIContent("场景全面引用资源检查", "默认检查模式只能检查基本类型的引用关系，如Renderer上引用的贴图资源等，检查速度快，引用关系仅包含自身；全面检查可以检查全场景所有类型的资源引用，包括一些脚本上引用的资源，检查速度慢，引用关系会包含父物体（切换该属性需要重新检查）");
        private GUIContent checkSceneObjectReferenceInSceneContent = new GUIContent("反向检查场景选中的节点在场景中被引用的情况", "选中Hierarchy的某个节点，在场景中查找是否有其他节点引用了该节点，如组件中拖入了该对象及对象下的某个组件等");
        private GUIContent ignoreSelectObjectSelfOnSceneRefCheckContent = new GUIContent("忽略选中对象自身及其子节点的引用", "在场景中搜寻引用关系时，不会考虑选中对象自身及其子节点下的资源引用了选中对象的情况");
        private GameObjectChecker gameObjectChecker = null;
     
        public override void SetCheckerConfig()
        {
            ShowRefCheckItem(true, false, !completeRefCheck);
            var cfg = new CheckerCfg("GameObjectChecker");
            gameObjectChecker = ObjectChecker.CreateChecker(this, cfg) as GameObjectChecker;
            gameObjectChecker.SetAllCheckItemVisible(false);

            gameObjectChecker.dontShowDocAndTitle = true;
            gameObjectChecker.refItem.show = true;
            gameObjectChecker.totalRefItem.show = true;
            gameObjectChecker.refItem.type = CheckType.ListShowFirstItem;
            gameObjectChecker.totalRefItem.type = CheckType.ListShowFirstItem;
            checkerList.Remove(gameObjectChecker);
        }

        public override ObjectChecker CurrentActiveChecker()
        {
            if (checkSceneObjectReferenceInScene == true)
                return gameObjectChecker;
            return base.CurrentActiveChecker();
        }

        public override void ShowCommonSideBarContent()
        {
            EditorGUI.BeginChangeCheck();
            if (checkSceneObjectReferenceInScene == false)
            {
                completeRefCheck = GUILayout.Toggle(completeRefCheck, completeRefCheckContent);
            }
            else
            {
                ignoreSelectObjectSelfOnSceneRefCheck = GUILayout.Toggle(ignoreSelectObjectSelfOnSceneRefCheck, ignoreSelectObjectSelfOnSceneRefCheckContent);
            }
            checkSceneObjectReferenceInScene = GUILayout.Toggle(checkSceneObjectReferenceInScene, checkSceneObjectReferenceInSceneContent);
            if (EditorGUI.EndChangeCheck())
            {
                ShowRefCheckItem(true, false, !completeRefCheck);
                showCheckerSelecter = !checkSceneObjectReferenceInScene;
            }
            var tex = checkSceneObjectReferenceInScene ? "反向检查场景中的引用" : "检查全场景资源";
            if (GUILayout.Button(tex))
            {
                if(checkSceneObjectReferenceInScene == true)
                {
                    CheckSelectObjectInSceneReference();
                }
                else
                {
                    DoTotalResCheck();
                }
            }
            if (checkSceneObjectReferenceInScene == false)
            {
                if (GUILayout.Button("检查场景中选中节点下资源"))
                {
                    DoSelectResCheck();
                }
            }
        }

        public void DoTotalResCheck()
        {
            Clear();
            SetupResourceTag(null);
            CheckCurrentSceneTotalRes();
            CheckDontDestroyOnLoadSceneRes();
            PostCheckResource();
            Refresh();
        }

        public void DoSelectResCheck()
        {
            Clear();
            SetupResourceTag(null);
            CheckResource(null);
            PostCheckResource();
            Refresh();
        }

        void CheckCurrentSceneTotalRes()
        {
            PrepareCheck();
            var scene = SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();
            CheckResInternal(rootObjects);
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
       
        public void CheckDontDestroyOnLoadSceneRes()
        {
            var rootObjects = GetDontDestroyOnLoadSceneRes();
            if (rootObjects != null)
            {
                CheckResInternal(rootObjects);
            }
            FinishCheckDontDestoryOnLoadSceneRes();
        }

        //一个比较trick的获取DontDestroyOnLoadScene节点下资源的方式
        //https://gamedev.stackexchange.com/questions/140014/how-can-i-get-all-dontdestroyonload-gameobjects
        public GameObject[] GetDontDestroyOnLoadSceneRes()
        {
            if (!Application.isPlaying)
                return null;
            var tempObjectName = "Resource Checker Plus Temp Object";
            resourceCheckerTempObject = new GameObject(tempObjectName);
            Object.DontDestroyOnLoad(resourceCheckerTempObject);
            var rootObjects = resourceCheckerTempObject.scene.GetRootGameObjects();
            rootObjects = rootObjects.Where(x => x.name != tempObjectName).ToArray();
            return rootObjects;
        }

        public void FinishCheckDontDestoryOnLoadSceneRes()
        {
            if (resourceCheckerTempObject != null)
            {
                Object.DestroyImmediate(resourceCheckerTempObject);
                resourceCheckerTempObject = null;
            }
        }

        public override void CheckResource(Object[] resources)
        {
            GameObject[] rootObjects = null;
            if (resources == null)
                rootObjects = Selection.gameObjects;
            else
                rootObjects = resources.Cast<GameObject>().ToArray();
            CheckResInternal(rootObjects);
        }

        private void CheckResInternal(GameObject[] rootObjects)
        {
            foreach (var go in rootObjects)
            {
                if (go == null)
                    continue;
                activeCheckerList.ForEach(x =>
                {
                    if (completeRefCheck)
                        x.SceneResCheck(go);
                    else
                        x.AddObjectDetailRefWrap(go, false);
                });
            }
        }

        private void CheckSelectObjectInSceneReference()
        {
            gameObjectChecker.Clear();
            var selectObject = Selection.gameObjects;
            foreach(var obj in selectObject)
            {
                var scene = SceneManager.GetActiveScene();
                var rootObjects = scene.GetRootGameObjects();
                CheckReferenceInScene(rootObjects, obj);

                var dontDestoryRes = GetDontDestroyOnLoadSceneRes();
                if (dontDestoryRes != null)
                {
                    CheckReferenceInScene(dontDestoryRes, obj);
                }
                FinishCheckDontDestoryOnLoadSceneRes();
            }

            gameObjectChecker.RefreshCheckResult();
        }

        private void CheckReferenceInScene(GameObject[] allObjects, GameObject rootObject)
        {
            var objects = GetAllSelectCheckObject(rootObject);
            foreach (var obj in allObjects)
            {
                var components = obj.GetComponentsInChildren<Component>(true);
                components = components.Where(x => !(x is Transform) && x != null).ToArray();
                foreach (var com in components)
                {
                    CheckHasReferenceInScene(com, objects, rootObject);
                }
            }
        }

        private List<Object> GetAllSelectCheckObject(GameObject selectObject)
        {
            var list = new List<Object>();
            var components = selectObject.GetComponentsInChildren<Transform>(true);
            //components = components.Where(x => !(x is Transform) && x != null).ToArray();
            var gos = components.Select(x => x.gameObject).Distinct().ToArray();
            list.AddRange(gos);

            foreach (var go in gos)
            {
                var coms = go.GetComponents<Component>();
                list.AddRange(coms);
            }
            return list;
        }

        private void CheckHasReferenceInScene(Component sceneComponent, List<Object> targetObjects, GameObject rootObject)
        {
            if (ignoreSelectObjectSelfOnSceneRefCheck == true && targetObjects.Contains(sceneComponent))
                return;
            var so = new SerializedObject(sceneComponent);
            var it = so.GetIterator();
            while(it.NextVisible(true))
            {
                if (it.propertyType == SerializedPropertyType.ObjectReference)
                {
                    foreach(var obj in targetObjects)
                    {
                        if (it.objectReferenceValue == obj)
                        {
                            var detailRef = obj is GameObject ? (obj as GameObject) : (obj as Component).gameObject;
                            gameObjectChecker.AddObjectDetail(sceneComponent.gameObject, rootObject, detailRef);
                        }
                    }
                }
            }
        }
    }
}
