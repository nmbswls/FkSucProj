using System.Text;
using My.Map.Entity;
using TMPro;
using UnityEngine;

namespace My.UI
{
    public class PlayerBuffHoverTipPanel : MonoBehaviour, IHoverTipPanel
    {
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI ValueText;
        public TextMeshProUGUI DescText;
        public RectTransform Root;

        private void Awake()
        {
            if (Root == null)
            {
                Root = transform as RectTransform;
            }
        }

        public void OnHoverTipUpdate(HoverTipParams tipParams, IHoverInfoProvider provider)
        {
            if (Root == null)
            {
                Root = transform as RectTransform;
            }

            Vector3 anchorPos = provider.TooltipPosition;
            anchorPos.z = 0;
            Root.position = anchorPos;
            Root.localPosition = new Vector3(Root.localPosition.x, Root.localPosition.y, 0);

            if (provider is not PlayerBuffIconSlot slot || slot.BoundBuff == null)
            {
                if (TitleText != null)
                {
                    TitleText.text = string.Empty;
                }

                if (ValueText != null)
                {
                    ValueText.text = string.Empty;
                }

                if (DescText != null)
                {
                    DescText.text = string.Empty;
                }

                return;
            }

            var buff = slot.BoundBuff;
            var def = buff.Def;
            if (TitleText != null)
            {
                TitleText.text = string.IsNullOrEmpty(def?.BuffId) ? "Buff" : def.BuffId;
            }

            if (ValueText != null)
            {
                string life = buff.Lifetime < 0f ? "永久" : $"{buff.Lifetime:0.#}s";
                ValueText.text = $"层数: {buff.Layer}  剩余: {life}";
            }

            if (DescText != null)
            {
                var sb = new StringBuilder();
                if (def != null)
                {
                    if (def.IsHidden)
                    {
                        sb.AppendLine("[隐藏]");
                    }

                    if (def.ModifierAttrs != null)
                    {
                        foreach (var m in def.ModifierAttrs)
                        {
                            sb.AppendLine($"{m.ModifierAttrId} : {m.ModifierValue}");
                        }
                    }

                    if (def.IsAura)
                    {
                        sb.AppendLine($"光环范围: {def.AuraRange}");
                    }
                }

                DescText.text = sb.Length > 0 ? sb.ToString().TrimEnd() : "无描述";
            }
        }
    }
}
