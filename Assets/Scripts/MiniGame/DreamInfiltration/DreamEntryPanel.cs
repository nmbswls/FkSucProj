using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.MiniGame.Dream
{
    public class DreamEntryPanel : PanelWithInput
    {
        [Header("Prefab（可空则按子节点名解析）")]
        [SerializeField] private RectTransform spotsContainer;
        [SerializeField] private RectTransform spotButtonTemplate;
        [SerializeField] private RectTransform detailRoot;
        [SerializeField] private TextMeshProUGUI detailBody;
        [SerializeField] private Button enterDreamButton;

        private TbCharDreamEntryInfo _characterDreamEntryTable;
        private RectTransform _rootRt;
        private DreamEntrySpotButtonView _selected;
        private readonly List<DreamEntrySpotButtonView> _figures = new();
        private bool _enterWired;

        // 选中上下文
        private DreamEntrySourceKind _selectedKind;
        private string _selectedPasserbyId = "";
        private int _selectedCharacterEntryId;
        private AbstractGroup _selectedGroup;
        private AbstractGroupStage _selectedStage;

        public override int FocusPriority => 820;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            _rootRt = GetComponent<RectTransform>();
            layer = UILayer.Overlay;
            ResolvePrefabRefs();
            EnsureDetailUi();
            if (spotButtonTemplate != null) spotButtonTemplate.gameObject.SetActive(false);
            var bg = _rootRt.Find("Bg")?.GetComponent<Image>();
            DreamUISpriteUtil.EnsureWhiteSprite(bg);
            WireEnterOnce();
        }

        private void ResolvePrefabRefs()
        {
            if (spotsContainer == null) spotsContainer = _rootRt.Find("Spots") as RectTransform;
            if (spotButtonTemplate == null && spotsContainer != null)
                spotButtonTemplate = spotsContainer.Find("SpotTemplate") as RectTransform;
            if (detailRoot == null) detailRoot = _rootRt.Find("Detail") as RectTransform;
            if (detailBody == null && detailRoot != null)
                detailBody = detailRoot.Find("Body")?.GetComponent<TextMeshProUGUI>();
            if (enterDreamButton == null && detailRoot != null)
                enterDreamButton = detailRoot.Find("EnterBtn")?.GetComponent<Button>();
        }

        void EnsureDetailUi()
        {
            if (detailRoot != null && detailBody != null && enterDreamButton != null) return;

            var go = new GameObject("Detail", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_rootRt, false);
            detailRoot = (RectTransform)go.transform;
            detailRoot.anchorMin = new Vector2(0.08f, 0.04f);
            detailRoot.anchorMax = new Vector2(0.72f, 0.22f);
            detailRoot.offsetMin = Vector2.zero;
            detailRoot.offsetMax = Vector2.zero;
            var bg = go.GetComponent<Image>();
            DreamUISpriteUtil.EnsureWhiteSprite(bg);
            bg.color = new Color(0.08f, 0.09f, 0.12f, 0.92f);

            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyGo.transform.SetParent(detailRoot, false);
            var bodyRt = (RectTransform)bodyGo.transform;
            bodyRt.anchorMin = new Vector2(0.03f, 0.15f);
            bodyRt.anchorMax = new Vector2(0.72f, 0.95f);
            bodyRt.offsetMin = Vector2.zero;
            bodyRt.offsetMax = Vector2.zero;
            detailBody = bodyGo.GetComponent<TextMeshProUGUI>();
            detailBody.fontSize = 20f;
            detailBody.color = new Color(0.9f, 0.9f, 0.92f, 1f);
            detailBody.alignment = TextAlignmentOptions.TopLeft;
            detailBody.enableWordWrapping = true;

            var btnGo = new GameObject("EnterBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(detailRoot, false);
            var btnRt = (RectTransform)btnGo.transform;
            btnRt.anchorMin = new Vector2(0.76f, 0.2f);
            btnRt.anchorMax = new Vector2(0.96f, 0.8f);
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;
            enterDreamButton = btnGo.GetComponent<Button>();
            DreamUISpriteUtil.EnsureWhiteSprite(btnGo.GetComponent<Image>());
            btnGo.GetComponent<Image>().color = new Color(0.35f, 0.45f, 0.55f, 0.95f);

            var btnLabelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnLabelGo.transform.SetParent(btnGo.transform, false);
            var btnLabelRt = (RectTransform)btnLabelGo.transform;
            btnLabelRt.anchorMin = Vector2.zero;
            btnLabelRt.anchorMax = Vector2.one;
            btnLabelRt.offsetMin = Vector2.zero;
            btnLabelRt.offsetMax = Vector2.zero;
            var btnLabel = btnLabelGo.GetComponent<TextMeshProUGUI>();
            btnLabel.text = "入梦";
            btnLabel.fontSize = 24f;
            btnLabel.alignment = TextAlignmentOptions.Center;
            btnLabel.color = Color.white;
        }

        void WireEnterOnce()
        {
            if (_enterWired || enterDreamButton == null) return;
            enterDreamButton.onClick.AddListener(OnEnterDreamClicked);
            _enterWired = true;
        }

        public override void Setup(object data = null)
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            AbstractGroupDreamService.EnsureDailySynced(glm);
            DreamPasserbyService.EnsureDailySynced(glm);

            _characterDreamEntryTable = CfgMgr.Cfgs?.TbCharDreamEntryInfo;
            ClearSelection();
            RebuildAll();
        }

        void RebuildAll()
        {
            ResolvePrefabRefs();
            EnsureDetailUi();
            WireEnterOnce();
            if (spotsContainer == null || spotButtonTemplate == null)
            {
                Debug.LogError("[DreamInfiltration] DreamEntryPanel missing Spots or SpotTemplate.");
                return;
            }

            ClearSpawnedSpots();
            _figures.Clear();

            RebuildPasserbyFigures();
            RebuildAbstractGroupFigure();
            RebuildCharacterEntries();
            RefreshDetailUi();
        }

        void RebuildPasserbyFigures()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            var entries = DreamPasserbyService.GetTodayEntries(glm);
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.PasserbyId)) continue;
                var cfg = CfgMgr.Cfgs?.TbDreamPasserby?.GetOrDefault(entry.PasserbyId);
                if (cfg == null) continue;

                var inst = Instantiate(spotButtonTemplate, spotsContainer);
                inst.gameObject.SetActive(true);
                var view = inst.GetComponent<DreamEntrySpotButtonView>();
                if (view == null)
                {
                    Debug.LogError("[DreamInfiltration] SpotTemplate missing DreamEntrySpotButtonView.");
                    continue;
                }

                view.BindPasserbyFigure(cfg, entry, OnFigureSelected);
                _figures.Add(view);
            }
        }

        void RebuildAbstractGroupFigure()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (!AbstractGroupDreamService.TryGetTodayGroupEntry(glm, out var groupCfg, out var stageCfg))
            {
                return;
            }

            var inst = Instantiate(spotButtonTemplate, spotsContainer);
            inst.gameObject.SetActive(true);
            var view = inst.GetComponent<DreamEntrySpotButtonView>();
            if (view == null) return;
            view.BindAbstractGroupFigure(groupCfg, stageCfg, 0.48f, 0.42f, OnFigureSelected);
            _figures.Add(view);
        }

        void RebuildCharacterEntries()
        {
            var entries = _characterDreamEntryTable?.DataList;
            var glm = MainGameManager.Instance?.gameLogicManager;
            var psm = glm?.playerDataManager;
            if (entries == null || psm == null || glm == null) return;

            var visibleIndex = 0;
            foreach (var entry in entries)
            {
                if (entry == null || !DreamCharacterEntryHelper.IsCharacterEntryUnlocked(entry, psm, glm))
                    continue;

                var character = CfgMgr.Cfgs?.TbCharacterInfo?.GetOrDefault(entry.CharacterKey);
                var displayName = string.IsNullOrEmpty(character?.Name) ? entry.CharacterKey : character.Name;

                var inst = Instantiate(spotButtonTemplate, spotsContainer);
                inst.gameObject.SetActive(true);
                var view = inst.GetComponent<DreamEntrySpotButtonView>();
                if (view == null) continue;
                view.BindCharacterEntry(entry.Id, displayName, visibleIndex, OnFigureSelected);
                // 角色详情用名字缓存：塞进 name
                view.gameObject.name = $"CharacterDream_{entry.Id}|{displayName}";
                _figures.Add(view);
                visibleIndex++;
            }
        }

        void ClearSpawnedSpots()
        {
            for (var i = spotsContainer.childCount - 1; i >= 0; i--)
            {
                var c = spotsContainer.GetChild(i);
                if (c == spotButtonTemplate) continue;
                Destroy(c.gameObject);
            }
        }

        void OnFigureSelected(DreamEntrySpotButtonView view)
        {
            if (view == null) return;
            _selected = view;
            foreach (var f in _figures)
            {
                if (f != null) f.SetSelected(f == view);
            }

            _selectedKind = view.SourceKind;
            _selectedPasserbyId = view.PasserbyId ?? "";
            _selectedCharacterEntryId = view.CharacterEntryId;
            _selectedGroup = null;
            _selectedStage = null;

            if (view.SourceKind == DreamEntrySourceKind.AbstractGroupEntry)
            {
                var glm = MainGameManager.Instance?.gameLogicManager;
                AbstractGroupDreamService.TryGetTodayGroupEntry(glm, out _selectedGroup, out _selectedStage);
            }

            RefreshDetailUi();
        }

        void RefreshDetailUi()
        {
            if (detailBody == null) return;
            if (_selected == null)
            {
                detailBody.text = "点击地图上的小人查看详情。\n右上角为角色梦境。\n团体更大且带边框。";
                if (enterDreamButton != null) enterDreamButton.interactable = false;
                return;
            }

            if (enterDreamButton != null) enterDreamButton.interactable = true;

            switch (_selectedKind)
            {
                case DreamEntrySourceKind.PasserbyEntry:
                {
                    var cfg = CfgMgr.Cfgs?.TbDreamPasserby?.GetOrDefault(_selectedPasserbyId);
                    DreamPasserbyDailyEntryPersist entry = null;
                    var glm = MainGameManager.Instance?.gameLogicManager;
                    foreach (var e in DreamPasserbyService.GetTodayEntries(glm))
                    {
                        if (e != null && e.PasserbyId == _selectedPasserbyId)
                        {
                            entry = e;
                            break;
                        }
                    }

                    var region = entry != null
                        ? CfgMgr.Cfgs?.TbDreamPasserbyRegion?.GetOrDefault(entry.RegionId)
                        : null;
                    detailBody.text = DreamEntryRewardSemantics.BuildPasserbyDetail(cfg, region, entry);
                    break;
                }
                case DreamEntrySourceKind.CharacterEntry:
                {
                    var name = _selected.gameObject.name;
                    var display = name;
                    var sep = name.IndexOf('|');
                    if (sep >= 0 && sep + 1 < name.Length) display = name.Substring(sep + 1);
                    detailBody.text = DreamEntryRewardSemantics.BuildCharacterDetail(display);
                    break;
                }
                case DreamEntrySourceKind.AbstractGroupEntry:
                {
                    var maxStage = _selectedGroup != null ? Mathf.Max(1, _selectedGroup.MaxStage) : 0;
                    var near = _selectedStage != null && maxStage > 0 && _selectedStage.Stage >= maxStage;
                    detailBody.text = DreamEntryRewardSemantics.BuildAbstractGroupDetail(
                        _selectedGroup, _selectedStage, maxStage, near);
                    break;
                }
            }
        }

        void OnEnterDreamClicked()
        {
            if (_selected == null) return;
            var glm = MainGameManager.Instance?.gameLogicManager;

            switch (_selectedKind)
            {
                case DreamEntrySourceKind.PasserbyEntry:
                {
                    if (!DreamPasserbyService.TryCreateGameplayContext(glm, _selectedPasserbyId, out var ctx, out var reason))
                    {
                        ToastBlocked(reason);
                        return;
                    }

                    TryBeginEntry(ctx);
                    break;
                }
                case DreamEntrySourceKind.CharacterEntry:
                {
                    var table = CfgMgr.Cfgs?.TbCharDreamEntryInfo;
                    var entry = table?.GetOrDefault(_selectedCharacterEntryId);
                    if (entry == null) return;
                    if (!DreamCharacterEntryHelper.TryCreateGameplayContext(entry.CharacterKey, entry.Id, out var ctx))
                        return;
                    TryBeginEntry(ctx);
                    break;
                }
                case DreamEntrySourceKind.AbstractGroupEntry:
                {
                    if (!AbstractGroupDreamService.TryCreateGameplayContext(glm, out var ctx, out var reason))
                    {
                        ToastBlocked(reason);
                        return;
                    }

                    TryBeginEntry(ctx);
                    break;
                }
            }
        }

        void ToastBlocked(string reason)
        {
            if (reason == "not_night")
                UIEventGrantToastPanel.ShowToast("入梦", "仅夜间可入梦", "白天无法开启梦境通道。");
            else if (reason == "used_today")
                UIEventGrantToastPanel.ShowToast("入梦", "今日已入梦", "请等待下一天夜间。");
        }

        private bool TryBeginEntry(DreamGameplayContext ctx)
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (!AbstractGroupDreamService.IsDreamAllowedTonight(glm, out var reason))
            {
                ToastBlocked(reason);
                return false;
            }

            DreamInfiltrationBootstrap.BeginGameplay(ctx);
            return true;
        }

        void ClearSelection()
        {
            _selected = null;
            _selectedPasserbyId = "";
            _selectedCharacterEntryId = 0;
            _selectedGroup = null;
            _selectedStage = null;
        }

        public override bool OnCancel()
        {
            UIManager.Instance?.HidePanel(DreamInfiltrationIds.EntryPanel);
            DreamInfiltrationBootstrap.ExitMiniGame();
            return true;
        }

        public override bool OnNavigate(Vector2 dir) => true;
        public override bool CapturesNavigateAxisForWorld => true;
    }
}
