using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using UnityEngine;
using static Config.Map.MapInteractPointConfig;

namespace Config.Map
{
    // 可移除障碍：策划只配「移除前」语义字段，状态 0/1 由逻辑层运行时合成。
    [CreateAssetMenu(menuName = "GP/Config/Entity/RemovableObstacle")]
    [Serializable]
    public class MapRemovableObstacleConfig : ScriptableObject
    {
        public string CfgId;
        public string ShowName;
        public string PrefabName;

        public float NameOffset = -1f;

        [Tooltip("状态 0 时显示的交互文案")]
        public string RemoveInteractLabel = "移除";

        public List<CommonCheckCond> RemoveConds = new();
        public List<InteractCheckCond> RemoveInteractConds = new();

        [Tooltip("移除时额外执行的输出（ChangeSelfStatus 由逻辑自动追加）")]
        public List<LogicInteractOutput> ExtraOutputsOnRemove = new();

        public StateChangeView RemoveChangeView = new();

        [Tooltip("远程条件满足时自动切到已移除态，无需玩家在本点交互")]
        public List<StateChangeRule> UnlockRules = new();

        [Tooltip("需要地图刷新项 UniqName；为 true 时 LocalSwitch 写入 PlayerData 稀疏存档")]
        public bool PersistByUniqName = true;

        [Tooltip("移除后写入的 LocalSwitch 名，用于读档恢复已移除态")]
        public string RemovedLocalSwitch = "removed";
    }
}
