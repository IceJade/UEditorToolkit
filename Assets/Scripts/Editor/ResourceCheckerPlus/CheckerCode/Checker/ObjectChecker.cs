/********************************************************************
	created:	16:9:2017   13:57
	filename: 	ObjectChecker.cs
	author:		zhangjian_dev(puppet_master)
	
	purpose:	检查器基类，负责基本输入，界面显示，选择，排序，筛选，导出等逻辑，并包含一些基础属性的检查
                每种资源（脚本）继承该类，添加具体检查属性字段，以及获取属性的方法即可
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
    public enum ObjectDetailFlag
    {
        None = 1,
        Warning = 2,
    }

    [System.Serializable]
    public class CheckerCfg
    {
        public string checkerName;
        public bool checkerEnabled;

        public CheckerCfg(string name, bool enabled = false)
        {
            checkerName = name;
            checkerEnabled = enabled;
        }
    }

    public class ObjectDetail
    {
        public Object checkObject;
        public string assetName;
        public string assetPath;
        public string assetGUID;
        public List<Object> referenceObjectList = new List<Object>();
        public List<Object> detailReferenceList = new List<Object>();
        public Dictionary<string, CheckValueItem> checkMap = new Dictionary<string, CheckValueItem>();
        public bool isSelected = false;
        public ObjectChecker currentChecker = null;
        public bool refObjectEnabled = true;
        public ObjectDetailFlag flag = ObjectDetailFlag.None;

        public ResourceWarningLevel warningLevel = ResourceWarningLevel.Normal;
        public string resourceWarningTips = "";
        public object specialCheckObject = null;

        public static string buildInType = "BuildIn";

        public ObjectDetail(Object obj, ObjectChecker checker)
        {
            assetPath = obj == null ? "" : AssetDatabase.GetAssetPath(obj);
            assetGUID = obj == null ? "" : AssetDatabase.AssetPathToGUID(assetPath);
            checker.CheckList.Add(this);
            currentChecker = checker;
            InitCheckObject(obj);
        }

        public void InitCheckObject(Object obj)
        {
            var checker = currentChecker;
            checkMap.Clear();
            checkObject = obj;
            assetName = obj == null ? "Null" : obj.name;
            assetPath = obj == null ? "" : AssetDatabase.GetAssetPath(obj);
            var preview = AssetPreview.GetMiniThumbnail(obj);
            AddOrSetCheckValue(checker.nameItem, assetName);
            AddOrSetCheckValue(checker.refItem, referenceObjectList);
            AddOrSetCheckValue(checker.totalRefItem, detailReferenceList);
            AddOrSetCheckValue(checker.pathItem, assetPath);
            AddOrSetCheckValue(checker.previewItem, preview);
            AddOrSetCheckValue(checker.activeItem, refObjectEnabled.ToString());
            AddOrSetCheckValue(checker.chineseCharItem, ResourceCheckerHelper.HasChineseCharInString(assetName).ToString());
            AddOrSetCheckValue(checker.spaceCharItem, assetName.Contains(" ").ToString());
            //Component调用该API会导致编辑器崩溃，Unity2018.3.6f1版本
#if UNITY_5_6_OR_NEWER
            var size = obj is Component || obj == null ? 0 : (int)Profiler.GetRuntimeMemorySizeLong(obj);
#else
            var size = obj is Component || obj == null ? 0 : (int)Profiler.GetRuntimeMemorySize(obj);
#endif
            AddOrSetCheckValue(checker.memorySizeItem, size);
            //var fileInfo = new FileInfo(assetPath);
            AddOrSetCheckValue(checker.oriFileSizeItem, ResourceCheckerHelper.GetRawAssetSize(assetPath));
            foreach (var v in checker.customCheckItems)
            {
                AddOrSetCheckValue(v, v.GetCheckValue(obj));
            }
            InitDetailCheckObject(obj);
        }

        public void AddObjectReference(Object refObj, Object detailRefObj)
        {
            AddRootObjectReference(refObj);
            AddDetailObjectReference(detailRefObj);
        }

        public void AddRootObjectReference(Object refObj)
        {
            if (refObj != null && !referenceObjectList.Contains(refObj))
            {
                referenceObjectList.Add(refObj);
            }
            CheckIsRefObjectActive(refObj);
        }

        public void AddDetailObjectReference(Object detailRefObj)
        {
            if (detailRefObj != null && !detailReferenceList.Contains(detailRefObj))
            {
                detailReferenceList.Add(detailRefObj);
            }
            CheckIsRefObjectActive(detailRefObj);
        }

        public bool CheckIsRefObjectActive(Object refObj)
        {
            GameObject go = null;
            var result = true;
            if (refObj is GameObject)
                go = refObj as GameObject;
            else if (refObj is Component)
                go = (refObj as Component).gameObject;
            if (go != null)
            {
                result = go.activeInHierarchy;
                refObjectEnabled &= result;
                AddOrSetCheckValue(currentChecker.activeItem, refObjectEnabled.ToString());
            }
            return result;
        }

        public void AddOrSetCheckValue(CheckItem key, object value)
        {
            CheckValueItem item = null;
            checkMap.TryGetValue(key.title, out item);
            if (item == null)
            {
                item = new CheckValueItem(key, this);
                checkMap.Add(key.title, item);
            }
            item.ValueItem = value;
        }

        public object GetCheckValue(CheckItem key)
        {
            var item = GetCheckValueItem(key);
            if (item == null)
            {
                return null;
            }
            return item.ValueItem;
        }

        public CheckValueItem GetCheckValueItem(CheckItem key)
        {
            CheckValueItem item = null;
            checkMap.TryGetValue(key.title, out item);
            return item;
        }

        public void ReloadCheckObject()
        {
            var path = AssetDatabase.GUIDToAssetPath(assetGUID);
            if (!path.Contains("unity_builtin_extra") && !path.Contains("unity default resources"))
            {
                InitCheckObject(AssetDatabase.LoadAssetAtPath<Object>(path));
            }
        }

        public virtual void InitDetailCheckObject(Object obj) { }

        public void ShowCheckItem()
        {
            if (isSelected)
                GUI.color = CheckerConfigManager.commonConfing.selectItemColor;
            //Temp
            else if (warningLevel == ResourceWarningLevel.Warning)
                GUI.color = CheckerConfigManager.commonConfing.warningItemColor;
            else if (warningLevel == ResourceWarningLevel.FatalError)
                GUI.color = CheckerConfigManager.commonConfing.errorItemColor;

            foreach (var checkItem in currentChecker.checkItemList)
            {
                if (checkItem.show == false)
                    continue;
                CheckValueItem valueItem = null;
                checkMap.TryGetValue(checkItem.title, out valueItem);
                if (valueItem == null) 
                    continue;
                valueItem.ShowCheckValue();
            }

            GUI.color = CheckerConfigManager.defaultTextColor;
        }

        public void SetResourceCheckResult(ResourceRuleCheckResult result)
        {
            if (warningLevel < result.warningLevel)
            {
                warningLevel = result.warningLevel;
            }
            //在Name Item上显示所有warning内容
            var item = GetCheckValueItem(currentChecker.nameItem);
            if (item != null)
            {
                item.SetCheckResult(result);
            }
        }
    }

    public delegate void CustomCheckObjectDetailRefDelegate(GameObject go, ObjectChecker checker);

    /// <summary>
    /// 检查基类
    /// </summary>
    public abstract class ObjectChecker
    {
        protected List<CustomCheckObjectDetailRefDelegate> customCheckObjectDetailRefDelegateList = new List<CustomCheckObjectDetailRefDelegate>();

        public static Dictionary<string, ObjectChecker> allCheckerDic = new Dictionary<string, ObjectChecker>();
        public List<CheckItem> checkItemList = new List<CheckItem>();
        public List<CheckItem> customCheckItems = new List<CheckItem>();
        public Dictionary<CheckItem, object> checkResultDic = new Dictionary<CheckItem, object>();
        public List<ObjectDetail> CheckList = new List<ObjectDetail>();
        public List<ObjectDetail> FilterList = new List<ObjectDetail>();
        public List<ObjectDetail> SelectList = new List<ObjectDetail>();
        public ListFilter filterItem = null;
        public ResCheckModuleBase checkModule = null;
        public CheckerCfg config = null;
        public Vector2 viewListScrollPos = Vector2.zero;
        public bool ctrlPressed = false;
        public bool shiftPressed = false;
        public int checkItemHeight = 42;
        public string checkerName = "Object";
        public string checkerFilter = "t:Object";
        public string postfix = "";
        public static string platformIOS = "iPhone";
        public static string platformAndroid = "Android";
        public static string platformStandalone = "Standalone";
        public bool enableReloadCheckItem = false;
        protected bool isReloadCheckItem = false;
        public bool isSpecialChecker = false;
        public EditorWindow currentWindow = null;

        private int firstVisible = 0;
        private int lastVisible = 0;
        private bool _checkerEnabled = false;
        public bool checkerEnabled
        {
            get { return _checkerEnabled; }
            set
            {
                _checkerEnabled = value;
                if (config != null)
                    config.checkerEnabled = value;
            }
        }

        public bool onlyShowWarningItem = false;
        public bool dontShowDocAndTitle = false;

        public CheckItem previewItem;
        public CheckItem nameItem;
        public CheckItem refItem;
        public CheckItem totalRefItem;
        public CheckItem pathItem;
        public CheckItem activeItem;
        public CheckItem memorySizeItem;
        public CheckItem oriFileSizeItem;

        public CheckItem chineseCharItem;
        public CheckItem spaceCharItem;

        public CheckItem totalCount;
        public CheckItem selectCount;
        public CheckItem warningCount;

        public ObjectChecker subChecker = null;

        public static string[] checkDragTypeStr = new string[] { "拖入检查，将Project中目录拖入槽内进行检查", "选中检查,选中Project面板内资源进行检查，支持多选", "对全工程内进行检查（工程大会很慢，建议前两种模式针对特定目录分析）" };

        public ObjectChecker()
        {
            previewItem = new CheckItem(this, "预览", CheckType.Texture);
            nameItem = new CheckItem(this, "名称", CheckType.String, OnNameButtonClick);
            refItem = new CheckItem(this, "引用", CheckType.List, OnRefButtonClick);
            totalRefItem = new CheckItem(this, "具体引用", CheckType.List, OnDetailRefButton);
            memorySizeItem = new CheckItem(this, "内存占用", CheckType.FormatSize);
            oriFileSizeItem = new CheckItem(this, "文件大小", CheckType.FormatSize);
            activeItem = new CheckItem(this, "引用对象全部激活");
            pathItem = new CheckItem(this, "路径");

            chineseCharItem = new CheckItem(this, "是否包含中文");
            spaceCharItem = new CheckItem(this, "是否包含空格");

            totalCount = new CheckItem(this, "数量", CheckType.Int, null, null, ItemFlag.CheckSummary);
            selectCount = new CheckItem(this, "选中数", CheckType.Int, null, null, ItemFlag.CheckSummary);
            warningCount = new CheckItem(this, "资源警告数量", CheckType.Int, null, null, ItemFlag.CheckSummary);

            nameItem.defaultWidth = 200;
            previewItem.order = -99;
            spaceCharItem.order = 96;
            chineseCharItem.order = 97;
            activeItem.order = 98;
            pathItem.order = 99;

            spaceCharItem.show = false;
            chineseCharItem.show = false;

            previewItem.itemFlag |= ItemFlag.NoCustomShow;
            nameItem.itemFlag |= ItemFlag.NoCustomShow;
            refItem.itemFlag |= ItemFlag.NoCustomShow;
            totalRefItem.itemFlag |= ItemFlag.NoCustomShow;
            //activeItem.itemFlag |= ItemFlag.NoCustomShow;
            //pathItem.itemFlag |= ItemFlag.NoCustomShow;

            InitChecker();
            //根据配置文件刷新，也可以反射创建自定义检查属性
            SetCheckItemConfig(true);
            filterItem = new ListFilter(this);
            currentWindow = ResourceCheckerPlus.instance;
        }

#region 选中处理

        public List<ObjectDetail> GetSelectObjectDetails()
        {
            return SelectList;
        }

        public void SelectObjectDetail(ObjectDetail detail)
        {
            bool current = !detail.isSelected;
            foreach (var v in SelectList)
                v.isSelected = false;
            if (shiftPressed)
            {
                ShiftSelectObject(detail);
            }
            else
            {
                if (!ctrlPressed)
                    SelectList.Clear();
                if (current)
                    SelectList.Add(detail);
                else
                    SelectList.Remove(detail);
            }
            List<Object> list = new List<Object>();
            foreach (var v in SelectList)
            {
                list.Add(v.checkObject);
                v.isSelected = true;
            }
            Selection.objects = list.ToArray();
            CheckDetailSummary();
            GUI.FocusControl(null);
        }

        private void ShiftSelectObject(ObjectDetail detail)
        {
            if (SelectList.Count > 0)
            {
                int start = SelectList.Min(x => FilterList.IndexOf(x));
                int end = FilterList.IndexOf(detail);
                ClearSelect();
                if (end > start)
                {
                    SelectList.AddRange(FilterList.GetRange(start, end - start + 1));
                }
                else
                {
                    SelectList.AddRange(FilterList.GetRange(end, start - end + 1));
                }
            }
        }

        public void SelectAll()
        {
            SelectList.Clear();
            SelectList.AddRange(FilterList);
            foreach (var v in SelectList)
                v.isSelected = true;
            Selection.objects = SelectList.Select(x => x.checkObject).ToArray();
            CheckDetailSummary();
        }

        public void ClearSelect()
        {
            foreach (var v in SelectList)
                v.isSelected = false;
            SelectList.Clear();
            CheckDetailSummary();
        }

        public void SelectObject(Object selectedObject)
        {
            if (ctrlPressed)
            {
                List<Object> currentSelection = new List<Object>(Selection.objects);
                if (currentSelection.Contains(selectedObject))
                    currentSelection.Add(selectedObject);
                else
                    currentSelection.Remove(selectedObject);
                Selection.objects = currentSelection.ToArray();
            }
            else
            {
                Selection.activeObject = selectedObject;
            }
            CheckDetailSummary();
        }

        public void SelectObjects(List<Object> selectedObjects)
        {
            if (ctrlPressed)
            {
                List<Object> currentSelection = new List<Object>(Selection.objects);
                currentSelection.AddRange(selectedObjects);
                Selection.objects = currentSelection.ToArray();
            }
            else
            {
                Selection.objects = selectedObjects.ToArray();
            }
            CheckDetailSummary();
        }

        public void RemoveSelectObjFromFilterList()
        {
            var selection = GetSelectObjectDetails();
            FilterList = FilterList.Where(x => !selection.Contains(x)).ToList();
            ClearSelect();
            RefreshCheckResult();
        }

        public List<Object> GetBatchOptionList()
        {
            var list = CheckerConfigManager.commonConfing.batchOptionType == BatchOptionSelection.CurrentSelect ? SelectList : FilterList;
            return list.Select(x => x.checkObject).ToList();
        }
#endregion

        public virtual void PrepareCheck() { }

        public abstract void InitChecker();

        public virtual void AddObjectDetail(Object rootObj) { }

        public virtual void ShowChildDetail(ObjectDetail detail) { }

        public virtual ObjectDetail AddObjectDetail(object obj, Object refObj, Object detailRefObj) { return null; }

        public virtual void AddObjectDetailRef(GameObject rootObj) { }

        public virtual void AddObjectDetailRefWrap(GameObject rootObj, bool checkRefPrefabReference)
        {
            AddObjectDetailRef(rootObj);
            if (checkRefPrefabReference)
            {
                var dependencies = EditorUtility.CollectDependencies(new Object[] { rootObj });
                var depGameObjects = dependencies.Where(x => x is GameObject && ResourceCheckerHelper.GetPrefabRoot(x as GameObject) != rootObj).ToArray();
                foreach(var dep in depGameObjects)
                {
                    var go = dep as GameObject;
                    if (go == null)
                        continue;
                    AddObjectDetailRef(go);
                }
            }

            foreach(var customFunc in customCheckObjectDetailRefDelegateList)
            {
                if (customFunc != null)
                {
                    customFunc(rootObj, this);
                }
            }
        }

        public virtual void PostInitChecker()
        {
            
        }

        public virtual void PostCheckResource()
        {
            DoResourceAutoCheck();
        }

        public virtual void SceneResCheck(GameObject rootObj)
        {
            var components = rootObj.GetComponentsInChildren<Component>(true);
            components = components.Where(x => !(x is Transform) && x != null).ToArray();
            var gos = components.Select(x => x.gameObject).Distinct().ToArray();
            foreach (var v in gos)
            {
                var dependency = EditorUtility.CollectDependencies(new Object[] { v });
                foreach (var dep in dependency)
                {
                    AddObjectDetail(dep, v.gameObject, null);
                }
            }
        }

        public virtual void DirectResCheck(Object[] selection)
        {
            var objects = GetAllDirectCheckObjectFromInput(selection, checkerFilter);
            if (objects != null && objects.Count > 0)
            {
                for (int i = 0; i < objects.Count; i++)
                {
                    var o = objects[i];
                    if (EditorUtility.DisplayCancelableProgressBar("正在检查" + checkerName + "类型资源", "已完成：" + i + "/" + objects.Count, (float)i / objects.Count))
                        break;
                    AddObjectDetail(o);
                    AddObjectDetail(o, null, null);
                }
                EditorUtility.ClearProgressBar();
            }
        }

        public virtual void ReferenceResCheck(Object[] selection, string filter, bool checkPrefabDetailRef, bool checkRefPrefabReference)
        {
            var objects = GetAllRefCheckObjectFromInput(selection, filter);
            if (objects != null && objects.Count > 0)
            {
                //加入全部查找列表
                checkModule.AddObjectToSideBarList(objects);
                //进行遍历检查
                for (int i = 0; i < objects.Count; i++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("正在检查" + filter + "引用的" + checkerName + "类型资源", "已完成：" + i + "/" + objects.Count, (float)i / objects.Count))
                        break;
                    var root = objects[i];
                    if (root == null)
                        continue;
                    ReferenceResCheck(root, checkPrefabDetailRef, checkRefPrefabReference);
                }
                EditorUtility.ClearProgressBar();
            }
        }

        public void ReferenceResCheck(Object root, bool checkPrefabDetailRef, bool checkRefPrefabReference)
        {
            if (root is SceneAsset)
            {
                CheckSceneReference(root);
            }
            else if (root is Material)
            {
                CheckMaterialReference(root);
            }
            else if (root is GameObject)
            {
                CheckPrefabReference(root, checkPrefabDetailRef, checkRefPrefabReference);
            }
        }

        public void CheckSceneReference(Object root)
        {
            var path = AssetDatabase.GetAssetPath(root);
            var dependency = AssetDatabase.GetDependencies(path);
            foreach (var dep in dependency)
            {
                var obj = AssetDatabase.LoadAssetAtPath<Object>(dep);
                AddObjectDetail(obj, root, null);
                if (dep.EndsWith(".FBX") || dep.EndsWith(".obj"))
                {
                    var fbxDep = EditorUtility.CollectDependencies(new Object[] { obj });
                    foreach (var fDep in fbxDep)
                    {
                        if (fDep is Mesh || fDep is AnimationClip)
                        {
                            AddObjectDetail(fDep, root, null);
                        }
                    }
                }
            }
        }

        public void CheckMaterialReference(Object root)
        {
            var dependencies = EditorUtility.CollectDependencies(new Object[] { root });
            AddObjectDetail(root);
            //检查每个prefab的dependencies
            foreach (Object depend in dependencies)
            {
                AddObjectDetail(depend, root, null);
            }
        }

        public virtual void CheckPrefabReference(Object root, bool checkDetailRef, bool checkRefPrefabReference)
        {
            if (checkDetailRef)
            {
                var go = root as GameObject;
                if (go != null)
                {
                    AddObjectDetailRefWrap(go, checkRefPrefabReference);
                }
            }
            else
            {
                var dependencies = EditorUtility.CollectDependencies(new Object[] { root });
                AddObjectDetail(root);
                //检查每个prefab的dependencies
                foreach (Object depend in dependencies)
                {
                    AddObjectDetail(depend, root, null);
                }
            }
        }

        public virtual void ReverseResCheckMainlyObjectSelf(Object[] selection, string resourceExten, Object rangeFolder)
        {
            var objects = GetAllRefCheckObjectFromInput(selection, "t:Object");
            if (objects == null || objects.Count == 0 || string.IsNullOrEmpty(resourceExten))
                return;
            AddObjectDetailBatch(objects);
            var checkAssetPath = GetAllReverseCheckResList(resourceExten, rangeFolder);
            for(int i = 0; i < checkAssetPath.Length; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("正在反向检查" + resourceExten + "类型资源引用", "已完成：" + i + "/" + checkAssetPath.Length, (float)i / checkAssetPath.Length))
                    break;
                var assetPath = checkAssetPath[i];
                if (string.IsNullOrEmpty(assetPath))
                    continue;
                var depends = AssetDatabase.GetDependencies(assetPath);
                foreach (var depend in depends)
                {
                    foreach (var obj in objects)
                    {
                        var tempPath = AssetDatabase.GetAssetPath(obj);
                        //排除自身
                        if (tempPath == depend && tempPath != assetPath)
                        {
                            //与正常的查询反过来，foundinreference是被查找的资源，前面的是引用被查找资源的资源
                            AddObjectDetail(obj, AssetDatabase.LoadAssetAtPath<Object>(assetPath), null);
                        }
                    }
                }
            }
            EditorUtility.ClearProgressBar();
        }

        public virtual void ReverseResCheckMainlyReference(Object[] selection, Object rangeFolder)
        {
            var objects = GetAllRefCheckObjectFromInput(selection, "t:Object");
            if (objects == null || objects.Count == 0 || string.IsNullOrEmpty(postfix))
                return;
            checkModule.AddObjectToSideBarList(objects);
            //获取当前所有后缀为当前checker的资源路径
            var checkAssetPath = GetAllReverseCheckResList(postfix, rangeFolder);
            for (int i = 0; i < checkAssetPath.Length; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("正在反向检查位于" + checkerFilter + "类型资源引用", "已完成：" + i + "/" + checkAssetPath.Length, (float)i / checkAssetPath.Length))
                    break;
                var assetPath = checkAssetPath[i];
                if (string.IsNullOrEmpty(assetPath))
                    continue;
                var depends = AssetDatabase.GetDependencies(assetPath);
                foreach (var depend in depends)
                {
                    foreach (var obj in objects)
                    {
                        var tempPath = AssetDatabase.GetAssetPath(obj);
                        //排除自身
                        if (tempPath == depend && tempPath != assetPath)
                        {
                            //与正常的查询反过来，foundinreference是被查找的资源，前面的是引用被查找资源的资源
                            AddObjectDetail(AssetDatabase.LoadAssetAtPath<Object>(assetPath), obj, null);
                        }
                    }
                }
            }
            EditorUtility.ClearProgressBar();
        }

        private string[] GetAllReverseCheckResList(string resourceExten, Object rangeFolder)
        {
            var checkAssetPath = AssetDatabase.GetAllAssetPaths().Where(x => x.EndsWith(resourceExten)).ToArray();
            if (rangeFolder != null)
            {
                var path = AssetDatabase.GetAssetPath(rangeFolder);
                checkAssetPath = checkAssetPath.Where(x => x.StartsWith(path)).ToArray();
            }
            return checkAssetPath;
        }

        public void MixCheckDirectAndRefRes(Object root, bool checkObjectDetailRef)
        {
            if (root is SceneAsset)
            {
                CheckSceneReference(root);
            }
            else if (root is Material)
            {
                CheckMaterialReference(root);
            }
            else if (root is GameObject)
            {
                //TODO:Mix检查模式下，暂时不开
                CheckPrefabReference(root, checkObjectDetailRef, false);
            }
            else
            {
                AddObjectDetail(root);
                AddObjectDetail(root, null, null);
            }
        }

        //TODO:提供一个单纯的只是增加Ref的接口，方便Compoent Checker和UGUI Checker直接增加ref的
        public virtual ObjectDetail AddObjectWithRef(object obj, Object refObj, Object rootObj)
        {
            ObjectDetail detail = null;
            if (checkModule is SceneResCheckModule)
            {
                detail = AddObjectDetail(obj, refObj, null);
            }
            else if (checkModule is ReferenceResCheckModule)
            {
                var refCheckModule = checkModule as ReferenceResCheckModule;
                if (refCheckModule.currentCheckType == ReferenceResCheckModule.ReferenceCheckType.Scene)
                {
                    if (refCheckModule.batchCheckSceneInfo)
                    {
                        detail = AddObjectDetail(obj, refObj, null);
                    }
                    else if (refCheckModule.checkSceneDetailRef)
                    {
                        detail = AddObjectDetail(obj, refCheckModule.GetCurrentCheckScene(), null);
                    }
                    else
                    {
                        detail = AddObjectDetail(obj, rootObj, refObj);
                    }
                }
                else
                {
                    detail = AddObjectDetail(obj, rootObj, refObj);
                }
            }
            else
            {
                detail = AddObjectDetail(obj, rootObj, refObj);
            }
            return detail;
        }

        public void AddObjectDetailBatch(List<Object> objList)
        {
            foreach (var o in objList)
            {
                AddObjectDetail(o);
                AddObjectDetail(o, null, null);
            }
        }

        public void SetCheckItemConfig(bool refreshOrder)
        {
            var cfg = ResourceCheckerPlus.instance.configManager.GetCheckerConfig(checkerName);
            if (cfg.checkItemCfg == null)
                return;
            //TODO：目前还没完成从代码创建到全配置创建，暂时过渡一下
            //foreach (var v in cfg.checkItemCfg)
            //{
            //    var item = CheckItem.CreateCheckItemFromConfig(this, v);
            //    AddCustomCheckItem(item);
            //}
            foreach(var item in checkItemList)
            {
                cfg.LoadItemCfg(item);
            }
            if (refreshOrder == true)
            {
                checkItemList.Sort(delegate (CheckItem item1, CheckItem item2) { return item1.order - item2.order; });
            }
            LoadPredefineFilter(cfg);
        }

        public void AddCustomCheckItem(CheckItem item)
        {
            if (item != null)
            {
                customCheckItems.Add(item);
            }
        }

        public void SetAllCheckItemVisible(bool show)
        {
            foreach (var v in checkItemList.Where(x => (x.itemFlag & ItemFlag.NoCustomShow) == 0))
            {
                v.show = show;
            }
        }

        public void RefreshCheckerItemConfig(bool refreshOrder)
        {
            var cfg = ResourceCheckerPlus.instance.configManager.GetCheckerConfig(checkerName);
            if (cfg.checkItemCfg == null)
                return;
            foreach (var item in checkItemList)
            {
                cfg.SerilizeItemCfg(item);
            }
            //TODO:
            ResourceCheckerPlus.instance.resCheckModeList.ForEach(x => x.checkerList.ForEach(y => y.SetCheckItemConfig(refreshOrder)));
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
        }

        public CheckItem CopyOrGetCheckItemByTitle(CheckItem source)
        {
            var item = checkItemList.Find(x => x.title == source.title);
            if (item == null)
            {
                item = new CheckItem(this, source.title, source.type);
            }
            return item;
        }

        public virtual void CheckDetailSummary()
        {
            checkResultDic[totalCount] = FilterList.Count;
            checkResultDic[selectCount] = SelectList.Count;
            checkResultDic[warningCount] = FilterList.Count(x => x.warningLevel > ResourceWarningLevel.Normal);
        }

        public virtual void BatchSetResConfig()
        {
            EditorUtility.DisplayDialog("提示", "该类型资源不支持批量修改，可以进行批量选中然后手动修改", "OK");
        }

        public virtual void CustomFunctionCheck()
        {
            CustomFunctionEditor.Init(this);
        }

        public void Clear()
        {
            viewListScrollPos = Vector2.zero;
            ClearSelect();
            CheckList.Clear();
            FilterList.Clear();
            if (CheckerConfigManager.commonConfing.clearFilterOnReCheck)
            {
                filterItem.Clear(true);
            }
            CheckDetailSummary();
        }

        public void Recover()
        {
            ClearSelect();
            FilterList.Clear();
            FilterList.AddRange(CheckList);
            filterItem.Clear(true);
            CheckDetailSummary();
        }

        public void CheckDetailSort(CheckItem item, bool positive)
        {
            switch (item.type)
            {
                case CheckType.String:
                    FilterList.Sort(delegate (ObjectDetail check1, ObjectDetail check2)
                    {
                        if (positive)
                            return string.Compare((string)check2.GetCheckValue(item), (string)check1.GetCheckValue(item));
                        return string.Compare((string)check1.GetCheckValue(item), (string)check2.GetCheckValue(item));
                    });
                    break;
                case CheckType.Int:
                case CheckType.FormatSize:
                    FilterList.Sort(delegate (ObjectDetail check1, ObjectDetail check2)
                    {
                        if (positive)
                            return (int)check1.GetCheckValue(item) - (int)check2.GetCheckValue(item);
                        return (int)check2.GetCheckValue(item) - (int)check1.GetCheckValue(item);
                    });
                    break;
                case CheckType.Float:
                    FilterList.Sort(delegate (ObjectDetail check1, ObjectDetail check2)
                    {
                        //float排序,暂时木有找到什么好方法....
                        var val1 = (float)check1.GetCheckValue(item) * 10000;
                        var val2 = (float)check2.GetCheckValue(item) * 10000;
                        if (positive)
                            return (int)val1 - (int)val2;
                        return (int)val2 - (int)val1;
                    });
                    break;
                case CheckType.List:
                    FilterList.Sort(delegate (ObjectDetail check1, ObjectDetail check2)
                    {
                        var check1List = check1.GetCheckValue(item) as List<Object>;
                        var check2List = check2.GetCheckValue(item) as List<Object>;
                        if (positive)
                            return check1List.Count - check2List.Count;
                        return check2List.Count - check1List.Count;
                    });
                    break;
                case CheckType.ListShowFirstItem:
                    FilterList.Sort(delegate (ObjectDetail check1, ObjectDetail check2)
                    {
                        var check1List = check1.GetCheckValue(item) as List<Object>;
                        var check2List = check2.GetCheckValue(item) as List<Object>;
                        var str1 = check1List.Count == 0 ? "Null" : check1List[0].ToString();
                        var str2 = check2List.Count == 0 ? "Null" : check2List[0].ToString();
                        if (positive)
                            return string.Compare(str2, str1);
                        return string.Compare(str1, str2);
                    });
                    break;
                default:
                    return;
            }
        }

        private void SortByWarningLevel(bool positive)
        {
            FilterList.Sort(delegate (ObjectDetail check1, ObjectDetail check2)
            {
                if (positive)
                    return check2.warningLevel - check1.warningLevel;
                return check1.warningLevel - check2.warningLevel;
            });
        }

        public virtual void RefreshCheckResult()
        {
            RecaculateAllItemWidth();
            FilterCheckResult();
            CheckDetailSummary();
            ClearSelect();
        }

        public virtual void ShowCheckerFliter()
        {
            filterItem.ShowFilter();
        }

        public void FilterCheckResult()
        {
            FilterList = filterItem.CheckDetailFilter(CheckList, onlyShowWarningItem);
        }


        public virtual void ShowCheckerTitle()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("系统设置", GUILayout.Width(70)))
            {
                ResourceCheckerConfigWindow.Init();
            }
            if (CheckList.Count != 0 && CheckerConfigManager.commonConfing.showDocument)
            {
                if (GUILayout.Button("使用文档", GUILayout.Width(70)))
                {
                    ResourceCheckerDocumentWindow.Init();
                }
            }
            
            EditorGUI.BeginChangeCheck();
            var config = CheckerConfigManager.commonConfing;
            if (!(checkModule is SceneResCheckModule))
            {
                config.inputType = (CheckInputMode)EditorGUILayout.Popup("",(int)config.inputType, checkDragTypeStr, GUILayout.Width(250));
            }
            if (config.enableAutoResourceCheck)
            {
                onlyShowWarningItem = GUILayout.Toggle(onlyShowWarningItem, "仅显示警告项", GUILayout.Width(90));
            }
            if (EditorGUI.EndChangeCheck())
            {
                RefreshCheckResult();
            }
            
            string title = "";
            if (CheckList.Count != 0 || dontShowDocAndTitle)
            {
                title = GenerateCheckSummary();
            }
            else if (checkModule != null)
            {
                title = checkModule.checkModeName.tooltip;
            }

            GUILayout.Label(title);
            ShowPredefineFilterGroup();
            GUILayout.EndHorizontal();
        }

        private string GenerateCheckSummary()
        {
            string summary = "";
            foreach (var v in checkResultDic)
            {
                if (v.Key == null || v.Value == null)
                    continue;
                if ((v.Key.itemFlag & ItemFlag.CheckSummary) == ItemFlag.CheckSummary)
                {
                    string value = v.Key.type == CheckType.FormatSize ? EditorUtility.FormatBytes((int)v.Value) : v.Value.ToString();
                    summary += v.Key.title + " : " + value + "   ";
                }
            }

            return summary;
        }

        public virtual void ShowCheckerSort()
        {
            viewListScrollPos.x = EditorGUILayout.BeginScrollView(new Vector2(viewListScrollPos.x, 0), GUILayout.Height(40)).x;
            GUILayout.BeginHorizontal();
            foreach (var item in checkItemList)
            {
                if (item.show == false)
                    continue;
                var config = CheckerConfigManager.commonConfing;
                if (config.enableAutoResourceCheck && item.title == "预览")
                {
                    if (IsWarningChecker())
                        GUI.color = config.errorItemColor;
                    if (GUILayout.Button("警告", GUILayout.Width(item.width)))
                    {
                        SortByWarningLevel(item.sortSymbol);
                        item.sortSymbol = !item.sortSymbol;
                    }
                    GUI.color = CheckerConfigManager.defaultTextColor;
                }
                else
                {
                    if (GUILayout.Button(item.title, GUILayout.Width(item.width)))
                    {
                        CheckDetailSort(item, item.sortSymbol);
                        //点击之后反向排序
                        item.sortSymbol = !item.sortSymbol;
                    }
                }
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        public virtual void ShowCheckResult()
        {
            ShowCheckerFliter();
            ShowCheckerSort();
            ShowCheckDetailResult();
        }

        private void OnKeyBoardEvent()
        {
            var evt = Event.current;
            //事件响应
            ctrlPressed = evt.control || evt.command;
            shiftPressed = evt.shift;
            if (evt.isKey && evt.type == EventType.KeyDown && !(this is SelfObjectChecker))
            {
                OnUpDownButtonSelect(evt.keyCode);
            }
        }

        private void OnUpDownButtonSelect(KeyCode key)
        {
            var list = GetSelectObjectDetails();
            ObjectDetail nextSelect = null;
            if (list.Count > 0)
            {
                var detail = list[0];
                var index = FilterList.IndexOf(detail);
                if (key == KeyCode.UpArrow)
                {
                    if (index - 1 >= 0)
                    {
                        nextSelect = FilterList[index - 1];
                        viewListScrollPos.y -= 41;
                    }
                }
                else if (key == KeyCode.DownArrow)
                {
                    if (index + 1 < FilterList.Count)
                    {
                        nextSelect = FilterList[index + 1];
                        viewListScrollPos.y += 41;
                    }
                }
                if (nextSelect != null)
                {
                    ClearSelect();
                    SelectObjectDetail(nextSelect);
                    currentWindow.Repaint();
                }
            }
            else
            {
                if (FilterList.Count > 0)
                {
                    var detail = FilterList[0];
                    ClearSelect();
                    SelectObjectDetail(detail);
                    viewListScrollPos.y = 0;
                    currentWindow.Repaint();
                }
            }
        }

        public void ShowCheckDetailResult()
        {
            OnKeyBoardEvent();
            //Unity在GUI中改变界面状态时，渲染界面会出错
            //http://answers.unity3d.com/questions/400454/argumentexception-getting-control-0s-position-in-a-1.html
            if (Event.current.type == EventType.Layout)
            {
                OptimizeCheckResult();
            }
            if (CheckList.Count == 0 && dontShowDocAndTitle == false)
            {
                ShowCheckerDocument();
            }
            else
            {
                ShowCheckDetailResultInternel();
            }
        }

        private void ShowCheckDetailResultInternel()
        {
            viewListScrollPos = EditorGUILayout.BeginScrollView(viewListScrollPos);
            for (int i = 0; i < FilterList.Count; i++)
            {
                var detail = FilterList[i];
                GUILayout.BeginHorizontal();
                if (IsItemVisible(i))
                {
                    detail.ShowCheckItem();
                }
                else
                {
                    GUILayout.Label(" ", GUILayout.Height(previewItem.width));
                }
                GUILayout.EndHorizontal();
                ShowChildDetail(detail);
            }
            EditorGUILayout.EndScrollView();
        }

        private void ShowCheckerDocument()
        {
            CheckerDocumentHelper.DrawDocuments();
        }

        public virtual List<Object> GetAllDirectCheckObjectFromInput(Object[] selection, string filter)
        {
            return GetAllObjectFromInput<Object>(selection, filter);
        }

        public virtual List<Object> GetAllRefCheckObjectFromInput(Object[] selection, string filter)
        {
            return GetAllObjectFromInput<Object>(selection, filter);
        }

        public static List<Object> GetAllObjectFromInput<T>(Object[] objects, string filter) where T : Object
        {
            var pathFolderList = new List<string>();
            var objlist = new List<Object>();
            var singleObjList = new List<string>();
            for (int i = 0; i < objects.Length; i++)
            {
                var obj = objects[i];
                if (obj == null)
                    continue;
                string temPath = AssetDatabase.GetAssetPath(obj);
                if (ResourceCheckerHelper.isFolder(temPath))
                {
                    pathFolderList.Add(temPath);
                }
                else if (obj is T)
                {
                    objlist.Add(obj);
                }
            }
            if (pathFolderList.Count > 0)
            {
                string[] guids = AssetDatabase.FindAssets(filter, pathFolderList.ToArray());
                singleObjList.AddRange(guids.Select(x => AssetDatabase.GUIDToAssetPath(x)));
            }

            for (int i = 0; i < singleObjList.Count; i++)
            {
                string s = singleObjList[i];
                if (EditorUtility.DisplayCancelableProgressBar("正在加载" + filter + "类型资源", "已完成：" + i + "/" + singleObjList.Count, (float)i / singleObjList.Count))
                {
                    break;
                }
                objlist.Add(AssetDatabase.LoadAssetAtPath<T>(s));
            }
            EditorUtility.ClearProgressBar();
            return objlist;
        }

        public void MoveAssetToPath()
        {
            var objects = GetBatchOptionList();
            MoveAssetEditor.Init(objects);
        }

        public void BuildAssetBundleOption()
        {
            var objects = GetBatchOptionList();
            BuildAssetBundleTool.Init(objects);
        }

        public void AddSelectObjectToCustomList()
        {
            var objects = GetSelectObjectDetails().Select(x => x.checkObject).ToList();
            checkModule.AddObjectToSideBarList(objects);
        }

        public void OnContexMenu()
        {
            var genericMenu = new GenericMenu();
            if (enableReloadCheckItem)
                genericMenu.AddItem(new GUIContent("刷新"), false, new GenericMenu.MenuFunction(ReloadCheckDetail));
            genericMenu.AddItem(new GUIContent("全选"), false, new GenericMenu.MenuFunction(SelectAll));
            genericMenu.AddItem(new GUIContent("取消"), false, new GenericMenu.MenuFunction(ClearSelect));
            genericMenu.AddItem(new GUIContent("全选引用选中内容的对象"), false, new GenericMenu.MenuFunction(SelectAllSelectObjectRef));
            if (totalRefItem.show)
                genericMenu.AddItem(new GUIContent("全选引用选中内容的具体子对象"), false, new GenericMenu.MenuFunction(SelectAllSelectObjectTotalRef));
            genericMenu.AddItem(new GUIContent("选中所有选中内容的根Prefab"), false, new GenericMenu.MenuFunction(SelectAllSelectObjectRootPrefab));
            genericMenu.AddItem(new GUIContent("批量移动到指定目录"), false, new GenericMenu.MenuFunction(MoveAssetToPath));
            genericMenu.AddItem(new GUIContent("批量修改格式"), false, new GenericMenu.MenuFunction(BatchSetResConfig));
            genericMenu.AddItem(new GUIContent("打AssetBundle"), false, new GenericMenu.MenuFunction(BuildAssetBundleOption));
            genericMenu.AddItem(new GUIContent("进阶检查"), false, new GenericMenu.MenuFunction(CustomFunctionCheck));
            genericMenu.AddItem(new GUIContent("检查结果导出Excel表"), false, new GenericMenu.MenuFunction(ExportCurrentCheckerResult));
            genericMenu.AddItem(new GUIContent("检查源对象导出Excel表"), false, new GenericMenu.MenuFunction(ExportSelectObjectsCheckerResult));
            genericMenu.AddItem(new GUIContent("将筛选结果作为原始结果", "将当前结果作为检查的原始结果，并清理掉所有筛选条件"), false, new GenericMenu.MenuFunction(SetCurFilterResultAsSource));

            genericMenu.ShowAsContext();
        }

        public virtual void ShowOptionButton()
        {
            if (GUILayout.Button("其他功能", GUILayout.Width(60)))
            {
                OnContexMenu();
            }
            //if (GUILayout.Button("测试", GUILayout.Width(60)))
            //{
            //    //Clear();
            //    //Resources.UnloadUnusedAssets();
            //    //TestDebug("maxUsedMemory", Profiler.maxUsedMemory);
            //    //TestDebug("GetTotalAllocatedMemoryLong", (int)Profiler.GetTotalAllocatedMemoryLong());
            //    //TestDebug("GetTotalUnusedReservedMemoryLong", (int)Profiler.GetTotalUnusedReservedMemoryLong());
            //    //TestDebug("GetAllocatedMemoryForGraphicsDriver", (int)Profiler.GetAllocatedMemoryForGraphicsDriver());
            //    //TestDebug("GetMonoHeapSizeLong", (int)Profiler.GetMonoHeapSizeLong());
            //    //TestDebug("GetMonoUsedSizeLong", (int)Profiler.GetMonoUsedSizeLong());
            //    //TestDebug("GetTempAllocatorSize", (int)Profiler.GetTempAllocatorSize());
            //    //TestDebug("GetTotalReservedMemoryLong", (int)Profiler.GetTotalReservedMemoryLong());
            //}
        }

        public virtual void OnSceneDraw(SceneView sceneView)
        {

        }

        //private void TestDebug(string name, long bytes)
        //{
        //    Debug.Log(name + " " + EditorUtility.FormatBytes(bytes) + " " + bytes);
        //}

        private void OnNameButtonClick(ObjectDetail detail)
        {
            //回调之前带了选中的功能，此处空实现
        }

        private void OnRefButtonClick(ObjectDetail detail)
        {
            if (checkModule != null)
            {
                checkModule.AddObjectToSideBarList(detail.referenceObjectList);
            }
            SelectObjects(detail.referenceObjectList);
        }

        private void OnDetailRefButton(ObjectDetail detail)
        {
            if (checkModule != null)
            {
                checkModule.AddObjectToSideBarList(detail.detailReferenceList);
            }
            SelectObjects(detail.detailReferenceList);
        }

        //当检查条目达到上千级别时，滚动条会很卡，目前的策略是看不见的就不显示了。貌似Unity5.6直接就有类似的控件
        //目前3000左右没有问题，不过上万之后还是会很卡
        //感谢这位老哥 http://blog.csdn.net/akof1314/article/details/70285033
        public void OptimizeCheckResult()
        {
            float y = viewListScrollPos.y;
            //可能还需要减去高度，不过多显示一两个无所谓了...
            float height = ResourceCheckerPlus.instance.position.height;
            int first = (int)Mathf.Floor(y / checkItemHeight);
            int last = first + (int)Mathf.Ceil(height / checkItemHeight);
            firstVisible = Mathf.Max(first - 10, 0);
            lastVisible = Mathf.Min(last, FilterList.Count - 1);
        }

        private bool IsItemVisible(int id)
        {
            if (id >= firstVisible && id <= lastVisible)
                return true;
            return false;
        }

        public static ObjectChecker CreateChecker(ResCheckModuleBase module, CheckerCfg cfg)
        {
            var type = ResourceCheckerAssemblyHelper.GetResourceCheckerType(cfg.checkerName);
            if (type == null)
                return null;
            var checker = System.Activator.CreateInstance(type) as ObjectChecker;
            checker.checkModule = module;
            checker.checkerEnabled = cfg.checkerEnabled;
            checker.config = cfg;
            module.checkerList.Add(checker);

            if (!allCheckerDic.ContainsKey(checker.checkerName))
            {
                allCheckerDic.Add(checker.checkerName, checker);
                checker.InitResourceRuleConfig();
            }
            return checker;
        }

        public void ExportCheckResult(string path)
        {
            var sw = new StreamWriter(path + "/" + checkerName + "CheckResult.txt", true, System.Text.Encoding.Default);
            var checkTitle = "";
            foreach (var v in checkItemList)
            {
                if (v.type == CheckType.Texture)
                    continue;
                checkTitle += v.title + "\t";
            }
            sw.WriteLine(checkTitle);
            for (int i = 0; i < FilterList.Count; i++)
            {
                var detail = FilterList[i];
                var line = "";
                checkItemList.ForEach(item => GenerateDetailLine(ref line, detail, item));
                sw.WriteLine(line);
            }
            sw.Close();
        }

        public void ExportSelectObjectsCheckResult(string path)
        {
            var sw = new StreamWriter(path + "/" + checkerName + "CheckResult.txt", true, System.Text.Encoding.Default);

            Object[] objects = Selection.objects;
            if(null != objects && objects.Length > 0)
            {
                foreach(var item in objects)
                    sw.WriteLine(item.name);
            }

            sw.Close();
        }

        private void GenerateDetailLine(ref string line, ObjectDetail detail, CheckItem item)
        {
            if (item.type != CheckType.Texture)
            {
                if (item.type == CheckType.FormatSize)
                {
                    line += EditorUtility.FormatBytes((int)detail.GetCheckValue(item));
                }
                else if (item.type == CheckType.List)
                {
                    var list = detail.GetCheckValue(item) as List<Object>;
                    line += list.Count.ToString();
                }
                else if (item.type == CheckType.ListShowFirstItem)
                {
                    var list = detail.GetCheckValue(item) as List<Object>;
                    line += list.Count == 0 ? "Null" : list[0].ToString();
                }
                else
                {
                    var value = detail.GetCheckValue(item);
                    var str = value != null ? value.ToString().Replace("\n", "").Replace(" ", "") : "";
                    line += str;
                }
                line += "\t";
            }
        }

        private void ExportCurrentCheckerResult()
        {
            var path = ResourceCheckerHelper.GenericExportFolderName();
            Directory.CreateDirectory(path);
            ExportCheckResult(path);
            AssetDatabase.Refresh();
        }

        private void ExportSelectObjectsCheckerResult()
        {
            var path = ResourceCheckerHelper.GenericExportFolderName();
            Directory.CreateDirectory(path);
            ExportSelectObjectsCheckResult(path);
            AssetDatabase.Refresh();
        }

        public void ClearCheckFilter()
        {
            filterItem.Clear(true);
        }

        public CheckItem GetCheckItemByCheckItemTitle(string title)
        {
            foreach(var item in checkItemList)
            {
                if (item.title == title)
                    return item;
            }
            return null;
        }

        //清除全部筛选条件，将当前筛选结果作为原始结果
        public void SetCurFilterResultAsSource()
        {
            CheckList.Clear();
            CheckList.AddRange(FilterList);
            filterItem.Clear(true);
        }

        public void ReloadCheckDetail()
        {
            isReloadCheckItem = true;
            foreach (var v in FilterList)
            {
                v.ReloadCheckObject();
            }
            CheckDetailSummary();
            isReloadCheckItem = false;
        }

        public void SelectAllSelectObjectRef()
        {
            checkModule.ClearSideBarList();
            foreach (var v in SelectList)
            {
                checkModule.AddObjectToSideBarList(v.referenceObjectList, false);
            }
            checkModule.SelectAll();
        }

        public void SelectAllSelectObjectTotalRef()
        {
            checkModule.ClearSideBarList();
            foreach (var v in SelectList)
            {
                checkModule.AddObjectToSideBarList(v.detailReferenceList, false);
            }
            checkModule.SelectAll();
        }

        public void SelectAllSelectObjectRootPrefab()
        {
            checkModule.ClearSideBarList();
            var goList = new List<Object>();
            foreach (var v in SelectList)
            {
                var go = v.checkObject as GameObject;
                if (go == null)
                    continue;
                var root = ResourceCheckerHelper.GetPrefabRoot(go);
                if (root == null)
                    continue;
                goList.Add(root);
            }
            checkModule.AddObjectToSideBarList(goList, false);
            checkModule.SelectAll();
        }

        private void RecaculateItemWidth(CheckItem item)
        {
            if ((item.itemFlag & ItemFlag.FixWidth) == ItemFlag.FixWidth)
                return;
            int itemWidth = 0;
            if (item.type == CheckType.Texture)
            {
                item.width = 40;
                return;
            }
            else if (item.type == CheckType.List || item.type == CheckType.Int)
            {
                itemWidth = GetTitleWidth(item);
            }
            else if (item.type == CheckType.ListShowFirstItem)
            {
                itemWidth = GetTitleWidth(item);
                foreach (var v in CheckList)
                {
                    var value = v.GetCheckValue(item);
                    var list = value as List<Object>;
                    int width = list == null || list.Count == 0 ? 0 : list[0].ToString().Length * 8 + 10;
                    if (itemWidth < width)
                        itemWidth = width;
                }
            }
            else
            {
                itemWidth = GetTitleWidth(item);
                foreach (var v in CheckList)
                {
                    var value = v.GetCheckValue(item);
                    int width = value == null ? 0 : value.ToString().Length * 8 + 10;
                    if (itemWidth < width)
                        itemWidth = width;
                }
            }
            item.width = Mathf.Max(itemWidth, item.defaultWidth);
        }

        public void RecaculateAllItemWidth()
        {
            checkItemList.ForEach(x => RecaculateItemWidth(x));
        }

        private int GetTitleWidth(CheckItem item)
        {
            var width = 0;
            var charArray = item.title.ToCharArray();
            foreach (var v in charArray)
            {
                //中文
                if (v >= 0x4e00 && v <= 0x9fbb)
                    width += 14;
                else
                    width += 8;
            }
            return width + 20;
        }

        public ObjectDetail GetCheckDetail(CheckItem item, object filter)
        {
            if (filter == null)
                return null;
            foreach (var v in FilterList)
            {
                var value = v.GetCheckValue(item);
                if (value != null && value.ToString() == filter.ToString())
                    return v;
            }
            return null;
        }

        public void RegistCustomCheckObjectDetailRefDelegate(CustomCheckObjectDetailRefDelegate delFunc)
        {
            if (!customCheckObjectDetailRefDelegateList.Contains(delFunc))
                customCheckObjectDetailRefDelegateList.Add(delFunc);
        }

        #region Resource Rule System
        public void DoResourceAutoCheck()
        {
            ClearAutoCheckResult();
            
            if (!CheckerConfigManager.commonConfing.enableAutoResourceCheck)
                return;
            var resourceRuleChecker = GetCheckerByName(checkerName);
            foreach(var detail in CheckList)
            {
                foreach(var checkItem in resourceRuleChecker.checkItemList)
                {
                    checkItem.AutoCheckResource(detail);
                }
            }
        }

        public void ClearAutoCheckResult()
        {
            foreach(var detail in CheckList)
            {
                foreach(var value in detail.checkMap.Values)
                {
                    value.ClearCheckResult();
                }
            }
        }

        public void SetResourceTag(List<ResourceTag> tags)
        {
            var resourceRuleChecker = GetCheckerByName(checkerName);
            resourceRuleChecker.checkItemList.ForEach(x => x.SetAutoCheckConfig(tags));
        }

        public void ClearResourceTag()
        {
            var resourceRuleChecker = GetCheckerByName(checkerName);
            resourceRuleChecker.checkItemList.ForEach(x => x.ClearAutoCheckSystem());
        }

        public void InitResourceRuleSystem(List<ResourceTag> resourceTags)
        {
            resourceTags.ForEach(x => InitResourceRuleByTag(x));
        }

        private void InitResourceRuleByTag(ResourceTag resourceTag)
        {
            var ruleConfig = ResourceRuleManager.GetCurrentCheckerResourceRule(checkerName);
            if (ruleConfig == null || ruleConfig.resourceRuleGroup == null)
                return;
            foreach (var rule in ruleConfig.resourceRuleGroup)
            {
                var item = GetCheckItemByCheckItemTitle(rule.checkItemName);
                if (item != null)
                    item.InitAutoCheckSystem(rule);
                else
                    Debug.LogError("Resource Rule Item Name doesn't match Check Item");
            }
        }

        private void InitResourceRuleConfig()
        {
            var ruleConfig = ResourceRuleManager.GetCurrentCheckerResourceRule(checkerName);
            if (ruleConfig == null || ruleConfig.resourceRuleGroup == null)
                return;
            foreach (var rule in ruleConfig.resourceRuleGroup)
            {
                var item = GetCheckItemByCheckItemTitle(rule.checkItemName);
                if (item != null)
                    item.InitAutoCheckSystem(rule);
                else
                    Debug.LogError("Resource Rule Item Name doesn't match Check Item");
            }
        }

        public bool IsWarningChecker()
        {
            object value = null;
            checkResultDic.TryGetValue(warningCount, out value);
            return value == null ? false : (int)value > 0;
        }

        public static ObjectChecker GetCheckerByName(string name)
        {
            ObjectChecker checker = null;
            allCheckerDic.TryGetValue(name, out checker);
            return checker;
        }
        #endregion

        #region Resource Chcker Test
        public static void SerilizeAllCheckItemConfig()
        {
            var allChecker = allCheckerDic.Values.ToList();
            allChecker.ForEach(x => x.SerilizeCurrentCheckItemConfig());
        }

        public void SerilizeCurrentCheckItemConfig()
        {
            var checkerCfg = ResourceCheckerPlus.instance.configManager.GetCheckerConfig(checkerName);
            if (checkerCfg.checkItemCfg == null)
                return;
            var list = new List<CheckItemConfig>();
            foreach(var item in checkItemList)
            {
                var cfg = new CheckItemConfig();
                cfg.ItemTitle = item.title;
                cfg.order = item.order;
                cfg.show = item.show;
                list.Add(cfg);
            }
            checkerCfg.checkItemCfg = list.ToArray();

            EditorUtility.SetDirty(checkerCfg);
            AssetDatabase.SaveAssets();
            ResourceCheckerPlus.instance.configManager.SaveCheckerConfig();
        }
        #endregion

        #region Predefine Filter
        protected static FilterItemCfgGroup nullFilterGroup = new FilterItemCfgGroup();
        protected List<FilterItemCfgGroup> predefineFilterGroups = new List<FilterItemCfgGroup>();
        protected string[] filterNames = null;
        protected int currentSelectPredefineFilter = 0;
        public void LoadPredefineFilter(CheckerConfig cfg)
        {
            var predefineFilterCfg = cfg.predefineFilter;
            predefineFilterGroups.Clear();
            if (predefineFilterCfg == null || predefineFilterCfg.Length == 0)
                return;
            nullFilterGroup.filterGroupName = "显示全部";
            predefineFilterGroups.Add(nullFilterGroup);
            predefineFilterGroups.AddRange(predefineFilterCfg);
            filterNames = predefineFilterGroups.Select(x => x.filterGroupName).ToArray();
            currentSelectPredefineFilter = cfg.defaultFilterIndex;
        }

        public void ShowPredefineFilterGroup()
        {
            if (predefineFilterGroups.Count <= 1)
                return;
            EditorGUI.BeginChangeCheck();
            currentSelectPredefineFilter = GUILayout.Toolbar(currentSelectPredefineFilter, filterNames);
            if (EditorGUI.EndChangeCheck())
            {
                var currentFilterGroup = predefineFilterGroups[currentSelectPredefineFilter];
                filterItem.Clear(true);
                if (currentFilterGroup != nullFilterGroup)
                {
                    filterItem.SetFilterByConfig(currentFilterGroup);
                }
                RefreshCheckResult();
            }
        }
        #endregion
    };
}