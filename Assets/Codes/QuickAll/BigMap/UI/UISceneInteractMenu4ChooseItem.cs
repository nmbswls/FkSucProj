using SuperScrollView;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class UISceneInteractMenu4ChooseItem : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public Image bgImage;
    public Image pointerImage; // 左侧指示箭头（可选）

    [Header("Colors")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1);
    public Color highlightColor = new Color(0.35f, 0.35f, 0.35f, 1);
    public Color selectedColor = new Color(0.15f, 0.4f, 0.15f, 1);
    public Color textNormal = Color.white;
    public Color textSelected = Color.yellow;

    public void Bind(string title, bool isCurrent, bool isSelected)
    {
        titleText.text = title;

        // 视觉规则：
        // - isSelected：表示“按F确认”的被选中项（绿色底+黄字）
        // - isCurrent：当前游标项（滚轮移动的焦点，高亮灰）
        // 二者都 true 时以 selected 优先，且可显示指示器
        if (isSelected)
        {
            bgImage.color = selectedColor;
            titleText.color = textSelected;
        }
        else if (isCurrent)
        {
            bgImage.color = highlightColor;
            titleText.color = textNormal;
        }
        else
        {
            bgImage.color = normalColor;
            titleText.color = textNormal;
        }

        if (pointerImage)
            pointerImage.enabled = isCurrent; // 只对当前焦点显示箭头
    }
}
