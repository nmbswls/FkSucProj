using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

namespace Map.Scene.UI
{
    public class ShowClickkkUI : MonoBehaviour
    {
        public static ShowClickkkUI Instance;

        public Image image;
        public TextMeshProUGUI hintText;
        public TextMeshProUGUI hitCount;

        public string WindowType;
        public Vector3? anchorWorldPos;
        public int Counter;

        public float Duration;


        private float _timer;
        void Awake()
        {
            Instance = this;

            Instance.gameObject.SetActive(false);
        }


        public void Update()
        {
            if(anchorWorldPos != null)
            {
                Vector2 screenPos = Camera.main.WorldToScreenPoint(anchorWorldPos.Value);
                transform.localPosition = screenPos;
            }

            if(gameObject.activeSelf)
            {
                hitCount.text = Counter.ToString();
            }
        }

        public void OpenClickkkHint(string windowType, Vector3 openWorldPos, float duration)
        {
            this.WindowType = windowType;
            gameObject.SetActive(true);

            if (windowType == "Touxi")
            {
                Counter = 0;
                anchorWorldPos = openWorldPos;
                hintText.text = "touxi";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void CloseClickkkWindow(string windowType, bool interrupt)
        {
            Counter = 0;
            anchorWorldPos = null;
            gameObject.SetActive(false);
        }
    }
}


