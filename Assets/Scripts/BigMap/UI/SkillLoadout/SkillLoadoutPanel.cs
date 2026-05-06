using System.Collections.Generic;
using My.Config;
using My.Player;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.SkillLoadout
{
    public class SkillLoadoutPanel : PanelBase, IInputConsumer
    {
        public const string Pid = "SkillLoadoutPanel";

        public static SkillLoadoutPanel Current { get; private set; }

        public static SkillLoadoutPanel Open()
        {
            return UIManager.Instance.ShowPanel(Pid) as SkillLoadoutPanel;
        }

        [SerializeField] bool buildWhenReferencesMissing = true;

        public string ActiveSchoolId { get; private set; }

        Transform _builtRoot;
        SkillPoolEntryView[] _poolCells;
        ISkillDropBehavior _poolSkillDropBehavior = new SchoolFilteredNormalSlotDropBehavior();

        Button[] _tabButtons;
        SkillSlotView[] _slotViews;
        int _tabCount;

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
                panelId = Pid;
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            if (buildWhenReferencesMissing && transform.Find("BuiltRoot") == null)
                BuildDefaultUi();

            BindBuiltReferencesIfNeeded();
            InitTabsFromTable();
            if (_tabCount > 0)
                SelectSchool(0);
            else
                RefreshAll();
        }

        public override void Show()
        {
            base.Show();
            Current = this;
            SkillDragSession.SetCanvas(GetComponentInParent<Canvas>());
            RefreshAll();
        }

        public override void Hide()
        {
            SkillDragSession.End();
            Current = null;
            base.Hide();
        }

        public override void Teardown()
        {
            SkillDragSession.End();
            Current = null;
            base.Teardown();
        }

        void BindBuiltReferencesIfNeeded()
        {
            if (_builtRoot != null) return;

            var root = transform.Find("BuiltRoot");
            if (root == null) return;

            _builtRoot = root;

            var poolRt = root.Find("Window/PoolScroll/Viewport/PoolContent");
            if (poolRt != null)
            {
                var cells = poolRt.GetComponentsInChildren<SkillPoolEntryView>(true);
                _poolCells = cells;
            }

            var tabRow = root.Find("Window/Tabs");
            if (tabRow != null)
                _tabButtons = tabRow.GetComponentsInChildren<Button>(true);

            var bar = root.Find("Window/BarRow");
            if (bar != null)
                _slotViews = bar.GetComponentsInChildren<SkillSlotView>(true);

            WireDragLayerFromHierarchy();

            var closeBtn = root.Find("Window/Header/CloseBtn")?.GetComponent<Button>();
            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(() => UIManager.Instance.HidePanel(Pid));
            }

            var bl = root.Find("BlockerButton")?.GetComponent<Button>();
            if (bl != null)
            {
                bl.onClick.RemoveAllListeners();
                bl.onClick.AddListener(() => UIManager.Instance.HidePanel(Pid));
            }
        }

        void BuildDefaultUi()
        {
            var root = new GameObject("BuiltRoot", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            var blocker = new GameObject("BlockerButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button));
            blocker.transform.SetParent(root.transform, false);
            var blRt = blocker.GetComponent<RectTransform>();
            blRt.anchorMin = Vector2.zero;
            blRt.anchorMax = Vector2.one;
            blRt.offsetMin = Vector2.zero;
            blRt.offsetMax = Vector2.zero;
            var blImg = blocker.GetComponent<Image>();
            blImg.color = new Color(0f, 0f, 0f, 0.55f);
            blImg.raycastTarget = true;

            var win = new GameObject("Window", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            win.transform.SetParent(root.transform, false);
            var winRt = win.GetComponent<RectTransform>();
            winRt.anchorMin = new Vector2(0.5f, 0.5f);
            winRt.anchorMax = new Vector2(0.5f, 0.5f);
            winRt.pivot = new Vector2(0.5f, 0.5f);
            winRt.sizeDelta = new Vector2(860f, 640f);
            var winImg = win.GetComponent<Image>();
            winImg.color = new Color(0.15f, 0.15f, 0.18f, 0.98f);
            winImg.raycastTarget = true;
            var winV = win.GetComponent<VerticalLayoutGroup>();
            winV.padding = new RectOffset(16, 16, 12, 12);
            winV.spacing = 12f;
            winV.childAlignment = TextAnchor.UpperCenter;
            winV.childControlHeight = true;
            winV.childControlWidth = true;
            winV.childForceExpandHeight = false;
            winV.childForceExpandWidth = true;

            var header = new GameObject("Header", typeof(RectTransform), typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            header.transform.SetParent(win.transform, false);
            var he = header.GetComponent<LayoutElement>();
            he.minHeight = 40f;
            he.preferredHeight = 40f;
            var hh = header.GetComponent<HorizontalLayoutGroup>();
            hh.childAlignment = TextAnchor.MiddleCenter;
            hh.childForceExpandWidth = true;

            var titleGo =
                new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            titleGo.transform.SetParent(header.transform, false);
            var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "Skill loadout";
            titleTmp.alignment = TextAlignmentOptions.Left;
            titleTmp.fontSize = 26f;
            titleTmp.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
                titleTmp.font = TMP_Settings.defaultFontAsset;
            var titleLe = titleGo.GetComponent<LayoutElement>();
            titleLe.flexibleWidth = 2f;

            var closeGo = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button),
                typeof(LayoutElement));
            closeGo.transform.SetParent(header.transform, false);
            var closImg = closeGo.GetComponent<Image>();
            closImg.color = new Color(0.8f, 0.25f, 0.25f, 1f);
            closImg.raycastTarget = true;
            var closeLe = closeGo.GetComponent<LayoutElement>();
            closeLe.preferredWidth = 72f;
            closeLe.preferredHeight = 32f;
            var closeBtn = closeGo.GetComponent<Button>();
            closeBtn.targetGraphic = closImg;
            var closeLblGo = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeLblGo.transform.SetParent(closeGo.transform, false);
            var closeLbl = closeLblGo.GetComponent<TextMeshProUGUI>();
            closeLbl.text = "Close";
            closeLbl.alignment = TextAlignmentOptions.Center;
            closeLbl.fontSize = 16f;
            closeLbl.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
                closeLbl.font = TMP_Settings.defaultFontAsset;
            StretchFull(closeLbl.rectTransform);

            var tabs = new GameObject("Tabs", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            tabs.transform.SetParent(win.transform, false);
            var tabsLe = tabs.GetComponent<LayoutElement>();
            tabsLe.minHeight = 36f;
            tabsLe.preferredHeight = 36f;
            var tabsH = tabs.GetComponent<HorizontalLayoutGroup>();
            tabsH.spacing = 8f;
            tabsH.childAlignment = TextAnchor.MiddleLeft;
            tabsH.childForceExpandHeight = true;
            tabsH.childForceExpandWidth = false;
            for (var i = 0; i < 8; i++)
            {
                var tb = new GameObject("Tab" + i, typeof(RectTransform), typeof(Image), typeof(Button),
                    typeof(LayoutElement));
                tb.transform.SetParent(tabs.transform, false);
                var tImg = tb.GetComponent<Image>();
                tImg.color = new Color(0.28f, 0.3f, 0.35f, 1f);
                tImg.raycastTarget = true;
                var tBt = tb.GetComponent<Button>();
                tBt.targetGraphic = tImg;
                var tLe = tb.GetComponent<LayoutElement>();
                tLe.preferredWidth = 120f;
                var tLblGo = new GameObject("Lbl", typeof(RectTransform), typeof(TextMeshProUGUI));
                tLblGo.transform.SetParent(tb.transform, false);
                var tLbl = tLblGo.GetComponent<TextMeshProUGUI>();
                tLbl.alignment = TextAlignmentOptions.Center;
                tLbl.fontSize = 16f;
                tLbl.raycastTarget = false;
                if (TMP_Settings.defaultFontAsset != null)
                    tLbl.font = TMP_Settings.defaultFontAsset;
                StretchFull(tLbl.rectTransform);
                tb.SetActive(false);
            }

            var poolScrollGo =
                new GameObject("PoolScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            poolScrollGo.transform.SetParent(win.transform, false);
            var poolScrollLe = poolScrollGo.GetComponent<LayoutElement>();
            poolScrollLe.minHeight = 360f;
            poolScrollLe.preferredHeight = 360f;
            poolScrollLe.flexibleHeight = 0f;
            var poolBg = poolScrollGo.GetComponent<Image>();
            poolBg.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);
            poolBg.raycastTarget = true;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(poolScrollGo.transform, false);
            var vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = new Vector2(6f, 6f);
            vpRt.offsetMax = new Vector2(-6f, -6f);
            var vpImg = viewport.GetComponent<Image>();
            vpImg.color = new Color(1f, 1f, 1f, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("PoolContent", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            var csf = content.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = poolScrollGo.GetComponent<ScrollRect>();
            scroll.viewport = vpRt;
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            for (var i = 0; i < 20; i++)
            {
                var row = new GameObject("PoolRow" + i, typeof(RectTransform), typeof(HorizontalLayoutGroup),
                    typeof(LayoutElement), typeof(Image));
                row.transform.SetParent(content.transform, false);
                var rowImg = row.GetComponent<Image>();
                rowImg.color = new Color(0.2f, 0.22f, 0.26f, 0.7f);
                rowImg.raycastTarget = true;
                var rowLe = row.GetComponent<LayoutElement>();
                rowLe.minHeight = 34f;
                rowLe.preferredHeight = 34f;
                var rowH = row.GetComponent<HorizontalLayoutGroup>();
                rowH.padding = new RectOffset(6, 6, 2, 2);
                rowH.childAlignment = TextAnchor.MiddleLeft;

                var ic = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                    typeof(LayoutElement));
                ic.transform.SetParent(row.transform, false);
                var icLe = ic.GetComponent<LayoutElement>();
                icLe.preferredWidth = 28f;
                icLe.preferredHeight = 28f;
                var icImg = ic.GetComponent<Image>();
                icImg.color = Color.white;
                icImg.raycastTarget = false;

                var txtGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
                txtGo.transform.SetParent(row.transform, false);
                var txtLe = txtGo.GetComponent<LayoutElement>();
                txtLe.flexibleWidth = 1f;
                var tmp = txtGo.GetComponent<TextMeshProUGUI>();
                tmp.fontSize = 16f;
                tmp.raycastTarget = false;
                if (TMP_Settings.defaultFontAsset != null)
                    tmp.font = TMP_Settings.defaultFontAsset;

                var entry = row.AddComponent<SkillPoolEntryView>();
                entry.label = tmp;
                entry.icon = icImg;
                entry.background = rowImg;
                row.SetActive(false);
            }

            var hintGo = new GameObject("PoolHint", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            hintGo.transform.SetParent(win.transform, false);
            var hintTmp = hintGo.GetComponent<TextMeshProUGUI>();
            hintTmp.text = "Drag skills into slots 3~7 (keyboard 1~5). Slots 0~2 are fixed.";
            hintTmp.fontSize = 15f;
            hintTmp.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
                hintTmp.font = TMP_Settings.defaultFontAsset;
            var hintLe = hintGo.GetComponent<LayoutElement>();
            hintLe.minHeight = 26f;

            var bar = new GameObject("BarRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            bar.transform.SetParent(win.transform, false);
            var barLe = bar.GetComponent<LayoutElement>();
            barLe.minHeight = 92f;
            barLe.preferredHeight = 92f;
            var barH = bar.GetComponent<HorizontalLayoutGroup>();
            barH.spacing = 8f;
            barH.childAlignment = TextAnchor.MiddleCenter;
            for (var s = 0; s < 8; s++)
            {
                var slot = new GameObject("Slot_" + s, typeof(RectTransform), typeof(LayoutElement));
                slot.transform.SetParent(bar.transform, false);
                var sLe = slot.GetComponent<LayoutElement>();
                sLe.preferredWidth = 88f;
                sLe.preferredHeight = 88f;

                var frame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
                frame.transform.SetParent(slot.transform, false);
                StretchFull(frame.GetComponent<RectTransform>());
                var fImg = frame.GetComponent<Image>();
                fImg.color = s < 3
                    ? new Color(0.35f, 0.32f, 0.25f, 1f)
                    : new Color(0.22f, 0.28f, 0.32f, 1f);
                fImg.raycastTarget = true;

                var z = frame.AddComponent<SkillSlotDropZone>();
                z.mode = s < 3 ? SkillSlotDropMode.Fixed : SkillSlotDropMode.CustomNormal;

                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconGo.transform.SetParent(slot.transform, false);
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.5f, 0.55f);
                iconRt.anchorMax = new Vector2(0.5f, 0.55f);
                iconRt.sizeDelta = new Vector2(52f, 52f);
                var iconImg = iconGo.GetComponent<Image>();
                iconImg.raycastTarget = false;

                var capGo = new GameObject("Cap", typeof(RectTransform), typeof(TextMeshProUGUI));
                capGo.transform.SetParent(slot.transform, false);
                var capTmp = capGo.GetComponent<TextMeshProUGUI>();
                capTmp.fontSize = 11f;
                capTmp.alignment = TextAlignmentOptions.Bottom;
                capTmp.raycastTarget = false;
                if (TMP_Settings.defaultFontAsset != null)
                    capTmp.font = TMP_Settings.defaultFontAsset;
                var capRt = capGo.GetComponent<RectTransform>();
                capRt.anchorMin = new Vector2(0f, 0f);
                capRt.anchorMax = new Vector2(1f, 0f);
                capRt.pivot = new Vector2(0.5f, 0f);
                capRt.anchoredPosition = new Vector2(0f, 4f);
                capRt.sizeDelta = new Vector2(-8f, 22f);

                var capLblGo = new GameObject("Idx", typeof(RectTransform), typeof(TextMeshProUGUI));
                capLblGo.transform.SetParent(slot.transform, false);
                var idxTmp = capLblGo.GetComponent<TextMeshProUGUI>();
                idxTmp.text = s.ToString();
                idxTmp.fontSize = 12f;
                idxTmp.alignment = TextAlignmentOptions.TopLeft;
                idxTmp.raycastTarget = false;
                if (TMP_Settings.defaultFontAsset != null)
                    idxTmp.font = TMP_Settings.defaultFontAsset;
                var idxRt = capLblGo.GetComponent<RectTransform>();
                idxRt.anchorMin = new Vector2(0f, 1f);
                idxRt.anchorMax = new Vector2(0f, 1f);
                idxRt.pivot = new Vector2(0f, 1f);
                idxRt.anchoredPosition = new Vector2(4f, -4f);
                idxRt.sizeDelta = new Vector2(28f, 18f);

                var sv = slot.AddComponent<SkillSlotView>();
                sv.SlotIndex = s;
                sv.label = capTmp;
                sv.icon = iconImg;
                z.view = sv;
            }

            BuildDragLayer();

            BindBuiltReferencesIfNeeded();
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        void BuildDragLayer()
        {
            var existing = transform.Find("DragLayer");
            if (existing != null)
                Destroy(existing.gameObject);

            var dragGo = new GameObject("DragLayer", typeof(RectTransform));
            dragGo.transform.SetParent(transform, false);
            var drt = dragGo.GetComponent<RectTransform>();
            StretchFull(drt);
            drt.SetAsLastSibling();

            var ghost = new GameObject("Ghost", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ghost.transform.SetParent(dragGo.transform, false);
            var ghostRt = ghost.GetComponent<RectTransform>();
            ghostRt.sizeDelta = new Vector2(64f, 64f);

            var ghostImg = ghost.GetComponent<Image>();
            ghostImg.color = new Color(1f, 1f, 1f, 0.85f);
            ghostImg.raycastTarget = false;

            var gLblGo = new GameObject("Lbl", typeof(RectTransform), typeof(TextMeshProUGUI));
            gLblGo.transform.SetParent(ghost.transform, false);
            var gTmp = gLblGo.GetComponent<TextMeshProUGUI>();
            gTmp.fontSize = 12f;
            gTmp.alignment = TextAlignmentOptions.Bottom;
            gTmp.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
                gTmp.font = TMP_Settings.defaultFontAsset;
            StretchFull(gTmp.rectTransform);

            SkillDragSession.Configure(ghost, ghostImg, gTmp, GetComponentInParent<Canvas>());
        }

        void WireDragLayerFromHierarchy()
        {
            var dragLayer = transform.Find("DragLayer");
            if (dragLayer == null) return;

            var ghostTf = dragLayer.Find("Ghost");
            if (ghostTf == null) return;

            var ghostImg = ghostTf.GetComponent<Image>();
            var ghostLbl = ghostTf.GetComponentInChildren<TextMeshProUGUI>();
            SkillDragSession.Configure(ghostTf.gameObject, ghostImg, ghostLbl, GetComponentInParent<Canvas>());
        }

        void InitTabsFromTable()
        {
            BindBuiltReferencesIfNeeded();

            var schools = SkillSchoolTable.Instance.AllSchools;
            _tabCount = 0;
            if (_tabButtons == null) return;

            for (var i = 0; i < _tabButtons.Length; i++)
            {
                var b = _tabButtons[i];
                if (i >= schools.Count)
                {
                    b.gameObject.SetActive(false);
                    continue;
                }

                b.gameObject.SetActive(true);
                _tabCount++;
                var nm = schools[i].DisplayName;
                var lbl = b.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl != null)
                    lbl.text = nm;
                var captured = i;
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => SelectSchool(captured));
            }
        }

        void SelectSchool(int tabIndex)
        {
            var schools = SkillSchoolTable.Instance.AllSchools;
            if (tabIndex < 0 || tabIndex >= schools.Count) return;
            ActiveSchoolId = schools[tabIndex].Id;

            if (_tabButtons != null)
            {
                for (var i = 0; i < _tabButtons.Length; i++)
                {
                    if (!_tabButtons[i].gameObject.activeSelf) continue;
                    var img = _tabButtons[i].GetComponent<Image>();
                    if (img == null) continue;
                    img.color = i == tabIndex
                        ? new Color(0.38f, 0.55f, 0.72f, 1f)
                        : new Color(0.28f, 0.3f, 0.35f, 1f);
                }
            }

            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            var sys = mgr?.SkillSystem;

            SkillSchoolTable.Instance.TryGetSchool(ActiveSchoolId, out var sch);
            var list = new List<string>();
            if (sch != null && sch.SkillIds != null && sys != null)
            {
                foreach (var id in sch.SkillIds)
                {
                    if (sys.IsLearned(id))
                        list.Add(id);
                }
            }

            if (_poolCells != null)
            {
                for (var i = 0; i < _poolCells.Length; i++)
                {
                    if (i >= list.Count)
                    {
                        _poolCells[i].SetVisible(false);
                        continue;
                    }

                    _poolCells[i].SetVisible(true);
                    _poolCells[i].Bind(list[i], _poolSkillDropBehavior);
                }
            }

            if (sys != null && _slotViews != null)
            {
                foreach (var slot in _slotViews)
                    slot.RefreshDisplay(sys);
            }
        }

        public void RefreshAll()
        {
            BindBuiltReferencesIfNeeded();
            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            var sys = mgr?.SkillSystem;
            if (sys != null && _slotViews != null)
            {
                foreach (var slot in _slotViews)
                    slot.RefreshDisplay(sys);
            }

            if (!string.IsNullOrEmpty(ActiveSchoolId))
            {
                var idx = FindTabIndexForSchool(ActiveSchoolId);
                if (idx >= 0)
                    SelectSchool(idx);
            }
            else if (_tabCount > 0)
                SelectSchool(0);

            TryRefreshHudBar();
        }

        int FindTabIndexForSchool(string id)
        {
            var schools = SkillSchoolTable.Instance.AllSchools;
            for (var i = 0; i < schools.Count; i++)
            {
                if (schools[i].Id == id)
                    return i;
            }
            return -1;
        }

        static void TryRefreshHudBar()
        {
            var hud = OverworldHUDPanel.Instance;
            if (hud != null && hud.SkilBar != null)
                hud.SkilBar.Refresh();
        }

        public bool OnConfirm() => false;

        public bool OnCancel()
        {
            UIManager.Instance.HidePanel(Pid);
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
