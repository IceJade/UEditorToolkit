using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ResourceCheckerPlus
{
    public class CheckerConfig : ScriptableObject
    {
        public CheckItemConfig[] checkItemCfg;
        public FilterItemCfgGroup[] predefineFilter;
        public int defaultFilterIndex = 0;

        public void SerilizeItemCfg(CheckItem item)
        {
            if (checkItemCfg == null || checkItemCfg.Length == 0)
                return;
            var cfg = checkItemCfg.FirstOrDefault(x => x.ItemTitle == item.title);
            if (cfg == null)
                return;
            if ((item.itemFlag & ItemFlag.NoCustomShow) == 0)
            {
                cfg.show = item.show;
            }
            cfg.order = item.order;
        }

        public void LoadItemCfg(CheckItem item)
        {
            if (checkItemCfg == null || checkItemCfg.Length == 0)
                return;
            var cfg = checkItemCfg.FirstOrDefault(x => x.ItemTitle == item.title);
            if (cfg == null)
                return;
            if ((item.itemFlag & ItemFlag.NoCustomShow) == 0)
            {
                item.show = cfg.show;
            }
            item.order = cfg.order;
        }
    }
}
