
//------------------------------------------------------------------------------
// 可派驻设施监工的角色表
//------------------------------------------------------------------------------

using Luban;
using SimpleJSON;

namespace cfg.demo
{
    public partial class TbCharacterFacilitySupervisor
    {
        readonly System.Collections.Generic.Dictionary<string, demo.CharacterFacilitySupervisorConfig> _dataMap;
        readonly System.Collections.Generic.List<demo.CharacterFacilitySupervisorConfig> _dataList;

        public TbCharacterFacilitySupervisor(JSONNode _buf)
        {
            int count = _buf.Count;
            _dataMap = new System.Collections.Generic.Dictionary<string, demo.CharacterFacilitySupervisorConfig>(count);
            _dataList = new System.Collections.Generic.List<demo.CharacterFacilitySupervisorConfig>(count);

            foreach (JSONNode _ele in _buf.Children)
            {
                demo.CharacterFacilitySupervisorConfig _v;
                { if(!_ele.IsObject) { throw new SerializationException(); }  _v = global::cfg.demo.CharacterFacilitySupervisorConfig.DeserializeCharacterFacilitySupervisorConfig(_ele);  }
                _dataList.Add(_v);
                _dataMap.Add(_v.CharacterKey, _v);
            }
        }

        public System.Collections.Generic.Dictionary<string, demo.CharacterFacilitySupervisorConfig> DataMap => _dataMap;
        public System.Collections.Generic.List<demo.CharacterFacilitySupervisorConfig> DataList => _dataList;

        public demo.CharacterFacilitySupervisorConfig GetOrDefault(string key) => _dataMap.TryGetValue(key, out var v) ? v : default;
        public demo.CharacterFacilitySupervisorConfig Get(string key) => _dataMap[key];

        public void ResolveRef(Tables tables)
        {
            foreach (var _v in _dataList)
            {
                _v.ResolveRef(tables);
            }
        }
    }
}
