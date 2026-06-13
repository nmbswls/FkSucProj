using My.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 底部进度条：由逻辑层传入 (startLogicTime, duration) 驱动，填充量通过 LogicTime 实时计算，
    // 不再自持时间计数器，避免与逻辑时间不一致
    public class BottomProgressPanel : MonoBehaviour
    {
        public TextMeshProUGUI hintTextComp;
        public Image ProgressBar;

        private float _startLogicTime = -1f;
        private float _duration = 1f;
        private bool _isActive = false;
        private bool _isCancelling = false;
        private float _cancelFadeTimer = 0f;

        public void Update()
        {
            if (_isCancelling)
            {
                _cancelFadeTimer -= Time.deltaTime;
                if (_cancelFadeTimer <= 0f)
                {
                    _isCancelling = false;
                    _isActive = false;
                    gameObject.SetActive(false);
                }
                return;
            }

            if (!_isActive)
            {
                return;
            }

            float elapsed = LogicTime.time - _startLogicTime;
            float fill = Mathf.Clamp01(elapsed / _duration);
            ProgressBar.fillAmount = fill;

            if (fill >= 1f)
            {
                _isActive = false;
                gameObject.SetActive(false);
            }
        }

        // 由 OverworldHUDPanel 调用；startLogicTime 作为幂等 key，重复调用相同 key 无副作用
        public void Setup(string hintText, float duration, float startLogicTime)
        {
            _startLogicTime = startLogicTime;
            _duration = Mathf.Max(duration, 0.01f);
            _isActive = true;
            _isCancelling = false;
            _cancelFadeTimer = 0f;

            if (hintTextComp != null)
            {
                hintTextComp.text = hintText;
            }
            ProgressBar.fillAmount = 0f;
            gameObject.SetActive(true);
        }

        // 提前取消（打断/取消技能时）：短暂淡出后隐藏
        public void TryCancel(float startLogicTime)
        {
            if (!_isActive || !Mathf.Approximately(_startLogicTime, startLogicTime))
            {
                return;
            }

            _isCancelling = true;
            _cancelFadeTimer = 0.25f;

            if (hintTextComp != null)
            {
                hintTextComp.text = "Cancel";
            }
        }

        // 立即隐藏，不淡出
        public void ForceHide(float startLogicTime)
        {
            if (!Mathf.Approximately(_startLogicTime, startLogicTime))
            {
                return;
            }

            _isActive = false;
            _isCancelling = false;
            gameObject.SetActive(false);
        }
    }
}
