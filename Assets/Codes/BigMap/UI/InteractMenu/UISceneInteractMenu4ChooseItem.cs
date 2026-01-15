using DG.Tweening;
using SuperScrollView;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class UISceneInteractMenu4ChooseItem : MonoBehaviour
    {
        public TextMeshProUGUI titleText;
        public Image bgImage;
        public Image pointerImage; // 左侧指示箭头（可选）

        public GameObject SelectHintBg;
        public Image SelectHintBgImage;
        public GameObject SelectHintArrow;
        public GameObject SelectButtonHint;

        //[Header("Colors")]
        //public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1);
        //public Color highlightColor = new Color(0.35f, 0.35f, 0.35f, 1);
        //public Color selectedColor = new Color(0.15f, 0.4f, 0.15f, 1);
        //public Color textNormal = Color.white;
        //public Color textSelected = Color.yellow;

        public Color pressedColor = new Color(0, 0, 0.5f); // 深蓝色
        public float duration = 0.15f; // 单程时间（变过去的时间）

        private Color originalColor; // 记录原始颜色（天蓝色）

        public void Awake()
        {
            originalColor = SelectHintBgImage.color;
        }
        /// <summary>
        /// 
        /// </summary>
        public void DoHintConfirm()
        {
            SelectHintBgImage.DOKill();
            SelectHintBgImage.color = originalColor;

            // 3. 执行变色
            // 0.15秒变深，LoopType.Yoyo 代表变完自动变回来，一共执行2次（去1回1）
            SelectHintBgImage.DOColor(pressedColor, duration)
                       .SetLoops(2, LoopType.Yoyo)
                       .SetEase(Ease.InOutQuad);
        }

        public void Bind(string title, bool isCurrent, bool isSelected, bool selectable, bool isSingle)
        {
            titleText.text = title;

            // 视觉规则：
            if (isCurrent)
            {
                SelectHintBg.SetActive(true);
                if(isSingle)
                {
                    SelectHintArrow.SetActive(false);
                }
                else
                {
                    SelectHintArrow.SetActive(true); 
                }
                SelectButtonHint.SetActive(true);
            }
            else
            {
                SelectHintBg.SetActive(false);
                SelectHintArrow.SetActive(false);
                SelectButtonHint.SetActive(false);
            }

            if (pointerImage)
                pointerImage.enabled = isCurrent; // 只对当前焦点显示箭头
        }
    }

}

