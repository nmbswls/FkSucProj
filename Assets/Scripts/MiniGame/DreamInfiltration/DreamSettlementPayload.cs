namespace My.MiniGame.Dream
{
    public sealed class DreamSettlementPayload
    {
        public bool Won;
        public string ThemeDisplayName = "";
        public int ForceScore;
        public int SoothingScore;
        public int TrickScore;

        public DreamEntrySourceKind EntrySource = DreamEntrySourceKind.PasserbyEntry;
        public string SpotId = "";
        public string PasserbyId = "";
        public string PasserbyRegionId = "";
        public string CharacterKey = "";
        public int CharDreamEntryId;
        public string AbstractGroupId = "";
        public int AbstractGroupStage;
        public DreamTendencyKind? VictoryTendency;
        public string ExtraSettlementNote = "";
        public bool AdvanceDayAfterClose = true;
    }
}
