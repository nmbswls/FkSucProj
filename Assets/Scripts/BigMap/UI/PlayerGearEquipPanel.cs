using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 部位养成 + 装备合并面板（Hub Tab: 部位）
    public class PlayerGearEquipPanel : PanelBase, IInputConsumer, IPlayerProgressionHubPage
    {
        public const string Pid = "PlayerGearEquip";

        Transform _root;
        Transform _partListParent;
        Transform _detailRoot;
        Transform _localStatsContent;
        Transform _equippedContent;
        Transform _candContent;
        readonly List<GameObject> _partRows = new();
        readonly List<GameObject> _localRows = new();
        readonly List<GameObject> _equippedRows = new();
        readonly List<GameObject> _candRows = new();

        PlayerEquipmentManager _eq;
        PlayerBodyPartSystem _bodyPart;
        EBodyPart _selectedPart = EBodyPart.Mouth;
        IPlayerProgressionHubHost _progressionHubHost;

        TextMeshProUGUI _detailTitle;
        TextMeshProUGUI _detailLevel;
        TextMeshProUGUI _detailGearPoint;

        public bool IsHostedByHub => _progressionHubHost != null;

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = Pid;
            }
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
                return;
            }

            Debug.LogError("[PlayerGearEquipPanel] Not hosted by PlayerProgressionHubPanel.");
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            if (!IsHostedByHub)
            {
                Debug.LogError("[PlayerGearEquipPanel] Setup without hub host.");
                return;
            }

            if (transform.Find("BuiltRoot") == null)
            {
                Debug.LogError("[PlayerGearEquipPanel] Prefab missing BuiltRoot.");
                return;
            }

            EnsureUiStructure();
            BindRefs();
            ApplyHostedChromeIfNeeded();
        }

        public override void Show()
        {
            if (!IsHostedByHub)
            {
                Debug.LogError("[PlayerGearEquipPanel] Show without hub host.");
                return;
            }

            base.Show();
            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            _eq = mgr?.EquipmentManager;
            _bodyPart = mgr?.BodyPartSystem;
            EnsureUiStructure();
            BindRefs();
            RefreshAll();
        }

        void EnsureUiStructure()
        {
            _root = transform.Find("BuiltRoot");
            if (_root == null)
            {
                return;
            }

            var window = _root.Find("Window");
            if (window == null)
            {
                return;
            }

            var categories = window.Find("Categories");
            if (categories != null)
            {
                categories.name = "PartList";
                _partListParent = categories;
            }

            var detail = window.Find("BodyPartDetail");
            if (detail == null)
            {
                var detailGo = new GameObject("BodyPartDetail", typeof(RectTransform), typeof(VerticalLayoutGroup));
                detailGo.transform.SetParent(window, false);
                var drt = detailGo.GetComponent<RectTransform>();
                drt.anchorMin = new Vector2(0.35f, 0f);
                drt.anchorMax = new Vector2(1f, 1f);
                drt.offsetMin = new Vector2(8f, 8f);
                drt.offsetMax = new Vector2(-8f, -8f);
                var vlg = detailGo.GetComponent<VerticalLayoutGroup>();
                vlg.spacing = 8f;
                vlg.childControlHeight = true;
                vlg.childForceExpandHeight = false;
                detail = detailGo.transform;

                CreateDetailHeader(detail);
                CreateSection(detail, "LocalStats", out _localStatsContent);
                CreateSection(detail, "Equipped", out _equippedContent);
                CreateSection(detail, "CandidatesScroll", out _candContent, withScroll: true);
            }

            _detailRoot = detail;
            BindRefs();
        }

        static void CreateDetailHeader(Transform parent)
        {
            var header = new GameObject("Header", typeof(RectTransform), typeof(VerticalLayoutGroup));
            header.transform.SetParent(parent, false);
            var hlg = header.GetComponent<VerticalLayoutGroup>();
            hlg.spacing = 4f;
            hlg.childControlHeight = true;
            hlg.childForceExpandHeight = false;

            CreateLabel(header.transform, "Title", 18);
            CreateLabel(header.transform, "Level", 14);
            CreateLabel(header.transform, "GearPoint", 14);
        }

        static TextMeshProUGUI CreateLabel(Transform parent, string name, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, fontSize + 8f);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.color = new Color(0.92f, 0.88f, 1f, 1f);
            tmp.alignment = TextAlignmentOptions.Left;
            return tmp;
        }

        static void CreateSection(Transform parent, string name, out Transform content, bool withScroll = false)
        {
            var section = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            section.transform.SetParent(parent, false);
            var vlg = section.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;

            var title = CreateLabel(section.transform, "SectionTitle", 15);
            title.text = name;

            if (withScroll)
            {
                var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
                scrollGo.transform.SetParent(section.transform, false);
                var srt = scrollGo.GetComponent<RectTransform>();
                srt.sizeDelta = new Vector2(0f, 160f);
                scrollGo.GetComponent<Image>().color = new Color(0.1f, 0.09f, 0.14f, 0.6f);

                var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
                viewport.transform.SetParent(scrollGo.transform, false);
                var vprt = viewport.GetComponent<RectTransform>();
                vprt.anchorMin = Vector2.zero;
                vprt.anchorMax = Vector2.one;
                vprt.offsetMin = Vector2.zero;
                vprt.offsetMax = Vector2.zero;
                viewport.GetComponent<Image>().color = Color.white;

                content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup)).transform;
                content.SetParent(viewport.transform, false);
                var crt = content.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0f, 1f);
                crt.anchorMax = new Vector2(1f, 1f);
                crt.pivot = new Vector2(0.5f, 1f);
                var cvlg = content.GetComponent<VerticalLayoutGroup>();
                cvlg.spacing = 4f;
                cvlg.childControlHeight = true;
                cvlg.childForceExpandHeight = false;

                var sr = scrollGo.GetComponent<ScrollRect>();
                sr.viewport = vprt;
                sr.content = crt;
                sr.horizontal = false;
                sr.vertical = true;
            }
            else
            {
                content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup)).transform;
                content.SetParent(section.transform, false);
                var cvlg = content.GetComponent<VerticalLayoutGroup>();
                cvlg.spacing = 4f;
                cvlg.childControlHeight = true;
                cvlg.childForceExpandHeight = false;
            }
        }

        void BindRefs()
        {
            _root = transform.Find("BuiltRoot");
            if (_root == null)
            {
                return;
            }

            _partListParent = _root.Find("Window/PartList") ?? _root.Find("Window/Categories");
            _detailRoot = _root.Find("Window/BodyPartDetail");
            if (_detailRoot != null)
            {
                _detailTitle = _detailRoot.Find("Header/Title")?.GetComponent<TextMeshProUGUI>();
                _detailLevel = _detailRoot.Find("Header/Level")?.GetComponent<TextMeshProUGUI>();
                _detailGearPoint = _detailRoot.Find("Header/GearPoint")?.GetComponent<TextMeshProUGUI>();
                _localStatsContent = _detailRoot.Find("LocalStats/Content");
                _equippedContent = _detailRoot.Find("Equipped/Content");
                _candContent = _detailRoot.Find("CandidatesScroll/Scroll/Viewport/Content")
                               ?? _detailRoot.Find("CandidatesScroll/Content");
            }
        }

        void RefreshAll()
        {
            BindRefs();
            if (_eq == null || _partListParent == null)
            {
                return;
            }

            _eq.EnsureAllPartsBudget();
            BuildPartList();
            RefreshDetail();
        }

        void BuildPartList()
        {
            ClearRows(_partRows);
            foreach (var def in BodyPartCatalog.GetAllPartsSorted())
            {
                if (def.PartId == EBodyPart.None)
                {
                    continue;
                }

                var part = def.PartId;
                var state = _bodyPart?.GetPartState(part);
                int lv = state?.Level ?? 1;
                int used = _eq.GetUsedGearPoint(part);
                int cap = _eq.GetPartGearPointCap(part);

                var btnGo = new GameObject($"Part_{part}", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(_partListParent, false);
                var rt = btnGo.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0f, 36f);
                var img = btnGo.GetComponent<Image>();
                bool selected = part == _selectedPart;
                img.color = selected
                    ? new Color(0.35f, 0.28f, 0.48f, 1f)
                    : new Color(0.18f, 0.16f, 0.24f, 1f);

                var txtGo = new GameObject("T", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGo.transform.SetParent(btnGo.transform, false);
                var tt = txtGo.GetComponent<TextMeshProUGUI>();
                tt.fontSize = 14;
                tt.color = Color.white;
                tt.alignment = TextAlignmentOptions.MidlineLeft;
                tt.text = $"  {def.DisplayName}  Lv.{lv}  [{used}/{cap}]";

                var captured = part;
                btnGo.GetComponent<Button>().onClick.AddListener(() =>
                {
                    _selectedPart = captured;
                    RefreshAll();
                });
                _partRows.Add(btnGo);
            }
        }

        void RefreshDetail()
        {
            if (_detailRoot == null)
            {
                return;
            }

            var def = BodyPartCatalog.GetPartDef(_selectedPart);
            var state = _bodyPart?.GetPartState(_selectedPart);
            if (def == null)
            {
                return;
            }

            int lv = state?.Level ?? 1;
            long exp = state?.Exp ?? 0;
            long expNext = _bodyPart?.GetExpToNextLevel(_selectedPart) ?? 0;
            int used = _eq?.GetUsedGearPoint(_selectedPart) ?? 0;
            int cap = _eq?.GetPartGearPointCap(_selectedPart) ?? 0;

            if (_detailTitle != null)
            {
                _detailTitle.text = def.DisplayName;
            }

            if (_detailLevel != null)
            {
                _detailLevel.text = expNext > 0
                    ? $"等级 {lv}  经验 {exp}  距下级 {expNext}"
                    : $"等级 {lv}  经验 {exp}  (已满级)";
            }

            if (_detailGearPoint != null)
            {
                _detailGearPoint.text = $"装备点数 {used}/{cap}";
            }

            BuildLocalStats(state);
            BuildEquippedList();
            BuildCandidates();
        }

        void BuildLocalStats(BodyPartRuntimeState state)
        {
            ClearRows(_localRows);
            if (_localStatsContent == null || state?.LocalStats == null)
            {
                return;
            }

            foreach (var kv in state.LocalStats)
            {
                string name = BodyPartCatalog.GetLocalAttrDisplayName(kv.Key);
                var row = CreateTextRow(_localStatsContent, $"{name}: {kv.Value}");
                _localRows.Add(row);
            }

            if (_localRows.Count == 0)
            {
                _localRows.Add(CreateTextRow(_localStatsContent, "(无 Local 属性)"));
            }
        }

        void BuildEquippedList()
        {
            ClearRows(_equippedRows);
            if (_equippedContent == null || _eq == null)
            {
                return;
            }

            var list = _eq.GetEquippedOnPart(_selectedPart);
            for (int i = 0; i < list.Count; i++)
            {
                var slot = list[i];
                if (slot == null || string.IsNullOrEmpty(slot.ItemId))
                {
                    continue;
                }

                var itemDef = ItemCatalog.GetItemDef(slot.ItemId);
                int cost = ItemGearRules.GetSlotCost(itemDef);
                string name = itemDef != null ? itemDef.DisplayName : slot.ItemId;
                int idx = i;
                var row = CreateButtonRow(_equippedContent, $"[{cost}] {name}  (点击卸下)", () =>
                {
                    _eq.TryUnequip(_selectedPart, idx, out _);
                    RefreshAll();
                });
                _equippedRows.Add(row);
            }

            if (_equippedRows.Count == 0)
            {
                _equippedRows.Add(CreateTextRow(_equippedContent, "(未装备)"));
            }
        }

        void BuildCandidates()
        {
            ClearRows(_candRows);
            if (_candContent == null || _eq == null)
            {
                return;
            }

            var list = _eq.ListMainBagCandidates(_selectedPart);
            foreach (var pair in list)
            {
                int flatIdx = pair.bagFlatIndex;
                var st = pair.stack;
                var itemDef = ItemCatalog.GetItemDef(st.ItemID);
                int cost = ItemGearRules.GetSlotCost(itemDef);
                string name = itemDef != null ? itemDef.DisplayName : st.ItemID;
                var row = CreateButtonRow(_candContent, $"[{cost}] {name} x{st.Count}", () =>
                {
                    _eq.TryEquipFromMainBag(_selectedPart, flatIdx, out _);
                    RefreshAll();
                });
                _candRows.Add(row);
            }

            if (_candRows.Count == 0)
            {
                _candRows.Add(CreateTextRow(_candContent, "(背包无可用装备)"));
            }
        }

        static void ClearRows(List<GameObject> rows)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null)
                {
                    Object.Destroy(rows[i]);
                }
            }

            rows.Clear();
        }

        static GameObject CreateTextRow(Transform parent, string text)
        {
            var go = new GameObject("row", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 24f);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 13;
            tmp.color = new Color(0.85f, 0.82f, 0.92f, 1f);
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            return go;
        }

        static GameObject CreateButtonRow(Transform parent, string text, UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = new GameObject("row", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 28f);
            btnGo.GetComponent<Image>().color = new Color(0.2f, 0.18f, 0.28f, 1f);
            var tmpGo = new GameObject("txt", typeof(RectTransform), typeof(TextMeshProUGUI));
            tmpGo.transform.SetParent(btnGo.transform, false);
            var tmp = tmpGo.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 13;
            tmp.color = Color.white;
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            btnGo.GetComponent<Button>().onClick.AddListener(onClick);
            return btnGo;
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
