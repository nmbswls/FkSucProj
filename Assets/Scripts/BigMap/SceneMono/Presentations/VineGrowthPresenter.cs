using Config.Map;
using Map.Entity;
using My.Map.Entity;
using My.Map.View;
using System.Collections.Generic;
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
        bool _visualGrown;

        public override void Bind(ILogicEntity logic)
        {
            base.Bind(logic);
            RealLogic.EventOnSelfAnim += OnSelfAnim;
            if (lineView != null)
            {
                lineView.ProgressChanged += OnVineProgressChanged;
            }

            ApplyGrowLengthFromLogic();
            ApplyStatusSnapshot(RealLogic.CurrStatusId);
        }

        public override void Unbind()
        {
            if (RealLogic != null)
            {
                RealLogic.EventOnSelfAnim -= OnSelfAnim;
            }

            if (lineView != null)
            {
                lineView.ProgressChanged -= OnVineProgressChanged;
            }

            _growPlaying = false;
            _visualGrown = false;
            lineView?.KillActiveTween();
            base.Unbind();
        }

        public override bool CanInteractEnable()
        {
            if (_growPlaying)
            {
                return false;
            }

            return base.CanInteractEnable();
        }

        public override List<SceneInteractSelection> GetInteractSelections()
        {
            if (_growPlaying)
            {
                return new List<SceneInteractSelection>();
            }

            return base.GetInteractSelections();
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
            IsSwitching = false;

            if (changeView != null && changeView.ChangingAnimName == GrowAnimName && changeView.ChangingDuration > 0f)
            {
                PlayGrowSequence(changeView.ChangingDuration);
                return;
            }

            if (_growPlaying)
            {
                if (IsLogicGrown(RealLogic.CurrStatusId))
                {
                    ApplyInteractionState(RealLogic.CurrStatusId);
                }

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
            _visualGrown = false;
            lineView.PlayGrow(duration, OnGrowComplete);
        }

        void OnGrowComplete()
        {
            _growPlaying = false;
            _visualGrown = true;
            lineView?.SetInstantFull();
            UpdateTopAnchor();

            int statusId = RealLogic != null ? RealLogic.CurrStatusId : 0;
            ApplyInteractionState(statusId != 0 ? statusId : 1);
        }

        void OnVineProgressChanged()
        {
            if (_growPlaying || _visualGrown || ShouldShowVine(RealLogic != null ? RealLogic.CurrStatusId : 0))
            {
                UpdateTopAnchor();
            }
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

        bool IsLogicGrown(int statusId) => statusId != 0;

        bool ShouldShowVine(int statusId) => IsLogicGrown(statusId) || _visualGrown || _growPlaying;

        void ApplyStatusSnapshot(int statusId)
        {
            if (IsLogicGrown(statusId))
            {
                _visualGrown = true;
            }

            bool showVine = ShouldShowVine(statusId);
            ApplyInteractionState(statusId);

            if (lineView == null)
            {
                return;
            }

            if (showVine)
            {
                lineView.SetInstantFull();
                UpdateTopAnchor();
            }
            else if (!_growPlaying)
            {
                _visualGrown = false;
                lineView.SetHidden();
            }
        }

        void ApplyInteractionState(int statusId)
        {
            bool grown = IsLogicGrown(statusId) || _visualGrown;

            // seedVisual 若指向包含 vine_line 的 view，不能整体隐藏，否则藤蔓和攀爬点一起消失
            if (seedVisual != null && !ContainsLineView(seedVisual))
            {
                seedVisual.SetActive(!grown);
            }

            if (climbTrigger != null)
            {
                climbTrigger.enabled = grown;
            }
        }

        bool ContainsLineView(GameObject root)
        {
            return lineView != null && lineView.transform.IsChildOf(root.transform);
        }
    }
}
