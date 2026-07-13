using System;
using System.Collections.Generic;
using My.Config;
using My.Input;
using My.Map;
using My.Map.Entity;
using My.Player;
using My.Quest;
using My.UI.Bag;
using UnityEngine;
using cfg.demo;

namespace My.UI
{
    public partial class OverworldHUDPanel
    {
        public const string AttachStruggleSkillId = "hit_attach";

        public static bool ShouldUseAttachStruggleSkill()
        {
            var player = MainGameManager.Instance?.gameLogicManager?.playerLogicEntity;
            return player != null && player.HasAttachingObj;
        }

        public static string[] BuildEffectiveSkillSlots(string[] source)
        {
            if (source == null)
            {
                return null;
            }

            if (!ShouldUseAttachStruggleSkill())
            {
                return source;
            }

            var ret = (string[])source.Clone();
            if (ret.Length > 0)
            {
                ret[0] = AttachStruggleSkillId;
            }

            return ret;
        }

        static bool IsWeaponHotkey(string keyName)
        {
            return keyName == EInputKey.Num1.ToString() || keyName == EInputKey.Num2.ToString();
        }

        static int KeyNameToWeaponSlotIndex(string keyName)
        {
            if (keyName == EInputKey.Num1.ToString())
            {
                return 0;
            }

            if (keyName == EInputKey.Num2.ToString())
            {
                return 1;
            }

            return -1;
        }

        bool TryHandleHumanQuickBarHotkey(string keyName)
        {
            var lgm = MainGameManager.Instance?.gameLogicManager;
            if (lgm == null || !lgm.IsHumanQuickBarAvailable())
            {
                return false;
            }

            var qb = lgm.playerDataManager?.HumanQuickBar;
            if (qb == null)
            {
                return false;
            }

            if (keyName == EInputKey.UseQuickItem.ToString())
            {
                if (PlayerHumanItemBarPanel.IsQuickUseBlocked())
                {
                    return true;
                }

                OnClickUseConsumable();
                return true;
            }

            if (IsWeaponHotkey(keyName))
            {
                int wIdx = KeyNameToWeaponSlotIndex(keyName);
                if (wIdx >= 0)
                {
                    qb.SelectWeaponSlot(wIdx);
                    OverworldHUDPanel.Instance?.MainBottomBar?.Refresh();
                    PlayerHumanItemBarPanel.RefreshFromGame();
                }

                return true;
            }

            return false;
        }

        public string GetSkillIdByKey(string keyName)
        {
            string skillId = string.Empty;
            var showSkills = MainGameManager.Instance.gameLogicManager.playerDataManager.GetSkillSlotsByState();

            bool isSkillSlot = false;
            int skillSLotIdx = -1;

            if (keyName == EInputKey.MouseLeft.ToString())
            {
                if (ShouldUseAttachStruggleSkill())
                {
                    return AttachStruggleSkillId;
                }

                var pdm = MainGameManager.Instance.gameLogicManager.playerDataManager;
                var leftClick = pdm?.HumanQuickBar?.ResolveLeftClickSkillId();
                if (!string.IsNullOrEmpty(leftClick))
                {
                    return leftClick;
                }

                skillSLotIdx = 0;
                isSkillSlot = true;
            }
            else if (keyName == EInputKey.MouseRight.ToString())
            {
                skillSLotIdx = 1;
                isSkillSlot = true;
            }
            if (keyName == EInputKey.Space.ToString())
            {
                skillSLotIdx = 2;
                isSkillSlot = true;
            }
            if (keyName == EInputKey.Num1.ToString())
            {
                skillSLotIdx = 3;
                isSkillSlot = true;
            }
            else if (keyName == EInputKey.Num2.ToString())
            {
                skillSLotIdx = 4;
                isSkillSlot = true;
            }
            else if (keyName == EInputKey.Num3.ToString())
            {
                skillSLotIdx = 5;
                isSkillSlot = true;
            }
            else if (keyName == EInputKey.Num4.ToString())
            {
                skillSLotIdx = 6;
                isSkillSlot = true;
            }
            else if (keyName == EInputKey.Num5.ToString())
            {
                skillSLotIdx = 7;
                isSkillSlot = true;
            }
            else if (keyName == EInputKey.Skill_02.ToString())
            {
                return "grapple_hook";
            }
            else if (keyName == EInputKey.EnterExpose.ToString())
            {
                var player = MainGameManager.Instance?.gameLogicManager?.playerLogicEntity;
                if (player != null && player.IsExposed)
                {
                    return "player_return_disguise";
                }

                return "player_enter_expose";
            }
            if (isSkillSlot)
            {
                return showSkills[skillSLotIdx];
            }

            return skillId;
        }

        public bool PeeviewUseSkillByKey(string keyName)
        {
            if (MainGameManager.Instance.gameLogicManager.IsDialogPlayering)
            {
                return false;
            }

            if (TryHandleHumanQuickBarHotkey(keyName))
            {
                return true;
            }

            if (keyName == EInputKey.EnterExpose.ToString())
            {
                var player = MainGameManager.Instance?.gameLogicManager?.playerLogicEntity;
                if (player == null
                    || player.LogicManager.PlayerHumanMode
                    || !player.DisguiseIfPossible)
                {
                    return false;
                }
            }

            var skillId = GetSkillIdByKey(keyName);
            if (string.IsNullOrEmpty(skillId))
            {
                return false;
            }

            if (!TryUseSkillWithDenyFeedback(skillId))
            {
                return false;
            }

            return true;
        }

        bool TryUseSkillWithDenyFeedback(string skillId)
        {
            var caster = MainGameManager.Instance?.gameLogicManager?.playerLogicEntity;
            if (caster == null)
            {
                return false;
            }

            if (!SkillCastConditionUtil.TryEvaluateReadiness(
                    caster,
                    caster.ablilityManager,
                    skillId,
                    out var denyMessage))
            {
                SkillUseDenyFeedback.Show(denyMessage);
                return false;
            }

            OnClickUseSkill(skillId);
            return true;
        }

        public void OnClickUseSkill(string skillId, Action<bool> onConfirm = null, bool isExtend = false)
        {
            var skillConf = SkillLibrary.GetSkillConfig(skillId);
            if (skillConf == null)
            {
                NotifySkillUseConfirmed(skillId, false, onConfirm);
                return;
            }

            var caster = MainGameManager.Instance?.gameLogicManager?.playerLogicEntity;
            if (caster != null
                && !SkillCastConditionUtil.TryEvaluateReadiness(
                    caster,
                    caster.ablilityManager,
                    skillConf,
                    out var denyMessage))
            {
                SkillUseDenyFeedback.Show(denyMessage);
                NotifySkillUseConfirmed(skillId, false, onConfirm);
                return;
            }

            var castOverrides = ResolveHumanWeaponCastOverrides(skillId);
            if (skillConf.IsCombo)
            {
                bool ok = TryCastPlayerSkill(skillId, null, null, null, castOverrides);
                NotifySkillUseConfirmed(skillId, ok, onConfirm);
                return;
            }

            var mainAbilityCfg = AbilityLibrary.GetAbilityConfig(skillConf.MainAbilityId);
            if (mainAbilityCfg == null)
            {
                Debug.LogError($"skill not found main ability:{skillConf.MainAbilityId}");
                NotifySkillUseConfirmed(skillId, false, onConfirm);
                return;
            }

            Vector2 dir = Vector2.one;
            if (MainGameManager.Instance.gameLogicManager.playerLogicEntity.FreeMoveInput.magnitude < 0.01f)
            {
                dir = MainGameManager.Instance.playerScenePresenter.PlayerEntity.FinalLook;
            }
            else
            {
                dir = MainGameManager.Instance.gameLogicManager.playerLogicEntity.FreeMoveInput;
            }

            if (mainAbilityCfg.CastType == MapAbilitySpecConfig.ECastType.NoTarget)
            {
                bool ok = TryCastPlayerSkill(skillId, dir, null, null, castOverrides);
                NotifySkillUseConfirmed(skillId, ok, onConfirm);
                return;
            }
            else if (mainAbilityCfg.CastType == MapAbilitySpecConfig.ECastType.ToFace)
            {
                var player = MainGameManager.Instance.playerScenePresenter.PlayerEntity;
                bool ok = TryCastPlayerSkill(
                    skillId,
                    dir,
                    player.Pos + player.CurrentLook * 1.0f,
                    null,
                    castOverrides);
                NotifySkillUseConfirmed(skillId, ok, onConfirm);
                return;
            }

            EnterSkillPreviewMode(skillId, (ret) => NotifySkillUseConfirmed(skillId, ret, onConfirm));
        }

        static bool TryCastPlayerSkill(
            string skillId,
            Vector2? inputVec,
            Vector2? castVec,
            ILogicEntity target,
            Dictionary<string, string> castOverrides)
        {
            var player = MainGameManager.Instance?.playerScenePresenter?.PlayerEntity;
            var skillSystem = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.SkillSystem;
            if (player?.ablilityManager == null)
            {
                return false;
            }

            if (skillSystem != null && skillSystem.IsTempSkill(skillId))
            {
                return player.ablilityManager.TryUseSkillFromConfig(skillId, inputVec, castVec, target, castOverrides);
            }

            return player.ablilityManager.UseSkill(skillId, inputVec, castVec, target, castOverrides);
        }

        void OnTempSkillChanged(PlayerTempSkillChangedEvent _)
        {
            PlayerHumanItemBarPanel.RefreshFromGame();
            MainBottomBar?.Refresh();
        }

        void NotifySkillUseConfirmed(string skillId, bool success, Action<bool> onConfirm)
        {
            if (success)
            {
                TryConsumeTempSkill(skillId);
            }

            onConfirm?.Invoke(success);
        }

        static void TryConsumeTempSkill(string skillId)
        {
            var pdm = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            if (pdm == null || !pdm.ConsumeTempSkillIfMatch(skillId))
            {
                return;
            }

            PlayerHumanItemBarPanel.RefreshFromGame();
            Instance?.MainBottomBar?.Refresh();
        }

        static Dictionary<string, string> ResolveHumanWeaponCastOverrides(string skillId)
        {
            var lgm = MainGameManager.Instance?.gameLogicManager;
            if (lgm == null || !lgm.IsHumanQuickBarAvailable())
            {
                return null;
            }

            var qb = lgm.playerDataManager?.HumanQuickBar;
            if (qb == null)
            {
                return null;
            }

            var activeWeaponSkill = qb.GetActiveWeaponSkillId();
            if (string.IsNullOrEmpty(activeWeaponSkill) || activeWeaponSkill != skillId)
            {
                return null;
            }

            return qb.BuildCastParamsForActiveWeapon();
        }

        public void OnClickUseConsumable()
        {
            if (PlayerHumanItemBarPanel.IsQuickUseBlocked())
            {
                return;
            }

            var lgm = MainGameManager.Instance?.gameLogicManager;
            if (lgm == null || !lgm.IsHumanQuickBarAvailable())
            {
                return;
            }

            var binding = lgm.playerDataManager.HumanQuickBar.GetActiveConsumableBinding();
            if (binding.IsEmpty)
            {
                return;
            }

            UseQuickBarBinding(binding);
        }

        void UseQuickBarBinding(QuickSlotBinding binding)
        {
            var pdm = MainGameManager.Instance.gameLogicManager.playerDataManager;
            var inv = pdm.InventorySystem;
            if (inv == null || !inv.CheckQuickSlotBindingAvailable(binding))
            {
                return;
            }

            var itemId = binding.ItemId;
            var itemUseCfg = ItemCatalog.GetPrimaryUse(itemId);
            if (itemUseCfg == null)
            {
                return;
            }

            if (itemUseCfg.UseType == EItemUseType.UseSkill)
            {
                var skillId = itemUseCfg.S1;
                bool usedEnchant = false;
                if (pdm.ItemEnchant != null && pdm.ItemEnchant.TryGetRemapSkill(itemId, out var enchantSkill))
                {
                    skillId = enchantSkill;
                    usedEnchant = true;
                }

                OnClickUseSkill(skillId, (ret) =>
                {
                    if (ret && usedEnchant)
                    {
                        pdm.ItemEnchant?.ConsumeEnchant(itemId);
                        PlayerHumanItemBarPanel.RefreshFromGame();
                    }

                    if (ret && ItemCatalog.ShouldConsumeOnUse(itemUseCfg))
                    {
                        inv.TryConsumeQuickSlotUse(binding, itemUseCfg);
                        pdm.HumanQuickBar?.PruneInvalidSlots();
                    }
                });
            }
            else
            {
                TryUseConsumableFromInventoryBag(binding);
            }

            pdm.HumanQuickBar?.PruneInvalidSlots();
            PlayerHumanItemBarPanel.RefreshFromGame();
        }

        static void TryUseConsumableFromInventoryBag(QuickSlotBinding binding)
        {
            var inv = MainGameManager.Instance.gameLogicManager.playerDataManager.InventorySystem;
            int bagId = (int)EPlayerBagId.Default;

            if (inv.TryFindCarriedStack(binding, out var flatIndex, out _))
            {
                PlayerBagUIPanel.Instance?.UseItem(bagId, flatIndex);
            }
        }

        protected void EnterSkillPreviewMode(string skillId, Action<bool> onConfirm = null)
        {
            UpdateHudMode(EHudMode.PreviewSkill);
            overworldSkillPreviewUI.Initialize(skillId, onConfirm);
        }

        public void CancelSkillCast()
        {
            if (HudMode != EHudMode.PreviewSkill)
            {
                return;
            }
            UpdateHudMode(EHudMode.Normal);
        }
    }
}
