using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;
using My.SecretBase;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class SecretBaseNpcHubPanel : PanelWithInput
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
        bool _listenersBound;

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
            if (info != null && !string.IsNullOrEmpty(info.Name))
            {
                displayName = info.Name;
            }

            if (nameText != null)
            {
                nameText.text = displayName;
            }

            if (registry != null && favorText != null)
            {
                int favor = registry.GetFavorValue(_row.CharacterKey);
                int level = registry.GetFavorLevel(_row.CharacterKey);
                int given = glm != null
                    ? registry.GetGiftsGivenToday(_row.CharacterKey, glm.SettlementDayIndex)
                    : 0;
                int limit = _row.GiftsPerDay > 0 ? _row.GiftsPerDay : 1;
                favorText.text = $"好感 Lv{level} ({favor})  今日送礼 {given}/{limit}";
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
                btnTalk.interactable = !string.IsNullOrEmpty(_row.DialogId);
            }
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

            CloseSelf();
            return true;
        }
    }
}
