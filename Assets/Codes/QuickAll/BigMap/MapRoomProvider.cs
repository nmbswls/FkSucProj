using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Map.Scene
{
    public class MapRoomProvider : MonoBehaviour
    {
        public string RoomId;

        public List<SpriteRenderer> RoomCeils;

        private float fadeSpeed = 4f;
        private float targetCeilAlpha;
        private float currCeilAlpha;

        // Update is called once per frame
        void Update()
        {
            // 以固定速率向 targetAlpha 靠拢
            float next = Mathf.MoveTowards(currCeilAlpha, targetCeilAlpha, fadeSpeed * Time.deltaTime);

            if (Mathf.Abs(next - currCeilAlpha) > Mathf.Epsilon)
            {
                currCeilAlpha = next;
                if(RoomCeils != null)
                {
                    foreach(var ceil in RoomCeils)
                    {
                        ceil.color = new Color(ceil.color.r, ceil.color.g, ceil.color.b, currCeilAlpha);
                    }
                }
            }
            else
            {
                if(currCeilAlpha == targetCeilAlpha)
                {
                    return;
                }
                currCeilAlpha = targetCeilAlpha;
                if (RoomCeils != null)
                {
                    foreach (var ceil in RoomCeils)
                    {
                        ceil.color = new Color(ceil.color.r, ceil.color.g, ceil.color.b, currCeilAlpha);
                    }
                }
            }
        }

       
        public void ShowFadeCeil()
        {
            targetCeilAlpha = 1;
        }

        public void HideFadeCeil()
        {
            targetCeilAlpha = 0;
        }
    }

}


