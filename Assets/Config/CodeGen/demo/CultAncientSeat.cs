using Luban;
using SimpleJSON;
using System.Collections.Generic;

namespace cfg.demo
{
    [System.Serializable]
    public sealed partial class CultAncientSeat : Luban.BeanBase
    {
        public CultAncientSeat(JSONNode buf)
        {
            if (!buf["seat_id"].IsNumber) throw new SerializationException(); SeatId = buf["seat_id"];
            if (!buf["display_name"].IsString) throw new SerializationException(); DisplayName = buf["display_name"];
            if (!buf["title"].IsString) throw new SerializationException(); Title = buf["title"];
            if (!buf["desc"].IsString) throw new SerializationException(); Desc = buf["desc"];
            if (!buf["state_desc"].IsString) throw new SerializationException(); StateDesc = buf["state_desc"];
            if (!buf["unlock_faith_cost"].IsNumber) throw new SerializationException(); UnlockFaithCost = buf["unlock_faith_cost"];
            if (!buf["default_unlocked"].IsBoolean) throw new SerializationException(); DefaultUnlocked = buf["default_unlocked"];
            var __json0 = buf["unlock_conds"]; if (!__json0.IsArray) throw new SerializationException(); UnlockConds = new List<CommonCheckCond>(__json0.Count); foreach (JSONNode __e0 in __json0.Children) { if (!__e0.IsObject) throw new SerializationException(); UnlockConds.Add(CommonCheckCond.DeserializeCommonCheckCond(__e0)); }
        }

        public static CultAncientSeat DeserializeCultAncientSeat(JSONNode buf) => new(buf);
        public int SeatId;
        public string DisplayName;
        public string Title;
        public string Desc;
        public string StateDesc;
        public int UnlockFaithCost;
        public bool DefaultUnlocked;
        public List<CommonCheckCond> UnlockConds;
        public const int __ID__ = 2140251041;
        public override int GetTypeId() => __ID__;
        public void ResolveRef(Tables tables) { }
    }
}
