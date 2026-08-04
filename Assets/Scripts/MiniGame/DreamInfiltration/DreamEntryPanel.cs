using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map;
using My.UI;
using UnityEngine;
using UnityEngine.UI;

namespace My.MiniGame.Dream
{
    public class DreamEntryPanel : PanelWithInput
    {
        [Header("Prefab（必填，勿在运行时创建 UI）")]
        [SerializeField] private RectTransform spotsContainer;
        [SerializeField] private RectTransform spotButtonTemplate;

        private TbDreamInfiltrationSpot _dreamSpotTable;
        private TbCharDreamEntryInfo _characterDreamEntryTable;
        private RectTransform _rootRt;
        private readonly List<(string themeId, string themeDisplayName)> _rolledThemes = new();

        public override int FocusPriority => 820;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            _rootRt = GetComponent<RectTransform>();
            layer = UILayer.Overlay;
            ResolvePrefabRefs();
            if (spotButtonTemplate != null) spotButtonTemplate.gameObject.SetActive(false);
            var bg = _rootRt.Find("Bg")?.GetComponent<Image>();
            DreamUISpriteUtil.EnsureWhiteSprite(bg);
        }

        private void ResolvePrefabRefs()
        {
            if (spotsContainer == null) spotsContainer = _rootRt.Find("Spots") as RectTransform;
            if (spotButtonTemplate == null && spotsContainer != null)
                spotButtonTemplate = spotsContainer.Find("SpotTemplate") as RectTransform;
        }

        public override void Setup(object data = null)
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            AbstractGroupDreamService.EnsureDailySynced(glm);

            _dreamSpotTable = CfgMgr.Cfgs?.TbDreamInfiltrationSpot;
            _characterDreamEntryTable = CfgMgr.Cfgs?.TbCharDreamEntryInfo;
            if (_dreamSpotTable == null || _dreamSpotTable.DataList == null || _dreamSpotTable.DataList.Count == 0)
            {
                Debug.LogError("[DreamInfiltration] TbDreamInfiltrationSpot missing or empty. Check demo_tbdreaminfiltrationspot.json and Tables loader.");
                return;
            }

            _rolledThemes.Clear();
            foreach (var s in _dreamSpotTable.DataList)
                _rolledThemes.Add(RollTheme(s));

            RebuildSpots();
        }

        public override void Show()
        {
            base.Show();
        }

        private static (string themeId, string themeDisplayName) RollTheme(DreamInfiltrationSpot spot)
        {
            var ids = spot.ThemeIds;
            var names = spot.ThemeDisplayNames;
            var weights = spot.ThemeWeightValues;
            if (ids == null || names == null || weights == null || ids.Count == 0)
                return ("default", "浅梦");
            var n = Mathf.Min(Mathf.Min(ids.Count, names.Count), weights.Count);
            if (n <= 0)
                return ("default", "浅梦");

            var sum = 0;
            for (var i = 0; i < n; i++)
                sum += Mathf.Max(1, weights[i]);
            var r = Random.Range(0, sum);
            var acc = 0;
            for (var i = 0; i < n; i++)
            {
                acc += Mathf.Max(1, weights[i]);
                if (r < acc)
                    return (ids[i], names[i]);
            }

            return (ids[n - 1], names[n - 1]);
        }

        private void RebuildSpots()
        {
            ResolvePrefabRefs();
            if (spotsContainer == null || spotButtonTemplate == null)
            {
                Debug.LogError("[DreamInfiltration] DreamEntryPanel missing Spots or SpotTemplate in prefab.");
                return;
            }

            ClearSpawnedSpots();

            if (_dreamSpotTable == null) return;

            for (var i = 0; i < _dreamSpotTable.DataList.Count; i++)
            {
                var spot = _dreamSpotTable.DataList[i];
                var rolled = _rolledThemes[i];
                var inst = Instantiate(spotButtonTemplate, spotsContainer);
                inst.gameObject.SetActive(true);
                var view = inst.GetComponent<DreamEntrySpotButtonView>();
                if (view != null)
                    view.BindFromData(spot, rolled.themeDisplayName, i, OnSpotClicked);
                else
                    Debug.LogError("[DreamInfiltration] SpotTemplate missing DreamEntrySpotButtonView.");
            }

            RebuildCharacterEntries();
            RebuildAbstractGroupEntry();
        }

        private void RebuildCharacterEntries()
        {
            var entries = _characterDreamEntryTable?.DataList;
            var glm = MainGameManager.Instance?.gameLogicManager;
            var psm = glm?.playerDataManager;
            if (entries == null || psm == null || glm == null)
            {
                return;
            }

            var visibleIndex = 0;
            foreach (var entry in entries)
            {
                if (entry == null || !DreamCharacterEntryHelper.IsCharacterEntryUnlocked(entry, psm, glm))
                {
                    continue;
                }

                var character = CfgMgr.Cfgs?.TbCharacterInfo?.GetOrDefault(entry.CharacterKey);
                var displayName = string.IsNullOrEmpty(character?.Name)
                    ? entry.CharacterKey
                    : character.Name;

                var inst = Instantiate(spotButtonTemplate, spotsContainer);
                inst.gameObject.SetActive(true);
                var view = inst.GetComponent<DreamEntrySpotButtonView>();
                if (view != null)
                {
                    view.BindCharacterEntry(entry.Id, displayName, visibleIndex, OnCharacterEntryClicked);
                    visibleIndex++;
                }
                else
                {
                    Debug.LogError("[DreamInfiltration] SpotTemplate missing DreamEntrySpotButtonView.");
                }
            }
        }

        private void RebuildAbstractGroupEntry()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (!AbstractGroupDreamService.TryGetTodayGroupEntry(glm, out var groupCfg, out var stageCfg))
            {
                return;
            }

            var inst = Instantiate(spotButtonTemplate, spotsContainer);
            inst.gameObject.SetActive(true);
            var view = inst.GetComponent<DreamEntrySpotButtonView>();
            if (view != null)
            {
                view.BindAbstractGroupEntry(groupCfg, stageCfg, OnAbstractGroupEntryClicked);
            }
            else
            {
                Debug.LogError("[DreamInfiltration] SpotTemplate missing DreamEntrySpotButtonView.");
            }
        }

        private void ClearSpawnedSpots()
        {
            for (var i = spotsContainer.childCount - 1; i >= 0; i--)
            {
                var c = spotsContainer.GetChild(i);
                if (c == spotButtonTemplate) continue;
                Destroy(c.gameObject);
            }
        }

        private bool TryBeginEntry(DreamGameplayContext ctx)
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (!AbstractGroupDreamService.IsDreamAllowedTonight(glm, out var reason))
            {
                if (reason == "not_night")
                    UIEventGrantToastPanel.ShowToast("入梦", "仅夜间可入梦", "白天无法开启梦境通道。");
                else if (reason == "used_today")
                    UIEventGrantToastPanel.ShowToast("入梦", "今日已入梦", "请等待下一天夜间。");
                return false;
            }

            DreamInfiltrationBootstrap.BeginGameplay(ctx);
            return true;
        }

        private void OnSpotClicked(int index)
        {
            var mg = MainGameManager.Instance;
            var glm = mg != null ? mg.gameLogicManager : null;
            if (glm == null)
            {
                Debug.LogWarning("[DreamInfiltration] GameLogicManager missing.");
                return;
            }

            if (_dreamSpotTable == null || index < 0 || index >= _dreamSpotTable.DataList.Count)
                return;

            var spot = _dreamSpotTable.DataList[index];
            if (!glm.CheckCommonCondsAll(spot.UnlockConds))
            {
                Debug.Log("[DreamInfiltration] Spot locked by CommonCheckCond.");
                return;
            }

            var rolled = _rolledThemes[index];
            var ctx = new DreamGameplayContext
            {
                ThemeId = rolled.themeId,
                ThemeDisplayName = rolled.themeDisplayName,
                EntrySource = DreamEntrySourceKind.FacilitySpot,
                SpotId = spot.SpotId,
            };

            TryBeginEntry(ctx);
        }

        public void OnCharacterEntryClicked(int entryId)
        {
            var mg = MainGameManager.Instance;
            var glm = mg != null ? mg.gameLogicManager : null;
            if (glm == null)
            {
                Debug.LogWarning("[DreamInfiltration] GameLogicManager missing.");
                return;
            }

            var table = CfgMgr.Cfgs?.TbCharDreamEntryInfo;
            var entry = table?.GetOrDefault(entryId);
            if (entry == null)
            {
                Debug.LogWarning($"[DreamInfiltration] Char dream entry not found: {entryId}");
                return;
            }

            if (!DreamCharacterEntryHelper.TryCreateGameplayContext(entry.CharacterKey, entryId, out var ctx))
            {
                return;
            }

            TryBeginEntry(ctx);
        }

        private void OnAbstractGroupEntryClicked()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (!AbstractGroupDreamService.TryCreateGameplayContext(glm, out var ctx, out var reason))
            {
                Debug.Log($"[DreamInfiltration] Abstract group entry blocked: {reason}");
                if (reason == "not_night")
                    UIEventGrantToastPanel.ShowToast("入梦", "仅夜间可入梦", "白天无法开启梦境通道。");
                else if (reason == "used_today")
                    UIEventGrantToastPanel.ShowToast("入梦", "今日已入梦", "请等待下一天夜间。");
                return;
            }

            TryBeginEntry(ctx);
        }

        public override bool OnCancel()
        {
            UIManager.Instance?.HidePanel(DreamInfiltrationIds.EntryPanel);
            DreamInfiltrationBootstrap.ExitMiniGame();
            return true;
        }

        public override bool OnNavigate(Vector2 dir)
        {
            return true;
        }

        public override bool CapturesNavigateAxisForWorld => true;
    }
}
