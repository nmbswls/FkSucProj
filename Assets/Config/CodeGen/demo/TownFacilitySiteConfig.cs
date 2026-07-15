
//------------------------------------------------------------------------------
// 地图设施站点：map_id + slot 绑定跨图共享设施 cfg
//------------------------------------------------------------------------------

using Luban;
using SimpleJSON;

namespace cfg.demo
{
    [System.Serializable]
    public sealed partial class TownFacilitySiteConfig : Luban.BeanBase
    {
        public TownFacilitySiteConfig(JSONNode _buf)
        {
            { if(!_buf["id"].IsNumber) { throw new SerializationException(); }  Id = _buf["id"]; }
            { if(!_buf["map_id"].IsString) { throw new SerializationException(); }  MapId = _buf["map_id"]; }
            { if(!_buf["slot"].IsString) { throw new SerializationException(); }  Slot = _buf["slot"]; }
            { if(!_buf["facility_cfg_id"].IsString) { throw new SerializationException(); }  FacilityCfgId = _buf["facility_cfg_id"]; }
            { if(!_buf["sort_order"].IsNumber) { throw new SerializationException(); }  SortOrder = _buf["sort_order"]; }
        }

        public static TownFacilitySiteConfig DeserializeTownFacilitySiteConfig(JSONNode _buf)
        {
            return new demo.TownFacilitySiteConfig(_buf);
        }

        public int Id;
        public string MapId;
        public string Slot;
        public string FacilityCfgId;
        public int SortOrder;

        public const int __ID__ = -2026071502;
        public override int GetTypeId() => __ID__;

        public void ResolveRef(Tables tables)
        {
        }
    }
}
