using My.Map;

namespace My.MiniGame.Dream
{
    public static class DreamInfiltrationOutcomeApplier
    {
        public static void Apply(DreamSettlementPayload payload)
        {
            if (payload == null) return;
            if (payload.EntrySource != DreamEntrySourceKind.CharacterEntry) return;
            if (string.IsNullOrEmpty(payload.CharacterKey) || payload.CharDreamEntryId <= 0) return;

            var psm = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            psm?.RecordDreamEntryResult(
                payload.CharacterKey,
                payload.CharDreamEntryId,
                payload.Won,
                payload.VictoryTendency);
        }
    }
}
