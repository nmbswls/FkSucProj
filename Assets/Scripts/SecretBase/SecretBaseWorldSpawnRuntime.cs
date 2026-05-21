using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;
using UnityEngine;

namespace My.SecretBase
{
    static class SecretBaseCharacterSpawn
    {
        public static bool IsVisible(GameLogicManager glm, SecretBaseCharacter row)
        {
            if (row == null || string.IsNullOrEmpty(row.SlotId) || glm == null)
            {
                return false;
            }

            return glm.CheckCommonCondsAll(row.SpawnConds);
        }
    }

    // 据点场景：设施 + 重要 NPC 生成
    public sealed class SecretBaseWorldSpawnRuntime
    {
        const string FacilityPrefabPath = "SecretBase/FacilitySlot";
        const string NpcPrefabPath = "SecretBase/NpcSlot";

        readonly List<ISecretBaseClickTarget> _spawned = new();

        public IReadOnlyList<ISecretBaseClickTarget> Spawned => _spawned;

        public void Refresh(GameLogicManager glm, Transform spawnRoot)
        {
            ClearSpawned();

            if (glm == null || spawnRoot == null)
            {
                return;
            }

            RefreshFacilities(glm, spawnRoot);
            RefreshCharacters(glm, spawnRoot);
        }

        void RefreshFacilities(GameLogicManager glm, Transform spawnRoot)
        {
            var table = CfgMgr.Cfgs?.TbSecretBaseFacility;
            if (table == null)
            {
                return;
            }

            var prefab = Resources.Load<GameObject>(FacilityPrefabPath);
            foreach (var row in table.DataList)
            {
                if (!SecretBaseFacilityUnlock.IsUnlocked(glm, row))
                {
                    continue;
                }

                SpawnFacility(row, prefab, spawnRoot);
            }
        }

        void RefreshCharacters(GameLogicManager glm, Transform spawnRoot)
        {
            var table = CfgMgr.Cfgs?.TbSecretBaseCharacter;
            if (table == null)
            {
                return;
            }

            var prefab = Resources.Load<GameObject>(NpcPrefabPath);
            foreach (var row in table.DataList)
            {
                if (!SecretBaseCharacterSpawn.IsVisible(glm, row))
                {
                    continue;
                }

                SpawnCharacter(row, prefab, spawnRoot);
            }
        }

        public void ClearSpawned()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                var t = _spawned[i] as MonoBehaviour;
                if (t != null)
                {
                    Object.Destroy(t.gameObject);
                }
            }

            _spawned.Clear();
        }

        void SpawnFacility(SecretBaseFacility row, GameObject prefab, Transform spawnRoot)
        {
            var go = InstantiateSlot(prefab, spawnRoot, $"Facility_{row.FacilityId}");
            go.transform.position = new Vector3(row.WorldX, row.WorldY, 0f);

            var interactable = go.GetComponent<SecretBaseInteractable>();
            if (interactable == null)
            {
                interactable = go.AddComponent<SecretBaseInteractable>();
            }

            interactable.Setup(row.PanelId, row.SortOrder);
            _spawned.Add(interactable);
        }

        void SpawnCharacter(SecretBaseCharacter row, GameObject prefab, Transform spawnRoot)
        {
            var go = InstantiateSlot(prefab, spawnRoot, $"Npc_{row.SlotId}");
            go.transform.position = new Vector3(row.WorldX, row.WorldY, 0f);

            var interactable = go.GetComponent<SecretBaseNpcInteractable>();
            if (interactable == null)
            {
                interactable = go.AddComponent<SecretBaseNpcInteractable>();
            }

            interactable.Setup(row);
            _spawned.Add(interactable);
        }

        static GameObject InstantiateSlot(GameObject prefab, Transform spawnRoot, string fallbackName)
        {
            if (prefab != null)
            {
                return Object.Instantiate(prefab, spawnRoot);
            }

            var go = new GameObject(fallbackName);
            go.transform.SetParent(spawnRoot, false);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.3f, 0.7f, 1f, 1f);
            return go;
        }
    }
}
