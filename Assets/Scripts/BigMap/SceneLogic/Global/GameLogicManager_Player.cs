using My.Map;
using My.Player;

namespace My
{
    public partial class GameLogicManager
    {
        // 联机扩展时在此维护 playerId -> PlayerSystemManager / PlayerLogicEntity 映射
        public PlayerSystemManager GetPlayerSystem(int playerId)
        {
            return playerId == GamePlayerIds.Local ? playerDataManager : null;
        }

        public PlayerLogicEntity GetPlayerEntity(int playerId)
        {
            return playerId == GamePlayerIds.Local ? playerLogicEntity : null;
        }

        public int LocalPlayerId => GamePlayerIds.Local;

        public PlayerSystemManager LocalPlayerSystem => playerDataManager;

        public PlayerLogicEntity LocalPlayerEntity => playerLogicEntity;
    }
}
