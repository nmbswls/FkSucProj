using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace My.Map.Scene
{
    public class YSortOrder : MonoBehaviour
    {
        public int baseOrder = 0;
        public static float factor = 100f;
        public Transform pivot; // 脚点；若为空用自身
        private SpriteRenderer sr;
        private SortingGroup group;

        void Awake() 
        { 
            sr = GetComponent<SpriteRenderer>();
            group = GetComponent<SortingGroup>();
        }
        void LateUpdate()
        {
            var p = pivot ? pivot.position : transform.position;

            if(sr != null)
            {
                sr.sortingOrder = baseOrder - Mathf.RoundToInt(p.y * factor);
            }
            if(group != null)
            {
                group.sortingOrder = baseOrder - Mathf.RoundToInt(p.y * factor);
            }
        }
    }
}

