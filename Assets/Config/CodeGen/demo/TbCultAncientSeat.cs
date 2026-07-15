using Luban;
using SimpleJSON;

namespace cfg.demo
{
    public partial class TbCultAncientSeat
    {
        readonly System.Collections.Generic.Dictionary<int, demo.CultAncientSeat> _dataMap;
        readonly System.Collections.Generic.List<demo.CultAncientSeat> _dataList;

        public TbCultAncientSeat(JSONNode buf)
        {
            _dataMap = new(buf.Count);
            _dataList = new(buf.Count);
            foreach (JSONNode element in buf.Children)
            {
                var value = demo.CultAncientSeat.DeserializeCultAncientSeat(element);
                _dataList.Add(value);
                _dataMap.Add(value.SeatId, value);
            }
        }

        public System.Collections.Generic.Dictionary<int, demo.CultAncientSeat> DataMap => _dataMap;
        public System.Collections.Generic.List<demo.CultAncientSeat> DataList => _dataList;
        public demo.CultAncientSeat GetOrDefault(int key) => _dataMap.TryGetValue(key, out var value) ? value : default;
        public demo.CultAncientSeat Get(int key) => _dataMap[key];
        public void ResolveRef(Tables tables) { foreach (var value in _dataList) value.ResolveRef(tables); }
    }
}
