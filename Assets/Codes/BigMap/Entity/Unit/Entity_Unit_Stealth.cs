
using System.Collections.Generic;

namespace My.Map
{
    public partial class BaseUnitLogicEntity
    {
        public class StealthInfo
        {
            public long stealthId;
            public Dictionary<long, float> SeeUnits = new();
        }
        public StealthInfo stealthInfo;

    }
}