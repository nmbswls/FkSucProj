using cfg.demo;
using UnityEngine;

namespace My.Map
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

    // 单侧短命 H 会话（接收或主动）；挂在 NPC 上，不进存档
    public struct HInteractionSnapshot
    {
        public bool IsActive;
        public EBodyPart ContactPart;
        public int SourceActId;
        public EHInteractionSource Source;
        public float ExpireAt;
    }

    public sealed class HInteractionSlot
    {
        public const float DefaultHoldSeconds = 8f;

        HInteractionSnapshot _current;

        public HInteractionSnapshot Current => _current;
        public bool HasActive => _current.IsActive && (_current.ExpireAt <= 0f || Time.time <= _current.ExpireAt);

        public void Begin(
            EBodyPart contactPart,
            EHInteractionSource source,
            int actId = 0,
            float holdSeconds = DefaultHoldSeconds)
        {
            _current = new HInteractionSnapshot
            {
                IsActive = true,
                ContactPart = contactPart,
                SourceActId = actId,
                Source = source,
                ExpireAt = holdSeconds > 0f ? Time.time + holdSeconds : 0f,
            };
        }

        public void NoteAct(int actId, float holdSeconds = DefaultHoldSeconds)
        {
            if (actId <= 0)
            {
                return;
            }

            if (!HasActive)
            {
                Begin(HActContactPart.InferDefault(actId), EHInteractionSource.CombatSkill, actId, holdSeconds);
                return;
            }

            _current.SourceActId = actId;
            if (holdSeconds > 0f)
            {
                _current.ExpireAt = Time.time + holdSeconds;
            }
        }

        public void NoteAct(int actId, EBodyPart contactPart, EHInteractionSource source, float holdSeconds = DefaultHoldSeconds)
        {
            if (!HasActive || _current.ContactPart == EBodyPart.None)
            {
                Begin(contactPart != EBodyPart.None ? contactPart : HActContactPart.InferDefault(actId), source, actId, holdSeconds);
                return;
            }

            if (contactPart != EBodyPart.None)
            {
                _current.ContactPart = contactPart;
            }

            _current.Source = source;
            NoteAct(actId, holdSeconds);
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

        public bool TryGet(out HInteractionSnapshot snapshot)
        {
            snapshot = default;
            if (!HasActive)
            {
                return false;
            }

            snapshot = _current;
            return true;
        }
    }

    // NPC 双槽：被动接收（推射精/内射）与主动施为（对玩家）
    public sealed class NpcHInteractionState
    {
        public HInteractionSlot Receive { get; } = new();
        public HInteractionSlot Active { get; } = new();
    }

    // 从 HAct 推断玩家接触/承精部位
    public static class HActContactPart
    {
        public static EBodyPart InferDefault(int actId)
        {
            var act = My.Config.CfgMgr.Cfgs?.TbHActInfo?.GetOrDefault(actId);
            if (act == null)
            {
                return EBodyPart.Womb;
            }

            var fromDesc = InferFromDesc(act.Desc);
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

        public static EBodyPart InferFromDesc(string desc)
        {
            if (string.IsNullOrEmpty(desc))
            {
                return EBodyPart.None;
            }

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
