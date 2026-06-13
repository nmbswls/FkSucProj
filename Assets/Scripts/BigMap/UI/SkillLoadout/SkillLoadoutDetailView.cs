using cfg.demo;
using My.Config;
using My.Map.Entity;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.SkillLoadout
{
    public sealed class SkillLoadoutDetailView : MonoBehaviour
    {
        enum DetailMode { None, LearnEntry, EquippedSlot }

        const string PathLearnBtn = "OptLine/LearnOrUpgrade/LearnBtn";
        const string PathUpgradeBtn = "OptLine/LearnOrUpgrade/UpgradeBtn";
        const string PathMaxShowBtn = "OptLine/LearnOrUpgrade/MaxShowBtn";
        const string PathEquipBtn = "OptLine/EquipOpt/EquipBtn";
        const string PathUnEquipBtn = "OptLine/EquipOpt/UnEquipBtn";
        const string PathLocateMark = "OptLine/LocateMark";
        const string PathHideBtn = "HideBtn";

        [SerializeField] TextMeshProUGUI nameLabel;
        [SerializeField] TextMeshProUGUI descText;
        [SerializeField] TextMeshProUGUI levelChangeText;
        [SerializeField] TextMeshProUGUI costLineText;
        [SerializeField] TextMeshProUGUI statusLineText;

        [SerializeField] Button btnClose;
        [SerializeField] Button btnLearn;
        [SerializeField] TextMeshProUGUI btnLearnLabel;
        [SerializeField] Button btnUpgrade;
        [SerializeField] TextMeshProUGUI btnUpgradeLabel;
        [SerializeField] Button btnMaxShow;
        [SerializeField] TextMeshProUGUI btnMaxShowLabel;
        [SerializeField] Button btnEquip;
        [SerializeField] TextMeshProUGUI btnEquipLabel;
        [SerializeField] Button btnUnequip;
        [SerializeField] TextMeshProUGUI btnUnequipLabel;
        [SerializeField] Button btnLocate;

        DetailMode _mode = DetailMode.None;
        int _entryId;
        int _upgradeEntryId;
        int _locateSchoolId;
        string _currentSkillId;
        SkillLoadoutSlotKind _equippedSlotKind;
        int _equippedSlotIndex;

        bool _learnModeEquipped;
        SkillLoadoutSlotKind _learnModeEquippedKind;
        int _learnModeEquippedSlot;

        System.Action<int> _onLearnClicked;
        System.Action<int> _onUpgradeClicked;
        System.Action<string> _onEquipClicked;
        System.Action<SkillLoadoutSlotKind, int> _onUnequipClicked;
        System.Action<int> _onLocateClicked;
        System.Action _onCloseClicked;

        void Awake()
        {
            EnsureReferences();
            if (descText != null)
            {
                descText.gameObject.SetActive(false);
            }
        }

        public void SetLearnHandler(System.Action<int> cb) => _onLearnClicked = cb;
        public void SetUpgradeHandler(System.Action<int> cb) => _onUpgradeClicked = cb;
        public void SetEquipHandler(System.Action<string> cb) => _onEquipClicked = cb;
        public void SetUnequipHandler(System.Action<SkillLoadoutSlotKind, int> cb) => _onUnequipClicked = cb;
        public void SetLocateHandler(System.Action<int> cb) => _onLocateClicked = cb;
        public void SetCloseHandler(System.Action cb) => _onCloseClicked = cb;

        void EnsureReferences()
        {
            if (nameLabel == null)
                nameLabel = transform.Find("NameLabel")?.GetComponent<TextMeshProUGUI>();
            if (descText == null)
                descText = transform.Find("DescText")?.GetComponent<TextMeshProUGUI>();
            if (levelChangeText == null)
                levelChangeText = transform.Find("LevelChange")?.GetComponent<TextMeshProUGUI>();
            if (costLineText == null)
                costLineText = transform.Find("CostLine")?.GetComponent<TextMeshProUGUI>();
            if (statusLineText == null)
                statusLineText = transform.Find("StatusLine")?.GetComponent<TextMeshProUGUI>();

            if (btnClose == null)
                btnClose = FindButton(PathHideBtn);
            if (btnLearn == null)
                btnLearn = FindButton(PathLearnBtn);
            if (btnLearnLabel == null && btnLearn != null)
                btnLearnLabel = btnLearn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (btnUpgrade == null)
                btnUpgrade = FindButton(PathUpgradeBtn);
            if (btnUpgradeLabel == null && btnUpgrade != null)
                btnUpgradeLabel = btnUpgrade.GetComponentInChildren<TextMeshProUGUI>(true);
            if (btnMaxShow == null)
                btnMaxShow = FindButton(PathMaxShowBtn);
            if (btnMaxShowLabel == null && btnMaxShow != null)
                btnMaxShowLabel = btnMaxShow.GetComponentInChildren<TextMeshProUGUI>(true);
            if (btnEquip == null)
                btnEquip = FindButton(PathEquipBtn);
            if (btnEquipLabel == null && btnEquip != null)
                btnEquipLabel = btnEquip.GetComponentInChildren<TextMeshProUGUI>(true);
            if (btnUnequip == null)
                btnUnequip = FindButton(PathUnEquipBtn);
            if (btnUnequipLabel == null && btnUnequip != null)
                btnUnequipLabel = btnUnequip.GetComponentInChildren<TextMeshProUGUI>(true);
            if (btnLocate == null)
                btnLocate = FindButton(PathLocateMark);

            WireButtons();
        }

        static Button FindButton(Transform root, string path)
        {
            return root.Find(path)?.GetComponent<Button>();
        }

        Button FindButton(string path) => FindButton(transform, path);

        void WireButtons()
        {
            WireBtn(btnClose, OnCloseBtnClicked);
            WireBtn(btnLearn, OnLearnBtnClicked);
            WireBtn(btnUpgrade, OnUpgradeBtnClicked);
            WireBtn(btnEquip, OnEquipBtnClicked);
            WireBtn(btnUnequip, OnUnequipBtnClicked);
            WireBtn(btnLocate, OnLocateBtnClicked);

            if (btnMaxShow != null)
            {
                btnMaxShow.interactable = false;
            }
        }

        static void WireBtn(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn == null)
            {
                return;
            }

            btn.onClick.RemoveListener(action);
            btn.onClick.AddListener(action);
        }

        public void Show(int entryId)
        {
            EnsureReferences();

            var entry = SkillLearnCatalog.TryGetLearnEntry(entryId);
            if (entry == null || string.IsNullOrEmpty(entry.SkillId))
            {
                Hide();
                return;
            }

            _mode = DetailMode.LearnEntry;
            _entryId = entryId;
            _currentSkillId = entry.SkillId;
            _equippedSlotKind = SkillLoadoutSlotKind.Active;
            _equippedSlotIndex = -1;

            var sys = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.SkillSystem;
            bool isLearned = sys != null && sys.IsSkillLearned(entry.SkillId);
            int currentLevel = isLearned && sys != null
                ? sys.GetSkillLevel(entry.SkillId)
                : 0;

            var upgradeEntry = isLearned && sys != null
                ? SkillLearnCatalog.TryFindNextLevelEntry(entry.SkillId, currentLevel)
                : null;
            _upgradeEntryId = upgradeEntry != null ? upgradeEntry.EntryId : 0;
            _locateSchoolId = entry.SchoolId;

            ResolveLearnModeEquipState(entry.SkillId, sys);

            var skillCfg = SkillLibrary.GetSkillConfig(entry.SkillId);
            int entryLevel = entry.SkillLevel > 0 ? entry.SkillLevel : 1;

            SetText(nameLabel, SkillLearnEntryTextUtil.ResolveDisplayName(entry, skillCfg, entry.SkillId));
            if (isLearned)
            {
                if (upgradeEntry != null)
                {
                    int nextLevel = upgradeEntry.SkillLevel > 0 ? upgradeEntry.SkillLevel : currentLevel + 1;
                    SetText(levelChangeText, $"当前等级 {currentLevel} -> 下一等级 {nextLevel}");
                }
                else
                {
                    SetText(levelChangeText, $"当前等级 {currentLevel}");
                }
            }
            else
            {
                SetText(levelChangeText, $"学习等级 {entryLevel}");
            }

            SetText(costLineText, SkillLearnEntryTextUtil.BuildLearnCostLine(entry.LearnConds));
            SetText(statusLineText, SkillLearnEntryTextUtil.BuildDetailStatusLine(entry, isLearned));

            ResetActionButtons();

            bool isInnate = sys != null && sys.innateSkillIds.Contains(entry.SkillId);
            if (isInnate)
            {
                ShowMaxShowHint("默认技能");
            }
            else if (!isLearned)
            {
                SetActive(btnLearn, true);
                SetBtnLabel(btnLearnLabel, "学习");
            }
            else if (upgradeEntry != null)
            {
                SetActive(btnUpgrade, true);
                SetBtnLabel(btnUpgradeLabel, "升级");
            }
            else
            {
                ShowMaxShowHint("已满级");
            }

            if (!isInnate && isLearned)
            {
                SetActive(btnEquip, !_learnModeEquipped);
                SetActive(btnUnequip, _learnModeEquipped);
                SetBtnLabel(btnEquipLabel, "装配");
                SetBtnLabel(btnUnequipLabel, "卸下");
            }

            SetActive(btnLocate, _locateSchoolId > 0);
            gameObject.SetActive(true);
        }

        void ResolveLearnModeEquipState(string skillId, PlayerSkillSystem sys)
        {
            _learnModeEquipped = false;
            _learnModeEquippedKind = SkillLoadoutSlotKind.Active;
            _learnModeEquippedSlot = -1;

            if (sys == null || string.IsNullOrEmpty(skillId))
            {
                return;
            }

            var cfg = SkillLibrary.GetSkillConfig(skillId);
            if (cfg != null && cfg.IsPassive)
            {
                for (int i = 0; i < sys.PassiveSkillSlots.Length; i++)
                {
                    if (string.Equals(sys.PassiveSkillSlots[i], skillId, System.StringComparison.Ordinal))
                    {
                        _learnModeEquipped = true;
                        _learnModeEquippedKind = SkillLoadoutSlotKind.Passive;
                        _learnModeEquippedSlot = i;
                        return;
                    }
                }
            }
            else
            {
                for (int i = 3; i <= 7; i++)
                {
                    if (string.Equals(sys.NormalSkillSlots[i], skillId, System.StringComparison.Ordinal))
                    {
                        _learnModeEquipped = true;
                        _learnModeEquippedKind = SkillLoadoutSlotKind.Active;
                        _learnModeEquippedSlot = i;
                        return;
                    }
                }
            }
        }

        public void ShowEquipped(SkillLoadoutSlotKind slotKind, int slotIndex, string skillId)
        {
            EnsureReferences();

            if (slotIndex < 0 || string.IsNullOrEmpty(skillId))
            {
                Hide();
                return;
            }

            var sys = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.SkillSystem;
            if (sys == null)
            {
                Hide();
                return;
            }

            _mode = DetailMode.EquippedSlot;
            _entryId = 0;
            _upgradeEntryId = 0;
            _currentSkillId = skillId;
            _equippedSlotKind = slotKind;
            _equippedSlotIndex = slotIndex;
            _learnModeEquipped = false;
            _learnModeEquippedSlot = -1;

            bool isGranted = sys.IsGrantedPassive(skillId) || sys.IsGrantedActive(skillId);
            bool isDefaultSlot = slotKind == SkillLoadoutSlotKind.Active && slotIndex < 3;
            bool isDefault = isGranted || isDefaultSlot;

            var entry = SkillLearnCatalog.TryFindLearnEntryBySkillId(skillId);
            _locateSchoolId = entry != null ? entry.SchoolId : SkillLearnCatalog.TryFindSchoolIdForSkill(skillId);

            var skillCfg = SkillLibrary.GetSkillConfig(skillId);
            int level = sys.IsSkillLearned(skillId) ? sys.GetSkillLevel(skillId) : 1;

            string statusLine = isDefault
                ? (isGranted ? "固有技能，不可卸下" : "默认槽位技能，不可卸下")
                : SkillLearnEntryTextUtil.BuildEquippedStatusLine(slotKind, slotIndex);

            SetText(nameLabel, SkillLearnEntryTextUtil.ResolveDisplayName(entry, skillCfg, skillId));
            SetText(levelChangeText, $"当前等级 {level}");
            SetText(costLineText, string.Empty);
            SetText(statusLineText, statusLine);

            ResetActionButtons();

            if (isDefault)
            {
                ShowMaxShowHint(isGranted ? "固有技能" : "默认技能");
            }
            else
            {
                SetActive(btnUnequip, true);
                SetBtnLabel(btnUnequipLabel, "卸下");
            }

            SetActive(btnLocate, _locateSchoolId > 0);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _mode = DetailMode.None;
            _entryId = 0;
            _upgradeEntryId = 0;
            _locateSchoolId = 0;
            _currentSkillId = null;
            _equippedSlotIndex = -1;
            _learnModeEquipped = false;
            _learnModeEquippedSlot = -1;
            gameObject.SetActive(false);
        }

        void ResetActionButtons()
        {
            SetActive(btnLearn, false);
            SetActive(btnUpgrade, false);
            SetActive(btnMaxShow, false);
            SetActive(btnEquip, false);
            SetActive(btnUnequip, false);
            SetActive(btnLocate, false);
        }

        void ShowMaxShowHint(string label)
        {
            SetActive(btnMaxShow, true);
            SetBtnLabel(btnMaxShowLabel, label);
        }

        void OnCloseBtnClicked()
        {
            _onCloseClicked?.Invoke();
            Hide();
        }

        void OnLearnBtnClicked()
        {
            if (_mode == DetailMode.LearnEntry && _entryId > 0)
            {
                _onLearnClicked?.Invoke(_entryId);
            }
        }

        void OnUpgradeBtnClicked()
        {
            if (_mode == DetailMode.LearnEntry && _upgradeEntryId > 0)
            {
                _onUpgradeClicked?.Invoke(_upgradeEntryId);
            }
        }

        void OnEquipBtnClicked()
        {
            if (_mode != DetailMode.LearnEntry || string.IsNullOrEmpty(_currentSkillId))
            {
                return;
            }

            _onEquipClicked?.Invoke(_currentSkillId);
        }

        void OnUnequipBtnClicked()
        {
            if (_mode == DetailMode.LearnEntry)
            {
                if (_learnModeEquipped && _learnModeEquippedSlot >= 0)
                {
                    _onUnequipClicked?.Invoke(_learnModeEquippedKind, _learnModeEquippedSlot);
                }

                return;
            }

            if (_mode == DetailMode.EquippedSlot && _equippedSlotIndex >= 0)
            {
                _onUnequipClicked?.Invoke(_equippedSlotKind, _equippedSlotIndex);
            }
        }

        void OnLocateBtnClicked()
        {
            if (_locateSchoolId > 0)
            {
                _onLocateClicked?.Invoke(_locateSchoolId);
            }
        }

        static void SetActive(Behaviour comp, bool active)
        {
            if (comp != null)
            {
                comp.gameObject.SetActive(active);
            }
        }

        static void SetBtnLabel(TextMeshProUGUI label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }

        static void SetText(TextMeshProUGUI text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }
    }
}
