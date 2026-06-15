namespace My.MiniGame.Dream
{
    public static class DreamVictoryTendencyResolver
    {
        // 并列优先：暴力 > 安抚 > 计谋
        public static DreamTendencyKind Resolve(int force, int soothe, int trick)
        {
            if (force >= soothe && force >= trick) return DreamTendencyKind.Force;
            if (soothe >= trick) return DreamTendencyKind.Soothing;
            return DreamTendencyKind.Trick;
        }
    }
}
