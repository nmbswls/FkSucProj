using My.Map.Entity;
using My.Map.Fight;
using UnityEngine;
using static My.Map.Fight.FightStruct;

namespace My.Map.Scene
{
    // 欲望等级>0：与「对玩家有敌意」单位贴近时概率剐蹭 — 短硬直位移、快感、CD、粉粒子 + 闪白
    public partial class PlayerScenePresenter
    {
        const float DesireGrazeCooldownSec = 2.2f;
        // 持续贴靠时：仅限制「触发成功后的 CD」会在 CD 结束瞬间几乎必中；失败后拉长抽距，避免每帧连抽
        const float DesireGrazeMinRollIntervalSec = 0.28f;
        const float DesireGrazeTriggerChance = 0.32f;
        const long DesireGrazePleasureAdd = 3200;
        const float DesireGrazeKnockImpulse = 3.4f;
        const float DesireGrazeOverlapRadius = 0.72f;

        const string DesireGrazeEffectName = "scene_h_collide";

        float _nextDesireGrazeProcAllowedTime;
        float _nextDesireGrazeRollAllowedTime;
        readonly Collider2D[] _desireGrazeHits = new Collider2D[24];

        void TickDesireBodyGrazing(float dt)
        {
            if (PlayerEntity == null || PlayerEntity.DesireLevel == 0)
            {
                return;
            }

            if (PlayerEntity.IsDead || PlayerEntity.MarkDestroyed)
            {
                return;
            }

            // 无逻辑 / 敏感内容关闭 / 附着 H /剧情接管 / 冲刺击退牵引等强制位移 / 硬控 — 不做剐蹭，避免误判与叠速度
            if (PlayerEntity.MarkNoLogic || PlayerEntity.MarkUnsensored)
            {
                return;
            }

            if (PlayerEntity.IsAttaching)
            {
                return;
            }

            if (PlayerEntity.DialogControlled || PlayerEntity.IsDialogMoving)
            {
                return;
            }

            if (PlayerEntity.controlledMoveCtx != null)
            {
                return;
            }

            if (PlayerEntity.CheckHasState(AttrIdConsts.Unmovable) || PlayerEntity.CheckHasState(AttrIdConsts.Stun))
            {
                return;
            }

            if (LogicTime.time < _nextDesireGrazeProcAllowedTime)
            {
                return;
            }

            int layerMask = 1 << LayerMask.NameToLayer("MapTarget");
            int count = Physics2D.OverlapCircleNonAlloc(
                transform.position,
                DesireGrazeOverlapRadius,
                _desireGrazeHits,
                layerMask);

            bool hitHostile = false;
            Vector2 awaySum = Vector2.zero;
            int awayCnt = 0;

            for (int i = 0; i < count; i++)
            {
                var col = _desireGrazeHits[i];
                if (col == null)
                {
                    continue;
                }

                var pres = col.GetComponentInParent<IScenePresentation>();
                if (pres == null)
                {
                    continue;
                }

                var logic = pres.GetLogicEntity();
                if (logic is not BaseUnitLogicEntity other || other.IsDead)
                {
                    continue;
                }

                if (other.Id == PlayerEntity.Id)
                {
                    continue;
                }

                if (!other.IsEnmityWith(PlayerEntity))
                {
                    continue;
                }

                hitHostile = true;
                Vector2 d = PlayerEntity.Pos - other.Pos;
                if (d.sqrMagnitude > 1e-5f)
                {
                    awaySum += d.normalized;
                    awayCnt++;
                }
            }

            if (!hitHostile)
            {
                return;
            }

            if (LogicTime.time < _nextDesireGrazeRollAllowedTime)
            {
                return;
            }

            if (Random.value > DesireGrazeTriggerChance)
            {
                _nextDesireGrazeRollAllowedTime = LogicTime.time + DesireGrazeMinRollIntervalSec;
                //return;
            }

            _nextDesireGrazeProcAllowedTime = LogicTime.time + DesireGrazeCooldownSec;
            _nextDesireGrazeRollAllowedTime = _nextDesireGrazeProcAllowedTime;

            Vector2 push = awayCnt > 0 ? (awaySum / awayCnt).normalized : Random.insideUnitCircle.normalized;
            if (push.sqrMagnitude < 1e-4f)
            {
                push = Random.insideUnitCircle.normalized;
            }

            PlayerEntity.externalVel += push * DesireGrazeKnockImpulse;

            PlayerEntity.ApplyResourceChange(AttrIdConsts.PlayerPleasure, DesireGrazePleasureAdd, false, EDmgFlag.None, null);

            if (MapSceneEffectManager.Instance != null)
            {
                MapSceneEffectManager.Instance.ShowSceneEffect(PlayerEntity.Pos, 0.55f, DesireGrazeEffectName, PlayerEntity.Id);
            }

            if (MainFlasher != null)
            {
                MainFlasher.TriggerPinkBodyGrazingFlash();
            }
        }
    }
}
