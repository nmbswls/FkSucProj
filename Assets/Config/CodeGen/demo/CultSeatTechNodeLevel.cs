using Luban;
using SimpleJSON;

namespace cfg.demo
{
    [System.Serializable]
    public sealed partial class CultSeatTechNodeLevel : Luban.BeanBase
    {
        public CultSeatTechNodeLevel(JSONNode buf)
        {
            if (!buf["node_id"].IsNumber) throw new SerializationException(); NodeId = buf["node_id"];
            if (!buf["level"].IsNumber) throw new SerializationException(); Level = buf["level"];
            var prereq = buf["prereq_node_ids"]; if (!prereq.IsArray) throw new SerializationException();
            PrereqNodeIds = new System.Collections.Generic.List<int>(prereq.Count);
            foreach (var item in prereq.Children) { if (!item.IsNumber) throw new SerializationException(); PrereqNodeIds.Add(item); }
            if (!buf["faith_cost"].IsNumber) throw new SerializationException(); FaithCost = buf["faith_cost"];
            if (!buf["effect_desc"].IsString) throw new SerializationException(); EffectDesc = buf["effect_desc"];
        }
        public static CultSeatTechNodeLevel DeserializeCultSeatTechNodeLevel(JSONNode buf) => new(buf);
        public int NodeId;
        public int Level;
        public System.Collections.Generic.List<int> PrereqNodeIds;
        public int FaithCost;
        public string EffectDesc;
        public const int __ID__ = 2140251043;
        public override int GetTypeId() => __ID__;
        public void ResolveRef(Tables tables) { }
    }
}
