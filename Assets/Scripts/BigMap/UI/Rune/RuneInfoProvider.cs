using cfg.demo;
using My.Config;
using My.UI;
using UnityEngine;

namespace My.UI.Rune
{
    // 模仿 TalentNodeHoverProvider，为槽位/符文格提供展示信息
    public sealed class RuneInfoProvider : BaseUIHoverProvider
    {
        RuneSlotKind _slotKind = RuneSlotKind.Fixed;
        string _runeId;
        ERuneEquipSlot _equipSlot = ERuneEquipSlot.None;
        bool _unlocked;
        bool _hasDetail;

        protected override void Awake()
        {
            base.Awake();
        }

        public void SetFixedSlot(string runeId, bool unlocked)
        {
            _slotKind = RuneSlotKind.Fixed;
            _runeId = runeId ?? string.Empty;
            _equipSlot = ERuneEquipSlot.None;
            _unlocked = unlocked;
            _hasDetail = unlocked && !string.IsNullOrEmpty(_runeId);
        }

        public void SetEquippableSlot(ERuneEquipSlot slot, string equippedRuneId, bool slotUnlocked)
        {
            _slotKind = RuneSlotKind.Equippable;
            _equipSlot = slot;
            _runeId = equippedRuneId ?? string.Empty;
            _unlocked = slotUnlocked;
            _hasDetail = slotUnlocked && !string.IsNullOrEmpty(_runeId);
        }

        public void SetOwnedRune(string runeId)
        {
            _slotKind = RuneSlotKind.Equippable;
            _equipSlot = ERuneEquipSlot.None;
            _runeId = runeId ?? string.Empty;
            _unlocked = true;
            _hasDetail = !string.IsNullOrEmpty(_runeId);
        }

        public override HoverTipParams? GetSimpleTipInfo()
        {
            if (!_hasDetail)
            {
                return null;
            }

            return InnerParams;
        }

        public string GetDisplayName()
        {
            var def = RuneCatalog.GetOrDefault(_runeId);
            if (def != null && !string.IsNullOrEmpty(def.Name))
            {
                return def.Name;
            }

            if (_slotKind == RuneSlotKind.Equippable && _equipSlot != ERuneEquipSlot.None)
            {
                return RuneCatalog.GetSlotDisplayName(_equipSlot);
            }

            return string.IsNullOrEmpty(_runeId) ? "空槽" : _runeId;
        }

        public string GetDetailText()
        {
            var def = RuneCatalog.GetOrDefault(_runeId);
            if (def == null)
            {
                if (_slotKind == RuneSlotKind.Equippable && _equipSlot != ERuneEquipSlot.None && !_unlocked)
                {
                    return $"装配槽 {RuneCatalog.GetSlotDisplayName(_equipSlot)} 尚未解锁。";
                }

                return string.Empty;
            }

            var lines = new System.Text.StringBuilder();
            lines.AppendLine(def.Desc ?? string.Empty);
            if (def.RuneType == ERuneType.Equippable && def.EquipSlot != ERuneEquipSlot.None)
            {
                lines.AppendLine($"槽位: {RuneCatalog.GetSlotDisplayName(def.EquipSlot)}");
            }

            if (!string.IsNullOrEmpty(def.PassiveSkillId))
            {
                lines.AppendLine($"被动: {def.PassiveSkillId}");
            }

            if (!string.IsNullOrEmpty(def.FuncUnlockKey))
            {
                lines.AppendLine($"解锁: {def.FuncUnlockKey}");
            }

            if (def.FuncOpenType != EFuncOpenType.Invalid)
            {
                lines.AppendLine($"功能: {def.FuncOpenType}");
            }

            return lines.ToString().TrimEnd();
        }

        public Sprite GetIconSprite()
        {
            var def = RuneCatalog.GetOrDefault(_runeId);
            if (def == null || string.IsNullOrEmpty(def.Icon))
            {
                return null;
            }

            return SimpleResManager.Load<Sprite>(def.Icon);
        }
    }
}
