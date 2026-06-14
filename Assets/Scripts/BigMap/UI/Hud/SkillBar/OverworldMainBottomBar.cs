using System.Collections.Generic;
using My.Map;
using My.Map.Entity;
using My.Player;
using UnityEngine;

namespace My.UI
{
    public class OverworldMainBottomBar : MonoBehaviour
    {
        protected OverworldHUDPanel HudPanel { get; private set; }

        public Transform SlotContainer;
        // 武器槽模板：根节点挂 WeaponQuickSlotCell，继承 QuickSlotCellBase → ItemCellBase
        public WeaponQuickSlotCell WeaponSlotTemplate;
        // 技能槽模板：根节点挂 OverworldSkillSlot，继承 SkillSlotBase，纯技能语义
        public OverworldSkillSlot SkillSlotTemplate;
        // 按键提示徽章模板，运行时拼装到每个槽下方
        public SlotKeyHintView KeyHintBadge;

        [Header("Slot Config")]
        public Vector2 slotSize = new Vector2(50f, 50f);

        readonly List<MainBottomBarSlotWrapper> _slots = new();
        List<MainBottomBarSlotDef> _layout = new();
        int _layoutSignature = int.MinValue;

        void Awake()
        {
            if (SlotContainer == null || WeaponSlotTemplate == null || SkillSlotTemplate == null)
            {
                Debug.LogError(
                    $"[OverworldMainBottomBar] Missing references: "
                    + $"SlotContainer={SlotContainer}, WeaponSlotTemplate={WeaponSlotTemplate}, SkillSlotTemplate={SkillSlotTemplate}");
                return;
            }

            WeaponSlotTemplate.gameObject.SetActive(false);
            SkillSlotTemplate.gameObject.SetActive(false);
            ClearPreviewChildren();
        }

        void ClearPreviewChildren()
        {
            for (int i = SlotContainer.childCount - 1; i >= 0; i--)
            {
                var child = SlotContainer.GetChild(i);
                if (child.gameObject == WeaponSlotTemplate.gameObject
                    || child.gameObject == SkillSlotTemplate.gameObject)
                {
                    continue;
                }

                Destroy(child.gameObject);
            }
        }

        public void InitBar(OverworldHUDPanel hudPanel)
        {
            HudPanel = hudPanel;
            Refresh();
        }

        public void InvalidateLayout()
        {
            _layoutSignature = int.MinValue;
        }

        public void Refresh(bool hint = false, bool forceLayoutRebuild = false)
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            var player = glm?.playerLogicEntity;
            var pdm = glm?.playerDataManager;
            if (player == null || pdm == null || SlotContainer == null
                || WeaponSlotTemplate == null || SkillSlotTemplate == null)
            {
                return;
            }

            var showSkills = pdm.GetSkillSlotsByState();
            if (showSkills == null)
            {
                return;
            }

            if (forceLayoutRebuild)
            {
                InvalidateLayout();
            }

            _layout = MainBottomBarLayout.Build(glm, pdm);
            int barMode = MainBottomBarLayout.GetBarMode(glm, pdm);
            EnsureBuiltSlots(_layout, barMode);

            bool humanQuickBar = glm.IsHumanQuickBarAvailable() && !pdm.IsUsingFaQingSkillBar();
            var qb = pdm.HumanQuickBar;

            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].Refresh(hint, showSkills, player, humanQuickBar, qb);
            }
        }

        void EnsureBuiltSlots(IReadOnlyList<MainBottomBarSlotDef> layout, int barMode)
        {
            int sig = MainBottomBarLayout.ComputeLayoutSignature(layout, barMode);
            if (sig == _layoutSignature && _slots.Count == layout.Count)
            {
                return;
            }

            _layoutSignature = sig;
            DestroyBuiltSlots();

            for (int i = 0; i < layout.Count; i++)
            {
                var def = layout[i];
                var slot = SpawnSlot(def, i);
                if (slot != null)
                {
                    _slots.Add(slot);
                }
            }
        }

        MainBottomBarSlotWrapper SpawnSlot(MainBottomBarSlotDef def, int barSlotIndex)
        {
            GameObject go;
            if (def.Kind == MainBottomBarSlotKind.Weapon)
            {
                go = Instantiate(WeaponSlotTemplate.gameObject, SlotContainer);
                go.name = $"WeaponSlot_{def.SourceIndex}";
            }
            else
            {
                go = Instantiate(SkillSlotTemplate.gameObject, SlotContainer);
                go.name = $"SkillSlot_{def.SourceIndex}";
            }

            go.SetActive(true);
            ApplySlotSize(go.transform as RectTransform);
            SpawnKeyHint(go, barSlotIndex);

            var wrapper = go.AddComponent<MainBottomBarSlotWrapper>();
            wrapper.Init(this, barSlotIndex, def);
            return wrapper;
        }

        void SpawnKeyHint(GameObject slotGo, int barSlotIndex)
        {
            if (KeyHintBadge == null)
            {
                return;
            }

            string hintText = MainBottomBarLayout.GetKeyHintText(_layout, barSlotIndex);
            var badge = Instantiate(KeyHintBadge, slotGo.transform);
            badge.gameObject.SetActive(true);
            badge.gameObject.name = "KeyHint";
            badge.SetText(hintText);

            // 定位到槽底部中心正下方，不占用槽自身布局尺寸
            var rt = badge.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -2f);
        }

        void ApplySlotSize(RectTransform rt)
        {
            if (rt == null)
            {
                return;
            }

            rt.sizeDelta = slotSize;
        }

        void DestroyBuiltSlots()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] != null)
                {
                    Destroy(_slots[i].gameObject);
                }
            }

            _slots.Clear();
        }

        public void OnSkillSlotClicked(int barSlotIndex, int skillSourceIndex)
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            var pdm = glm?.playerDataManager;
            var showSkills = pdm?.GetSkillSlotsByState();

            if (showSkills == null || skillSourceIndex < 0 || skillSourceIndex >= showSkills.Length)
            {
                return;
            }

            string skillId = showSkills[skillSourceIndex];
            if (string.IsNullOrEmpty(skillId))
            {
                return;
            }

            var player = glm?.playerLogicEntity;
            if (player != null
                && !SkillCastConditionUtil.TryEvaluateReadiness(
                    player, player.ablilityManager, skillId, out var denyMessage))
            {
                SkillUseDenyFeedback.Show(denyMessage);
                return;
            }

            HudPanel.OnClickUseSkill(skillId);
        }
    }
}
