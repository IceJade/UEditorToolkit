using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEditor.IMGUI.Controls;

public class ReferenceFinderWindow : EditorWindow
{
    //依赖模式的key
    const string isDependPrefKey = "ReferenceFinderData_IsDepend";

    private static ReferenceFinderData data = new ReferenceFinderData();
    private static bool initializedData = false;
    
    private bool isDepend = false;

    private bool needUpdateAssetTree = false;
    private bool initializedGUIStyle = false;
    //工具栏按钮样式
    private GUIStyle toolbarButtonGUIStyle;
    //工具栏样式
    private GUIStyle toolbarGUIStyle;

    private GUIStyle commonButtonStyle;
    private GUIStyle selectedButtonStyle;

    //选中资源列表
    private List<string> selectedAssetGuid = new List<string>();    

    private AssetTreeView m_AssetTreeView;

    [SerializeField]
    private TreeViewState m_TreeViewState;
    
    //[MenuItem("Assets/Reverse Find References", false, 26)]
    static private void Find()
    {
        EditorSettings.serializationMode = SerializationMode.ForceText;
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (!string.IsNullOrEmpty(path))
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            var withoutExtensions = new List<string>(){".prefab",".unity",".mat",".asset"};
            string[] files = Directory.GetFiles(Application.dataPath, "*.*", SearchOption.AllDirectories)
                .Where(s => withoutExtensions.Contains(Path.GetExtension(s).ToLower())).ToArray();
            int startIndex = 0;
 
            EditorApplication.update = delegate()
            {
                string file = files[startIndex];
            
                bool isCancel = EditorUtility.DisplayCancelableProgressBar("匹配资源中", file, (float)startIndex / (float)files.Length);
 
                if (Regex.IsMatch(File.ReadAllText(file), guid))
                {
                    Debug.Log(file, AssetDatabase.LoadAssetAtPath<Object>(GetRelativeAssetsPath(file)));
                }
 
                startIndex++;
                if (isCancel || startIndex >= files.Length)
                {
                    EditorUtility.ClearProgressBar();
                    EditorApplication.update = null;
                    startIndex = 0;
                    Debug.Log("匹配结束");
                }
 
            };
        }
    }
 
    static private string GetRelativeAssetsPath(string path)
    {
        return "Assets" + Path.GetFullPath(path).Replace(Path.GetFullPath(Application.dataPath), "").Replace('\\', '/');
    }

    //查找资源引用信息
    [MenuItem("Assets/Find References In Project %#&f", false, 25)]
    static void FindRef()
    {
        InitDataIfNeeded();
        OpenWindow();
        ReferenceFinderWindow window = GetWindow<ReferenceFinderWindow>();
        window.UpdateSelectedAssets();
    }
    
    //打开窗口
    //[MenuItem("Window/Reference Finder", false, 1000)]
    static void OpenWindow()
    {
        ReferenceFinderWindow window = GetWindow<ReferenceFinderWindow>();
        window.wantsMouseMove = false;
        window.titleContent = new GUIContent("资源引用查看器");
        window.Show();
        window.Focus();        
    }

    //初始化数据
    public static void InitDataIfNeeded()
    {
        if (!initializedData)
        {
            //初始化数据
            if(!data.ReadFromCache())
            {
                data.CollectDependenciesInfo();
            }
            initializedData = true;
        }
    }

    //初始化GUIStyle
    void InitGUIStyleIfNeeded()
    {
        if (!initializedGUIStyle)
        {
            toolbarButtonGUIStyle = new GUIStyle("ToolbarButton");
            toolbarGUIStyle = new GUIStyle("Toolbar");

            commonButtonStyle = new GUIStyle("ToolbarButton");
            commonButtonStyle.normal.background = GUI.skin.button.normal.background; // 恢复默认背景
            commonButtonStyle.normal.textColor = GUI.skin.button.normal.textColor; // 恢复默认文本颜色

            selectedButtonStyle = new GUIStyle("ToolbarButton");
            selectedButtonStyle.normal.background = EditorGUIUtility.whiteTexture; // 设置选中状态的背景
            selectedButtonStyle.normal.textColor = Color.black; // 设置选中状态的文本颜色

            initializedGUIStyle = true;
        }
    }
    
    //更新选中资源列表
    private void UpdateSelectedAssets()
    {
        selectedAssetGuid.Clear();
        foreach(var obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            //如果是文件夹
            if (Directory.Exists(path))
            {
                string[] folder = new string[] { path };
                //将文件夹下所有资源作为选择资源
                string[] guids = AssetDatabase.FindAssets(null, folder);
                foreach(var guid in guids)
                {
                    if (!selectedAssetGuid.Contains(guid) &&
                        !Directory.Exists(AssetDatabase.GUIDToAssetPath(guid)))
                    {
                        selectedAssetGuid.Add(guid);
                    }                        
                }
            }
            //如果是文件资源
            else
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                selectedAssetGuid.Add(guid);
            }
        }
        needUpdateAssetTree = true;
    }

    //通过选中资源列表更新TreeView
    private void UpdateAssetTree()
    {
        if (needUpdateAssetTree && selectedAssetGuid.Count > 0)
        {
            var root = SelectedAssetGuidToRootItem(selectedAssetGuid);
            if(m_AssetTreeView == null)
            {
                //初始化TreeView
                if (m_TreeViewState == null)
                    m_TreeViewState = new TreeViewState();
                var headerState = AssetTreeView.CreateDefaultMultiColumnHeaderState(position.width);
                var multiColumnHeader = new MultiColumnHeader(headerState);
                m_AssetTreeView = new AssetTreeView(m_TreeViewState, multiColumnHeader);
            }
            m_AssetTreeView.assetRoot = root;
            m_AssetTreeView.CollapseAll();
            m_AssetTreeView.Reload();
            needUpdateAssetTree = false;
        }
    }

    private void OnEnable()
    {
        isDepend = PlayerPrefs.GetInt(isDependPrefKey, 0) == 1;
    }

    private void OnGUI()
    {
        InitGUIStyleIfNeeded();
        DrawOptionBar();
        UpdateAssetTree();
        if (m_AssetTreeView != null)
        {
            //绘制Treeview
            m_AssetTreeView.OnGUI(new Rect(0, toolbarGUIStyle.fixedHeight, position.width, position.height - toolbarGUIStyle.fixedHeight));
        }        
    }
    
    //绘制上条
    public void DrawOptionBar()
    {
        EditorGUILayout.BeginHorizontal(toolbarGUIStyle);
        //刷新数据
        if (GUILayout.Button("刷新", toolbarButtonGUIStyle, GUILayout.Width(100)))
        {
            data.CollectDependenciesInfo();
            needUpdateAssetTree = true;
            EditorGUIUtility.ExitGUI();
        }

        // 被引用
        if (GUILayout.Button("被引用资源", isDepend ? commonButtonStyle : selectedButtonStyle, GUILayout.Width(100)))
        {
            OnModelSelect(false);
            EditorGUIUtility.ExitGUI();
        }
        // 依赖资源
        if (GUILayout.Button("依赖资源", !isDepend ? commonButtonStyle : selectedButtonStyle, GUILayout.Width(100)))
        {
            OnModelSelect(true);
            EditorGUIUtility.ExitGUI();
        }
        ////修改模式
        //bool PreIsDepend = isDepend;
        //isDepend = GUILayout.Toggle(isDepend, isDepend ? "引用" : "被引用", toolbarButtonGUIStyle,GUILayout.Width(100));
        //if(PreIsDepend != isDepend){
        //    OnModelSelect();
        //}
        GUILayout.FlexibleSpace();

        //扩展
        if (GUILayout.Button("展开", toolbarButtonGUIStyle))
        {
            if (m_AssetTreeView != null) m_AssetTreeView.ExpandAll();
        }
        //折叠
        if (GUILayout.Button("收起", toolbarButtonGUIStyle))
        {
            if (m_AssetTreeView != null) m_AssetTreeView.CollapseAll();
        }
        EditorGUILayout.EndHorizontal();
    }
    
    private void OnModelSelect(bool depend)
    {
        if (isDepend == depend) return;

        isDepend = depend;
        needUpdateAssetTree = true;
        PlayerPrefs.SetInt(isDependPrefKey, isDepend ? 1 : 0);
    }


    //生成root相关
    private HashSet<string> updatedAssetSet = new HashSet<string>();
    //通过选择资源列表生成TreeView的根节点
    private  AssetViewItem SelectedAssetGuidToRootItem(List<string> selectedAssetGuid)
    {
        updatedAssetSet.Clear();
        int elementCount = 0;
        var root = new AssetViewItem { id = elementCount, depth = -1, displayName = "Root", data = null };
        int depth = 0;
        var stack = new Stack<string>();
        foreach (var childGuid in selectedAssetGuid)
        {
            var child = CreateTree(childGuid, ref elementCount, depth, stack);
            if (child != null)
                root.AddChild(child);
        }
        updatedAssetSet.Clear();
        return root;
    }
    //通过每个节点的数据生成子节点
    private  AssetViewItem CreateTree(string guid, ref int elementCount, int _depth, Stack<string> stack)
    {
        if (stack.Contains(guid))
            return null;

        stack.Push(guid);
        data.UpdateAssetState(guid);    
        ++elementCount;
        var referenceData = data.assetDict[guid];
        var root = new AssetViewItem { id = elementCount, displayName = referenceData.name, data = referenceData, depth = _depth };
        var childGuids = isDepend ? referenceData.dependencies : referenceData.references;
        foreach (var childGuid in childGuids)
        {
            var child = CreateTree(childGuid, ref elementCount, _depth + 1, stack);
            if (child != null)
                root.AddChild(child);
        }

        stack.Pop();
        return root;
    }

    public static ReferenceFinderData.AssetDescription GetReferenceData(string guid)
    {
        InitDataIfNeeded();

        return data.assetDict[guid];
    }
}
