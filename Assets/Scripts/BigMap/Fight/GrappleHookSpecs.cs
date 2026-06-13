namespace My.Map.Fight
{
    public static class GrappleHookSpecs
    {
        public const string BulletId = "grapple_hook";
        public const float MaxLength = 6f;
        // 出钩飞行与绳伸速度（世界单位/秒）；6m 射程约 0.75s，便于看清链节与钩头
        public const float FlySpeed = 8f;
        public const float PullDuration = 0.35f;
    }
}
