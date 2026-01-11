
using System.Collections.Generic;
using My.Saving;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;

namespace My
{


    /// <summary>
    /// 系统
    /// </summary>
    public class PlayerProgressionSystem
    {

        protected GameLogicManager LogicManager { get; private set; }

        public PlayerGearManager GearManager;
        public PlayerTalentManager TalentManager;

        public ProgressionAggregator ProgressionRoot { get; private set; }

        //public ProgressionNode BaseStatsModule { get; private set; } // 基础成长(升级/转生)

        //public LevelProgression LevelData { get; private set; }

        public PlayerProgressionSystem(GameLogicManager logicManager)
        {
            this.LogicManager = LogicManager;
        }

        public void InitializeSystem(SaveData savingData = null)
        {
            TalentManager = new();
            TalentManager.Initialize();

            GearManager = new();
            GearManager.Initialize();

            ProgressionRoot = new("Root");
            ProgressionRoot.AddChild(TalentManager.TalentAggregator);
            ProgressionRoot.AddChild(GearManager.GearAggregator);

            ProgressionRoot.OnStatsChanged += (src) => {
                RefreshPlayerBigMapAttr();
            };
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

    }


    public class PlayerBasicGrowth
    {
        public ProgressionAggregator BasicAggregator;

        public void Initialize(SaveData savingData = null)
        {
            BasicAggregator = new("BasicTotal");
        }


        public void LevelUp(int newLevel)
        {
            //LevelData.SetLevel(newLevel);
            // 此时 BaseStatsModule 脏了 -> TotalStats 脏了
            // 下次 GetValue 时会自动重算
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

