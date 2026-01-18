


using System.Collections.Generic;
using My.Map.Logic;
using UnityEngine;

public class UnitAnimHolder : MonoBehaviour
{
    public class OneWrapper
    {
        public string Name;
        public AnimationClip Clip;
        public float Speed;
    };

    public List<OneWrapper> AnimClips;
}