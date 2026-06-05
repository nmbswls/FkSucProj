using My.Map.Entity;
using My.Map.Scene;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.Map.Hunting
{
    /// <summary>
    /// 狩猎模式鼠标悬浮 NPC 详情（独立于 SceneInteractMenu）。
    /// </summary>
    public class HuntingNpcDetailView : MonoBehaviour
    {
        public RectTransform DetailRoot;
        public TextMeshProUGUI NameText;
        public Image SJProgressBar;

        public TextMeshProUGUI NpcHpText;
        public TextMeshProUGUI NpcWillText;
        public RectTransform ExecuteHintRoot;
        public TextMeshProUGUI ExecuteHintText;

        private SceneNpcPresenter _target;

        public void SetActiveRoot(bool on)
        {
            if (DetailRoot != null)
            {
                DetailRoot.gameObject.SetActive(on);
            }
        }

        public void SetTarget(SceneNpcPresenter npc, bool canExecute, bool canControl)
        {
            _target = npc;
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

            bool showHint = canExecute || canControl;
            if (ExecuteHintRoot != null)
            {
                ExecuteHintRoot.gameObject.SetActive(showHint);
            }

            if (ExecuteHintText != null && showHint)
            {
                ExecuteHintText.text = "点击选择行动";
            }

            RefreshLayout();
        }

        public void Clear()
        {
            _target = null;
            if (DetailRoot != null)
            {
                DetailRoot.gameObject.SetActive(false);
            }

            if (ExecuteHintRoot != null)
            {
                ExecuteHintRoot.gameObject.SetActive(false);
            }
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

        private void RefreshStats(SceneNpcPresenter npc)
        {
            if (npc == null)
            {
                return;
            }

            if (SJProgressBar != null)
            {
                var sjProgress = npc.NpcEntity.GetAttr(AttrIdConsts.NPCSJProgress);
                SJProgressBar.fillAmount = sjProgress * 1.0f / 100_000f;
            }

            if(NpcHpText != null)
            {
                var hpVal = (long)(npc.NpcEntity.GetAttr(AttrIdConsts.HP) * 0.001);
                var hpMaxVal = (long)(npc.NpcEntity.GetResourceMax(AttrIdConsts.HP) * 0.001);
                NpcHpText.text = $"{hpVal}/{hpMaxVal}";
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
}
