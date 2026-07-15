
//------------------------------------------------------------------------------
// 设施改造项；与 facility_renovation 表对应
//------------------------------------------------------------------------------

using Luban;
using SimpleJSON;

namespace cfg.demo
{
    [System.Serializable]
    public sealed partial class FacilityRenovationConfig : Luban.BeanBase
    {
        public FacilityRenovationConfig(JSONNode _buf)
        {
            { if(!_buf["facility_id"].IsString) { throw new SerializationException(); }  FacilityId = _buf["facility_id"]; }
            { if(!_buf["renovation_id"].IsString) { throw new SerializationException(); }  RenovationId = _buf["renovation_id"]; }
            { if(!_buf["display_name"].IsString) { throw new SerializationException(); }  DisplayName = _buf["display_name"]; }
            { if(!_buf["desc"].IsString) { throw new SerializationException(); }  Desc = _buf["desc"]; }
            { if(!_buf["min_level"].IsNumber) { throw new SerializationException(); }  MinLevel = _buf["min_level"]; }
            { if(!_buf["sort_order"].IsNumber) { throw new SerializationException(); }  SortOrder = _buf["sort_order"]; }
            { var __json0 = _buf["unlock_conds"]; if(!__json0.IsArray) { throw new SerializationException(); } UnlockConds = new System.Collections.Generic.List<demo.CommonCheckCond>(__json0.Count); foreach(JSONNode __e0 in __json0.Children) { demo.CommonCheckCond __v0;  { if(!__e0.IsObject) { throw new SerializationException(); }  __v0 = global::cfg.demo.CommonCheckCond.DeserializeCommonCheckCond(__e0);  }  UnlockConds.Add(__v0); }   }
            { var __json0 = _buf["learn_costs"]; if(!__json0.IsArray) { throw new SerializationException(); } LearnCosts = new System.Collections.Generic.List<demo.TalentUnlockCost>(__json0.Count); foreach(JSONNode __e0 in __json0.Children) { demo.TalentUnlockCost __v0;  { if(!__e0.IsObject) { throw new SerializationException(); }  __v0 = global::cfg.demo.TalentUnlockCost.DeserializeTalentUnlockCost(__e0);  }  LearnCosts.Add(__v0); }   }
            { var __json0 = _buf["daily_outputs"]; if(!__json0.IsArray) { throw new SerializationException(); } DailyOutputs = new System.Collections.Generic.List<demo.TalentUnlockCost>(__json0.Count); foreach(JSONNode __e0 in __json0.Children) { demo.TalentUnlockCost __v0;  { if(!__e0.IsObject) { throw new SerializationException(); }  __v0 = global::cfg.demo.TalentUnlockCost.DeserializeTalentUnlockCost(__e0);  }  DailyOutputs.Add(__v0); }   }
        }

        public static FacilityRenovationConfig DeserializeFacilityRenovationConfig(JSONNode _buf)
        {
            return new demo.FacilityRenovationConfig(_buf);
        }

        public string FacilityId;
        public string RenovationId;
        public string DisplayName;
        public string Desc;
        public int MinLevel;
        public int SortOrder;
        public System.Collections.Generic.List<demo.CommonCheckCond> UnlockConds;
        public System.Collections.Generic.List<demo.TalentUnlockCost> LearnCosts;
        public System.Collections.Generic.List<demo.TalentUnlockCost> DailyOutputs;

        public const int __ID__ = -2026031501;
        public override int GetTypeId() => __ID__;

        public void ResolveRef(Tables tables)
        {
            foreach (var _e in UnlockConds) { _e?.ResolveRef(tables); }
            foreach (var _e in LearnCosts) { _e?.ResolveRef(tables); }
            foreach (var _e in DailyOutputs) { _e?.ResolveRef(tables); }
        }
    }
}
