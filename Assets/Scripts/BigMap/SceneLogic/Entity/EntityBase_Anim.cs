

using System;
using System.Collections.Generic;

namespace My.Map
{

    public abstract partial class LogicEntityBase
    {
        //public event Action EventOnAnimLayerUpdate;
        public event Action<string, int, bool> EventOnAnimPlay;

        private uint _nextAnimId = 1;

        /// <summary>
        /// 获取 动画覆盖
        /// 如；覆盖idle 需要从 这里获取
        /// </summary>
        /// <param name="rawAnimName"></param>
        /// <returns></returns>
        public string GetAnimOverride(string rawAnimName)
        {
            var buffs = BuffContainer.Values;
            foreach (var b in buffs)
            {
                if(b.Def.DurationEffect == null)
                {
                    continue;
                }
                if(b.Def.DurationEffect.DurationType != Entity.EBuffDurationType.AnimOverride)
                {
                    continue;
                }

                if(b.Def.DurationEffect.ParamStr1 == rawAnimName)
                {
                    return b.Def.DurationEffect.ParamStr2;
                }
            }

            return rawAnimName;
        }


        public void PlayerAnim(string animName, int layer = 0)
        {
            EventOnAnimPlay?.Invoke(animName, layer, false);
        }
    }
}