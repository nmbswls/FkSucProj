using System.Collections.Generic;
using cfg.demo;
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

        private DreamInfiltrationDatabase _db;
        private RectTransform _rootRt;
        private readonly List<DreamThemeWeight> _rolledThemes = new();

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
            _db = DreamInfiltrationDatabase.LoadOrDefault();
            _rolledThemes.Clear();
            foreach (var s in _db.Spots)
                _rolledThemes.Add(RollTheme(s));

            RebuildSpots();
        }

        public override void Show()
        {
            base.Show();
        }


        private static DreamThemeWeight RollTheme(DreamEntrySpotDef spot)
        {
            var list = spot.ThemeWeights;
            if (list == null || list.Count == 0)
                return new DreamThemeWeight { ThemeId = "default", ThemeDisplayName = "浅梦", Weight = 1 };
            int sum = 0;
            foreach (var t in list) sum += Mathf.Max(1, t.Weight);
            int r = Random.Range(0, sum);
            int acc = 0;
            foreach (var t in list)
            {
                acc += Mathf.Max(1, t.Weight);
                if (r < acc) return t;
            }

            return list[^1];
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

            for (var i = 0; i < _db.Spots.Count; i++)
            {
                var spot = _db.Spots[i];
                var rolled = _rolledThemes[i];
                var inst = Instantiate(spotButtonTemplate, spotsContainer);
                inst.gameObject.SetActive(true);
                var view = inst.GetComponent<DreamEntrySpotButtonView>();
                if (view != null)
                    view.BindFromData(spot, rolled, i, OnSpotClicked);
                else
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

        private void OnSpotClicked(int index)
        {
            var mg = MainGameManager.Instance;
            var glm = mg != null ? mg.gameLogicManager : null;
            if (glm == null)
            {
                Debug.LogWarning("[DreamInfiltration] GameLogicManager missing.");
                return;
            }

            var spot = _db.Spots[index];
            var conds = new List<CommonCheckCond>();
            if (spot.UnlockConds != null)
            {
                foreach (var row in spot.UnlockConds)
                    conds.Add(DreamCheckUtil.ToCommonCheckCond(row));
            }

            if (!glm.CheckCommonCondsAll(conds))
            {
                Debug.Log("[DreamInfiltration] Spot locked by CommonCheckCond.");
                return;
            }

            var rolled = _rolledThemes[index];
            var ctx = new DreamGameplayContext
            {
                ThemeId = rolled.ThemeId,
                ThemeDisplayName = rolled.ThemeDisplayName,
            };
            UIManager.Instance?.HidePanel(DreamInfiltrationIds.EntryPanel);
            UIManager.Instance?.ShowPanel(DreamInfiltrationIds.GameplayPanel, ctx, UILayer.Overlay);
        }

        public override bool OnCancel()
        {
            UIManager.Instance?.HidePanel(DreamInfiltrationIds.EntryPanel);
            DreamInfiltrationLogicPause.ExitMiniGame();
            return true;
        }

        public override bool OnNavigate(Vector2 dir)
        {
            return true;
        }
    }
}
