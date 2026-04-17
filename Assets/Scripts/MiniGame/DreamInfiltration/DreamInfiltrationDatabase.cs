using System;
using System.Collections.Generic;
using cfg.demo;
using UnityEngine;

namespace My.MiniGame.Dream
{
    public enum DreamTendencyKind
    {
        Force = 0,
        Soothing = 1,
        Trick = 2,
    }

    [Serializable]
    public class DreamUnlockCondRow
    {
        public ECommonCheckType Type = ECommonCheckType.None;
        public long Param1;
        public long Param2;
        public long Param3;
        public long Param4;
        public string Param5 = "";
        public string Param6 = "";
    }

    [Serializable]
    public class DreamThemeWeight
    {
        public string ThemeId = "default";
        public string ThemeDisplayName = "浅梦";
        [Min(1)] public int Weight = 10;
    }

    [Serializable]
    public class DreamEntrySpotDef
    {
        public string SpotId = "spot_a";
        public string DisplayName = "入口";
        [Tooltip("在背景图上的归一化坐标 0~1")]
        public Vector2 Anchor01 = new Vector2(0.25f, 0.55f);
        public List<DreamUnlockCondRow> UnlockConds = new();
        public List<DreamThemeWeight> ThemeWeights = new()
        {
            new DreamThemeWeight { ThemeId = "ruins", ThemeDisplayName = "废墟回响", Weight = 10 },
            new DreamThemeWeight { ThemeId = "garden", ThemeDisplayName = "花园低语", Weight = 10 },
            new DreamThemeWeight { ThemeId = "maze", ThemeDisplayName = "迷宫心象", Weight = 10 },
        };
    }

    [CreateAssetMenu(menuName = "My/MiniGame/Dream Infiltration Database", fileName = "DreamInfiltrationDatabase")]
    public class DreamInfiltrationDatabase : ScriptableObject
    {
        public List<DreamEntrySpotDef> Spots = new();

        public static DreamInfiltrationDatabase LoadOrDefault()
        {
            var db = Resources.Load<DreamInfiltrationDatabase>("Config/DreamInfiltrationDatabase");
            return db != null ? db : CreateDefaultInstance();
        }

        public static DreamInfiltrationDatabase CreateDefaultInstance()
        {
            var d = CreateInstance<DreamInfiltrationDatabase>();
            d.Spots = new List<DreamEntrySpotDef>
            {
                new DreamEntrySpotDef
                {
                    SpotId = "north",
                    DisplayName = "北门潜入口",
                    Anchor01 = new Vector2(0.22f, 0.62f),
                    UnlockConds = new List<DreamUnlockCondRow>
                    {
                        new DreamUnlockCondRow { Type = ECommonCheckType.None },
                    },
                },
                new DreamEntrySpotDef
                {
                    SpotId = "east",
                    DisplayName = "东侧裂隙",
                    Anchor01 = new Vector2(0.72f, 0.48f),
                    UnlockConds = new List<DreamUnlockCondRow>
                    {
                        new DreamUnlockCondRow { Type = ECommonCheckType.None },
                    },
                },
                new DreamEntrySpotDef
                {
                    SpotId = "locked_demo",
                    DisplayName = "需条件（演示 AlwaysFail）",
                    Anchor01 = new Vector2(0.48f, 0.28f),
                    UnlockConds = new List<DreamUnlockCondRow>
                    {
                        new DreamUnlockCondRow { Type = ECommonCheckType.AlwaysFail },
                    },
                },
            };
            return d;
        }
    }
}
