using My.Map;
using My.Player;
using UnityEngine;

namespace My.UI
{
    // 底部主栏槽位包装器，由 OverworldMainBottomBar 统一持有。
    // 内部持有底层 slot（WeaponQuickSlotCell 或 OverworldSkillSlot），bar 只与此 wrapper 交互。
    // 运行时 AddComponent 添加到实例化的槽位 prefab 根节点。
    public class MainBottomBarSlotWrapper : MonoBehaviour
    {
        public MainBottomBarSlotDef Def { get; private set; }
        public int BarSlotIndex { get; private set; }

        WeaponQuickSlotCell _weaponCell;
        OverworldSkillSlot _skillSlot;

        public void Init(OverworldMainBottomBar bar, int barSlotIndex, MainBottomBarSlotDef def)
        {
            BarSlotIndex = barSlotIndex;
            Def = def;

            if (def.Kind == MainBottomBarSlotKind.Weapon)
            {
                _weaponCell = GetComponent<WeaponQuickSlotCell>();
                if (_weaponCell == null)
                {
                    Debug.LogError("[MainBottomBarSlotWrapper] WeaponQuickSlotCell not found on weapon slot.", this);
                }
            }
            else
            {
                _skillSlot = GetComponent<OverworldSkillSlot>();
                if (_skillSlot == null)
                {
                    Debug.LogError("[MainBottomBarSlotWrapper] OverworldSkillSlot not found on skill slot.", this);
                }
                else
                {
                    _skillSlot.SetupForBar(bar, barSlotIndex, def.SourceIndex);
                }
            }
        }

        public void Refresh(
            bool hint,
            string[] showSkills,
            PlayerLogicEntity player,
            bool humanQuickBar,
            PlayerHumanQuickBarSystem qb)
        {
            if (Def.Kind == MainBottomBarSlotKind.Weapon)
            {
                _weaponCell?.Bind(Def.SourceIndex, qb != null && qb.ActiveWeaponIndex == Def.SourceIndex);
            }
            else
            {
                _skillSlot?.Refresh(hint, showSkills, player, humanQuickBar);
            }
        }
    }
}
