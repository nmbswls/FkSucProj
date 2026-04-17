#if UNITY_EDITOR
using UnityEditor;

namespace My.EditorTools
{
    public static class ForceRefreshAssetsMenu
    {
        [MenuItem("Tools/My/Refresh AssetDatabase")]
        public static void RefreshAssetDatabase()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }
    }
}
#endif
