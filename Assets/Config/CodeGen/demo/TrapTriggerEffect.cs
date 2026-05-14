//------------------------------------------------------------------------------
// Luban 风格手工表行：trap 触发效果（与 Luban 导出格式一致，便于日后接 Excel 再生成覆盖）
//------------------------------------------------------------------------------

using Luban;
using SimpleJSON;

namespace cfg.demo
{
    [System.Serializable]
    public sealed class TrapTriggerEffect : Luban.BeanBase
    {
        public const int KindAddBuff = 0;

        public TrapTriggerEffect(JSONNode _buf)
        {
            { if (!_buf["kind"].IsNumber) { throw new SerializationException(); } Kind = _buf["kind"].AsInt; }
            { if (!_buf["buff_id"].IsString) { throw new SerializationException(); } BuffId = _buf["buff_id"]; }
            { if (!_buf["layer"].IsNumber) { throw new SerializationException(); } Layer = _buf["layer"].AsInt; }
            { if (!_buf["duration"].IsNumber) { throw new SerializationException(); } Duration = _buf["duration"]; }
            { if (!_buf["target_type"].IsNumber) { throw new SerializationException(); } TargetType = _buf["target_type"].AsInt; }
        }

        public static TrapTriggerEffect DeserializeTrapTriggerEffect(JSONNode _buf)
        {
            return new TrapTriggerEffect(_buf);
        }

        public int Kind;
        public string BuffId;
        public int Layer;
        public float Duration;
        public int TargetType;

        public const int __ID__ = 991100100;
        public override int GetTypeId() => __ID__;

        public void ResolveRef(Tables tables)
        {
        }

        public override string ToString()
        {
            return "{ kind:" + Kind + ",buffId:" + BuffId + "}";
        }
    }
}
