
using System.Collections.Generic;
using My.Player;
using My.Saving;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;

namespace My.Player
{


    /// <summary>
    /// 系统
    /// </summary>
    public class PlayerProgressionSystem : IPlayerSystem
    {

        protected GameLogicManager LogicManager { get; private set; }

        public PlayerMain BaseStats { get; private set; } 
        public PlayerGearManager GearManager;
        public PlayerTalentManager TalentManager;

        public ProgressionAggregator ProgressionRoot { get; private set; }

        

        //public LevelProgression LevelData { get; private set; }

        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            this.LogicManager = ctx;

            TalentManager = new();
            TalentManager.Initialize(savingData);

            GearManager = new();
            GearManager.Initialize(savingData);

            BaseStats = new();
            BaseStats.Initialize(savingData);

            ProgressionRoot = new("Root");
            ProgressionRoot.AddChild(BaseStats.MainAggregator);
            ProgressionRoot.AddChild(GearManager.GearAggregator);
            ProgressionRoot.AddChild(TalentManager.TalentAggregator);

            ProgressionRoot.OnStatsChanged += (src) => {
                RefreshPlayerBigMapAttr();
            };
        }


        public void Tick(float dt)
        {

        }

        private Dictionary<int, float> _lastKnownValues = new Dictionary<int, float>();
        public void RefreshPlayerBigMapAttr()
        {
            //TotalStats.GetFinalAttribute(StatID.Attack);

            // 1. 获取最新的养成数据 (触发 RebuildCache)
            StatMap currentStats = ProgressionRoot.GetRawCache();

            // 2. 遍历养成系统的所有生效属性
            foreach (var kvp in currentStats)
            {
                int statId = kvp.Key;
                float newValue = kvp.Value;

                // --- 优化点：值比对 ---
                // 只有当值真的变了，才通知战斗系统
                // 这一步能拦截掉 99% 的“无效更新”（比如其他属性变了，但攻击力没变）
                if (_lastKnownValues.TryGetValue(statId, out float oldValue))
                {
                    if (Mathf.Approximately(oldValue, newValue)) continue;
                }

                // 3. 应用变更
                //_combatEntity.SetAttribute(statId, newValue);
                _lastKnownValues[statId] = newValue; // 更新本地缓存
            }
        }

        // 获取战斗属性 (高频调用)
        public float GetFinalAttribute(int id)
        {
            return ProgressionRoot.GetValue(id);
        }


        #region 监听

        public void OnPlayerKillUnit()
        {
            
        }

        #endregion


    }


    public class PlayerMain
    {
        public ProgressionAggregator MainAggregator;

        private BasicProgressionProvider BasicProvider;
        private LevelProgressionProvider LevelProvider;

        public void Initialize(SaveData savingData = null)
        {
            MainAggregator = new("Main");

            BasicProvider = new();
            LevelProvider = new();

            if(savingData != null)
            {
                LevelProvider.SetLevel(savingData.PlayerData.Level);
            }

            MainAggregator.AddChild(BasicProvider);
            MainAggregator.AddChild(LevelProvider);
        }


        public void OnLevelUpdate(int newLevel)
        {
            LevelProvider.SetLevel(newLevel);
        }
    }


    public class PlayerGearManager
    {
        public ProgressionAggregator GearAggregator;

        /// <summary>
        /// 装备映射
        /// </summary>
        public Dictionary<int, PlayerGear> Slot2Gears = new();

        public void Initialize(SaveData savingData = null)
        {
            GearAggregator = new("GearTotal");
        }
    }

    public class PlayerTalentManager
    {
        public ProgressionAggregator TalentAggregator;

        public Dictionary<int, PlayerTalentNode> TalentNodeDict = new();

        public void Initialize(SaveData savingData = null)
        {
            TalentAggregator = new("TalentTotal");
        }
    }

    public class PlayerTalentNode
    {
        public TalentNodeProgressionProvider Provider;

        public PlayerTalentNode()
        {
            Provider = new();
            OnInfoRefresh();
        }

        public void OnInfoRefresh()
        {

        }
    }



}

