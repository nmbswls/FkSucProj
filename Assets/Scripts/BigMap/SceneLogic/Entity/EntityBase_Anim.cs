

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
        /// 使用新的基础层级
        /// </summary>
        public List<string> AnimOverrideList { get; protected set; } = new();

        //public List<AnimLayerStruct> AnimLayers { get; set; } = new();

        //public virtual void AddAnimLayer(string animName, int layer = 0, int priorirt = 1)
        //{
        //    foreach (var a in AnimLayers)
        //    {
        //        if (a.Name == animName)
        //        {
        //            return;
        //        }
        //    }

        //    AnimLayers.Add(new AnimLayerStruct()
        //    {
        //        Layer = layer,
        //        Name = animName,
        //        Priority = priorirt
        //    });

        //    EventOnAnimLayerUpdate?.Invoke();
        //}

        //public virtual void RemoveAnimLayer(string animName)
        //{
        //    AnimLayers.RemoveAll(a => a.Name == animName);

        //    EventOnAnimLayerUpdate?.Invoke();
        //}

        public void PlayerAnim(string animName, int layer = 0)
        {
            EventOnAnimPlay?.Invoke(animName, layer, false);
        }


    }
}