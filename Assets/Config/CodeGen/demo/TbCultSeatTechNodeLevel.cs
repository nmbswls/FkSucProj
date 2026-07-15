using Luban;
using SimpleJSON;

namespace cfg.demo
{
    public partial class TbCultSeatTechNodeLevel
    {
        readonly System.Collections.Generic.Dictionary<(int, int), demo.CultSeatTechNodeLevel> _dataMap;
        readonly System.Collections.Generic.List<demo.CultSeatTechNodeLevel> _dataList;
        public TbCultSeatTechNodeLevel(JSONNode buf)
        {
            _dataMap = new(buf.Count); _dataList = new(buf.Count);
            foreach (JSONNode element in buf.Children)
            {
                var value = demo.CultSeatTechNodeLevel.DeserializeCultSeatTechNodeLevel(element);
                _dataList.Add(value); _dataMap.Add((value.NodeId, value.Level), value);
            }
        }
        public System.Collections.Generic.List<demo.CultSeatTechNodeLevel> DataList => _dataList;
        public demo.CultSeatTechNodeLevel Get(int nodeId, int level) => _dataMap.TryGetValue((nodeId, level), out var value) ? value : default;
        public void ResolveRef(Tables tables) { foreach (var value in _dataList) value.ResolveRef(tables); }
    }
}
