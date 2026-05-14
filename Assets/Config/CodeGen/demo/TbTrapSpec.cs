//------------------------------------------------------------------------------
// Luban 风格：TbTrapSpec
//------------------------------------------------------------------------------

using Luban;
using SimpleJSON;
using System.Collections.Generic;

namespace cfg.demo
{
    public sealed class TbTrapSpec
    {
        private readonly Dictionary<string, TrapSpecRow> _dataMap;
        private readonly List<TrapSpecRow> _dataList;

        public TbTrapSpec(JSONNode _buf)
        {
            int count = _buf.Count;
            _dataMap = new Dictionary<string, TrapSpecRow>(count);
            _dataList = new List<TrapSpecRow>(count);

            foreach (JSONNode _ele in _buf.Children)
            {
                TrapSpecRow _v;
                { if (!_ele.IsObject) { throw new SerializationException(); } _v = TrapSpecRow.DeserializeTrapSpecRow(_ele); }
                _dataList.Add(_v);
                _dataMap.Add(_v.Id, _v);
            }
        }

        public Dictionary<string, TrapSpecRow> DataMap => _dataMap;
        public List<TrapSpecRow> DataList => _dataList;

        public TrapSpecRow GetOrDefault(string key) => _dataMap.TryGetValue(key, out var v) ? v : default;
        public TrapSpecRow Get(string key) => _dataMap[key];
        public TrapSpecRow this[string key] => _dataMap[key];

        public void ResolveRef(Tables tables)
        {
            foreach (var _v in _dataList)
            {
                _v.ResolveRef(tables);
            }
        }
    }
}
