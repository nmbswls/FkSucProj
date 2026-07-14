using System.Collections.Generic;
using cfg.demo;

namespace My.Config
{
    public static class EventGrantCatalog
    {
        public static EventGrant Get(string id)
        {
            if (string.IsNullOrEmpty(id) || CfgMgr.Cfgs?.TbEventGrant == null)
            {
                return null;
            }

            return CfgMgr.Cfgs.TbEventGrant.GetOrDefault(id);
        }

        public static IReadOnlyList<EventGrant> All
        {
            get
            {
                var list = CfgMgr.Cfgs?.TbEventGrant?.DataList;
                return list ?? (IReadOnlyList<EventGrant>)System.Array.Empty<EventGrant>();
            }
        }
    }
}
