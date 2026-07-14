using System.Collections.Generic;
using System.Text;
using cfg.demo;
using My;
using My.Config;
using My.SecretBase;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.UI
{
    public class SecretBaseNpcHubPanel : PanelWithInput, IPointerClickHandler
    {
        public const string PanelIdConst = "SecretBaseNpcHubPanel";

        public class Payload
        {
            public SecretBaseCharacter CharacterRow;
        }

        [Header("Hub")]
        [SerializeField] Image portraitImage;
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI favorText;
        [SerializeField] Button btnTalk;
        [SerializeField] Button btnGiftMode;
        [SerializeField] Button btnClose;
        [SerializeField] GameObject hubRoot;
        [SerializeField] GameObject giftPickerRoot;

        [Header("Gift")]
        [SerializeField] RectTransform giftListContent;
        [SerializeField] SecretBaseNpcGiftCell giftCellTemplate;
        [SerializeField] Button btnGiveGift;
        [SerializeField] TextMeshProUGUI giftHintText;
        [SerializeField] Button btnGiftBack;

        SecretBaseCharacter _row;
        string _selectedGiftItemId;
        readonly List<SecretBaseNpcGiftCell> _giftCells = new();
        readonly List<Button> _customOptionButtons = new();
        bool _listenersBound;
        bool _showFavorDetails;
        string _customOptionGroupId;

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = PanelIdConst;
            }

            Layer = UILayer.Popup;
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            BindListeners();
            if (giftCellTemplate != null)
            {
                giftCellTemplate.gameObject.SetActive(false);
            }
        }

        void BindListeners()
        {
            if (_listenersBound)
            {
                return;
            }

            _listenersBound = true;
            if (btnClose != null)
            {
                btnClose.onClick.AddListener(CloseSelf);
            }

            if (btnTalk != null)
            {
                btnTalk.onClick.AddListener(OnTalk);
            }

            if (btnGiftMode != null)
            {
                btnGiftMode.onClick.AddListener(ShowGiftPicker);
            }

            if (btnGiftBack != null)
            {
                btnGiftBack.onClick.AddListener(ShowHub);
            }

            if (btnGiveGift != null)
            {
                btnGiveGift.onClick.AddListener(OnGiveGift);
            }
        }

        public override void Setup(object data = null)
        {
            _row = (data as Payload)?.CharacterRow;
            _selectedGiftItemId = null;
            _showFavorDetails = false;
            _customOptionGroupId = string.Empty;
            BindListeners();
            RefreshHub();
            ShowHub();
        }

        public override void Show()
        {
            base.Show();
            RefreshHub();
        }

        void RefreshHub()
        {
            if (_row == null)
            {
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            var registry = glm?.worldPersistState?.NpcCharacters;
            string displayName = _row.CharacterKey;
            var info = CfgMgr.Cfgs?.TbCharacterInfo?.GetOrDefault(_row.CharacterKey);
            bool supportsFavor = info != null && info.SupportsFavor;
            if (info != null && !string.IsNullOrEmpty(info.Name))
            {
                displayName = info.Name;
            }

            if (nameText != null)
            {
                nameText.text = displayName;
            }

            if (favorText != null)
            {
                favorText.gameObject.SetActive(supportsFavor);
                favorText.raycastTarget = supportsFavor;
                if (supportsFavor && registry != null)
                {
                    int favor = registry.GetFavorValue(_row.CharacterKey);
                    if (!_showFavorDetails)
                    {
                        favorText.text = $"好感 {favor}";
                    }
                    else
                    {
                        favorText.text = BuildFavorDetails(
                            info, registry, glm, favor);
                    }
                }

            }

            if (btnGiftMode != null)
            {
                btnGiftMode.gameObject.SetActive(supportsFavor && string.IsNullOrEmpty(_customOptionGroupId));
            }

            if (portraitImage != null)
            {
                Sprite sprite = null;
                if (!string.IsNullOrEmpty(_row.PortraitPath))
                {
                    sprite = Resources.Load<Sprite>(_row.PortraitPath);
                }

                portraitImage.enabled = sprite != null;
                portraitImage.sprite = sprite;
                portraitImage.color = sprite == null
                    ? new Color(0.35f, 0.45f, 0.55f, 1f)
                    : Color.white;
            }

            if (btnTalk != null)
            {
                btnTalk.gameObject.SetActive(string.IsNullOrEmpty(_customOptionGroupId));
                btnTalk.interactable = !string.IsNullOrEmpty(_row.DialogId);
            }

            RebuildCustomOptions(glm);
        }

        void RebuildCustomOptions(GameLogicManager glm)
        {
            for (int i = _customOptionButtons.Count - 1; i >= 0; i--)
            {
                if (_customOptionButtons[i] != null)
                {
                    Destroy(_customOptionButtons[i].gameObject);
                }
            }

            _customOptionButtons.Clear();
            if (_row == null || btnTalk == null || hubRoot == null)
            {
                return;
            }

            var rows = new List<SecretBaseCharacterOption>();
            var table = CfgMgr.Cfgs?.TbSecretBaseCharacterOption?.DataList;
            if (table != null)
            {
                for (int i = 0; i < table.Count; i++)
                {
                    var option = table[i];
                    if (option != null && option.SlotId == _row.SlotId
                        && string.Equals(option.ParentOptionId ?? string.Empty, _customOptionGroupId ?? string.Empty)
                        && (glm == null || glm.CheckCommonCondsAll(option.ShowConds)))
                    {
                        rows.Add(option);
                    }
                }
            }

            rows.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            int buttonOffset = 0;
            if (!string.IsNullOrEmpty(_customOptionGroupId))
            {
                CreateCustomOptionButton("Back", "返回", true, 0, () =>
                {
                    _customOptionGroupId = string.Empty;
                    RefreshHub();
                });
                buttonOffset = 1;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                var option = rows[i];
                bool enabled = glm != null && glm.CheckCommonCondsAll(option.EnableConds);
                CreateCustomOptionButton(
                    option.OptionId,
                    option.DisplayName,
                    enabled,
                    i + buttonOffset,
                    () => ExecuteCustomOption(option));
            }
        }

        void CreateCustomOptionButton(string id, string displayName, bool enabled, int index, UnityEngine.Events.UnityAction action)
        {
            var button = Instantiate(btnTalk, hubRoot.transform);
            button.name = $"CustomOption_{id}";
            button.gameObject.SetActive(true);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            button.interactable = enabled;

            var text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
            {
                text.text = displayName;
            }

            if (button.transform is RectTransform rt)
            {
                int column = index % 3;
                int row = index / 3;
                float xMin = 0.18f + column * 0.22f;
                float yMax = 0.40f - row * 0.14f;
                rt.anchorMin = new Vector2(xMin, yMax - 0.12f);
                rt.anchorMax = new Vector2(xMin + 0.18f, yMax);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
            }

            _customOptionButtons.Add(button);
        }

        void ExecuteCustomOption(SecretBaseCharacterOption option)
        {
            if (option == null)
            {
                return;
            }

            switch (option.ActionType)
            {
                case ESecretBaseCharacterOptionAction.Talk:
                    OnTalk();
                    break;
                case ESecretBaseCharacterOptionAction.Gift:
                    ShowGiftPicker();
                    break;
                case ESecretBaseCharacterOptionAction.OpenTalentTree:
                    if (!string.IsNullOrEmpty(option.ActionParam))
                    {
                        var tree = CfgMgr.Cfgs?.TbTalentTree?.GetOrDefault(option.ActionParam);
                        if (tree == null || (!string.IsNullOrEmpty(tree.OwnerCharacterKey)
                                            && tree.OwnerCharacterKey != _row.CharacterKey))
                        {
                            Debug.LogWarning($"[SecretBaseNpcHubPanel] Invalid talent tree option: {option.OptionId}");
                            return;
                        }

                        CloseSelf();
                        My.UI.Talent.CharacterTalentTreePanel.Open(option.ActionParam, _row.CharacterKey);
                    }
                    break;
                case ESecretBaseCharacterOptionAction.OpenOptionGroup:
                    _customOptionGroupId = string.IsNullOrEmpty(option.ActionParam)
                        ? option.OptionId
                        : option.ActionParam;
                    RefreshHub();
                    break;
                case ESecretBaseCharacterOptionAction.OpenPanel:
                    if (!string.IsNullOrEmpty(option.ActionParam))
                    {
                        UIManager.Instance.ShowPanel(option.ActionParam);
                    }
                    break;
            }
        }

        string BuildFavorDetails(
            cfg.demo.CharacterInfo info,
            My.WorldNpcCharacterPersistRegistry registry,
            GameLogicManager glm,
            int favor)
        {
            var rows = new List<CharacterFavorInfo>();
            var table = CfgMgr.Cfgs?.TbCharacterFavorInfo?.DataList;
            if (table != null)
            {
                for (int i = 0; i < table.Count; i++)
                {
                    if (table[i] != null && table[i].Key == _row.CharacterKey)
                    {
                        rows.Add(table[i]);
                    }
                }
            }

            rows.Sort((a, b) => a.FavorLevel.CompareTo(b.FavorLevel));
            int level = registry.GetFavorLevel(_row.CharacterKey, glm);
            int given = glm != null
                ? registry.GetGiftsGivenToday(_row.CharacterKey, glm.SettlementDayIndex)
                : 0;
            int limit = info.GiftsPerDay > 0 ? info.GiftsPerDay : 1;

            var builder = new StringBuilder();
            builder.Append($"好感 Lv{level} ({favor})  今日送礼 {given}/{limit}");
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                bool unlocked = level >= row.FavorLevel;
                bool valueReached = favor >= row.NeedValue;
                bool conditionMet = row.BreakthroughConds == null
                    || row.BreakthroughConds.Count == 0
                    || (glm != null && glm.CheckCommonCondsAll(row.BreakthroughConds));

                builder.Append($"\nLv{row.FavorLevel}: {row.NeedValue} ");
                if (unlocked)
                {
                    builder.Append("已解锁");
                }
                else if (!valueReached)
                {
                    builder.Append($"还需 {row.NeedValue - favor}");
                }
                else if (conditionMet)
                {
                    builder.Append("可突破");
                }
                else
                {
                    builder.Append("条件未满足");
                }

                if (row.BreakthroughConds != null && row.BreakthroughConds.Count > 0)
                {
                    builder.Append($" ({BuildConditionHint(row.BreakthroughConds)})");
                }
            }

            return builder.ToString();
        }

        static string BuildConditionHint(List<CommonCheckCond> conditions)
        {
            if (conditions == null || conditions.Count == 0)
            {
                return string.Empty;
            }

            var hints = new List<string>();
            for (int i = 0; i < conditions.Count; i++)
            {
                var cond = conditions[i];
                if (cond == null)
                {
                    continue;
                }

                hints.Add(cond.Type switch
                {
                    ECommonCheckType.OwnItem => $"收集 {cond.Param5} x{cond.Param1}",
                    ECommonCheckType.TaskFinish => $"完成任务 {cond.Param1}",
                    ECommonCheckType.TaskStep => $"完成任务步骤 {cond.Param5}",
                    ECommonCheckType.CheckVariable => $"满足条件 {cond.Param5}",
                    ECommonCheckType.FuncOpen => $"解锁功能 {(EFuncOpenType)cond.Param1}",
                    ECommonCheckType.CharacterFavorLevel => $"{cond.Param5}好感达到 Lv{cond.Param1}",
                    _ => "特殊条件",
                });
            }

            return string.Join("、", hints);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_row == null || eventData == null || favorText == null
                || !favorText.gameObject.activeInHierarchy
                || eventData.pointerPressRaycast.gameObject != favorText.gameObject)
            {
                return;
            }

            _showFavorDetails = !_showFavorDetails;
            RefreshHub();
        }

        void ShowHub()
        {
            if (hubRoot != null)
            {
                hubRoot.SetActive(true);
            }

            if (giftPickerRoot != null)
            {
                giftPickerRoot.SetActive(false);
            }
        }

        void ShowGiftPicker()
        {
            if (hubRoot != null)
            {
                hubRoot.SetActive(false);
            }

            if (giftPickerRoot != null)
            {
                giftPickerRoot.SetActive(true);
            }

            _selectedGiftItemId = null;
            if (btnGiveGift != null)
            {
                btnGiveGift.interactable = false;
            }

            RebuildGiftList();
        }

        void RebuildGiftList()
        {
            for (int i = _giftCells.Count - 1; i >= 0; i--)
            {
                if (_giftCells[i] != null)
                {
                    Destroy(_giftCells[i].gameObject);
                }
            }

            _giftCells.Clear();

            if (giftCellTemplate == null || giftListContent == null)
            {
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            var inv = glm?.playerDataManager?.InventorySystem;
            var gifts = SecretBaseGiftInventoryQuery.BuildList(inv);

            if (giftHintText != null)
            {
                giftHintText.text = gifts.Count > 0 ? "选择要赠送的礼物" : "背包与仓库中没有可赠送的礼物";
            }

            for (int i = 0; i < gifts.Count; i++)
            {
                var entry = gifts[i];
                var cellGo = Instantiate(giftCellTemplate.gameObject, giftListContent);
                cellGo.SetActive(true);
                var cell = cellGo.GetComponent<SecretBaseNpcGiftCell>();
                if (cell == null)
                {
                    continue;
                }

                cell.Bind(entry.ItemId, entry.Count, entry.ItemId == _selectedGiftItemId, OnGiftCellSelected);
                _giftCells.Add(cell);
            }
        }

        void OnGiftCellSelected(string itemId)
        {
            _selectedGiftItemId = itemId;
            if (btnGiveGift != null)
            {
                btnGiveGift.interactable = !string.IsNullOrEmpty(_selectedGiftItemId);
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            var inv = glm?.playerDataManager?.InventorySystem;
            var gifts = SecretBaseGiftInventoryQuery.BuildList(inv);
            for (int i = 0; i < _giftCells.Count && i < gifts.Count; i++)
            {
                if (_giftCells[i] == null)
                {
                    continue;
                }

                var entry = gifts[i];
                _giftCells[i].Bind(entry.ItemId, entry.Count, entry.ItemId == _selectedGiftItemId, OnGiftCellSelected);
            }
        }

        void OnTalk()
        {
            if (_row == null)
            {
                return;
            }

            SecretBaseNpcSocialService.TryTalk(_row);
        }

        void OnGiveGift()
        {
            if (_row == null || string.IsNullOrEmpty(_selectedGiftItemId))
            {
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            var result = SecretBaseNpcSocialService.TryGiveGift(glm, _row, _selectedGiftItemId, out var gain);
            if (result == ESecretBaseGiveGiftResult.Ok)
            {
                if (giftHintText != null)
                {
                    giftHintText.text = $"赠送成功，好感 +{gain}";
                }

                RefreshHub();
                RebuildGiftList();
            }
            else if (giftHintText != null)
            {
                giftHintText.text = SecretBaseNpcSocialService.FormatGiveGiftError(result);
            }
        }

        void CloseSelf()
        {
            UIManager.Instance.HidePanel(PanelIdConst);
        }

        public override bool OnCancel()
        {
            if (giftPickerRoot != null && giftPickerRoot.activeSelf)
            {
                ShowHub();
                return true;
            }

            if (!string.IsNullOrEmpty(_customOptionGroupId))
            {
                _customOptionGroupId = string.Empty;
                RefreshHub();
                return true;
            }

            CloseSelf();
            return true;
        }
    }
}
