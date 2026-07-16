using My.Map.Entity;
using My.Map;

namespace My.Player.Bag
{
    // 负重：根据上限同步超重 debuff（当前仅作 HUD 提示，无实际惩罚）。
    public sealed class PlayerCarryWeightMonitor
    {
        public const string OverweightBuffId = "player_overweight_debuff";

        bool _overweightActive;

        public void Sync(GameLogicManager logic, PlayerSystemManager player, PlayerInventorySystem inventory)
        {
            if (logic == null || player == null || inventory == null)
            {
                return;
            }

            var playerEntity = logic.playerLogicEntity;
            if (playerEntity == null || playerEntity.IsDead)
            {
                ClearOverweightBuff(logic, playerEntity);
                return;
            }

            long current = PlayerCarryWeightCalculator.CalculateTotalCarryWeight(inventory, player);
            long limit = PlayerCarryWeightCalculator.CalculateCarryWeightLimit(player);
            bool shouldOverweight = limit > 0 && current > limit;

            if (shouldOverweight == _overweightActive)
            {
                return;
            }

            _overweightActive = shouldOverweight;
            if (shouldOverweight)
            {
                logic.globalBuffManager?.RemoveAllBuffById(playerEntity.Id, OverweightBuffId);
                logic.globalBuffManager?.AddBuff(playerEntity.Id, OverweightBuffId, overrideDuration: -1f);
            }
            else
            {
                ClearOverweightBuff(logic, playerEntity);
            }
        }

        public void Clear(GameLogicManager logic)
        {
            _overweightActive = false;
            ClearOverweightBuff(logic, logic?.playerLogicEntity);
        }

        static void ClearOverweightBuff(GameLogicManager logic, BaseUnitLogicEntity playerEntity)
        {
            if (logic?.globalBuffManager == null || playerEntity == null)
            {
                return;
            }

            logic.globalBuffManager.RemoveAllBuffById(playerEntity.Id, OverweightBuffId);
        }
    }
}
