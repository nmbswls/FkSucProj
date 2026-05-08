using System;
using System.Collections.Generic;
using My.Map;
using My.Map.Entity;
using My.Saving;
using UnityEngine;

namespace My.Player
{
    // 魔力衣装：潜入选定后锁定；rawOverRate 参与 RefreshClothesRelateYCAttrs，MaxClothes 同步 PlayerClothes 上限，牢固度修正移动磨损
    public sealed class PlayerMagicClothesManager
    {
        public const string DefaultStealthDefId = "magic_clothes_standard";

        readonly PlayerSystemManager _playerMgr;

        static readonly Dictionary<string, MagicClothesDef> Catalog = BuildCatalog();

        public string SelectedDefId { get; private set; }
        public bool LockedForStealth { get; private set; }

        public PlayerMagicClothesManager(PlayerSystemManager playerMgr)
        {
            _playerMgr = playerMgr;
        }

        static Dictionary<string, MagicClothesDef> BuildCatalog()
        {
            return new Dictionary<string, MagicClothesDef>(StringComparer.Ordinal)
            {
                [DefaultStealthDefId] = new MagicClothesDef
                {
                    Id = DefaultStealthDefId,
                    RawOverRate10000 = 10000,
                    MaxClothes = 100_000,
                    Firmness10000 = 10000,
                    MoveWearDistancePerCheck = 30f,
                    MoveWearChancePermille = 60,
                    MoveWearBaseLoss = 1200,
                },
                ["magic_clothes_silk"] = new MagicClothesDef
                {
                    Id = "magic_clothes_silk",
                    RawOverRate10000 = 11500,
                    MaxClothes = 85_000,
                    Firmness10000 = 8000,
                    MoveWearDistancePerCheck = 25f,
                    MoveWearChancePermille = 75,
                    MoveWearBaseLoss = 1500,
                },
                ["magic_clothes_guard"] = new MagicClothesDef
                {
                    Id = "magic_clothes_guard",
                    RawOverRate10000 = 9200,
                    MaxClothes = 120_000,
                    Firmness10000 = 14500,
                    MoveWearDistancePerCheck = 35f,
                    MoveWearChancePermille = 45,
                    MoveWearBaseLoss = 900,
                },
            };
        }

        public static MagicClothesDef GetDefOrNull(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            return Catalog.TryGetValue(id, out var d) ? d : null;
        }

        public MagicClothesDef GetActiveDef() => GetDefOrNull(SelectedDefId);

        public IReadOnlyCollection<string> EnumerateDefIds() => Catalog.Keys;

        public void LoadFromSave(PlayerData pd)
        {
            SelectedDefId = null;
            LockedForStealth = false;
            if (pd == null)
            {
                return;
            }

            SelectedDefId = string.IsNullOrEmpty(pd.MagicClothesDefId) ? null : pd.MagicClothesDefId;
            LockedForStealth = pd.MagicClothesLockedForStealth;
            if (SelectedDefId != null && !Catalog.ContainsKey(SelectedDefId))
            {
                SelectedDefId = null;
                LockedForStealth = false;
            }
        }

        public void SaveTo(PlayerData pd)
        {
            if (pd == null)
            {
                return;
            }

            pd.MagicClothesDefId = SelectedDefId ?? string.Empty;
            pd.MagicClothesLockedForStealth = LockedForStealth;
        }

        public bool IsLockedWithSelection => LockedForStealth && !string.IsNullOrEmpty(SelectedDefId) && Catalog.ContainsKey(SelectedDefId);

        // 潜入选衣入口：已锁定时失败
        public bool TrySelectAndLock(string defId, PlayerLogicEntity player)
        {
            if (LockedForStealth)
            {
                return false;
            }

            if (GetDefOrNull(defId) == null)
            {
                Debug.LogWarning($"PlayerMagicClothesManager: unknown def {defId}");
                return false;
            }

            if (player == null)
            {
                Debug.LogWarning("PlayerMagicClothesManager.TrySelectAndLock: player is null");
                return false;
            }

            if (player.LogicManager.PlayerHumanMode)
            {
                Debug.LogWarning("PlayerMagicClothesManager.TrySelectAndLock: player is in human mode");
                return false;
            }

            SelectedDefId = defId;
            LockedForStealth = true;
            ApplyToPlayer(player);
            return true;
        }

        // 伪装类地图玩家初始化：已有存档则只应用；否则用默认套并锁定（可由 UI 在进图前改为 TrySelectAndLock）
        public void OnStealthMapPlayerInitialized(PlayerLogicEntity player)
        {
            if (player == null)
            {
                return;
            }

            if (player.LogicManager.PlayerHumanMode)
            {
                return;
            }

            if (IsLockedWithSelection)
            {
                ApplyToPlayer(player);
                return;
            }

            TrySelectAndLock(DefaultStealthDefId, player);
        }

        public void ApplyToPlayer(PlayerLogicEntity player)
        {
            if (player == null)
            {
                return;
            }

            if (player.LogicManager.PlayerHumanMode)
            {
                return;
            }

            var def = GetActiveDef();
            if (def == null)
            {
                return;
            }

            player.ApplyMagicClothesRuntime(def.MaxClothes);
        }

        public bool ShouldApplyMoveWear(PlayerLogicEntity p)
        {
            if (p == null || !IsLockedWithSelection)
            {
                return false;
            }

            if (p.LogicManager.PlayerHumanMode)
            {
                return false;
            }

            if (p.IsZhaZhiMode)
            {
                return false;
            }

            if (p.IsExposed)
            {
                return false;
            }

            if (p.MotorState != EMotorState.Free)
            {
                return false;
            }

            if (p.FreeMoveInput.sqrMagnitude < 0.01f)
            {
                return false;
            }

            return true;
        }

        public long ComputeMoveWearLoss(MagicClothesDef def)
        {
            if (def == null)
            {
                return 0;
            }

            int f = Math.Max(1000, def.Firmness10000);
            return Math.Max(1, def.MoveWearBaseLoss * 10000L / f);
        }

        public int GetMoveWearEffectiveChancePermille(MagicClothesDef def)
        {
            if (def == null)
            {
                return 0;
            }

            int f = Math.Max(1000, def.Firmness10000);
            return Math.Clamp(def.MoveWearChancePermille * 10000 / f, 0, 999);
        }

        public long GetRawOverRate10000ForRefresh()
        {
            var def = GetActiveDef();
            if (def == null)
            {
                return 10000;
            }

            return def.RawOverRate10000;
        }
    }

    public sealed class MagicClothesDef
    {
        public string Id;
        public long RawOverRate10000;
        public long MaxClothes;
        public int Firmness10000;
        public float MoveWearDistancePerCheck;
        public int MoveWearChancePermille;
        public long MoveWearBaseLoss;
    }
}
