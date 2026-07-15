using Luban;
using SimpleJSON;

namespace cfg.demo
{
public partial class TbFacilityDefinition
{
    readonly System.Collections.Generic.Dictionary<string, demo.FacilityDefinitionConfig> _dataMap;
    readonly System.Collections.Generic.List<demo.FacilityDefinitionConfig> _dataList;
    public TbFacilityDefinition(JSONNode buf)
    {
        _dataMap = new(buf.Count);
        _dataList = new(buf.Count);
        foreach (JSONNode ele in buf.Children)
        {
            var value = demo.FacilityDefinitionConfig.DeserializeFacilityDefinitionConfig(ele);
            _dataList.Add(value);
            _dataMap.Add(value.FacilityId, value);
        }
    }
    public System.Collections.Generic.Dictionary<string, demo.FacilityDefinitionConfig> DataMap => _dataMap;
    public System.Collections.Generic.List<demo.FacilityDefinitionConfig> DataList => _dataList;
    public demo.FacilityDefinitionConfig GetOrDefault(string key) => _dataMap.TryGetValue(key, out var value) ? value : default;
    public demo.FacilityDefinitionConfig Get(string key) => _dataMap[key];
    public demo.FacilityDefinitionConfig this[string key] => _dataMap[key];
    public void ResolveRef(Tables tables) { foreach (var value in _dataList) value.ResolveRef(tables); }
}
}
