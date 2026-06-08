using My.Map.Entity;
using My.Map.Scene;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.Map.Hunting
{
    /// <summary>
    /// 狩猎模式 NPC 详情：Preview 仅名字+血，Pinned 显示完整战术信息与操作提示。
    /// </summary>
    public class HuntingNpcDetailView : MonoBehaviour
    {
        public enum EDetailMode
        {
            None,
            Preview,
            Pinned,
        }

        public RectTransform DetailRoot;
        public TextMeshProUGUI NameText;
        public Image SJProgressBar;
        public TextMeshProUGUI SJProgressText;
        public TextMeshProUGUI NpcHpText;
        public TextMeshProUGUI NpcWillText;
        public RectTransform ExecuteHintRoot;
        public TextMeshProUGUI ExecuteHintText;

        SceneNpcPresenter _target;
        EDetailMode _mode = EDetailMode.None;
        Camera _uiCam;

        public EDetailMode Mode => _mode;

        public bool IsPinnedVisible =>
            _mode == EDetailMode.Pinned && DetailRoot != null && DetailRoot.gameObject.activeSelf;

        void Awake()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                _uiCam = canvas.worldCamera;
            }
        }

        public void SetActiveRoot(bool on)
        {
            if (DetailRoot != null)
            {
                DetailRoot.gameObject.SetActive(on);
            }
        }

        public void SetTarget(SceneNpcPresenter npc, EDetailMode mode, bool canExecute, bool canControl)
        {
            _target = npc;
            _mode = npc == null ? EDetailMode.None : mode;

            if (DetailRoot != null)
            {
                DetailRoot.gameObject.SetActive(npc != null);
            }

            if (npc == null)
            {
                if (ExecuteHintRoot != null)
                {
                    ExecuteHintRoot.gameObject.SetActive(false);
                }
                return;
            }

            if (NameText != null)
            {
                NameText.text = npc.ShowName;
            }

            RefreshStats(npc);

            bool isPinned = mode == EDetailMode.Pinned;
            SetPinnedExtrasVisible(isPinned);

            if (ExecuteHintRoot != null)
            {
                bool showHint = isPinned && (canExecute || canControl);
                ExecuteHintRoot.gameObject.SetActive(showHint);
            }

            if (ExecuteHintText != null && isPinned)
            {
                ExecuteHintText.text = "再次点击取消";
            }

            RefreshLayout();
        }

        public void Clear()
        {
            _target = null;
            _mode = EDetailMode.None;
            if (DetailRoot != null)
            {
                DetailRoot.gameObject.SetActive(false);
            }

            if (ExecuteHintRoot != null)
            {
                ExecuteHintRoot.gameObject.SetActive(false);
            }
        }

        public bool ContainsScreenPoint(Vector2 screenPos, float paddingPx = 10f)
        {
            if (!IsPinnedVisible || DetailRoot == null)
            {
                return false;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(
                DetailRoot,
                screenPos,
                _uiCam,
                Vector4.one * paddingPx);
        }

        public void RefreshLayout()
        {
            if (_target == null || DetailRoot == null || UIManager.Instance == null)
            {
                return;
            }

            var hintPos = _target.GetHintAnchorPosition();
            var gameplayCam = Camera.main;
            if (gameplayCam == null)
            {
                return;
            }

            Vector3 screenPos = gameplayCam.WorldToScreenPoint(hintPos);
            var rootRt = UIManager.Instance.RootCanvas.transform as RectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rootRt,
                    screenPos,
                    UIManager.Instance.UICamera,
                    out Vector2 localInRoot))
            {
                return;
            }

            var positionParent = DetailRoot.parent as RectTransform;
            if (positionParent == null)
            {
                return;
            }

            Vector3 worldOnCanvas = rootRt.TransformPoint(new Vector3(localInRoot.x, localInRoot.y, 0f));
            DetailRoot.localPosition = positionParent.InverseTransformPoint(worldOnCanvas);
        }

        void SetPinnedExtrasVisible(bool visible)
        {
            if (SJProgressBar != null)
            {
                SJProgressBar.gameObject.SetActive(visible);
            }

            if (SJProgressText != null)
            {
                SJProgressText.gameObject.SetActive(visible);
            }

            if (NpcWillText != null)
            {
                NpcWillText.gameObject.SetActive(visible);
            }
        }

        void RefreshStats(SceneNpcPresenter npc)
        {
            if (npc == null)
            {
                return;
            }

            if (NpcHpText != null)
            {
                var hpVal = (long)(npc.NpcEntity.GetAttr(AttrIdConsts.HP) * 0.001);
                var hpMaxVal = (long)(npc.NpcEntity.GetResourceMax(AttrIdConsts.HP) * 0.001);
                NpcHpText.text = $"{hpVal}/{hpMaxVal}";
            }

            if (_mode != EDetailMode.Pinned)
            {
                return;
            }

            var sjProgress = npc.NpcEntity.GetAttr(AttrIdConsts.NPCSJProgress);
            float sjFill = sjProgress * 1.0f / 100_000f;
            int sjPercent = Mathf.Clamp(Mathf.RoundToInt(sjFill * 100f), 0, 100);

            if (SJProgressBar != null)
            {
                SJProgressBar.fillAmount = sjFill;
                SJProgressBar.color = HuntingStatVisual.GetSjBarColor(sjPercent);
            }

            if (SJProgressText != null)
            {
                SJProgressText.text = $"{sjPercent}%";
            }

            if (NpcWillText != null)
            {
                var hShield = npc.NpcEntity.GetAttr(AttrIdConsts.UnitHShield);
                NpcWillText.text = hShield > 0
                    ? ((int)Mathf.Ceil(hShield * 1.0f / 1000f)).ToString()
                    : string.Empty;
            }
        }
    }

    // 狩猎 UI 共用 SJ 条配色
    static class HuntingStatVisual
    {
        public static Color GetSjBarColor(int sjPercent)
        {
            if (sjPercent < 30)
            {
                return new Color(0.75f, 0.75f, 0.75f, 1f);
            }

            if (sjPercent < 60)
            {
                return new Color(1f, 0.85f, 0.2f, 1f);
            }

            if (sjPercent < 85)
            {
                return new Color(1f, 0.55f, 0.1f, 1f);
            }

            return new Color(1f, 0.25f, 0.2f, 1f);
        }
    }
}
