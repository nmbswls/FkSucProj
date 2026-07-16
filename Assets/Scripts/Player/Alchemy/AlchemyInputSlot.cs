namespace My.Player.Alchemy
{
    // 单次炼金投入格：仅表中登记的 item 可被解析。
    public readonly struct AlchemyInputSlot
    {
        public AlchemyInputSlot(string itemId, int count = 1)
        {
            ItemId = itemId;
            Count = count < 1 ? 1 : count;
        }

        public string ItemId { get; }
        public int Count { get; }
    }
}
