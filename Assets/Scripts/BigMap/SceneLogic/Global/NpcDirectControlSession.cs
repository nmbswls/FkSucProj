using System.Collections.Generic;
using My.Map;
using My.Map.Entity;
using My.Player;

namespace My
{
    public class NpcDirectControlSession
    {
        static readonly string[] DefaultPlayerBuffIds =
        {
            "lock_move",
            "phase_move",
            "super_armor",
        };

        public bool Active { get; private set; }
        public int ControllingPlayerId { get; private set; }
        public long ControlledNpcId { get; private set; }
        public string SourceSkillId { get; private set; }

        readonly List<long> _playerBuffInstIds = new();
        GameLogicManager _owner;

        public bool TryEnter(GameLogicManager glm, NpcUnitLogicEntity npc, int playerId, string sourceSkillId = null)
        {
            if (glm == null || npc == null || Active)
            {
                return false;
            }

            if (npc is not INpcDirectControlTarget target || !target.CanAcceptDirectControl(playerId))
            {
                return false;
            }

            _owner = glm;
            ControllingPlayerId = playerId;
            ControlledNpcId = npc.Id;
            SourceSkillId = sourceSkillId;
            Active = true;

            ApplySessionBuffs(glm, playerId);
            target.OnDirectControlBegin(playerId);

            glm.EventOnLogicEntityDespawned += OnEntityDespawned;
            glm.EventOnHardAreaClearStarting += OnAreaClearStarting;
            return true;
        }

        public void Exit()
        {
            if (!Active || _owner == null)
            {
                return;
            }

            var glm = _owner;
            glm.EventOnLogicEntityDespawned -= OnEntityDespawned;
            glm.EventOnHardAreaClearStarting -= OnAreaClearStarting;

            var npc = GetControlledNpc(glm);
            if (npc is INpcDirectControlTarget target)
            {
                target.OnDirectControlEnd(ControllingPlayerId);
            }

            ClearSessionBuffs(glm);
            ResetState();
        }

        public NpcUnitLogicEntity GetControlledNpc(GameLogicManager glm)
        {
            if (!Active || glm == null || ControlledNpcId == 0)
            {
                return null;
            }

            return glm.AreaManager.GetLogicEntiy(ControlledNpcId, false) as NpcUnitLogicEntity;
        }

        void ApplySessionBuffs(GameLogicManager glm, int playerId)
        {
            var player = glm.GetPlayerEntity(playerId);
            if (player == null)
            {
                return;
            }

            _playerBuffInstIds.Clear();
            foreach (var buffId in DefaultPlayerBuffIds)
            {
                long instId = glm.globalBuffManager.AddBuff(player.Id, buffId);
                if (instId != 0)
                {
                    _playerBuffInstIds.Add(instId);
                }
            }
        }

        void ClearSessionBuffs(GameLogicManager glm)
        {
            var player = glm.GetPlayerEntity(ControllingPlayerId);
            if (player != null)
            {
                foreach (var instId in _playerBuffInstIds)
                {
                    glm.globalBuffManager.RequestRemoveBuff(player, instId);
                }
            }

            _playerBuffInstIds.Clear();
        }

        void ResetState()
        {
            Active = false;
            ControllingPlayerId = GamePlayerIds.Local;
            ControlledNpcId = 0;
            SourceSkillId = null;
            _owner = null;
        }

        void OnEntityDespawned(ILogicEntity entity)
        {
            if (!Active || entity == null)
            {
                return;
            }

            if (entity.Id == ControlledNpcId)
            {
                Exit();
                return;
            }

            if (entity.Id == _owner?.GetPlayerEntity(ControllingPlayerId)?.Id)
            {
                Exit();
            }
        }

        void OnAreaClearStarting()
        {
            if (Active)
            {
                Exit();
            }
        }
    }
}
