namespace My.Map.Scene
{
    // Unit 表现层 view 子树约定路径（仅用于 fallback Find，主路径仍为 Inspector 序列化）
    public static class UnitPresentationPaths
    {
        public const string View = "view";
        public const string ViewLegacy = "View";
        public const string Agent = "agent";
        public const string Shadow = "shadow";
        public const string WeaponRoot = "WeaponRoot";
        public const string BindPoint1 = "BindPoint1";
        public const string BindEffectRoot = "BindEffectRoot";
        public const string AttachmentRoot = "AttachmentRoot";

        // forward_hint prefab: root/view/rotator
        public const string ForwardHintRotator = "view/rotator";
    }
}
