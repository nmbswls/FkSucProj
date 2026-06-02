using System.Collections;
using System.Collections.Generic;
using My;
using My.Map;
using My.Map.Entity;
using UnityEngine;

namespace My.Map.Scene
{
    public partial class PlayerScenePresenter
    {
        private float forbidExitMoveDuration = 1f;

        private bool _inForbidInnerZone;
        private readonly List<ForbidZoneChecker> _forbidCheckerScratch = new(8);

        public enum EForbidPhase
        {
            Idle,
            WaitingDialog,
            Sliding,
        }

        private EForbidPhase _forbidPhase;
        private Coroutine _forbidSlideRoutine;

        // 本次禁区退出对应的区域（用于把目标/落点推出 Inner，避免 valid pos 仍在内圈重叠里）
        private ForbidZoneChecker _forbidExitZone;

        public EForbidPhase ForbidPhase => _forbidPhase;

        private void OnDestroy()
        {
            CancelForbidExitPipeline();
        }

        public void ForbidExit_RunSlideAfterDialog()
        {
            if (_forbidPhase != EForbidPhase.WaitingDialog)
                return;
            StartForbidSlide(forbidExitMoveDuration);
        }

        public override bool CheckCanActiveMove()
        {
            if (_inForbidInnerZone)
                return false;
            return base.CheckCanActiveMove();
        }

        private static bool ForbidZoneCondEnabled(ForbidZoneChecker checker)
        {
            if (checker.EnableCondition == null || checker.EnableCondition.Count == 0)
                return true;
            var glm = MainGameManager.Instance.gameLogicManager;
            foreach (var cond in checker.EnableCondition)
            {
                if (!glm.CheckCommonCond(cond))
                    return false;
            }
            return true;
        }

        private void GatherForbidZonesAt(Vector2 worldPos)
        {
            _forbidCheckerScratch.Clear();
            int zn = Physics2D.OverlapPointNonAlloc(worldPos, zoneTriggerCache, 1 << LayerMask.NameToLayer("Zone"));
            for (int i = 0; i < zn; i++)
            {
                var col = zoneTriggerCache[i];
                if (col == null) continue;
                var checker = col.GetComponentInParent<ForbidZoneChecker>();
                if (checker == null) continue;
                if (!ForbidZoneCondEnabled(checker)) continue;
                if (_forbidCheckerScratch.Contains(checker)) continue;
                _forbidCheckerScratch.Add(checker);
            }
        }

        private static bool InnerContains(ForbidZoneChecker fz, Vector2 world)
        {
            return fz != null && fz.InnerCol != null && ForbidZoneCondEnabled(fz) && fz.InnerCol.OverlapPoint(world);
        }

        // 若点落在内圈碰撞体内，沿 bounds 中心方向外推直到离开（修正 LastValid 在边界内/数值误差）
        private static void PushOutOfInnerIfNeeded(ForbidZoneChecker fz, ref Vector2 world)
        {
            if (!InnerContains(fz, world))
                return;
            var col = fz.InnerCol;
            Vector2 c = col.bounds.center;
            Vector2 dir = world - c;
            if (dir.sqrMagnitude < 1e-8f)
                dir = Vector2.right;
            dir.Normalize();
            const float step = 0.04f;
            for (int i = 0; i < 64; i++)
            {
                world += dir * step;
                if (!col.OverlapPoint(world))
                    return;
            }
        }

        private void SanitizeSlideTarget(ref Vector2 targetWorld)
        {
            if (_forbidExitZone != null)
                PushOutOfInnerIfNeeded(_forbidExitZone, ref targetWorld);
            else
            {
                GatherForbidZonesAt(targetWorld);
                foreach (var fz in _forbidCheckerScratch)
                    PushOutOfInnerIfNeeded(fz, ref targetWorld);
            }
        }

        private void TickForbiddenAreaMove()
        {
            if (_forbidPhase == EForbidPhase.Sliding)
                return;

            Vector2 worldPos = rb != null ? rb.position : (Vector2)transform.position;
            GatherForbidZonesAt(worldPos);

            ForbidZoneChecker innerChecker = null;
            bool inInner = false;
            for (int i = 0; i < _forbidCheckerScratch.Count; i++)
            {
                var fz = _forbidCheckerScratch[i];
                if (fz.InnerCol != null && fz.InnerCol.OverlapPoint(worldPos))
                {
                    inInner = true;
                    innerChecker = fz;
                    break;
                }
            }

            bool inOuter = false;
            if (!inInner)
            {
                for (int i = 0; i < _forbidCheckerScratch.Count; i++)
                {
                    var fz = _forbidCheckerScratch[i];
                    if (fz.OuterCol != null && fz.OuterCol.OverlapPoint(worldPos))
                    {
                        inOuter = true;
                        break;
                    }
                }
            }

            if (!inInner && _forbidPhase == EForbidPhase.WaitingDialog)
                _forbidPhase = EForbidPhase.Idle;

            _inForbidInnerZone = inInner || _forbidPhase != EForbidPhase.Idle;

            if (inInner && LastValidMovePos.HasValue && _forbidPhase == EForbidPhase.Idle)
                BeginForbidExit(innerChecker);

            // 完全离开内外圈才更新合法点，避免在外环内把 LastValid 刷到更靠内的位置
            if (!inInner && _forbidPhase == EForbidPhase.Idle && !inOuter)
                LastValidMovePos = worldPos;
        }

        private void BeginForbidExit(ForbidZoneChecker checker)
        {
            _forbidExitZone = checker;

            string dialogId = checker != null ? checker.EnterInnerDialogId : "default_fobbid_zone";
            bool lockTime = checker != null && checker.DialogLockGlobalTime;

            if (string.IsNullOrEmpty(dialogId))
            {
                StartForbidSlide(forbidExitMoveDuration);
                return;
            }

            _forbidPhase = EForbidPhase.WaitingDialog;
            bool played = MainGameManager.Instance.PlayDialog(dialogId, null, lockTime, ForbidExit_RunSlideAfterDialog);
            if (!played)
                ForbidExit_RunSlideAfterDialog();
        }

        private void ApplyPlayerWorldPos(Vector2 world)
        {
            if (rb != null)
                rb.position = world;
            transform.position = new Vector3(world.x, world.y, transform.position.z);
            PlayerEntity.SetPosition(MainGameManager.Instance.GetLogicPosFromWorldPos(world));
        }

        private void StartForbidSlide(float duration)
        {
            if (!LastValidMovePos.HasValue)
            {
                _forbidPhase = EForbidPhase.Idle;
                _forbidExitZone = null;
                return;
            }

            if (_forbidSlideRoutine != null)
            {
                StopCoroutine(_forbidSlideRoutine);
                _forbidSlideRoutine = null;
            }

            Vector2 target = LastValidMovePos.Value;
            SanitizeSlideTarget(ref target);

            _forbidPhase = EForbidPhase.Sliding;
            CharacterController?.ResetSmoothedMoveVelocity();
            _forbidSlideRoutine = StartCoroutine(CoForbidSlide(target, Mathf.Max(0.05f, duration)));
        }

        private IEnumerator CoForbidSlide(Vector2 targetWorld, float duration)
        {
            Vector2 start = rb != null ? rb.position : (Vector2)transform.position;
            float el = 0f;
            try
            {
                while (el < duration)
                {
                    el += Time.unscaledDeltaTime;
                    float u = Mathf.Clamp01(el / duration);
                    u = u * u * (3f - 2f * u);
                    var p = Vector2.Lerp(start, targetWorld, u);
                    ApplyPlayerWorldPos(p);
                    yield return null;
                }

                Vector2 landed = targetWorld;
                SanitizeSlideTarget(ref landed);
                ApplyPlayerWorldPos(landed);

                // 用实际落点刷新合法点，避免仍记录在内圈重叠内的旧值导致下一帧又判进内圈
                GatherForbidZonesAt(landed);
                bool stillInner = false;
                for (int i = 0; i < _forbidCheckerScratch.Count; i++)
                {
                    var fz = _forbidCheckerScratch[i];
                    if (fz.InnerCol != null && fz.InnerCol.OverlapPoint(landed))
                    {
                        stillInner = true;
                        break;
                    }
                }
                if (!stillInner)
                    LastValidMovePos = landed;
            }
            finally
            {
                _forbidPhase = EForbidPhase.Idle;
                _forbidSlideRoutine = null;
                _forbidExitZone = null;
                CharacterController?.ResetSmoothedMoveVelocity();
            }
        }

        private void CancelForbidExitPipeline()
        {
            if (_forbidSlideRoutine != null)
            {
                StopCoroutine(_forbidSlideRoutine);
                _forbidSlideRoutine = null;
            }
            _forbidPhase = EForbidPhase.Idle;
            _forbidExitZone = null;
        }
    }
}
