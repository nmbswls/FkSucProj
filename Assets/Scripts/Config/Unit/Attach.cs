
using System;
using UnityEngine;

namespace My.Config
{
    [Serializable]
    [Obsolete("Player attach config has moved to Luban PlayerAttachInfo.")]
    public class PlayerAttachObjCfg : ScriptableObject
    {
        public string AttachId;
    }
}
