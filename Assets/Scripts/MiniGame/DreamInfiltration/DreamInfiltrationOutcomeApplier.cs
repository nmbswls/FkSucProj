using My.Map;

namespace My.MiniGame.Dream
{
    public static class DreamInfiltrationOutcomeApplier
    {
        public static void Apply(DreamSettlementPayload payload)
        {
            if (payload == null || !payload.Won) return;
            if (payload.EntrySource != DreamEntrySourceKind.CharacterEntry) return;
            if (string.IsNullOrEmpty(payload.CharacterKey) || payload.CharDreamEntryId <= 0) return;
            if (!payload.VictoryTendency.HasValue) return;

            var psm = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            psm?.IncrementDreamEntryTendencyWin(
                payload.CharacterKey, payload.CharDreamEntryId, payload.VictoryTendency.Value);
        }
    }
}
