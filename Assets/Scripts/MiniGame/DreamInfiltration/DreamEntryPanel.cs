using System.Collections.Generic;
using cfg.demo;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.MiniGame.Dream
{
    public class DreamEntryPanel : PanelWithInput
    {
        [SerializeField] private string panelId = DreamInfiltrationIds.EntryPanel;
        [SerializeField] private CanvasGroup canvasGroup;

        private DreamInfiltrationDatabase _db;
        private RectTransform _rootRt;
        private readonly List<DreamThemeWeight> _rolledThemes = new();
        private bool _chromeBuilt;

        public override int FocusPriority => 820;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            _rootRt = GetComponent<RectTransform>();
            layer = UILayer.Overlay;
            BuildLayoutIfNeeded();
        }

        private void BuildLayoutIfNeeded()
        {
            if (_chromeBuilt) return;
            _chromeBuilt = true;

            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(_rootRt, false);
            var bgRt = (RectTransform)bg.transform;
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bg.GetComponent<Image>();
            bgImg.sprite = DreamUISpriteUtil.WhiteSprite();
            bgImg.color = new Color(0.08f, 0.06f, 0.14f, 1f);

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(_rootRt, false);
            var titleRt = (RectTransform)titleGo.transform;
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -24f);
            titleRt.sizeDelta = new Vector2(720f, 48f);
            var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "梦境潜入";
            titleTmp.fontSize = 32;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.92f, 0.88f, 1f);

            var hintGo = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
            hintGo.transform.SetParent(_rootRt, false);
            var hintRt = (RectTransform)hintGo.transform;
            hintRt.anchorMin = new Vector2(0.5f, 0f);
            hintRt.anchorMax = new Vector2(0.5f, 0f);
            hintRt.pivot = new Vector2(0.5f, 0f);
            hintRt.anchoredPosition = new Vector2(0f, 16f);
            hintRt.sizeDelta = new Vector2(900f, 36f);
            var hintTmp = hintGo.GetComponent<TextMeshProUGUI>();
            hintTmp.text = "点击点位进入潜行；Esc 关闭";
            hintTmp.fontSize = 16;
            hintTmp.alignment = TextAlignmentOptions.Center;
            hintTmp.color = new Color(0.75f, 0.72f, 0.8f);
        }

        public override void Setup(object data = null)
        {
            _db = DreamInfiltrationDatabase.LoadOrDefault();
            _rolledThemes.Clear();
            foreach (var s in _db.Spots)
            {
                _rolledThemes.Add(RollTheme(s));
            }

            RebuildSpots();
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
            const string holderName = "Spots";
            var old = _rootRt.Find(holderName);
            if (old != null) Destroy(old.gameObject);

            var holder = new GameObject(holderName, typeof(RectTransform));
            holder.transform.SetParent(_rootRt, false);
            var hrt = (RectTransform)holder.transform;
            hrt.anchorMin = Vector2.zero;
            hrt.anchorMax = Vector2.one;
            hrt.offsetMin = Vector2.zero;
            hrt.offsetMax = Vector2.zero;

            for (var i = 0; i < _db.Spots.Count; i++)
            {
                var spot = _db.Spots[i];
                var rolled = _rolledThemes[i];
                CreateSpotButton(hrt, spot, rolled, i);
            }
        }

        private void CreateSpotButton(RectTransform parent, DreamEntrySpotDef spot, DreamThemeWeight rolled, int index)
        {
            var go = new GameObject($"Spot_{spot.SpotId}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(spot.Anchor01.x, spot.Anchor01.y);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(120f, 120f);
            rt.anchoredPosition = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.sprite = DreamUISpriteUtil.WhiteSprite();
            img.color = new Color(0.35f, 0.28f, 0.55f, 0.92f);

            var btn = go.GetComponent<Button>();
            var captured = index;
            btn.onClick.AddListener(() => OnSpotClicked(captured));

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var lrt = (RectTransform)labelGo.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(4f, 28f);
            lrt.offsetMax = new Vector2(-4f, -4f);
            var tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = $"{spot.DisplayName}\n<size=75%><#cccccc>{rolled.ThemeDisplayName}</size>";
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
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
            return true;
        }
    }
}
