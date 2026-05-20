using System.Collections.Generic;
using cfg.demo;
using My.Map.Logic;
using My.Map.SavePoint;

namespace My.Player
{
    public partial class PlayerSystemManager
    {
        public bool IsSavePointUnlocked(string savePointId) =>
            SavePointUnlockHelper.IsFormallyUnlocked(logicManager, savePointId);

        public bool CanShowSavePoint(string savePointId) =>
            SavePointUnlockHelper.CanShowAndInteract(logicManager, savePointId);

        public List<SavePoint> GetUnlockedSavePointsForTeleport() =>
            SavePointUnlockHelper.GetFormallyUnlockedConfigs(logicManager);
    }
}
