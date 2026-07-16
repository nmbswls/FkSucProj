using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;
using My.Home;
using My.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Home
{
    public sealed class TownFacilityDetailPanel : PanelWithInput
    {
        public const string PanelIdConst = "TownFacilityDetailPanel";

        [SerializeField] TextMeshProUGUI txtTitle;
        [SerializeField] TextMeshProUGUI txtLevel;
        [SerializeField] TextMeshProUGUI txtDesc;
        [SerializeField] GameObject statusCardRoot;
        [SerializeField] TextMeshProUGUI txtStatusTitle;
        [SerializeField] TextMeshProUGUI txtStatusDesc;
        [SerializeField] TextMeshProUGUI txtDailyOutput;
        [SerializeField] GameObject nextLevelCardRoot;
        [SerializeField] TextMeshProUGUI txtNextLevelHeader;
        [SerializeField] TextMeshProUGUI txtNextLevelTitle;
        [SerializeField] TextMeshProUGUI txtNextLevelDesc;
        [SerializeField] TextMeshProUGUI txtUpgradeCosts;
        [SerializeField] TextMeshProUGUI txtNextDailyOutput;
        [SerializeField] TextMeshProUGUI txtUpgradeHint;
        [SerializeField] TextMeshProUGUI txtSupervisorHeader;
        [SerializeField] TownFacilitySupervisorSlotView[] supervisorSlots;
        [SerializeField] TextMeshProUGUI txtHelperHeader;
        [SerializeField] TextMeshProUGUI txtWorkforceValue;
        [SerializeField] Slider workforceSlider;
        [SerializeField] TextMeshProUGUI txtRenovationHeader;
        [SerializeField] RectTransform renovationListRoot;
        [SerializeField] TownFacilityRenovationSlotView[] renovationSlots;
        [SerializeField] Button btnLearnRenovation;
        [SerializeField] TextMeshProUGUI txtLearnRenovation;
        [SerializeField] Button btnUpgrade;
        [SerializeField] TextMeshProUGUI txtUpgrade;
        [SerializeField] Button btnClose;

        TownFacilityDetailOpenArgs _args;
        FixedFacilityInfo _facility;
        string _selectedRenovationId;
        string _activeRenovationId;
        int _facilityLevel;

        const int ExpectedRenovationSlotCount = 3;
        const int ExpectedSupervisorSlotCount = 2;

        int _siteId;
        long _instanceId;
        string _facilityId;
        FacilityDefinition _staticDef;

        static readonly Color ColorActive = new(0.22f, 0.45f, 0.62f, 0.98f);
        static readonly Color ColorSelected = new(0.28f, 0.38f, 0.48f, 0.95f);
        static readonly Color ColorNormal = new(0.2f, 0.24f, 0.3f, 0.9f);
        static readonly Color ColorLocked = new(0.16f, 0.16f, 0.18f, 0.75f);

        System.Action<string, string, int> _onFacilityLevelChanged;

        void Awake()
        {
            panelId = PanelIdConst;
            layer = UILayer.Popup;
            EnsureUpgradeSectionReferences();
            ValidatePrefabBindings();

            if (btnClose != null)
            {
                btnClose.onClick.AddListener(CloseSelf);
            }

            if (workforceSlider != null)
            {
                workforceSlider.onValueChanged.AddListener(OnWorkforceChanged);
            }

            if (btnUpgrade != null)
            {
                btnUpgrade.onClick.AddListener(OnClickUpgrade);
            }

            if (btnLearnRenovation != null)
            {
                btnLearnRenovation.onClick.AddListener(OnClickLearnRenovation);
            }
        }

        public static TownFacilityDetailPanel Open(TownFacilityDetailOpenArgs args)
        {
            return UIManager.Instance.ShowPanel(PanelIdConst, args) as TownFacilityDetailPanel;
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            _args = data as TownFacilityDetailOpenArgs;
            Refresh();
        }

        public override void Show()
        {
            base.Show();
            HomeTownViewController.EnterFacilityManagementView();
            var glm = MainGameManager.Instance?.gameLogicManager;
            var hm = glm?.homeDataManager;
            if (hm != null)
            {
                hm.EvOnFacilityUpdate -= Refresh;
                hm.EvOnFacilityUpdate += Refresh;
            }

            if (glm?.townFacilityDevelopmentSystem != null)
            {
                _onFacilityLevelChanged ??= OnFacilityLevelChanged;
                glm.townFacilityDevelopmentSystem.EvOnFacilityDevelopmentLevelChanged -= _onFacilityLevelChanged;
                glm.townFacilityDevelopmentSystem.EvOnFacilityDevelopmentLevelChanged += _onFacilityLevelChanged;
            }

            Refresh();
        }

        public override void Hide()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            var hm = glm?.homeDataManager;
            if (hm != null)
            {
                hm.EvOnFacilityUpdate -= Refresh;
            }

            if (glm?.townFacilityDevelopmentSystem != null && _onFacilityLevelChanged != null)
            {
                glm.townFacilityDevelopmentSystem.EvOnFacilityDevelopmentLevelChanged -= _onFacilityLevelChanged;
            }

            HomeTownViewController.LeaveFacilityManagementView();
            base.Hide();
        }

        void OnFacilityLevelChanged(string logicAreaId, string facilityId, int level)
        {
            if (_args == null)
            {
                return;
            }

            if (!string.Equals(logicAreaId, _args.LogicAreaId, System.StringComparison.Ordinal))
            {
                return;
            }

            if (!string.Equals(facilityId, _facilityId, System.StringComparison.Ordinal))
            {
                return;
            }

            Refresh();
        }

        void Refresh()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            var hm = glm?.homeDataManager;
            if (_args == null || hm == null)
            {
                SetEmpty("Invalid facility");
                return;
            }

            if (string.IsNullOrEmpty(_args.LogicAreaId))
            {
                _args.LogicAreaId = TownFacilityUtil.ResolveCurrentLogicAreaId(glm?.AreaManager);
            }

            if (!string.IsNullOrEmpty(_args.LogicAreaId))
            {
                hm.SetTownContext(_args.LogicAreaId);
            }

            hm.RefreshFixedFacilities();
            int siteId = _args.SiteId;
            string facilityId = _args.FacilityId;
            if (siteId <= 0 && !string.IsNullOrEmpty(facilityId) && !string.IsNullOrEmpty(_args.LogicAreaId))
            {
                siteId = TownFacilitySiteCatalog.FindByMapAndFacility(_args.LogicAreaId, facilityId)?.Id ?? 0;
                _args.SiteId = siteId;
            }

            if (siteId > 0)
            {
                var site = TownFacilitySiteCatalog.Get(siteId);
                facilityId = site?.FacilityCfgId ?? facilityId;
                _args.FacilityId = facilityId;
            }

            if (string.IsNullOrEmpty(facilityId))
            {
                SetEmpty("Facility not found");
                return;
            }

            _facility = hm.FixedFacilities.Find(
                f => !f.Removed
                     && f.FacilityId == facilityId
                     && (_args.InstanceId == 0 || f.InstanceId == _args.InstanceId));
            if (_facility != null)
            {
                hm.EnsureFacilityPersistRecord(_facility);
            }

            var devDef = FacilityDevelopmentCatalog.GetDefinition(facilityId);
            var staticDef = _facility != null
                ? hm.GetFacilityDefinition(_facility)
                : FacilityDefinitionCatalog.Get(facilityId);
            _staticDef = staticDef;
            long instanceId = _facility?.InstanceId ?? _args.InstanceId;
            _siteId = siteId;
            _instanceId = instanceId;
            _facilityId = facilityId;
            string displayName = devDef?.DisplayName
                ?? staticDef?.DisplayName
                ?? facilityId;
            _facilityLevel = siteId > 0
                ? glm.townFacilityDevelopmentSystem?.GetFacilityDevelopmentLevel(_args.LogicAreaId, siteId) ?? 0
                : glm.townFacilityDevelopmentSystem?.GetFacilityDevelopmentLevel(_args.LogicAreaId, instanceId, facilityId) ?? 0;
            var levelDef = _facilityLevel > 0 ? FacilityDevelopmentCatalog.GetLevel(facilityId, _facilityLevel) : null;
            var nextLevelDef = FacilityDevelopmentCatalog.GetLevel(facilityId, _facilityLevel + 1);

            _activeRenovationId = siteId > 0
                ? hm.GetTownFacilityBySite(hm.CurrentTownId, siteId)?.RenovationId
                : _facility != null
                    ? hm.GetFacilityRenovation(_facility.InstanceId, facilityId)
                    : hm.GetTownFacility(hm.CurrentTownId, 0, facilityId)?.RenovationId;
            if (string.IsNullOrEmpty(_selectedRenovationId))
            {
                _selectedRenovationId = _activeRenovationId;
            }

            if (txtTitle != null)
            {
                txtTitle.text = displayName;
            }

            if (txtLevel != null)
            {
                txtLevel.text = _facilityLevel > 0 ? $"Lv.{_facilityLevel}" : "未建造";
            }

            var renovations = FacilityRenovationCatalog.GetRenovationsForFacility(facilityId);
            bool showRenovations = renovations.Count > 0 && _facilityLevel > 0;
            if (txtDesc != null)
            {
                txtDesc.gameObject.SetActive(false);
            }

            RefreshUpgradeSection(glm, _args.LogicAreaId, siteId, instanceId, facilityId, levelDef, nextLevelDef);

            if (txtRenovationHeader != null)
            {
                txtRenovationHeader.gameObject.SetActive(showRenovations);
                txtRenovationHeader.text = "改造项";
            }

            RefreshWorkforce(hm, _staticDef, _facilityLevel, _siteId, _instanceId, _facilityId);
            RefreshSupervisors(hm, _staticDef, _facilityLevel, _siteId, _instanceId, _facilityId);
            if (showRenovations)
            {
                RefreshRenovationSlots(glm, facilityId, renovations);
            }
            else
            {
                HideRenovationSlots();
            }

            RefreshLearnButton(glm, facilityId);
            RefreshUpgradeButton(glm, _args.LogicAreaId, siteId, instanceId, facilityId, _facilityLevel, nextLevelDef);
        }

        void RefreshUpgradeSection(
            GameLogicManager glm,
            string logicAreaId,
            int siteId,
            long instanceId,
            string facilityId,
            FacilityDevelopmentLevel levelDef,
            FacilityDevelopmentLevel nextLevelDef)
        {
            bool hasStatusCard = statusCardRoot != null || txtStatusTitle != null;
            if (statusCardRoot != null)
            {
                statusCardRoot.SetActive(hasStatusCard);
            }

            if (txtStatusTitle != null)
            {
                if (_facilityLevel > 0)
                {
                    string stageName = string.IsNullOrEmpty(levelDef?.DisplayName) ? string.Empty : $" · {levelDef.DisplayName}";
                    txtStatusTitle.text = $"当前  Lv.{_facilityLevel}{stageName}";
                }
                else
                {
                    txtStatusTitle.text = "当前  未建造";
                }
            }

            if (txtStatusDesc != null)
            {
                txtStatusDesc.text = _facilityLevel > 0
                    ? levelDef?.Desc ?? string.Empty
                    : nextLevelDef?.Desc ?? "建造后可派遣人手并启用产出";
            }

            if (txtDailyOutput != null)
            {
                if (_facilityLevel > 0)
                {
                    string interval = TownFacilityDetailUiFormatter.FormatOutputInterval(1);
                    string items = TownFacilityDetailUiFormatter.FormatOutputItems(levelDef?.DailyOutputs);
                    txtDailyOutput.text = $"产出  {interval}  {items}";
                }
                else
                {
                    txtDailyOutput.text = "产出  <color=#9AA8B8>建造后解锁</color>";
                }
            }

            bool showNext = nextLevelDef != null;
            if (nextLevelCardRoot != null)
            {
                nextLevelCardRoot.SetActive(showNext);
            }

            if (!showNext)
            {
                return;
            }

            if (txtNextLevelHeader != null)
            {
                txtNextLevelHeader.text = _facilityLevel <= 0 ? "建造预览" : "升级预览";
            }

            if (txtNextLevelTitle != null)
            {
                string stageName = string.IsNullOrEmpty(nextLevelDef.DisplayName) ? string.Empty : $" · {nextLevelDef.DisplayName}";
                txtNextLevelTitle.text = $"Lv.{nextLevelDef.Level}{stageName}";
            }

            if (txtNextLevelDesc != null)
            {
                txtNextLevelDesc.text = nextLevelDef.Desc ?? string.Empty;
            }

            var pdm = glm?.playerDataManager;
            if (txtUpgradeCosts != null)
            {
                string costs = TownFacilityDetailUiFormatter.FormatUpgradeCosts(nextLevelDef.UnlockCosts, pdm);
                txtUpgradeCosts.text = $"消耗\n{costs}";
            }

            if (txtNextDailyOutput != null)
            {
                string interval = TownFacilityDetailUiFormatter.FormatOutputInterval(1);
                string items = TownFacilityDetailUiFormatter.FormatOutputItems(nextLevelDef.DailyOutputs);
                txtNextDailyOutput.text = $"升级后产出  {interval}  {items}";
            }

            string failReason = null;
            bool canUpgrade = siteId > 0
                ? glm?.townFacilityDevelopmentSystem?.CanUpgradeFacility(logicAreaId, siteId, out failReason) == true
                : glm?.townFacilityDevelopmentSystem?.CanUpgradeFacility(
                    logicAreaId,
                    instanceId,
                    facilityId,
                    out failReason) == true;

            if (txtUpgradeHint != null)
            {
                txtUpgradeHint.text = TownFacilityDetailUiFormatter.BuildUpgradeHint(
                    canUpgrade,
                    failReason,
                    nextLevelDef,
                    glm);
            }
        }

        void RefreshSupervisors(HomeDataManager hm, FacilityDefinition staticDef, int level, int siteId, long instanceId, string facilityId)
        {
            int slotCount = hm.GetMaxSupervisorSlots(staticDef);
            bool show = level > 0 && slotCount > 0;
            if (txtSupervisorHeader != null)
            {
                txtSupervisorHeader.gameObject.SetActive(show);
                txtSupervisorHeader.text = "领头者";
            }

            if (supervisorSlots == null)
            {
                return;
            }

            for (int i = 0; i < supervisorSlots.Length; i++)
            {
                var slot = supervisorSlots[i];
                if (slot == null)
                {
                    continue;
                }

                if (!show || i >= slotCount)
                {
                    slot.gameObject.SetActive(false);
                    continue;
                }

                slot.gameObject.SetActive(true);
                string assignedKey = hm.GetFacilitySupervisor(siteId, instanceId, facilityId, i);
                string display = "选择监工";
                string desc = "点击指派具名角色";
                if (!string.IsNullOrEmpty(assignedKey))
                {
                    var info = CfgMgr.Cfgs?.TbCharacterInfo?.GetOrDefault(assignedKey);
                    var supervisorCfg = TownFacilitySupervisorCatalog.Get(assignedKey);
                    display = info?.Name ?? assignedKey;
                    desc = string.IsNullOrEmpty(supervisorCfg?.DisplayTitle)
                        ? "已派驻"
                        : supervisorCfg.DisplayTitle;
                }

                if (slot.Label != null)
                {
                    slot.Label.text = display;
                }

                if (slot.Desc != null)
                {
                    slot.Desc.text = desc;
                }

                if (slot.Button != null)
                {
                    slot.Button.onClick.RemoveAllListeners();
                    int capturedSlot = i;
                    slot.Button.onClick.AddListener(() => OnClickSupervisorSlot(capturedSlot));
                }
            }
        }

        void OnClickSupervisorSlot(int slotIndex)
        {
            TownFacilitySupervisorPickPanel.Open(new TownFacilitySupervisorPickOpenArgs
            {
                LogicAreaId = _args.LogicAreaId,
                SiteId = _siteId,
                InstanceId = _instanceId,
                FacilityId = _facilityId,
                SlotIndex = slotIndex,
                CurrentCharacterKey = MainGameManager.Instance?.gameLogicManager?.homeDataManager
                    ?.GetFacilitySupervisor(_siteId, _instanceId, _facilityId, slotIndex),
            });
        }

        void RefreshWorkforce(HomeDataManager hm, FacilityDefinition staticDef, int level, int siteId, long instanceId, string facilityId)
        {
            bool show = level > 0
                        && staticDef != null
                        && hm.SupportsHelperWorkforce(staticDef);
            if (txtHelperHeader != null)
            {
                txtHelperHeader.gameObject.SetActive(show);
                txtHelperHeader.text = "帮工人手";
            }

            if (workforceSlider != null)
            {
                workforceSlider.gameObject.SetActive(show);
            }

            if (!show)
            {
                return;
            }

            int capacity = hm.GetHelperWorkforceCapacity(staticDef);
            int workers = hm.GetHelperWorkforce(siteId, instanceId, facilityId);
            workforceSlider.wholeNumbers = true;
            workforceSlider.minValue = 0;
            workforceSlider.maxValue = Mathf.Max(1, capacity);
            workforceSlider.SetValueWithoutNotify(workers);
            if (txtWorkforceValue != null)
            {
                txtWorkforceValue.text = $"帮工 {workers}/{capacity}";
            }
        }

        void RefreshRenovationSlots(GameLogicManager glm, string facilityId, List<FacilityRenovationDefinition> renovations)
        {
            if (renovationListRoot != null)
            {
                renovationListRoot.gameObject.SetActive(true);
            }

            if (renovationSlots == null || renovationSlots.Length == 0)
            {
                return;
            }

            for (int i = 0; i < renovationSlots.Length; i++)
            {
                var slot = renovationSlots[i];
                if (slot == null)
                {
                    continue;
                }

                if (i >= renovations.Count || renovations[i] == null)
                {
                    slot.gameObject.SetActive(false);
                    continue;
                }

                var renovation = renovations[i];
                slot.gameObject.SetActive(true);
                if (slot.Label != null)
                {
                    slot.Label.text = renovation.DisplayName;
                }

                if (slot.Desc != null)
                {
                    var goldPreview = FormatGoldOutputPreview(renovation);
                    slot.Desc.text = string.IsNullOrEmpty(goldPreview)
                        ? renovation.Desc
                        : $"{renovation.Desc}\n{goldPreview}";
                }

                if (slot.Button != null)
                {
                    slot.Button.onClick.RemoveAllListeners();
                    var captured = renovation;
                    slot.Button.onClick.AddListener(() => OnSelectRenovation(captured.RenovationId));
                }

                ApplyRenovationRowStyle(slot, renovation);
            }
        }

        void HideRenovationSlots()
        {
            if (renovationListRoot != null)
            {
                renovationListRoot.gameObject.SetActive(false);
            }

            if (renovationSlots == null)
            {
                return;
            }

            foreach (var slot in renovationSlots)
            {
                if (slot != null)
                {
                    slot.gameObject.SetActive(false);
                }
            }
        }

        static string FormatGoldOutputPreview(FacilityRenovationDefinition renovation)
        {
            if (renovation?.OutputItems == null)
            {
                return null;
            }

            foreach (var output in renovation.OutputItems)
            {
                if (output != null && output.ItemId == "gold" && output.Count > 0)
                {
                    return $"每日金币 +{output.Count}";
                }
            }

            return null;
        }

        void ApplyRenovationRowStyle(TownFacilityRenovationSlotView slot, FacilityRenovationDefinition renovation)
        {
            if (slot?.Button == null || renovation == null)
            {
                return;
            }

            var img = slot.Button.GetComponent<Image>();
            if (img == null)
            {
                return;
            }

            bool isActive = renovation.RenovationId == _activeRenovationId;
            bool isSelected = renovation.RenovationId == _selectedRenovationId;
            bool unlocked = _facilityLevel >= renovation.MinLevel
                && FacilityRenovationCatalog.CanLearn(renovation, _facilityLevel, MainGameManager.Instance?.gameLogicManager, out _);

            if (isActive)
            {
                img.color = ColorActive;
            }
            else if (isSelected)
            {
                img.color = ColorSelected;
            }
            else if (unlocked)
            {
                img.color = ColorNormal;
            }
            else
            {
                img.color = ColorLocked;
            }
        }

        void RefreshRenovationRowStyles(string facilityId)
        {
            var renovations = FacilityRenovationCatalog.GetRenovationsForFacility(facilityId);
            if (renovationSlots == null)
            {
                return;
            }

            for (int i = 0; i < renovationSlots.Length && i < renovations.Count; i++)
            {
                if (renovationSlots[i] != null && renovationSlots[i].gameObject.activeSelf)
                {
                    ApplyRenovationRowStyle(renovationSlots[i], renovations[i]);
                }
            }
        }

        void RefreshLearnButton(GameLogicManager glm, string facilityId)
        {
            if (btnLearnRenovation == null)
            {
                return;
            }

            bool show = _facilityLevel > 0
                        && !string.IsNullOrEmpty(_selectedRenovationId)
                        && _selectedRenovationId != _activeRenovationId;
            var renovation = show ? FacilityRenovationCatalog.Get(facilityId, _selectedRenovationId) : null;
            bool canLearn = renovation != null
                            && FacilityRenovationCatalog.CanLearn(renovation, _facilityLevel, glm, out _);

            btnLearnRenovation.gameObject.SetActive(show && canLearn);
            btnLearnRenovation.interactable = canLearn;
            if (txtLearnRenovation != null)
            {
                txtLearnRenovation.text = "学习";
            }
        }

        void RefreshUpgradeButton(GameLogicManager glm, string logicAreaId, int siteId, long instanceId, string facilityId, int level, FacilityDevelopmentLevel nextLevelDef)
        {
            if (btnUpgrade == null)
            {
                return;
            }

            string failReason = null;
            bool canUpgrade = siteId > 0
                ? glm?.townFacilityDevelopmentSystem?.CanUpgradeFacility(logicAreaId, siteId, out failReason) == true
                : glm?.townFacilityDevelopmentSystem?.CanUpgradeFacility(
                    logicAreaId,
                    instanceId,
                    facilityId,
                    out failReason) == true;
            btnUpgrade.gameObject.SetActive(nextLevelDef != null);
            btnUpgrade.interactable = canUpgrade;
            if (txtUpgrade != null)
            {
                if (nextLevelDef == null)
                {
                    txtUpgrade.text = "已满级";
                }
                else if (canUpgrade)
                {
                    txtUpgrade.text = level <= 0 ? $"建造至 Lv.{nextLevelDef.Level}" : $"升级至 Lv.{nextLevelDef.Level}";
                }
                else
                {
                    txtUpgrade.text = level <= 0 ? "建造条件未满足" : "升级条件未满足";
                }
            }
        }

        void OnSelectRenovation(string renovationId)
        {
            _selectedRenovationId = renovationId;
            RefreshRenovationRowStyles(_args.FacilityId);
            RefreshLearnButton(MainGameManager.Instance?.gameLogicManager, _args.FacilityId);
        }

        void OnClickLearnRenovation()
        {
            var hm = MainGameManager.Instance?.gameLogicManager?.homeDataManager;
            if (hm == null || string.IsNullOrEmpty(_selectedRenovationId))
            {
                return;
            }

            if (_args.SiteId > 0)
            {
                if (hm.TryLearnFacilityRenovation(_args.SiteId, _selectedRenovationId, out _))
                {
                    _activeRenovationId = _selectedRenovationId;
                    Refresh();
                }

                return;
            }

            long instanceId = _facility?.InstanceId ?? _args.InstanceId;
            string facilityId = _facility?.FacilityId ?? _args.FacilityId;
            if (hm.TryLearnFacilityRenovation(instanceId, facilityId, _selectedRenovationId, out _))
            {
                _activeRenovationId = _selectedRenovationId;
                Refresh();
            }
        }

        void OnWorkforceChanged(float value)
        {
            var hm = MainGameManager.Instance?.gameLogicManager?.homeDataManager;
            if (hm == null)
            {
                return;
            }

            int workers = Mathf.RoundToInt(value);
            if (hm.TrySetHelperWorkforce(_siteId, _instanceId, _facilityId, workers, out _))
            {
                if (txtWorkforceValue != null && _staticDef != null)
                {
                    int capacity = hm.GetHelperWorkforceCapacity(_staticDef);
                    txtWorkforceValue.text = $"帮工 {workers}/{capacity}";
                }
            }
        }

        void OnClickUpgrade()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm?.townFacilityDevelopmentSystem == null || _args == null)
            {
                return;
            }

            string failReason = null;
            bool upgraded;
            if (_args.SiteId > 0)
            {
                upgraded = glm.townFacilityDevelopmentSystem.TryUpgradeFacility(_args.LogicAreaId, _args.SiteId, out failReason);
            }
            else
            {
                long instanceId = _facility?.InstanceId ?? _args.InstanceId;
                string facilityId = _facility?.FacilityId ?? _args.FacilityId;
                if (string.IsNullOrEmpty(facilityId))
                {
                    return;
                }

                upgraded = glm.townFacilityDevelopmentSystem.TryUpgradeFacility(
                    _args.LogicAreaId,
                    instanceId,
                    facilityId,
                    out failReason);
            }

            if (upgraded)
            {
                Refresh();
                return;
            }

            ShowUpgradeFailHint(failReason);
            Refresh();
        }

        void ShowUpgradeFailHint(string failReason)
        {
            string message = TownFacilityDetailUiFormatter.ResolveFailReason(failReason);
            if (txtUpgradeHint != null)
            {
                txtUpgradeHint.text = $"<color=#E88888>{message}</color>";
            }

            UIEventGrantToastPanel.ShowToast("设施升级", string.Empty, message);
        }

        void EnsureUpgradeSectionReferences()
        {
            var content = transform.Find("Content");
            if (content == null)
            {
                return;
            }

            statusCardRoot ??= content.Find("StatusCard")?.gameObject;
            txtStatusTitle ??= content.Find("StatusCard/StatusTitle")?.GetComponent<TextMeshProUGUI>();
            txtStatusDesc ??= content.Find("StatusCard/StatusDesc")?.GetComponent<TextMeshProUGUI>();
            txtDailyOutput ??= content.Find("StatusCard/DailyOutput")?.GetComponent<TextMeshProUGUI>();
            nextLevelCardRoot ??= content.Find("NextLevelCard")?.gameObject;
            txtNextLevelHeader ??= content.Find("NextLevelCard/NextLevelHeader")?.GetComponent<TextMeshProUGUI>();
            txtNextLevelTitle ??= content.Find("NextLevelCard/NextLevelTitle")?.GetComponent<TextMeshProUGUI>();
            txtNextLevelDesc ??= content.Find("NextLevelCard/NextLevelDesc")?.GetComponent<TextMeshProUGUI>();
            txtUpgradeCosts ??= content.Find("NextLevelCard/UpgradeCosts")?.GetComponent<TextMeshProUGUI>();
            txtNextDailyOutput ??= content.Find("NextLevelCard/NextDailyOutput")?.GetComponent<TextMeshProUGUI>();
            txtUpgradeHint ??= content.Find("NextLevelCard/UpgradeHint")?.GetComponent<TextMeshProUGUI>();

            if (statusCardRoot != null)
            {
                return;
            }

            BuildUpgradeSectionUi(content as RectTransform);
        }

        void BuildUpgradeSectionUi(RectTransform content)
        {
            const float shiftDown = 168f;
            ShiftRect(content.Find("SupervisorHeader") as RectTransform, shiftDown);
            ShiftRect(content.Find("SupervisorList") as RectTransform, shiftDown);
            ShiftRect(content.Find("HelperHeader") as RectTransform, shiftDown);
            ShiftRect(content.Find("WorkforceRow") as RectTransform, shiftDown);
            ShiftRect(content.Find("RenovationHeader") as RectTransform, shiftDown);
            ShiftRect(content.Find("RenovationList") as RectTransform, shiftDown);
            ShiftRect(content.Find("BtnLearnRenovation") as RectTransform, shiftDown);

            var cardRect = content;
            cardRect.offsetMin = new Vector2(cardRect.offsetMin.x, cardRect.offsetMin.y - 70f);
            cardRect.offsetMax = new Vector2(cardRect.offsetMax.x, cardRect.offsetMax.y + 10f);

            var titleFont = txtTitle != null ? txtTitle.font : null;
            statusCardRoot = CreatePanelCard(content, "StatusCard", new Color(.14f, .17f, .22f, .95f),
                new Vector2(16f, -188f), new Vector2(-16f, -60f));
            txtStatusTitle = CreateInfoText(statusCardRoot.transform, "StatusTitle", "当前等级", 15, true,
                new Vector2(12f, -34f), new Vector2(-12f, -10f), titleFont);
            txtStatusDesc = CreateInfoText(statusCardRoot.transform, "StatusDesc", string.Empty, 14, false,
                new Vector2(12f, -72f), new Vector2(-12f, -36f), titleFont);
            txtDailyOutput = CreateBottomInfoText(statusCardRoot.transform, "DailyOutput", "每日产出", 14,
                new Vector2(12f, 10f), new Vector2(-12f, 34f), titleFont);

            nextLevelCardRoot = CreatePanelCard(content, "NextLevelCard", new Color(.12f, .16f, .21f, .95f),
                new Vector2(16f, -332f), new Vector2(-16f, -196f));
            txtNextLevelHeader = CreateInfoText(nextLevelCardRoot.transform, "NextLevelHeader", "升级预览", 14, false,
                new Vector2(12f, -30f), new Vector2(-12f, -8f), titleFont);
            txtNextLevelHeader.color = new Color(.62f, .78f, .92f, 1f);
            txtNextLevelTitle = CreateInfoText(nextLevelCardRoot.transform, "NextLevelTitle", "Lv.2", 15, true,
                new Vector2(12f, -58f), new Vector2(-12f, -34f), titleFont);
            txtNextLevelDesc = CreateInfoText(nextLevelCardRoot.transform, "NextLevelDesc", string.Empty, 13, false,
                new Vector2(12f, -88f), new Vector2(-12f, -60f), titleFont);
            txtUpgradeCosts = CreateInfoText(nextLevelCardRoot.transform, "UpgradeCosts", "消耗", 13, false,
                new Vector2(12f, -150f), new Vector2(-12f, -92f), titleFont);
            txtNextDailyOutput = CreateInfoText(nextLevelCardRoot.transform, "NextDailyOutput", "升级后产出", 13, false,
                new Vector2(12f, -178f), new Vector2(-12f, -154f), titleFont);
            txtUpgradeHint = CreateBottomInfoText(nextLevelCardRoot.transform, "UpgradeHint", string.Empty, 12,
                new Vector2(12f, 8f), new Vector2(-12f, 32f), titleFont);

            if (txtDesc != null)
            {
                txtDesc.gameObject.SetActive(false);
            }
        }

        static void ShiftRect(RectTransform rect, float deltaY)
        {
            if (rect == null)
            {
                return;
            }

            rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y - deltaY);
            rect.offsetMax = new Vector2(rect.offsetMax.x, rect.offsetMax.y - deltaY);
        }

        static GameObject CreatePanelCard(RectTransform parent, string name, Color color, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            go.GetComponent<Image>().color = color;
            return go;
        }

        static TextMeshProUGUI CreateInfoText(
            Transform parent,
            string name,
            string value,
            float fontSize,
            bool bold,
            Vector2 offsetMin,
            Vector2 offsetMax,
            TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.enableWordWrapping = true;
            text.richText = true;
            text.raycastTarget = false;
            text.color = Color.white;
            if (bold)
            {
                text.fontStyle = FontStyles.Bold;
            }

            if (font != null)
            {
                text.font = font;
            }

            return text;
        }

        static TextMeshProUGUI CreateBottomInfoText(
            Transform parent,
            string name,
            string value,
            float fontSize,
            Vector2 offsetMin,
            Vector2 offsetMax,
            TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.enableWordWrapping = true;
            text.richText = true;
            text.raycastTarget = false;
            text.color = Color.white;
            if (font != null)
            {
                text.font = font;
            }

            return text;
        }

        void ValidatePrefabBindings()
        {
            if (txtTitle == null || renovationListRoot == null || renovationSlots == null || renovationSlots.Length < ExpectedRenovationSlotCount)
            {
                Debug.LogError("TownFacilityDetailPanel prefab bindings missing.");
            }
        }

        void SetEmpty(string message)
        {
            if (txtTitle != null) txtTitle.text = message;
            if (txtLevel != null) txtLevel.text = string.Empty;
            if (txtDesc != null) txtDesc.text = string.Empty;
            if (statusCardRoot != null) statusCardRoot.SetActive(false);
            if (nextLevelCardRoot != null) nextLevelCardRoot.SetActive(false);
            if (workforceSlider != null) workforceSlider.gameObject.SetActive(false);
            if (btnUpgrade != null) btnUpgrade.gameObject.SetActive(false);
            if (btnLearnRenovation != null) btnLearnRenovation.gameObject.SetActive(false);
            if (renovationListRoot != null) renovationListRoot.gameObject.SetActive(false);
            HideRenovationSlots();
        }

        void CloseSelf()
        {
            UIManager.Instance.HidePanel(PanelIdConst);
        }

        public override bool OnCancel()
        {
            CloseSelf();
            return true;
        }
    }
}
