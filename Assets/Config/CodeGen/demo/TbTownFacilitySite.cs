
//------------------------------------------------------------------------------
// 地图设施站点表
//------------------------------------------------------------------------------

using Luban;
using SimpleJSON;

namespace cfg.demo
{
    public partial class TbTownFacilitySite
    {
        readonly System.Collections.Generic.Dictionary<int, demo.TownFacilitySiteConfig> _dataMap;
        readonly System.Collections.Generic.List<demo.TownFacilitySiteConfig> _dataList;

        public TbTownFacilitySite(JSONNode _buf)
        {
            int count = _buf.Count;
            _dataMap = new System.Collections.Generic.Dictionary<int, demo.TownFacilitySiteConfig>(count);
            _dataList = new System.Collections.Generic.List<demo.TownFacilitySiteConfig>(count);

            foreach (JSONNode _ele in _buf.Children)
            {
                demo.TownFacilitySiteConfig _v;
                { if(!_ele.IsObject) { throw new SerializationException(); }  _v = global::cfg.demo.TownFacilitySiteConfig.DeserializeTownFacilitySiteConfig(_ele);  }
                _dataList.Add(_v);
                _dataMap.Add(_v.Id, _v);
            }
        }

        public System.Collections.Generic.Dictionary<int, demo.TownFacilitySiteConfig> DataMap => _dataMap;
        public System.Collections.Generic.List<demo.TownFacilitySiteConfig> DataList => _dataList;

        public demo.TownFacilitySiteConfig GetOrDefault(int key) => _dataMap.TryGetValue(key, out var v) ? v : default;
        public demo.TownFacilitySiteConfig Get(int key) => _dataMap[key];

        public void ResolveRef(Tables tables)
        {
            foreach (var _v in _dataList)
            {
                _v.ResolveRef(tables);
            }
        }
    }
}
