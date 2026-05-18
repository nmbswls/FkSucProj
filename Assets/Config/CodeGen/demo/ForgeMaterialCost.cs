
//------------------------------------------------------------------------------
// Hand-maintained (aligned with Luban export).
//------------------------------------------------------------------------------

using Luban;
using SimpleJSON;

namespace cfg.demo
{
    [System.Serializable]
    public sealed partial class ForgeMaterialCost : Luban.BeanBase
    {
        public ForgeMaterialCost(JSONNode _buf)
        {
            { if (!_buf["item_id"].IsString) { throw new SerializationException(); } ItemId = _buf["item_id"]; }
            { if (!_buf["count"].IsNumber) { throw new SerializationException(); } Count = _buf["count"]; }
        }

        public static ForgeMaterialCost DeserializeForgeMaterialCost(JSONNode _buf)
        {
            return new ForgeMaterialCost(_buf);
        }

        public string ItemId;
        public long Count;

        public const int __ID__ = -1827364519;
        public override int GetTypeId() => __ID__;

        public void ResolveRef(Tables tables)
        {
        }

        public override string ToString()
        {
            return "{ itemId:" + ItemId + ",count:" + Count + "}";
        }
    }
}
