
using System.Collections.Generic;
using My.Saving;
using UnityEditorInternal.Profiling.Memory.Experimental;

namespace My
{


    /// <summary>
    /// 系统
    /// </summary>
    public class PlayerProgressionSystem
    {
        public PlayerGearManager GearManager;
        public PlayerTalentManager TalentManager;

        public ProgressionAggregator TotalStats { get; private set; }

        //public ProgressionNode BaseStatsModule { get; private set; } // 基础成长(升级/转生)

        //public LevelProgression LevelData { get; private set; }

        public PlayerProgressionSystem()
        {


        }

        public void InitializeSystem(SaveData savingData = null)
        {
            TalentManager = new();
            TalentManager.Initialize();

            GearManager = new();
            GearManager.Initialize();

            TotalStats = new("Root");
            TotalStats.AddChild(TalentManager.TalentAggregator);
            TotalStats.AddChild(GearManager.GearAggregator);
        }

    }

    public class PlayerGearManager
    {
        public ProgressionAggregator GearAggregator;

        public Dictionary<int, PlayerTalentNode> TalentNodeDict = new();

        public void Initialize(SaveData savingData = null)
        {

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

