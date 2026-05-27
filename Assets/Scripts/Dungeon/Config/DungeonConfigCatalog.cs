using System.Collections.Generic;
using UnityEngine;

namespace My.Dungeon
{
    public static class DungeonConfigCatalog
    {
        private static readonly Dictionary<string, DungeonDef> _byId = new();
        private static bool _loaded;

        public static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            _byId.Clear();

            var defs = Resources.LoadAll<DungeonDef>("Config/Dungeon");
            foreach (var def in defs)
            {
                if (def == null || string.IsNullOrEmpty(def.DungeonId))
                {
                    continue;
                }

                _byId[def.DungeonId] = def;
            }
        }

        public static DungeonDef GetOrDefault(string dungeonId)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(dungeonId))
            {
                return null;
            }

            _byId.TryGetValue(dungeonId, out var def);
            if (def == null && dungeonId == "test_cave")
            {
                def = DungeonDefaultContent.GetOrCreateTestCave();
                _byId[dungeonId] = def;
            }

            return def;
        }
    }
}
