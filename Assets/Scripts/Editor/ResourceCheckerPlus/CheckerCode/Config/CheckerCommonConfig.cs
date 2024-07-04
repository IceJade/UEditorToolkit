using UnityEngine;

namespace ResourceCheckerPlus
{
    /// <summary>
    /// 配置信息类，ScriptableObject与Mono脚本一样，要求类名与文件名一致，否则重启Unity后会丢失脚本
    /// </summary>
    public class CheckerCommonConfig : ScriptableObject
    {
        public Color selectItemColor = Color.green;                                                     
        public Color warningItemColor = Color.yellow;
        public Color errorItemColor = Color.red;
        public int sideBarWidth = 250;
        public bool autoFilterOnSideBarButtonClick = false;                                  //在侧边栏点击时自动进行引用筛选
        public bool clearFilterOnReCheck = true;                                             //在重新检查时是否清除筛选条件
        public bool showDocument = true;                                                     //显示Resource Checker Plus内置使用文档
        public BatchOptionSelection batchOptionType = BatchOptionSelection.AllInFilterList;  //进行批量移动或修改格式的操作是针对全列表还是选中列表
        public CheckInputMode inputType = CheckInputMode.DragMode;                           //检查模式：拖入检查或选中检查
        public bool enableEditResourceRule = false;                                          //是否可以编辑资源规则，项目中制订资源标准的同学打开此选项
        public string checkResultExportPath = "";
        public int maxCheckRecordCount = 8;
        public float maxMemoryCache = 4.0f;                                                  //连续多次检查可能导致内存过大，内存超过该值后检查前清理废弃资源

        public bool enableAutoResourceCheck = true;                                          //开启自动资源检查
    }
}
