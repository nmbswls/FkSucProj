//------------------------------------------------------------------------------
// Luban 风格：单条陷阱配置（主键 id 与逻辑层 CfgId 一致）
//------------------------------------------------------------------------------

using Luban;
using SimpleJSON;
using System.Collections.Generic;

namespace cfg.demo
{
    [System.Serializable]
    public sealed class TrapSpecRow : Luban.BeanBase
    {
        public TrapSpecRow(JSONNode _buf)
        {
            { if (!_buf["id"].IsString) { throw new SerializationException(); } Id = _buf["id"]; }
            { if (!_buf["trigger_radius"].IsNumber) { throw new SerializationException(); } TriggerRadius = _buf["trigger_radius"]; }
            { if (!_buf["camp_filter"].IsNumber) { throw new SerializationException(); } CampFilter = _buf["camp_filter"].AsInt; }
            { if (!_buf["only_player"].IsBoolean) { throw new SerializationException(); } OnlyPlayer = _buf["only_player"].AsBool; }
            { if (!_buf["post_trigger"].IsNumber) { throw new SerializationException(); } PostTrigger = _buf["post_trigger"].AsInt; }
            { if (!_buf["sleep_duration"].IsNumber) { throw new SerializationException(); } SleepDuration = _buf["sleep_duration"]; }
            {
                var __json0 = _buf["trigger_effects"];
                if (!__json0.IsArray) { throw new SerializationException(); }
                TriggerEffects = new List<TrapTriggerEffect>(__json0.Count);
                foreach (JSONNode __e0 in __json0.Children)
                {
                    TrapTriggerEffect __v0;
                    { if (!__e0.IsObject) { throw new SerializationException(); } __v0 = TrapTriggerEffect.DeserializeTrapTriggerEffect(__e0); }
                    TriggerEffects.Add(__v0);
                }
            }
        }

        public static TrapSpecRow DeserializeTrapSpecRow(JSONNode _buf)
        {
            return new TrapSpecRow(_buf);
        }

        public string Id;
        public float TriggerRadius;
        public int CampFilter;
        public bool OnlyPlayer;
        public int PostTrigger;
        public float SleepDuration;
        public List<TrapTriggerEffect> TriggerEffects;

        public const int __ID__ = 991100200;
        public override int GetTypeId() => __ID__;

        public void ResolveRef(Tables tables)
        {
        }

        public override string ToString()
        {
            return "{ id:" + Id + "}";
        }
    }
}
