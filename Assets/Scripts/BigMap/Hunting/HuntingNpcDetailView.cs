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

        public void SetTarget(SceneNpcPresenter npc, bool canExecute)
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

            if (ExecuteHintRoot != null)
            {
                ExecuteHintRoot.gameObject.SetActive(canExecute);
            }

            if (ExecuteHintText != null && canExecute)
            {
                ExecuteHintText.text = "点击处决";
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
            if (_target == null || DetailRoot == null)
            {
                return;
            }

            var hintPos = _target.GetHintAnchorPosition();
            Vector3 screenPos = Camera.main.WorldToScreenPoint(hintPos);
            var canvasRt = UIManager.Instance.RootCanvas.transform as RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRt,
                screenPos,
                UIManager.Instance.UICamera,
                out Vector2 localPos);
            DetailRoot.localPosition = localPos;

            if (ExecuteHintRoot != null && ExecuteHintRoot.gameObject.activeSelf)
            {
                ExecuteHintRoot.localPosition = localPos + new Vector2(0f, 48f);
            }
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
