using My.MapExport;
using UnityEditor;
using UnityEngine;

public static class DynamicEntityRefreshInfoExportUtil
{
    class CloneHolder : ScriptableObject
    {
        public DynamicEntityRefreshInfo RefreshInfo;
    }

    // 深拷贝 RefreshInfo（含 SerializeReference InitInfo / Variables），避免场景引用写入 asset 时丢失。
    public static DynamicEntityRefreshInfo CloneForExport(DynamicEntityRefreshInfo source)
    {
        if (source == null)
        {
            return null;
        }

        var holder = ScriptableObject.CreateInstance<CloneHolder>();
        holder.RefreshInfo = source;
        var clonedHolder = Object.Instantiate(holder);
        var result = clonedHolder.RefreshInfo;
        Object.DestroyImmediate(holder);
        Object.DestroyImmediate(clonedHolder);
        return result;
    }
}
