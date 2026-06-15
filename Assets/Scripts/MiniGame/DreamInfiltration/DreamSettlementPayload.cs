namespace My.MiniGame.Dream
{
    public sealed class DreamSettlementPayload
    {
        public bool Won;
        public string ThemeDisplayName = "";
        public int ForceScore;
        public int SoothingScore;
        public int TrickScore;

        public DreamEntrySourceKind EntrySource = DreamEntrySourceKind.FacilitySpot;
        public string SpotId = "";
        public string CharacterKey = "";
        public int CharDreamEntryId;
        public DreamTendencyKind? VictoryTendency;
    }
}
