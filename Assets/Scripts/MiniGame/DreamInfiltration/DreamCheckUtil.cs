using cfg.demo;
using SimpleJSON;

namespace My.MiniGame.Dream
{
    // 将 Serializable 条件转为 Luban CommonCheckCond，供 GameLogicManager.CheckCommonCond 使用
    public static class DreamCheckUtil
    {
        public static CommonCheckCond ToCommonCheckCond(DreamUnlockCondRow row)
        {
            var n = new JSONObject
            {
                ["type"] = (int)row.Type,
                ["param1"] = row.Param1,
                ["param2"] = row.Param2,
                ["param3"] = row.Param3,
                ["param4"] = row.Param4,
                ["param5"] = row.Param5 ?? "",
                ["param6"] = row.Param6 ?? "",
            };
            return CommonCheckCond.DeserializeCommonCheckCond(n);
        }
    }
}
