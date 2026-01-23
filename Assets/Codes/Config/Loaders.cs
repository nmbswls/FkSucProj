using Config.Map;
using Config.Unit;
using My;
using My.Config;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Config
{

    public static class MapLootPointConfigLoader
    {

        private static Dictionary<string, MapLootPointConfig> _byId = new Dictionary<string, MapLootPointConfig>();

        public static MapLootPointConfig Get(string cfgId)
        {
            if (_byId.TryGetValue(cfgId, out var data))
                return data;

            var loadOne = Load(cfgId);
            _byId[cfgId] = loadOne;
            return loadOne;
        }


        private static MapLootPointConfig Load(string cfgId)
        {
            var data = Resources.Load<MapLootPointConfig>($"Config/Entity/LootPoint/{cfgId}");
            if (data == null)
                Debug.LogError($"MapNpcConfigLoader not found at Resources/Config/Entity/LootPoint/{cfgId}");
            return data;
        }

        public static void Clear()
        {
            _byId.Clear();
        }
    }


    public static class MapNpcConfigLoader
    {

        private static Dictionary<string, MapNpcConfig> _byId = new Dictionary<string, MapNpcConfig>();

        public static MapNpcConfig Get(string cfgId)
        {
            if (_byId.TryGetValue(cfgId, out var data))
                return data;

            var loadOne = Load(cfgId);
            _byId[cfgId] = loadOne;
            return loadOne;
        }


        // 传入名称，如 "Fireball"；路径相对 Resources 根（不含扩展名）
        private static MapNpcConfig Load(string cfgId)
        {
            var data = Resources.Load<MapNpcConfig>($"Config/Entity/Npc/{cfgId}");
            if (data == null)
                Debug.LogError($"MapNpcConfigLoader not found at Resources/Config/Npc/{cfgId}");
            return data;
        }

        public static void Clear()
        {
            _byId.Clear();
        }
    }

    
    public static class MapUnitStrategyTemplateLoader
    {

        private static Dictionary<string, MapUnitStrategyTemplate> _byId = new Dictionary<string, MapUnitStrategyTemplate>();

        public static MapUnitStrategyTemplate Get(string cfgId)
        {
            if (_byId.TryGetValue(cfgId, out var data))
                return data;

            var loadOne = Load(cfgId);
            _byId[cfgId] = loadOne;
            return loadOne;
        }


        // 传入名称，如 "Fireball"；路径相对 Resources 根（不含扩展名）
        private static MapUnitStrategyTemplate Load(string cfgId)
        {
            var data = Resources.Load<MapUnitStrategyTemplate>($"Config/Unit/Strategy/{cfgId}");
            if (data == null)
                Debug.LogError($"MapUnitStrategyTemplateLoader not found at Resources/Config/Unit/Strategy/{cfgId}");
            return data;
        }

        public static void Clear()
        {
            _byId.Clear();
        }
    }

    public static class MapAreaEffectLoader
    {

        private static Dictionary<string, MapAreaEffectConfig> _byId = new Dictionary<string, MapAreaEffectConfig>();

        public static MapAreaEffectConfig Get(string cfgId)
        {
            if (_byId.TryGetValue(cfgId, out var data))
                return data;

            var loadOne = Load(cfgId);
            _byId[cfgId] = loadOne;
            return loadOne;
        }


        private static MapAreaEffectConfig Load(string cfgId)
        {
            var data = Resources.Load<MapAreaEffectConfig>($"Config/Entity/AreaEffect/{cfgId}");
            if (data == null)
                Debug.LogError($"MapAreaEffectLoader not found at Resources/Config/Entity/AreaEffect/{cfgId}");
            return data;
        }

        public static void Clear()
        {
            _byId.Clear();
        }
    }

    public static class MapInteractPointLoader
    {

        private static Dictionary<string, MapInteractPointConfig> _byId = new Dictionary<string, MapInteractPointConfig>();

        public static MapInteractPointConfig Get(string cfgId)
        {
            if (_byId.TryGetValue(cfgId, out var data))
                return data;

            var loadOne = Load(cfgId);
            _byId[cfgId] = loadOne;
            return loadOne;
        }


        private static MapInteractPointConfig Load(string cfgId)
        {
            var data = Resources.Load<MapInteractPointConfig>($"Config/Entity/InteractPoint/{cfgId}");
            if (data == null)
                Debug.LogError($"MapInteractPointLoader not found at Resources/Config/Entity/InteractPoint/{cfgId}");
            return data;
        }

        public static void Clear()
        {
            _byId.Clear();
        }
    }

    public static class MapDestoryObjCfgtLoader
    {

        private static Dictionary<string, MapDestoryObjConfig> _byId = new Dictionary<string, MapDestoryObjConfig>();

        public static MapDestoryObjConfig Get(string cfgId)
        {
            if (_byId.TryGetValue(cfgId, out var data))
                return data;

            var loadOne = Load(cfgId);
            _byId[cfgId] = loadOne;
            return loadOne;
        }


        private static MapDestoryObjConfig Load(string cfgId)
        {
            var data = Resources.Load<MapDestoryObjConfig>($"Config/Entity/DestroyObj/{cfgId}");
            if (data == null)
                Debug.LogError($"MapAreaEffectLoader not found at Resources/Config/Entity/DestroyObj/{cfgId}");
            return data;
        }

        public static void Clear()
        {
            _byId.Clear();
        }
    }

    public static class GatherPointCfgtLoader
    {

        private static Dictionary<string, GatherPointConfig> _byId = new Dictionary<string, GatherPointConfig>();

        public static GatherPointConfig Get(string cfgId)
        {
            if (_byId.TryGetValue(cfgId, out var data))
                return data;

            var loadOne = Load(cfgId);
            _byId[cfgId] = loadOne;
            return loadOne;
        }


        private static GatherPointConfig Load(string cfgId)
        {
            var data = Resources.Load<GatherPointConfig>($"Config/Entity/GatherPoint/{cfgId}");
            if (data == null)
                Debug.LogError($"MapAreaEffectLoader not found at Resources/Config/Entity/GatherPoint/{cfgId}");
            return data;
        }

        public static void Clear()
        {
            _byId.Clear();
        }
    }

    public static class MapHomePlacementEntityCfgtLoader
    {

        private static Dictionary<string, MapHomePlacementEntityConfig> _byId = new Dictionary<string, MapHomePlacementEntityConfig>();

        public static MapHomePlacementEntityConfig Get(string cfgId)
        {
            if (_byId.TryGetValue(cfgId, out var data))
                return data;

            var loadOne = Load(cfgId);
            _byId[cfgId] = loadOne;
            return loadOne;
        }


        private static MapHomePlacementEntityConfig Load(string cfgId)
        {
            var data = Resources.Load<MapHomePlacementEntityConfig>($"Config/Entity/HomePlacement/{cfgId}");
            if (data == null)
                Debug.LogError($"MapAreaEffectLoader not found at Resources/Config/Entity/HomePlacement/{cfgId}");
            return data;
        }

        public static void Clear()
        {
            _byId.Clear();
        }
    }


    public static class MapEventGroupCfgLoader
    {

        private static Dictionary<string, MapEventGroupConfig> _byId = new Dictionary<string, MapEventGroupConfig>();

        public static MapEventGroupConfig Get(string cfgId)
        {
            if (_byId.TryGetValue(cfgId, out var data))
                return data;

            var loadOne = Load(cfgId);
            _byId[cfgId] = loadOne;
            return loadOne;
        }


        private static MapEventGroupConfig Load(string cfgId)
        {
            var data = Resources.Load<MapEventGroupConfig>($"Config/Entity/EventGroup/{cfgId}");
            if (data == null)
                Debug.LogError($"MapAreaEffectLoader not found at Resources/Config/Entity/EventGroup/{cfgId}");
            return data;
        }

        public static void Clear()
        {
            _byId.Clear();
        }
    }
    public static class MapDynamicSpawnerCfgLoader
    {

        private static Dictionary<string, MapDynamicSpawnerConfig> _byId = new Dictionary<string, MapDynamicSpawnerConfig>();

        public static MapDynamicSpawnerConfig Get(string cfgId)
        {
            if (_byId.TryGetValue(cfgId, out var data))
                return data;

            var loadOne = Load(cfgId);
            _byId[cfgId] = loadOne;
            return loadOne;
        }


        private static MapDynamicSpawnerConfig Load(string cfgId)
        {
            var data = Resources.Load<MapDynamicSpawnerConfig>($"Config/Entity/DynamicSpawner/{cfgId}");
            if (data == null)
                Debug.LogError($"MapDynamicSpawnerCfgLoader not found at Resources/Config/Entity/DynamicSpawner/{cfgId}");
            return data;
        }

        public static void Clear()
        {
            _byId.Clear();
        }
    }

    #region build

    public static class HomePlacementCfgtLoader
    {

        private static Dictionary<string, HomePlaceableObject> _byId = new Dictionary<string, HomePlaceableObject>();

        public static HomePlaceableObject Get(string cfgId)
        {
            if (_byId.TryGetValue(cfgId, out var data))
                return data;

            var loadOne = Load(cfgId);
            _byId[cfgId] = loadOne;
            return loadOne;
        }


        private static HomePlaceableObject Load(string cfgId)
        {
            var data = Resources.Load<HomePlaceableObject>($"Config/Placement/{cfgId}");
            if (data == null)
                Debug.LogError($"MapAreaEffectLoader not found at Resources/Config/Placement/{cfgId}");
            return data;
        }

        public static void Clear()
        {
            _byId.Clear();
        }
    }

    #endregion


    public static class MapPlayerAttachObjCfgLoader
    {

        private static Dictionary<string, PlayerAttachObjCfg> _byId = new Dictionary<string, PlayerAttachObjCfg>();

        public static PlayerAttachObjCfg Get(string cfgId)
        {
            if (_byId.TryGetValue(cfgId, out var data))
                return data;

            var loadOne = Load(cfgId);
            _byId[cfgId] = loadOne;
            return loadOne;
        }


        private static PlayerAttachObjCfg Load(string cfgId)
        {
            var data = Resources.Load<PlayerAttachObjCfg>($"Config/Misc/Attach/{cfgId}");
            if (data == null)
                Debug.LogError($"MapPlayerAttachObjCfgLoader not found at Resources/Config/Misc/Attach/{cfgId}");
            return data;
        }

        public static void Clear()
        {
            _byId.Clear();
        }
    }
}