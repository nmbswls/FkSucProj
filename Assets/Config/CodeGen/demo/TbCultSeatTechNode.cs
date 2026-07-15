using Luban;
using SimpleJSON;

namespace cfg.demo
{
    public partial class TbCultSeatTechNode
    {
        readonly System.Collections.Generic.Dictionary<int, demo.CultSeatTechNode> _dataMap;
        readonly System.Collections.Generic.List<demo.CultSeatTechNode> _dataList;
        public TbCultSeatTechNode(JSONNode buf)
        {
            _dataMap = new(buf.Count); _dataList = new(buf.Count);
            foreach (JSONNode element in buf.Children)
            {
                var value = demo.CultSeatTechNode.DeserializeCultSeatTechNode(element);
                _dataList.Add(value); _dataMap.Add(value.NodeId, value);
            }
        }
        public System.Collections.Generic.Dictionary<int, demo.CultSeatTechNode> DataMap => _dataMap;
        public System.Collections.Generic.List<demo.CultSeatTechNode> DataList => _dataList;
        public demo.CultSeatTechNode GetOrDefault(int key) => _dataMap.TryGetValue(key, out var value) ? value : default;
        public demo.CultSeatTechNode Get(int key) => _dataMap[key];
        public void ResolveRef(Tables tables) { foreach (var value in _dataList) value.ResolveRef(tables); }
    }
}
