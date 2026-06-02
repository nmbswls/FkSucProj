using System.Collections.Generic;
using My.Config;
using My.UI;
using UnityEngine;

namespace My.Player
{
    // 运行时道具附魔：按 itemId 记录，不修改底层物品数据
    public sealed class PlayerItemEnchantSystem : IPlayerSystem
    {
        static readonly Dictionary<string, string> SkillRemap = new()
        {
            { "smoke_grenade", "spawn_attract" },
            { "sound_ball", "player_push_surround" },
        };

        readonly PlayerSystemManager _player;
        readonly HashSet<string> _enchantedItemIds = new();

        public PlayerItemEnchantSystem(PlayerSystemManager player)
        {
            _player = player;
        }

        public static bool CanEnchantItemId(string itemId)
        {
            return !string.IsNullOrEmpty(itemId)
                   && ItemCatalog.IsQuickBarConsumable(itemId)
                   && SkillRemap.ContainsKey(itemId);
        }

        public void InitSystem(GameLogicManager ctx, Saving.SaveData savingData)
        {
            ClearAll();
        }

        public void PostInit(PlayerSystemManager owner)
        {
        }

        public void Tick(float dt)
        {
        }

        public bool IsEnchanted(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && _enchantedItemIds.Contains(itemId);
        }

        public bool TryGetRemapSkill(string itemId, out string skillId)
        {
            skillId = null;
            if (!IsEnchanted(itemId) || !SkillRemap.TryGetValue(itemId, out skillId))
            {
                return false;
            }

            return !string.IsNullOrEmpty(skillId);
        }

        public bool TryEnchant(string itemId, out string failReason)
        {
            failReason = null;
            if (string.IsNullOrEmpty(itemId))
            {
                failReason = "empty_item";
                return false;
            }

            if (!ItemCatalog.IsQuickBarConsumable(itemId))
            {
                failReason = "need_consumable";
                return false;
            }

            if (!SkillRemap.ContainsKey(itemId))
            {
                failReason = "no_remap";
                return false;
            }

            _enchantedItemIds.Add(itemId);
            return true;
        }

        public void ConsumeEnchant(string itemId)
        {
            if (!string.IsNullOrEmpty(itemId))
            {
                _enchantedItemIds.Remove(itemId);
            }
        }

        public void ClearAll()
        {
            _enchantedItemIds.Clear();
        }

        public void PruneByInventory()
        {
            if (_enchantedItemIds.Count == 0)
            {
                return;
            }

            var inv = _player.InventorySystem;
            if (inv == null)
            {
                ClearAll();
                return;
            }

            int before = _enchantedItemIds.Count;
            _enchantedItemIds.RemoveWhere(itemId => inv.GetCarriedItemTotal(itemId) <= 0);
            if (_enchantedItemIds.Count != before)
            {
                PlayerHumanItemBarPanel.RefreshFromGame();
            }
        }
    }
}
