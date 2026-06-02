using Config.Map;
using Map.Entity;
using My.Map.Entity;
using My.Map.View;
using System.Globalization;
using UnityEngine;
using static Config.Map.MapInteractPointConfig;

namespace My.Map.Scene
{
    public class VineGrowthPresenter : InteractPointPresenter
    {
        const string GrowAnimName = "grow";

        [SerializeField] VineGrowthLineView lineView;
        [SerializeField] GameObject seedVisual;
        [SerializeField] Transform topAnchor;
        [SerializeField] Collider2D climbTrigger;

        bool _growPlaying;

        public override void Bind(ILogicEntity logic)
        {
            base.Bind(logic);
            RealLogic.EventOnSelfAnim += OnSelfAnim;
            ApplyGrowLengthFromLogic();
            ApplyStatusSnapshot(RealLogic.CurrStatusId);
        }

        public override void Unbind()
        {
            if (RealLogic != null)
            {
                RealLogic.EventOnSelfAnim -= OnSelfAnim;
            }

            _growPlaying = false;
            lineView?.KillActiveTween();
            base.Unbind();
        }

        void OnSelfAnim(string animName, float durationSec)
        {
            if (_growPlaying || animName != GrowAnimName)
            {
                return;
            }

            PlayGrowSequence(durationSec);
        }

        public override void OnStatusChanged(StateChangeView changeView)
        {
            base.OnStatusChanged(changeView);

            if (_growPlaying)
            {
                return;
            }

            if (changeView != null && changeView.ChangingAnimName == GrowAnimName && changeView.ChangingDuration > 0f)
            {
                PlayGrowSequence(changeView.ChangingDuration);
                return;
            }

            ApplyStatusSnapshot(RealLogic.CurrStatusId);
        }

        void PlayGrowSequence(float duration)
        {
            if (lineView == null)
            {
                return;
            }

            _growPlaying = true;
            IsSwitching = true;
            switchingTimer = duration;

            if (seedVisual != null)
            {
                seedVisual.SetActive(true);
            }

            lineView.PlayGrow(duration, OnGrowComplete);
        }

        void OnGrowComplete()
        {
            _growPlaying = false;
            UpdateTopAnchor();
            ApplyStatusSnapshot(1);
        }

        void ApplyGrowLengthFromLogic()
        {
            float length = VineGrowthDefs.DefaultGrowLength;
            if (RealLogic != null)
            {
                var raw = RealLogic.GetRuntimeVariable(VineGrowthDefs.GrowLengthKey);
                if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) && parsed > 0f)
                {
                    length = parsed;
                }
            }

            lineView?.Configure(length);
            UpdateTopAnchor();
        }

        void UpdateTopAnchor()
        {
            if (topAnchor == null || lineView == null)
            {
                return;
            }

            topAnchor.position = lineView.GetTopWorldPosition();
        }

        void ApplyStatusSnapshot(int statusId)
        {
            bool grown = statusId != 0;
            if (seedVisual != null)
            {
                seedVisual.SetActive(!grown);
            }

            if (climbTrigger != null)
            {
                climbTrigger.enabled = grown;
            }

            if (lineView == null)
            {
                return;
            }

            if (grown)
            {
                UpdateTopAnchor();
                lineView.SetInstantFull();
            }
            else
            {
                lineView.SetHidden();
            }
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            if (!IsSwitching)
            {
                return;
            }

            switchingTimer -= dt;
            if (switchingTimer <= 0f)
            {
                IsSwitching = false;
            }

            if (!_growPlaying)
            {
                MainGameManager.Instance.ShowFakeFxEffect("switching", transform.position);
            }
        }
    }
}
