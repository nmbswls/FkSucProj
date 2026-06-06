using cfg.demo;
using UnityEngine;

namespace My.UI.Rune
{
    // 与 TalentTreeNodeBinder 类似：槽位定义由预制体配置，运行时只读取
    public sealed class RuneSlotBinder : MonoBehaviour
    {
        [SerializeField] RuneSlotKind slotKind = RuneSlotKind.Fixed;
        [SerializeField] string fixedRuneId;
        [SerializeField] ERuneEquipSlot equipSlot = ERuneEquipSlot.None;

        public RuneSlotKind SlotKind => slotKind;
        public string FixedRuneId => fixedRuneId;
        public ERuneEquipSlot EquipSlot => equipSlot;
    }
}
