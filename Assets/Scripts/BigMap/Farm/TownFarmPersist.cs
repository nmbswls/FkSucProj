using System;
using System.Collections.Generic;
using My.Saving;

namespace My.Farm
{
    [Serializable]
    public class FarmCellPersist
    {
        public int Cx;
        public int Cy;
        public string CropId;
        public int GrowProgress;
        public bool Watered;
        public bool Fertilized;
    }

    [Serializable]
    public class FarmPlotPersist
    {
        public string PlotId;
        public List<FarmCellPersist> Cells = new();
    }

    [Serializable]
    public class FarmPlanEntryPersist
    {
        public string CropId;
        public int TargetCount;
        public int Priority;
    }

    [Serializable]
    public class TownFarmPersist
    {
        public string LogicAreaId;
        public List<FarmPlotPersist> Plots = new();
        public PlayerBagPersist SeedBasket = new();
        public PlayerBagPersist ProduceWarehouse = new();
        public List<FarmPlanEntryPersist> AutoPlantPlan = new();
        public int HarvestWorkforce;
        public int LastSettledDay = -1;
    }
}
