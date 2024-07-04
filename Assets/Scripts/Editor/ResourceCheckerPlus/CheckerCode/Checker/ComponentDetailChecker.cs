using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;

namespace ResourceCheckerPlus
{
    public class ComponentFiledEditor : SideBarWindow
    {
        private static ComponentDetailChecker checker = null;

        public static void Init(ComponentChecker.ComponentDetail detail)
        {
            checker = new ComponentDetailChecker();
            
            var window = GetWindow<ComponentFiledEditor>();
            checker.currentWindow = window;
            checker.currentWindow.titleContent = new GUIContent("组件属性检查", "可以显示任意组件中各类型字段属性值");
            checker.InitComponentDetailChecker(detail);
            checker.checkModule = detail.currentChecker.checkModule;
            checker.pathItem.show = !(checker.checkModule is SceneResCheckModule);
            window.SetSideBarWide(CheckerConfigManager.commonConfing.sideBarWidth);
        }

        public override void ShowLeftSide()
        {
            if (checker == null || ResourceCheckerPlus.instance == null)
                Close();
            if (checker != null && ResourceCheckerPlus.instance != null)
            {
                checker.ShowSideBar();
            }
        }

        public override void ShowRightSide()
        {
            if (checker != null && ResourceCheckerPlus.instance != null)
            {
                checker.ShowCheckerTitle();
                checker.ShowCheckResult();
            }
        }
    }

    public class ComponentDetailChecker : ObjectChecker
    {
        public class ComponentFieldDetail : ObjectDetail
        {
            public ComponentFieldDetail(Object obj, ComponentDetailChecker checker) : base(obj, checker)
            {

            }

            public override void InitDetailCheckObject(Object obj)
            {
                ComponentDetailChecker checker = currentChecker as ComponentDetailChecker;
                Component com = obj as Component;
                if (com == null)
                    return;

                AddOrSetCheckValue(checker.activeInHierarchyItem, com.gameObject.activeInHierarchy.ToString());
                AddOrSetCheckValue(checker.activeSelfItem, com.gameObject.activeSelf.ToString());
            }
        }

        public enum ReflectionType
        {
            Property,
            Field,
        }

        public class ReflectionItem
        {
            public string name;
            public bool enable;
            public ReflectionType type;

            public ReflectionItem(string n, bool e, ReflectionType t)
            {
                name = n;
                enable = e;
                type = t;
            }
        }

        //public CheckItem EnableItem;
        public CheckItem hasMissingPropertyItem;
        public CheckItem activeSelfItem;
        public CheckItem activeInHierarchyItem;

        private List<CheckItem> propertyItemList = new List<CheckItem>();
        public ComponentChecker.ComponentDetail componentDetail = null;
        public List<ReflectionItem> reflectionItemList = new List<ReflectionItem>();
        public BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic;

        private bool showProperty = true;
        private bool showField = true;
        //private bool ignoreObsoleteAttribute = true;
        private bool checkMissingProperty = false;
        private Vector2 scrollPos = Vector2.zero;

        public override void InitChecker()
        {
            checkerName = "ComponentDetail";
            activeItem.show = false;
            refItem.title = "根对象";
            totalRefItem.title = "组件对象";

            hasMissingPropertyItem = new CheckItem(this, "是否包含Missing引用");
            hasMissingPropertyItem.show = false;
            hasMissingPropertyItem.itemFlag = ItemFlag.NoCustomShow;

            memorySizeItem.show = false;

            activeInHierarchyItem = new CheckItem(this, "自身及根节点全部激活");
            activeSelfItem = new CheckItem(this, "仅自身节点是否激活");
        }

        public void AddComponentDetail(Object obj, GameObject rootObject)
        {
            var com = obj as Component;
            if (com == null)
                return;
            var detail = new ComponentFieldDetail(obj, this);
            detail.referenceObjectList.Add(rootObject);
            detail.detailReferenceList.Add(com.gameObject);
            var hasMissingProperty = "Unknown";
            if (checkMissingProperty == true)
            {
                hasMissingProperty = ResourceCheckerHelper.HasMissingProperty(com).ToString();
            }
            detail.AddOrSetCheckValue(hasMissingPropertyItem, hasMissingProperty);
        }

        public void GenerateCheckItem()
        {
            foreach (var v in reflectionItemList)
            {
                if (v.enable)
                {
                    var item = new CheckItem(this, v.name);
                    AddCustomCheckItem(item);

                    propertyItemList.Add(item);
                    if (v.type == ReflectionType.Field)
                        GetFieldValue(item);
                    else
                        GetPropertyValue(item);
                }
            }
            filterItem.RefreshFilterItems();
        }


        public void ClearComponentDetailChecker()
        {
            Clear();
            foreach (var v in propertyItemList)
            {
                customCheckItems.Remove(v);
                checkItemList.Remove(v);
            }
            propertyItemList.Clear();
        }

        public void CheckComponent()
        {
            ClearComponentDetailChecker();
            foreach (var v in componentDetail.componentList)
            {
                var com = v.Key;
                var go = v.Value as GameObject;
                AddComponentDetail(com, go);
            }
            GenerateCheckItem();
            RefreshCheckResult();
        }

        public void InitComponentDetailChecker(ComponentChecker.ComponentDetail detail)
        {
            componentDetail = detail;
            GenerateItem();
            CheckComponent();
        }

        public void GetPropertyValue(CheckItem item)
        {
            var proInfo = componentDetail.checkObject.GetType().GetProperty(item.title, bindingFlags);
            if (proInfo != null)
            {
                foreach (var v in CheckList)
                {
                    var value = proInfo.GetValue(v.checkObject, null);
                    if (value != null)
                        v.AddOrSetCheckValue(item, value.ToString());
                }
            }
        }

        public void GetFieldValue(CheckItem item)
        {
            var finfo = componentDetail.checkObject.GetType().GetField(item.title, bindingFlags);
            if (finfo != null)
            {
                foreach (var v in CheckList)
                {
                    var value = finfo.GetValue(v.checkObject);
                    if (value != null)
                        v.AddOrSetCheckValue(item, value.ToString());
                }
            }
        }

        public override void ShowOptionButton()
        {
            EditorGUI.BeginChangeCheck();
            checkMissingProperty = GUILayout.Toggle(checkMissingProperty, "检查Missing属性", GUILayout.Width(150));
            if (EditorGUI.EndChangeCheck())
            {
                hasMissingPropertyItem.show = checkMissingProperty;
                //RecaculateAllItemWidth();
            }
            base.ShowOptionButton();
        }

        public void ShowSideBar()
        {
            if (GUILayout.Button("检查组件属性"))
            {
                CheckComponent();
            }
            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            //ignoreObsoleteAttribute = GUILayout.Toggle(ignoreObsoleteAttribute, "忽略过时属性");
#if UNITY_2017_3_OR_NEWER
            bindingFlags = (BindingFlags)EditorGUILayout.EnumFlagsField("反射类型", bindingFlags);
#else
            bindingFlags = (BindingFlags)EditorGUILayout.EnumMaskField("反射类型", bindingFlags);
#endif
            if (EditorGUI.EndChangeCheck())
            {
                GenerateItem();
            }
            GUILayout.EndHorizontal();

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            showProperty = EditorGUILayout.Foldout(showProperty, "Property");
            if (showProperty)
            {
                for (var i = 0; i < reflectionItemList.Count; i++)
                {
                    var v = reflectionItemList[i];
                    if (v.type != ReflectionType.Property)
                        continue;
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(30);
                    v.enable = GUILayout.Toggle(v.enable, new GUIContent(v.name));
                    GUILayout.EndHorizontal();
                }
            }

            showField = EditorGUILayout.Foldout(showField, "Field");
            if (showField)
            {
                for (int i = 0; i < reflectionItemList.Count; i++)
                {
                    var v = reflectionItemList[i];
                    if (v.type != ReflectionType.Field)
                        continue;
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(30);
                    v.enable = GUILayout.Toggle(v.enable, new GUIContent(v.name));
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("全部开启"))
            {
                SetAllReflectionItem(true);
            }
            if (GUILayout.Button("全部关闭"))
            {
                SetAllReflectionItem(false);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();
        }

        private void SetAllReflectionItem(bool enable)
        {
            foreach (var v in reflectionItemList)
            {
                v.enable = enable;
            }
        }

        private void GenerateItem()
        {
            reflectionItemList.Clear();
            var type = componentDetail.checkObject.GetType();
            //var propertyList = type.GetProperties(bindingFlags).Select(x => x.Name).ToArray();
            //var fieldList = type.GetFields(bindingFlags).Select(x => x.Name).ToArray();
            //剔除过期属性
            var propertyList = type.GetProperties(bindingFlags)
                   .Where(x => x.GetCustomAttributes(typeof(System.ObsoleteAttribute), true).Length == 0)
                   .Select(x => x.Name)
                   .ToArray();

            var fieldList = type.GetFields(bindingFlags)
                   .Where(x => x.GetCustomAttributes(typeof(System.ObsoleteAttribute), true).Length == 0)
                   .Select(x => x.Name).
                   ToArray();

            for (var i = 0; i < propertyList.Length; i++)
            {
                reflectionItemList.Add(new ReflectionItem(propertyList[i], false, ReflectionType.Property));
            }
            for (var i = 0; i < fieldList.Length; i++)
            {
                reflectionItemList.Add(new ReflectionItem(fieldList[i], false, ReflectionType.Field));
            }
            HandleSpecialProperty();
        }

        private void HandleSpecialProperty()
        {
            foreach(var item in reflectionItemList)
            {
                if (item.name == "enabled")
                    item.enable = true;
            }
        }

    }
}
