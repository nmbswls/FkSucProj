using cfg.demo;

namespace My.UI.Rune
{
    public class RuneDragPayload
    {
        public string RuneId;
        public RuneDragSourceType SourceType;
        public int SourceIndex;
        public ERuneEquipSlot SourceEquipSlot;
    }

    public enum RuneDragSourceType
    {
        OwnedGrid = 0,
        EquipSlot = 1,
    }
}
