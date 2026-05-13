using System.Collections.Generic;
using My;
using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class PlayerGearEquipPanel : PanelBase, IInputConsumer
    {
        public const string Pid = "PlayerGearEquip";

        Transform _root;
        Transform _catParent;
        Transform _candContent;
        readonly List<GameObject> _candRows = new();

        PlayerEquipmentManager _eq;
        EGearCategory? _pendingCat;
        int _pendingSlot;
        IPlayerProgressionHubHost _progressionHubHost;

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = Pid;
            }

            layer = UILayer.Popup;
        }

        public static PlayerGearEquipPanel Open()
        {
            PlayerProgressionHubPanel.OpenGear();
            var hubMono = UIManager.Instance.GetShowingPanel(PlayerProgressionHubPanel.Pid) as MonoBehaviour;
            return hubMono != null ? hubMono.GetComponentInChildren<PlayerGearEquipPanel>(true) : null;
        }

        public void SetProgressionHubHost(IPlayerProgressionHubHost host)
        {
            _progressionHubHost = host;
            ApplyHostedChromeIfNeeded();
        }

        void ApplyHostedChromeIfNeeded()
        {
            if (_progressionHubHost == null || _root == null)
            {
                return;
            }

            var blocker = _root.Find("BlockerButton");
            if (blocker != null)
            {
                blocker.gameObject.SetActive(false);
            }

            var closeTr = _root.Find("Window/Header/CloseBtn");
            var closeBtn = closeTr != null ? closeTr.GetComponent<Button>() : null;
            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(CloseSelfOrHub);
            }
        }

        public void CloseSelfOrHub()
        {
            if (_progressionHubHost != null)
            {
                _progressionHubHost.CloseHub();
            }
            else
            {
                UIManager.Instance.HidePanel(Pid);
            }
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            if (transform.Find("BuiltRoot") == null)
            {
                Debug.LogError("[PlayerGearEquipPanel] Prefab 缺少 BuiltRoot。请检查 Resources/UI/Prefabs/PlayerGearEquipPanel.prefab 层级或从版本库恢复。");
                return;
            }

            BindRefs();
            ApplyHostedChromeIfNeeded();
        }

        public override void Show()
        {
            base.Show();
            _eq = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.EquipmentManager;
            RefreshAll();
        }

        void BindRefs()
        {
            _root = transform.Find("BuiltRoot");
            if (_root == null)
            {
                return;
            }

            _catParent = _root.Find("Window/Categories");
            _candContent = _root.Find("Window/CandidatesScroll/Viewport/Content");
        }

        void RefreshAll()
        {
            BindRefs();
            if (_eq == null || _catParent == null)
            {
                return;
            }

            _eq.EnsureAllCategoriesSized();
            BuildCategoryRows();
            RefreshCandidates();
        }

        void BuildCategoryRows()
        {
            for (int i = _catParent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(_catParent.GetChild(i).gameObject);
            }

            string[] titles = { "Equip", "Pocket", "Insertion", "Misc (gear_misc)" };
            for (int ci = 0; ci < PlayerEquipmentManager.CategoryCount; ci++)
            {
                var cat = (EGearCategory)ci;
                int cap = _eq.GetSlotCap(cat);
                var row = new GameObject($"Cat_{ci}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                row.transform.SetParent(_catParent, false);
                var h = row.GetComponent<HorizontalLayoutGroup>();
                h.spacing = 6f;
                h.childAlignment = TextAnchor.MiddleLeft;

                var labGo = new GameObject("Lab", typeof(RectTransform), typeof(TextMeshProUGUI));
                labGo.transform.SetParent(row.transform, false);
                var labRt = labGo.GetComponent<RectTransform>();
                labRt.sizeDelta = new Vector2(160f, 28f);
                var lab = labGo.GetComponent<TextMeshProUGUI>();
                lab.text = $"{titles[ci]} x{cap}";
                lab.fontSize = 15;
                lab.color = Color.white;

                for (int s = 0; s < cap; s++)
                {
                    var slot = _eq.GetSlot(cat, s);
                    var btnGo = new GameObject($"slot_{s}", typeof(RectTransform), typeof(Image), typeof(Button));
                    btnGo.transform.SetParent(row.transform, false);
                    var brt = btnGo.GetComponent<RectTransform>();
                    brt.sizeDelta = new Vector2(120f, 32f);
                    var img = btnGo.GetComponent<Image>();
                    img.color = new Color(0.25f, 0.25f, 0.3f, 1f);
                    var txtGo = new GameObject("T", typeof(RectTransform), typeof(TextMeshProUGUI));
                    txtGo.transform.SetParent(btnGo.transform, false);
                    var tt = txtGo.GetComponent<TextMeshProUGUI>();
                    tt.fontSize = 13;
                    tt.color = Color.white;
                    tt.alignment = TextAlignmentOptions.Center;
                    string label = "(empty)";
                    if (slot != null && !string.IsNullOrEmpty(slot.ItemId))
                    {
                        var def = ItemCatalog.GetItemDef(slot.ItemId);
                        label = def != null ? def.DisplayName : slot.ItemId;
                    }

                    tt.text = label;
                    int catI = ci;
                    int si = s;
                    btnGo.GetComponent<Button>().onClick.AddListener(() => OnGearSlotClicked((EGearCategory)catI, si));
                }
            }
        }

        void OnGearSlotClicked(EGearCategory cat, int slotIdx)
        {
            var slot = _eq.GetSlot(cat, slotIdx);
            if (slot != null && !string.IsNullOrEmpty(slot.ItemId))
            {
                _eq.TryUnequip(cat, slotIdx, out _);
                _pendingCat = null;
                RefreshAll();
                return;
            }

            _pendingCat = cat;
            _pendingSlot = slotIdx;
            RefreshCandidates();
        }

        void RefreshCandidates()
        {
            if (_candContent == null)
            {
                return;
            }

            for (int i = 0; i < _candRows.Count; i++)
            {
                if (_candRows[i] != null)
                {
                    Object.Destroy(_candRows[i]);
                }
            }

            _candRows.Clear();
            if (_eq == null || _pendingCat == null)
            {
                return;
            }

            var cat = _pendingCat.Value;
            var list = _eq.ListMainBagCandidates(cat);
            foreach (var pair in list)
            {
                int flatIdx = pair.bagFlatIndex;
                var st = pair.stack;
                var def = ItemCatalog.GetItemDef(st.ItemID);
                string name = def != null ? def.DisplayName : st.ItemID;
                var btnGo = new GameObject("cand", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(_candContent, false);
                var rt = btnGo.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0f, 28f);
                btnGo.GetComponent<Image>().color = new Color(0.2f, 0.22f, 0.28f, 1f);
                var tt = new GameObject("txt", typeof(RectTransform), typeof(TextMeshProUGUI));
                tt.transform.SetParent(btnGo.transform, false);
                var tmp = tt.GetComponent<TextMeshProUGUI>();
                tmp.fontSize = 14;
                tmp.color = Color.white;
                tmp.text = $"{name}  x{st.Count}  @{flatIdx}";
                int f = flatIdx;
                btnGo.GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (_pendingCat != null)
                    {
                        _eq.TryEquipFromMainBagSlot(_pendingCat.Value, _pendingSlot, f, out _);
                        RefreshAll();
                    }
                });
                _candRows.Add(btnGo);
            }
        }

        public bool OnConfirm() => false;

        public bool OnCancel()
        {
            CloseSelfOrHub();
            return true;
        }

        public bool OnNavigate(Vector2 dir) => false;

        public bool OnHotkey(string keyName) => false;

        public bool OnScroll(float deltaY) => false;

        public bool OnClick(int button, Vector2 mousePos) => false;

        public bool OnHoldStart(string holdKey) => false;

        public bool OnHoldUpdate(string holdKey) => false;

        public bool OnHoldingEnd(string holdKey) => false;
    }
}
