


using System;
using System.Collections.Generic;
using My.Map.Logic;
using UnityEngine;

public class UnitAnimHolder : MonoBehaviour
{
    [Serializable]
    public class OneWrapper
    {
        public string Name;
        public AnimationClip Clip;
        public float Speed = 1.0f;
    };

    public List<OneWrapper> AnimClips;


    private void Awake()
    {
        
    }
}