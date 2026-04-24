

using System;
using System.Collections.Generic;

namespace My.Map
{

    public abstract partial class LogicEntityBase
    {
        public event Action<string, int, bool> EventOnAnimPlay;

        public event EventHandler<AnimLayerRefreshEventArgs> EventOnAnimLayerRefreshed;

        private long _nextAnimHandle = 1;

        private sealed class AnimStackEntry
        {
            public long Handle;
            public string AnimName;
            public int Layer;
            public EAnimRequestSource Source;
            public EAnimReleasePolicy ReleasePolicy;
            public long AbilitySessionId;
            public int AbilityPhaseIndex;
        }

        private readonly Dictionary<int, List<AnimStackEntry>> _animStacks = new();

        /// <summary>
        /// 获取 动画覆盖
        /// 如；覆盖idle 需要从 这里获取
        /// </summary>
        public string GetAnimOverride(string rawAnimName)
        {
            var buffs = BuffContainer.Values;
            foreach (var b in buffs)
            {
                if(b.Def.DurationEffect == null)
                {
                    continue;
                }
                if(b.Def.DurationEffect.DurationType != Entity.EBuffDurationType.AnimOverride)
                {
                    continue;
                }

                if(b.Def.DurationEffect.ParamStr1 == rawAnimName)
                {
                    return b.Def.DurationEffect.ParamStr2;
                }
            }

            return rawAnimName;
        }

        public long PushAnimRequest(in AnimPlayRequest request)
        {
            var handle = _nextAnimHandle++;
            var entry = new AnimStackEntry
            {
                Handle = handle,
                AnimName = request.AnimName,
                Layer = request.Layer,
                Source = request.Source,
                ReleasePolicy = request.ReleasePolicy,
                AbilitySessionId = request.AbilitySessionId,
                AbilityPhaseIndex = request.AbilityPhaseIndex,
            };

            if (!_animStacks.TryGetValue(request.Layer, out var list))
            {
                list = new List<AnimStackEntry>(4);
                _animStacks[request.Layer] = list;
            }

            list.Add(entry);
            EmitAnimLayerRefresh(request.Layer);
            return handle;
        }

        public bool ReleaseAnimRequest(long handle, EAnimReleaseReason reason = EAnimReleaseReason.Manual)
        {
            if (handle == 0)
            {
                return false;
            }

            foreach (var kv in _animStacks)
            {
                var list = kv.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Handle != handle)
                    {
                        continue;
                    }

                    if (!ShouldReleaseByReason(list[i].ReleasePolicy, reason))
                    {
                        return false;
                    }

                    list.RemoveAt(i);
                    if (list.Count == 0)
                    {
                        _animStacks.Remove(kv.Key);
                    }

                    EmitAnimLayerRefresh(kv.Key);
                    return true;
                }
            }

            return false;
        }

        // 技能系统等：阶段/会话清理时无视 policy，避免配置遗漏导致栈泄漏
        public bool ReleaseAnimRequestForced(long handle)
        {
            if (handle == 0)
            {
                return false;
            }

            foreach (var kv in _animStacks)
            {
                var list = kv.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Handle != handle)
                    {
                        continue;
                    }

                    list.RemoveAt(i);
                    if (list.Count == 0)
                    {
                        _animStacks.Remove(kv.Key);
                    }

                    EmitAnimLayerRefresh(kv.Key);
                    return true;
                }
            }

            return false;
        }

        public void ReleaseAnimRequestsByAbilitySession(long abilitySessionId)
        {
            if (abilitySessionId == 0)
            {
                return;
            }

            var refreshedLayers = new HashSet<int>();
            var layerKeys = new List<int>(_animStacks.Keys);
            foreach (var layer in layerKeys)
            {
                if (!_animStacks.TryGetValue(layer, out var list))
                {
                    continue;
                }

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var e = list[i];
                    if (e.Source != EAnimRequestSource.AbilityPhase || e.AbilitySessionId != abilitySessionId)
                    {
                        continue;
                    }

                    list.RemoveAt(i);
                    refreshedLayers.Add(layer);
                }

                if (list.Count == 0)
                {
                    _animStacks.Remove(layer);
                }
            }

            foreach (var layer in refreshedLayers)
            {
                EmitAnimLayerRefresh(layer);
            }
        }

        public bool TryPeekAnimStackTop(int layer, out AnimStackTopSnapshot top)
        {
            if (_animStacks.TryGetValue(layer, out var list) && list.Count > 0)
            {
                var e = list[^1];
                top = new AnimStackTopSnapshot(e.Handle, e.AnimName, e.Layer, e.Source, e.ReleasePolicy);
                return true;
            }

            top = default;
            return false;
        }

        public void PlayerAnim(string animName, int layer = 0)
        {
            var resolved = GetAnimOverride(animName);
            PushAnimRequest(new AnimPlayRequest
            {
                AnimName = resolved,
                Layer = layer,
                Source = EAnimRequestSource.Manual,
                ReleasePolicy = EAnimReleasePolicy.OnClipEnd,
                AbilitySessionId = 0,
                AbilityPhaseIndex = -1,
            });
        }

        private static bool ShouldReleaseByReason(EAnimReleasePolicy policy, EAnimReleaseReason reason)
        {
            return reason switch
            {
                EAnimReleaseReason.ClipEnded => policy.HasFlag(EAnimReleasePolicy.OnClipEnd),
                EAnimReleaseReason.PhaseCleanup => policy.HasFlag(EAnimReleasePolicy.OnPhaseExit),
                EAnimReleaseReason.AbilityEnded => policy.HasFlag(EAnimReleasePolicy.OnAbilityEnd),
                EAnimReleaseReason.Manual => true,
                _ => true,
            };
        }

        private void EmitAnimLayerRefresh(int layer)
        {
            AnimStackTopSnapshot? top = null;
            if (TryPeekAnimStackTop(layer, out var snap))
            {
                top = snap;
            }

            EventOnAnimLayerRefreshed?.Invoke(this, new AnimLayerRefreshEventArgs(layer, top));
        }
    }
}
