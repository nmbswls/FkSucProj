using Luban;
using SimpleJSON;

namespace cfg.demo
{
    [System.Serializable]
    public sealed partial class CultSeatTechNode : Luban.BeanBase
    {
        public CultSeatTechNode(JSONNode buf)
        {
            if (!buf["node_id"].IsNumber) throw new SerializationException(); NodeId = buf["node_id"];
            if (!buf["seat_id"].IsNumber) throw new SerializationException(); SeatId = buf["seat_id"];
            if (!buf["display_name"].IsString) throw new SerializationException(); DisplayName = buf["display_name"];
            if (!buf["desc"].IsString) throw new SerializationException(); Desc = buf["desc"];
            if (!buf["max_level"].IsNumber) throw new SerializationException(); MaxLevel = buf["max_level"];
            if (!buf["ring"].IsNumber) throw new SerializationException(); Ring = buf["ring"];
            if (!buf["angle_deg"].IsNumber) throw new SerializationException(); AngleDeg = buf["angle_deg"];
            if (!buf["icon_path"].IsString) throw new SerializationException(); IconPath = buf["icon_path"];
        }
        public static CultSeatTechNode DeserializeCultSeatTechNode(JSONNode buf) => new(buf);
        public int NodeId;
        public int SeatId;
        public string DisplayName;
        public string Desc;
        public int MaxLevel;
        public int Ring;
        public int AngleDeg;
        public string IconPath;
        public const int __ID__ = 2140251042;
        public override int GetTypeId() => __ID__;
        public void ResolveRef(Tables tables) { }
    }
}
