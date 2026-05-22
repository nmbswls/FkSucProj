using System.Collections.Generic;
using cfg.demo;
using My.Map;

namespace My.Player
{
    public partial class PlayerSystemManager
    {
        public bool IsSavePointActivated(string savePointId) =>
            SavePointUnlockHelper.IsActivated(logicManager, savePointId);

        public bool ShouldShowSavePointOnMap(string savePointId) =>
            SavePointUnlockHelper.ShouldShowOnMap(logicManager, savePointId);

        public List<SavePoint> GetActivatedSavePointsForTeleport() =>
            SavePointUnlockHelper.GetActivatedSavePointConfigs(logicManager);
    }
}
