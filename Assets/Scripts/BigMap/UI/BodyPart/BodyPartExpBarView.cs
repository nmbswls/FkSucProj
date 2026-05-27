using cfg.demo;
using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.BodyPart
{
    // 部位详情：等级 + 本级经验进度条
    public sealed class BodyPartExpBarView : MonoBehaviour
    {
        [SerializeField] Image fillImage;
        [SerializeField] TextMeshProUGUI levelText;
        [SerializeField] TextMeshProUGUI expText;

        void Awake()
        {
            EnsureFillImageType();
        }

        public void Refresh(PlayerBodyPartSystem bodyPart, EBodyPart part)
        {
            EnsureFillImageType();

            if (bodyPart == null || part == EBodyPart.None)
            {
                BindEmpty();
                return;
            }

            var state = bodyPart.GetPartState(part);
            if (state == null)
            {
                BindEmpty();
                return;
            }

            if (!BodyPartCatalog.TryGetLevelProgress(part, state.Level, state.Exp, out var progress))
            {
                BindEmpty();
                return;
            }

            if (levelText != null)
            {
                if (expText == null && !progress.IsMaxLevel)
                {
                    levelText.text = $"Lv.{state.Level}  {progress.ExpInLevel}/{progress.ExpSpan}";
                }
                else if (expText == null && progress.IsMaxLevel)
                {
                    levelText.text = $"Lv.{state.Level}  已满级";
                }
                else
                {
                    levelText.text = $"Lv.{state.Level}";
                }
            }

            if (expText != null)
            {
                expText.text = progress.IsMaxLevel
                    ? "已满级"
                    : $"{progress.ExpInLevel} / {progress.ExpSpan}";
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = progress.IsMaxLevel ? 1f : progress.Fill01;
                fillImage.gameObject.SetActive(true);
            }
        }

        void BindEmpty()
        {
            if (levelText != null)
            {
                levelText.text = string.Empty;
            }

            if (expText != null)
            {
                expText.text = string.Empty;
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = 0f;
            }
        }

        void EnsureFillImageType()
        {
            if (fillImage == null)
            {
                return;
            }

            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }
}
