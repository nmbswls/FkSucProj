using cfg.demo;
using UnityEngine;

namespace My.UI.Rune
{
    // 与 TalentTreeNodeBinder 类似：槽位定义由预制体或运行时赋值
    public sealed class RuneSlotBinder : MonoBehaviour
    {
        [SerializeField] RuneSlotKind slotKind = RuneSlotKind.Fixed;
        [SerializeField] string fixedRuneId;
        [SerializeField] ERuneEquipSlot equipSlot = ERuneEquipSlot.None;

        public RuneSlotKind SlotKind => slotKind;
        public string FixedRuneId => fixedRuneId;
        public ERuneEquipSlot EquipSlot => equipSlot;

        public void ConfigureFixed(string runeId)
        {
            slotKind = RuneSlotKind.Fixed;
            fixedRuneId = runeId;
            equipSlot = ERuneEquipSlot.None;
        }

        public void ConfigureEquippable(ERuneEquipSlot slot)
        {
            slotKind = RuneSlotKind.Equippable;
            fixedRuneId = string.Empty;
            equipSlot = slot;
        }
    }
}
