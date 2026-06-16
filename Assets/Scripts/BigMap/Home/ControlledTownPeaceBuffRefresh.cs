using My;

namespace My.Home
{
    // 玩家掌控城镇：根据区域控制权为玩家挂/卸 peace buff（具体 buff 配置在 TmpBuffLibrary）
    public static class ControlledTownPeaceBuffRefresh
    {
        public const string BuffId = "homestead_peace";

        static bool _eventsBound;

        public static void BindRefreshEvents(GameLogicManager glm)
        {
            if (_eventsBound || glm?.worldPersistState == null)
            {
                return;
            }

            _eventsBound = true;
            glm.worldPersistState.EvOnLogicAreaHomesteadChanged += (_, __) => Refresh(glm);
        }

        public static void Refresh(GameLogicManager glm)
        {
            if (glm?.playerLogicEntity == null || glm.globalBuffManager == null || glm.logicAreaHomesteadManager == null)
            {
                return;
            }

            var player = glm.playerLogicEntity;
            var logicAreaId = glm.logicAreaHomesteadManager.ResolveCurrentLogicAreaId();
            bool shouldHave = !string.IsNullOrEmpty(logicAreaId)
                && glm.logicAreaHomesteadManager.IsAreaUnderPlayerControl(logicAreaId);
            bool has = glm.globalBuffManager.CheckHasBuff(player.Id, BuffId);

            if (shouldHave && !has)
            {
                glm.globalBuffManager.RequestAddBuff(player.Id, BuffId);
            }
            else if (!shouldHave && has)
            {
                glm.globalBuffManager.RemoveAllBuffById(player.Id, BuffId);
            }
        }
    }
}
