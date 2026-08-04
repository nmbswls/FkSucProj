using My.Map;

namespace My.MiniGame.Dream
{
    public static class DreamInfiltrationOutcomeApplier
    {
        public static void Apply(DreamSettlementPayload payload)
        {
            if (payload == null) return;

            var glm = MainGameManager.Instance?.gameLogicManager;
            if (payload.EntrySource == DreamEntrySourceKind.CharacterEntry)
            {
                if (string.IsNullOrEmpty(payload.CharacterKey) || payload.CharDreamEntryId <= 0) return;
                var psm = glm?.playerDataManager;
                psm?.RecordDreamEntryResult(
                    payload.CharacterKey,
                    payload.CharDreamEntryId,
                    payload.Won,
                    payload.VictoryTendency);
                return;
            }

            if (payload.EntrySource == DreamEntrySourceKind.AbstractGroupEntry)
            {
                var note = AbstractGroupDreamService.ApplySettlement(glm, payload);
                if (!string.IsNullOrEmpty(note))
                {
                    payload.ExtraSettlementNote = note;
                }
            }
        }
    }
}
