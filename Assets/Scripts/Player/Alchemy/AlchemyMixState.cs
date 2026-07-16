using System.Collections.Generic;

namespace My.Player.Alchemy
{
    // 投入素材 + 炉子/工具加成后，解析得到的功效与属性集合（键为配表 int id）。
    public sealed class AlchemyMixState
    {
        readonly Dictionary<int, int> _virtues = new();
        readonly Dictionary<int, int> _aspects = new();
        readonly Dictionary<string, int> _materialCounts = new();

        public IReadOnlyDictionary<int, int> Virtues => _virtues;
        public IReadOnlyDictionary<int, int> Aspects => _aspects;
        public IReadOnlyDictionary<string, int> MaterialCounts => _materialCounts;

        public int GetVirtue(int virtueId)
            => virtueId > 0 && _virtues.TryGetValue(virtueId, out var value) ? value : 0;

        public int GetAspect(int aspectId)
            => aspectId > 0 && _aspects.TryGetValue(aspectId, out var value) ? value : 0;

        public int GetMaterialCount(string itemId)
            => !string.IsNullOrEmpty(itemId) && _materialCounts.TryGetValue(itemId, out var value) ? value : 0;

        internal void AddVirtue(int virtueId, int amount)
        {
            if (virtueId <= 0 || amount == 0)
            {
                return;
            }

            _virtues.TryGetValue(virtueId, out var current);
            _virtues[virtueId] = current + amount;
        }

        internal void AddAspect(int aspectId, int amount)
        {
            if (aspectId <= 0 || amount == 0)
            {
                return;
            }

            _aspects.TryGetValue(aspectId, out var current);
            _aspects[aspectId] = current + amount;
        }

        internal void AddMaterialCount(string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0)
            {
                return;
            }

            _materialCounts.TryGetValue(itemId, out var current);
            _materialCounts[itemId] = current + count;
        }

        internal void AmplifyVirtueFromMaterials(int virtueId, int materialAmount, int amplifyPercent)
        {
            if (virtueId <= 0 || materialAmount <= 0 || amplifyPercent <= 0)
            {
                return;
            }

            int bonus = materialAmount * amplifyPercent / 100;
            if (bonus > 0)
            {
                AddVirtue(virtueId, bonus);
            }
        }
    }
}
