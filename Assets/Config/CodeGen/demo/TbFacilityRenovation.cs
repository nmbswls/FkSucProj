
//------------------------------------------------------------------------------
// 设施改造项
//------------------------------------------------------------------------------

using Luban;
using SimpleJSON;

namespace cfg.demo
{
    public partial class TbFacilityRenovation
    {
        readonly System.Collections.Generic.List<demo.FacilityRenovationConfig> _dataList;
        System.Collections.Generic.Dictionary<(string, string), demo.FacilityRenovationConfig> _dataMapUnion;

        public TbFacilityRenovation(JSONNode _buf)
        {
            int count = _buf.Count;
            _dataList = new System.Collections.Generic.List<demo.FacilityRenovationConfig>(count);

            foreach (JSONNode _ele in _buf.Children)
            {
                demo.FacilityRenovationConfig _v;
                { if(!_ele.IsObject) { throw new SerializationException(); }  _v = global::cfg.demo.FacilityRenovationConfig.DeserializeFacilityRenovationConfig(_ele);  }
                _dataList.Add(_v);
            }

            _dataMapUnion = new System.Collections.Generic.Dictionary<(string, string), demo.FacilityRenovationConfig>();
            foreach (var _v in _dataList)
            {
                _dataMapUnion.Add((_v.FacilityId, _v.RenovationId), _v);
            }
        }

        public System.Collections.Generic.List<demo.FacilityRenovationConfig> DataList => _dataList;

        public demo.FacilityRenovationConfig Get(string facility_id, string renovation_id) =>
            _dataMapUnion.TryGetValue((facility_id, renovation_id), out demo.FacilityRenovationConfig __v) ? __v : default;

        public void ResolveRef(Tables tables)
        {
            foreach (var _v in _dataList)
            {
                _v.ResolveRef(tables);
            }
        }
    }
}
