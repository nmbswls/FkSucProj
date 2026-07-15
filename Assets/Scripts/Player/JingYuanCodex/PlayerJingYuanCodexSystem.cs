using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Quest;
using My.Saving;
using UnityEngine;

namespace My.Player
{
    public enum EJingYuanProgressSource
    {
        Blurt,
        ItemGrant,
    }

    public sealed class JingYuanCodexRuntimeState
    {
        public string CodexId;
        public int ExtractCount;
        public long TotalAmount;
        public int Level;
    }

    public sealed class PlayerJingYuanCodexSystem : IPlayerSystem
    {
        const string LogTag = "[PlayerJingYuanCodexSystem]";
        readonly Dictionary<string, JingYuanCodexRuntimeState> _progress = new(StringComparer.Ordinal);

        GameLogicManager _logic;
        public PlayerJingYuanCodexSystem(PlayerSystemManager owner)
        {
        }

        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            _logic = ctx;
            _progress.Clear();

            if (savingData?.PlayerData?.JingYuanCodexProgress != null)
            {
                foreach (var entry in savingData.PlayerData.JingYuanCodexProgress)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.CodexId))
                    {
                        continue;
                    }

                    var state = GetOrCreateState(entry.CodexId);
                    state.ExtractCount = Math.Max(0, entry.ExtractCount);
                    state.TotalAmount = Math.Max(0, entry.TotalAmount);
                    state.Level = JingYuanCodexCatalog.ResolveLevel(entry.CodexId, state.ExtractCount, state.TotalAmount);
                }
            }

        }

        public void PostInit(PlayerSystemManager owner)
        {
        }

        public void Tick(float dt)
        {
        }

        public void WriteToSave(PlayerData pd)
        {
            if (pd == null)
            {
                return;
            }

            pd.JingYuanCodexProgress ??= new List<JingYuanCodexProgressPersist>();
            pd.JingYuanCodexProgress.Clear();
            foreach (var kv in _progress)
            {
                var state = kv.Value;
                if (state == null || string.IsNullOrEmpty(state.CodexId))
                {
                    continue;
                }

                if (state.ExtractCount <= 0 && state.TotalAmount <= 0)
                {
                    continue;
                }

                pd.JingYuanCodexProgress.Add(new JingYuanCodexProgressPersist
                {
                    CodexId = state.CodexId,
                    ExtractCount = state.ExtractCount,
                    TotalAmount = state.TotalAmount,
                });
            }

        }

        public bool TryGetProgress(string codexId, out JingYuanCodexRuntimeState state)
        {
            state = null;
            if (string.IsNullOrEmpty(codexId))
            {
                return false;
            }

            return _progress.TryGetValue(codexId, out state);
        }

        void HandleBlurtAbsorbed(string jingyuanTag, float sjAmount)
        {
            if (string.IsNullOrEmpty(jingyuanTag) || sjAmount <= 0f)
            {
                return;
            }

            var defs = JingYuanCodexCatalog.GetDefsByTag(jingyuanTag);
            if (defs.Count == 0)
            {
                return;
            }

            var amountDelta = (long)Mathf.Max(0f, sjAmount * 1000f);
            foreach (var def in defs)
            {
                if (def == null)
                {
                    continue;
                }

                AddProgress(def.CodexId, 1, amountDelta, EJingYuanProgressSource.Blurt);
            }

        }

        public bool AddProgress(string codexId, int extractDelta, long amountDelta, EJingYuanProgressSource source)
        {
            if (JingYuanCodexCatalog.GetDef(codexId) == null)
            {
                Debug.LogWarning($"{LogTag} AddProgress failed: invalid codex '{codexId}'.");
                return false;
            }

            if (extractDelta <= 0 && amountDelta <= 0)
            {
                return false;
            }

            var state = GetOrCreateState(codexId);
            var oldLevel = state.Level;
            if (extractDelta > 0)
            {
                state.ExtractCount += extractDelta;
            }

            if (amountDelta > 0)
            {
                state.TotalAmount += amountDelta;
            }

            state.Level = JingYuanCodexCatalog.ResolveLevel(codexId, state.ExtractCount, state.TotalAmount);

            PlayerEventBus.Publish(new PlayerJingYuanCodexProgressEvent
            {
                CodexId = codexId,
                ExtractCount = state.ExtractCount,
                TotalAmount = state.TotalAmount,
                Level = state.Level,
                Source = source,
            });

            if (state.Level > oldLevel)
            {
                PlayerEventBus.Publish(new PlayerJingYuanCodexLevelUpEvent
                {
                    CodexId = codexId,
                    OldLevel = oldLevel,
                    NewLevel = state.Level,
                });
            }

            return true;
        }


        JingYuanCodexRuntimeState GetOrCreateState(string codexId)
        {
            if (!_progress.TryGetValue(codexId, out var state))
            {
                state = new JingYuanCodexRuntimeState { CodexId = codexId };
                _progress[codexId] = state;
            }

            return state;
        }

    }
}
