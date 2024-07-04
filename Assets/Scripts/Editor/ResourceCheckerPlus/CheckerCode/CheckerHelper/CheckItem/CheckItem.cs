using System.Collections.Generic;

namespace ResourceCheckerPlus
{
    public enum CheckType
    {
        String,
        Int,
        Float,
        Custom,
        FormatSize,
        Texture,
        List,
        ListShowFirstItem,
        None,
        WarningLevel,
    }

    //控制检查项组件的一些属性
    public enum ItemFlag
    {
        Default             = 1 << 0,
        NoCustomShow        = 1 << 1,        //一些主要Item显示与否，不可自行配置是否显示
        NoCheckIfHide       = 1 << 2,        //如果不显示，不进行检查，主要用于ComponentDetailChecker
        FixWidth            = 1 << 3,        //检查项固定宽度，不进行自适应宽度调整
        CheckSummary        = 1 << 4,        //用于当前Checker检查的统计信息，不作为checkmap的key，每个checker仅一份数据
        SceneCheckInfo      = 1 << 5,        //用于场景批量检查信息统计数据 
    }

    /// <summary>
    /// 检查控件类，用于存储，显示，有待重构
    /// </summary>
    public delegate bool CustomFilter(object o);
    public delegate void CustomClickOption(ObjectDetail detail);
    public class CheckItem
    {
        public string title;                                                        //显示名称
        public int width = 80;                                                      //显示宽度
        public int defaultWidth = 80;                                               //默认宽度
        public CheckType type;                                                      //检查类型
        public CustomFilter customFilter;                                           //自定义筛选函数
        public CustomClickOption clickOption;                                       //点击操作
        public bool show = true;                                                    //是否显示
        public int order = 0;                                                       //显示顺序
        public bool sortSymbol = true;                                              //排序顺序
        public ItemFlag itemFlag = ItemFlag.Default;                                //检查项属性
        public object args1 = null;                                                 //自定义参数
        private ObjectChecker currentChecker = null;                                //Checker
        public bool showFirstItemList = false;                                      //显示List的第一个对象，List作为字符操作

        public ResourceRuleGroup resourceRuleGroup =  null;

        public CheckItem(ObjectChecker checker, string t, CheckType ty = CheckType.String, CustomClickOption option = null, CustomFilter f = null, ItemFlag flag = ItemFlag.Default)
        {
            title = t;
            type = ty;
            customFilter = f;
            clickOption = option;
            itemFlag = flag;
            currentChecker = checker;

            if ((itemFlag & ItemFlag.CheckSummary) == ItemFlag.CheckSummary || ((itemFlag & ItemFlag.SceneCheckInfo) == ItemFlag.SceneCheckInfo))
            {
                checker.checkResultDic.Add(this, null);
            }
            else
            {
                order = checker.checkItemList.Count;
                checker.checkItemList.Add(this);
            }
        }

        public static CheckItem CreateCheckItemFromConfig(ObjectChecker checker, CheckItemConfig cfg)
        {
            //先带着命名空间
            var type = System.Type.GetType("ResourceCheckerPlus." + cfg.ItemClassName);
            //如果为null，再不带命名空间试一下，防止有哥们忘了加命名空间
            if (type == null)
                type = System.Type.GetType(cfg.ItemClassName);
            if (type == null)
                return null;
            var item = System.Activator.CreateInstance(type, checker, cfg.ItemTitle) as CheckItem;
            return item;
        }

        public virtual object GetCheckValue(UnityEngine.Object obj)
        {
            return "null";
        }

        public void AutoCheckResource(ObjectDetail detail)
        {
            if (resourceRuleGroup == null)
                return;
            resourceRuleGroup.AutoCheckResource(detail);
        }

        public void InitAutoCheckSystem(ResourceRuleGroup ruleGroup)
        {
            resourceRuleGroup = null;
            resourceRuleGroup = ruleGroup;
            ruleGroup.Init(currentChecker, this);
        }

        public void SetAutoCheckConfig(List<ResourceTag> tags)
        {
            if (resourceRuleGroup == null)
                return;
            resourceRuleGroup.SetAutoCheckConfig(tags);
        }

        public void ClearAutoCheckSystem()
        {
            if (resourceRuleGroup == null)
                return;
            resourceRuleGroup.ClearResourceRuleGroup();
        }
    };
}