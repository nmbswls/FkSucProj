
//------------------------------------------------------------------------------
// Hand-maintained (aligned with Luban export).
//------------------------------------------------------------------------------

using Luban;
using SimpleJSON;

namespace cfg.demo
{
    public partial class TbForgeRecipe
    {
        private readonly System.Collections.Generic.Dictionary<int, ForgeRecipe> _dataMap;
        private readonly System.Collections.Generic.List<ForgeRecipe> _dataList;

        public TbForgeRecipe(JSONNode _buf)
        {
            int count = _buf.Count;
            _dataMap = new System.Collections.Generic.Dictionary<int, ForgeRecipe>(count);
            _dataList = new System.Collections.Generic.List<ForgeRecipe>(count);

            foreach (JSONNode _ele in _buf.Children)
            {
                ForgeRecipe _v;
                { if (!_ele.IsObject) { throw new SerializationException(); } _v = ForgeRecipe.DeserializeForgeRecipe(_ele); }
                _dataList.Add(_v);
                _dataMap.Add(_v.Id, _v);
            }
        }

        public System.Collections.Generic.Dictionary<int, ForgeRecipe> DataMap => _dataMap;
        public System.Collections.Generic.List<ForgeRecipe> DataList => _dataList;

        public ForgeRecipe GetOrDefault(int key) => _dataMap.TryGetValue(key, out var v) ? v : default;
        public ForgeRecipe Get(int key) => _dataMap[key];
        public ForgeRecipe this[int key] => _dataMap[key];

        public void ResolveRef(Tables tables)
        {
            foreach (var _v in _dataList)
            {
                _v.ResolveRef(tables);
            }
        }
    }
}
