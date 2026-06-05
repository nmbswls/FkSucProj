using My.Map.Entity;
using My.Player;

namespace My.Map
{
    public partial class NpcUnitLogicEntity : INpcDirectControlTarget
    {
        public bool AiBrainSuspended { get; private set; }

        public bool CanAcceptDirectControl(int playerId)
        {
            if (playerId != GamePlayerIds.Local)
            {
                return false;
            }

            if (IsDead || MarkDestroyed || MarkUnsensored || IsAttaching)
            {
                return false;
            }

            if (CheckHasState(AttrIdConsts.NoSelect))
            {
                return false;
            }

            return true;
        }

        public void OnDirectControlBegin(int playerId)
        {
            AiBrainSuspended = true;
            FreeMoveInput = UnityEngine.Vector2.zero;

            AggroSystem?.ClearTarget(0f);
            if (AIBrain != null
                && (AIBrain.CurrentState == AIBrain.StateCombat || AIBrain.CurrentState == AIBrain.StateFlee))
            {
                AIBrain.ChangeState(AIBrain.StateIdle);
            }
        }

        public void OnDirectControlEnd(int playerId)
        {
            AiBrainSuspended = false;
            FreeMoveInput = UnityEngine.Vector2.zero;
        }
    }
}
