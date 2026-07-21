using cfg.demo;
using UnityEngine;

namespace My.Player
{
    public enum EHInteractionSource
    {
        None = 0,
        CombatSkill = 1,
        StaticAbsorb = 2,
        CloseupKaiYou = 3,
        CloseupKnockdown = 4,
        CloseupHTangle = 5,
    }

    // 当前 H 互动会话：fcked 只表示“可内射吸收”，细节放这里
    public struct HInteractionSnapshot
    {
        public bool IsActive;
        public long PartnerEntityId;
        public EBodyPart ContactPart;
        public int SourceActId;
        public EHInteractionSource Source;
        public float ExpireAt;
    }

    public sealed class HInteractionTracker
    {
        public const float DefaultHoldSeconds = 8f;

        HInteractionSnapshot _current;

        public HInteractionSnapshot Current => _current;
        public bool HasActive => _current.IsActive && (_current.ExpireAt <= 0f || Time.time <= _current.ExpireAt);

        public void Begin(
            long partnerEntityId,
            EBodyPart contactPart,
            EHInteractionSource source,
            int actId = 0,
            float holdSeconds = DefaultHoldSeconds)
        {
            if (partnerEntityId == 0)
            {
                Clear();
                return;
            }

            _current = new HInteractionSnapshot
            {
                IsActive = true,
                PartnerEntityId = partnerEntityId,
                ContactPart = contactPart,
                SourceActId = actId,
                Source = source,
                ExpireAt = holdSeconds > 0f ? Time.time + holdSeconds : 0f,
            };
        }

        // HAct 结算推进射精条时刷新会话（保留接触部位）
        public void NoteActSettlement(long partnerEntityId, int actId, float holdSeconds = DefaultHoldSeconds)
        {
            if (partnerEntityId == 0 || actId <= 0)
            {
                return;
            }

            if (!HasActive || _current.PartnerEntityId != partnerEntityId)
            {
                Begin(partnerEntityId, InferDefaultContactPart(actId), EHInteractionSource.CombatSkill, actId, holdSeconds);
                return;
            }

            _current.SourceActId = actId;
            if (holdSeconds > 0f)
            {
                _current.ExpireAt = Time.time + holdSeconds;
            }
        }

        public void SetContactPart(EBodyPart contactPart)
        {
            if (!_current.IsActive)
            {
                return;
            }

            _current.ContactPart = contactPart;
        }

        public void Clear()
        {
            _current = default;
        }

        public void ClearIfPartner(long partnerEntityId)
        {
            if (_current.IsActive && _current.PartnerEntityId == partnerEntityId)
            {
                Clear();
            }
        }

        public bool TryGetForPartner(long partnerEntityId, out HInteractionSnapshot snapshot)
        {
            snapshot = default;
            if (!HasActive || _current.PartnerEntityId != partnerEntityId)
            {
                return false;
            }

            snapshot = _current;
            return true;
        }

        // 无显式部位时：优先从 HAct 描述推断接触/承精部位，再回退 filter 池
        public static EBodyPart InferDefaultContactPart(int actId)
        {
            var act = My.Config.CfgMgr.Cfgs?.TbHActInfo?.GetOrDefault(actId);
            if (act == null)
            {
                return EBodyPart.Womb;
            }

            var fromDesc = InferContactPartFromDesc(act.Desc);
            if (fromDesc != EBodyPart.None)
            {
                return fromDesc;
            }

            if (act.FilterTypes != null)
            {
                for (var i = 0; i < act.FilterTypes.Count; i++)
                {
                    var tag = act.FilterTypes[i];
                    if (tag == "KaiYou")
                    {
                        return EBodyPart.Breast;
                    }

                    if (tag == "Unsensor" || tag == "Charmed" || tag == "KnockDown")
                    {
                        return EBodyPart.Womb;
                    }
                }
            }

            return EBodyPart.Womb;
        }

        // 按描述关键词推断承精/主接触部位（内射读榨取用，不是演出分镜）
        static EBodyPart InferContactPartFromDesc(string desc)
        {
            if (string.IsNullOrEmpty(desc))
            {
                return EBodyPart.None;
            }

            // 口系优先于泛插入（如「手口」「口爆」「口乳」）
            if (desc.Contains("口") || desc.Contains("吻") || desc.Contains("69") || desc.Contains("坐脸"))
            {
                return EBodyPart.Mouth;
            }

            if (desc.Contains("乳") || desc.Contains("胸"))
            {
                return EBodyPart.Breast;
            }

            if (desc.Contains("尻") || desc.Contains("肛") || desc.Contains("屁股"))
            {
                return EBodyPart.Tail;
            }

            if (desc.Contains("入") || desc.Contains("插入") || desc.Contains("骑乘") ||
                desc.Contains("穴") || desc.Contains("传教士") || desc.Contains("体位"))
            {
                return EBodyPart.Womb;
            }

            return EBodyPart.None;
        }
    }
}
