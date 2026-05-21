using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;
using UnityEngine;

namespace My.SecretBase
{
    static class SecretBaseFacilityUnlock
    {
        public static bool IsUnlocked(GameLogicManager glm, SecretBaseFacility row)
        {
            if (row == null || string.IsNullOrEmpty(row.FacilityId) || glm == null)
            {
                return false;
            }

            var persist = glm.worldPersistState;
            if (persist != null && persist.IsSecretBaseFacilityUnlocked(row.FacilityId))
            {
                return true;
            }

            if (!glm.CheckCommonCondsAll(row.UnlockConds))
            {
                return false;
            }

            persist?.MarkSecretBaseFacilityUnlocked(row.FacilityId);
            return true;
        }
    }

    public sealed class SecretBaseFacilityRuntime
    {
        const string SlotPrefabPath = "SecretBase/FacilitySlot";

        readonly List<SecretBaseInteractable> _spawned = new();
        Transform _spawnRoot;

        public IReadOnlyList<SecretBaseInteractable> Spawned => _spawned;

        public void Refresh(GameLogicManager glm, Transform spawnRoot)
        {
            _spawnRoot = spawnRoot;
            ClearSpawned();

            if (glm == null || spawnRoot == null)
            {
                return;
            }

            var table = CfgMgr.Cfgs?.TbSecretBaseFacility;
            if (table == null)
            {
                return;
            }

            var prefab = Resources.Load<GameObject>(SlotPrefabPath);
            foreach (var row in table.DataList)
            {
                if (!SecretBaseFacilityUnlock.IsUnlocked(glm, row))
                {
                    continue;
                }

                SpawnOne(row, prefab);
            }
        }

        public void ClearSpawned()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                if (_spawned[i] != null)
                {
                    Object.Destroy(_spawned[i].gameObject);
                }
            }

            _spawned.Clear();
        }

        void SpawnOne(SecretBaseFacility row, GameObject prefab)
        {
            GameObject go;
            if (prefab != null)
            {
                go = Object.Instantiate(prefab, _spawnRoot);
            }
            else
            {
                go = new GameObject($"Facility_{row.FacilityId}");
                go.transform.SetParent(_spawnRoot, false);
                var col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(1f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = row.SortOrder;
                go.AddComponent<SecretBaseInteractable>();
            }

            go.transform.position = new Vector3(row.WorldX, row.WorldY, 0f);
            var interactable = go.GetComponent<SecretBaseInteractable>();
            if (interactable == null)
            {
                interactable = go.AddComponent<SecretBaseInteractable>();
            }

            interactable.Setup(row.PanelId, row.SortOrder);
            _spawned.Add(interactable);
        }
    }
}
