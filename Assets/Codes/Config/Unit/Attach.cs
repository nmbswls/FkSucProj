
using System;
using UnityEngine;

namespace My.Config
{
    [CreateAssetMenu(menuName = "GP/Config/Misc/PlayerAttach")]
    [Serializable]
    public class PlayerAttachObjCfg : ScriptableObject
    {
        public string AttachId;

        public string AttachMainBuff;
        public GameObject AttachViewPrefab;

        public float AutoDropTime = 0;
        public float HitCount = 3;
    }
}