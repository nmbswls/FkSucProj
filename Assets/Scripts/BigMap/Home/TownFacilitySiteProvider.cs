using My.Map;

using UnityEngine;



namespace My.Home

{

    // 与 MapScenePrefabProvider 同节点；编辑器校验设施信标配置。

    [RequireComponent(typeof(MapScenePrefabProvider))]

    public class TownFacilitySiteProvider : MonoBehaviour

    {

        public int SiteId;



#if UNITY_EDITOR

        void OnValidate()

        {

            var provider = GetComponent<MapScenePrefabProvider>();

            if (provider == null || string.IsNullOrWhiteSpace(provider.Key))

            {

                return;

            }



            if (SiteId <= 0)

            {

                Debug.LogWarning($"[TownFacilitySiteProvider] '{name}' missing SiteId.", this);

            }

        }

#endif

    }

}

