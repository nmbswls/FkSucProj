using Luban;
using SimpleJSON;

namespace cfg.demo
{
[System.Serializable]
public sealed partial class FacilityDefinitionConfig : Luban.BeanBase
{
    public FacilityDefinitionConfig(JSONNode buf)
    {
        if (!buf["facility_id"].IsString) throw new SerializationException();
        FacilityId = buf["facility_id"];
        if (!buf["display_name"].IsString) throw new SerializationException();
        DisplayName = buf["display_name"];
        if (!buf["capabilities"].IsNumber) throw new SerializationException();
        Capabilities = buf["capabilities"];
        if (!buf["max_workforce"].IsNumber) throw new SerializationException();
        MaxWorkforce = buf["max_workforce"];
        if (!buf["presentation_prefab"].IsString) throw new SerializationException();
        PresentationPrefab = buf["presentation_prefab"];
        if (!buf["interaction_handler_id"].IsString) throw new SerializationException();
        InteractionHandlerId = buf["interaction_handler_id"];
    }

    public static FacilityDefinitionConfig DeserializeFacilityDefinitionConfig(JSONNode buf) => new(buf);
    public string FacilityId;
    public string DisplayName;
    public int Capabilities;
    public int MaxWorkforce;
    public string PresentationPrefab;
    public string InteractionHandlerId;
    public const int __ID__ = 193847201;
    public override int GetTypeId() => __ID__;
    public void ResolveRef(Tables tables) { }
}
}
