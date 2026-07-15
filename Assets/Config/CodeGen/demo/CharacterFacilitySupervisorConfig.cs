
//------------------------------------------------------------------------------
// 可派驻设施监工的角色配置
//------------------------------------------------------------------------------

using Luban;
using SimpleJSON;

namespace cfg.demo
{
    [System.Serializable]
    public sealed partial class CharacterFacilitySupervisorConfig : Luban.BeanBase
    {
        public CharacterFacilitySupervisorConfig(JSONNode _buf)
        {
            { if(!_buf["character_key"].IsString) { throw new SerializationException(); }  CharacterKey = _buf["character_key"]; }
            { if(!_buf["can_assign_supervisor"].IsBoolean) { throw new SerializationException(); }  CanAssignSupervisor = _buf["can_assign_supervisor"]; }
            { if(!_buf["sort_order"].IsNumber) { throw new SerializationException(); }  SortOrder = _buf["sort_order"]; }
            { if(!_buf["display_title"].IsString) { throw new SerializationException(); }  DisplayTitle = _buf["display_title"]; }
        }

        public static CharacterFacilitySupervisorConfig DeserializeCharacterFacilitySupervisorConfig(JSONNode _buf)
        {
            return new demo.CharacterFacilitySupervisorConfig(_buf);
        }

        public string CharacterKey;
        public bool CanAssignSupervisor;
        public int SortOrder;
        public string DisplayTitle;

        public const int __ID__ = -2026071521;
        public override int GetTypeId() => __ID__;

        public void ResolveRef(Tables tables)
        {
        }
    }
}
