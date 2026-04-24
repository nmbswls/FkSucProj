

using My.Map;

namespace My
{
    public class AreaWantedManager
    {

        public int CurrentWantedVal;
        public float LastWantedTime;

        public void AddWantedVal(int val)
        {
            this.CurrentWantedVal += val * 1000;
            LastWantedTime = LogicTime.time;
        }

        /// <summary>
        /// 检查衰减
        /// </summary>
        /// <param name="dt"></param>
        public void Tick(float dt)
        {
            if(LogicTime.time - LastWantedTime < 30.0f)
            {
                return;
            }

            CurrentWantedVal -= (int)(dt * 10 * 1000);
        }
    }
}
