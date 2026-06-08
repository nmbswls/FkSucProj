

using My.Map;
using My.Map.Entity;
using My.Map.Scene;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class SceneNPCHStatUIStruct : MonoBehaviour
    {
        const float BaseHuntScale = 1.2f;

        public GameObject Go;
        public SceneNpcPresenter bindingNpc;

        public Image FaQingFireHint;
        public Image SJProgressBar;
        public TextMeshProUGUI NpcWillText;
        public TextMeshProUGUI SJProgressText;

        float _focusScale = 1f;

        void Awake()
        {
            EnsureSjProgressText();

            if (NpcWillText != null)
            {
                NpcWillText.fontSize = Mathf.Max(NpcWillText.fontSize, 18f);
                NpcWillText.outlineWidth = 0.2f;
                NpcWillText.outlineColor = new Color32(0, 0, 0, 180);
            }
        }

        public void Bind(SceneNpcPresenter npcPresenter)
        {
            bindingNpc = npcPresenter;
            _focusScale = 1f;
            ApplyScale();
            gameObject.SetActive(true);
        }

        public void Unbind()
        {
            bindingNpc = null;
            _focusScale = 1f;
            transform.localScale = Vector3.one;
            gameObject.SetActive(false);
        }

        public void SetFocusScale(float focusScale)
        {
            _focusScale = Mathf.Max(1f, focusScale);
            ApplyScale();
        }

        void ApplyScale()
        {
            float s = BaseHuntScale * _focusScale;
            transform.localScale = new Vector3(s, s, 1f);
        }

        public void UpdateView()
        {
            if (bindingNpc == null)
            {
                return;
            }

            var sjProgress = bindingNpc.NpcEntity.GetAttr(AttrIdConsts.NPCSJProgress);
            float sjFill = sjProgress * 1.0f / 100_000f;
            int sjPercent = Mathf.Clamp(Mathf.RoundToInt(sjFill * 100f), 0, 100);

            if (SJProgressBar != null)
            {
                SJProgressBar.fillAmount = sjFill;
                SJProgressBar.color = GetSjBarColor(sjPercent);
            }

            if (SJProgressText != null)
            {
                SJProgressText.text = $"{sjPercent}%";
            }

            var hVal = bindingNpc.NpcEntity.GetAttr(AttrIdConsts.NPCHVal);
            if (hVal < 20_000)
            {
                FaQingFireHint.color = Color.white;
                FaQingFireHint.transform.localScale = Vector3.one * 0.6f;
            }
            else if (hVal < 40_000)
            {
                FaQingFireHint.color = Color.white;
                FaQingFireHint.transform.localScale = Vector3.one * 0.7f;
            }
            else if (hVal < 60_000)
            {
                FaQingFireHint.color = Color.red;
                FaQingFireHint.transform.localScale = Vector3.one * 0.8f;
            }
            else if (hVal < 80_000)
            {
                FaQingFireHint.color = Color.red;
                FaQingFireHint.transform.localScale = Vector3.one * 0.9f;
            }
            else
            {
                FaQingFireHint.color = Color.red;
                FaQingFireHint.transform.localScale = Vector3.one * 1.1f;
            }

            var hShield = bindingNpc.NpcEntity.GetAttr(AttrIdConsts.UnitHShield);
            if (hShield > 0)
            {
                NpcWillText.text = ((int)Mathf.Ceil(hShield * 1.0f / 1000f)).ToString();
            }
            else
            {
                NpcWillText.text = string.Empty;
            }
        }

        void EnsureSjProgressText()
        {
            if (SJProgressText != null)
            {
                return;
            }

            var found = transform.Find("SJProgressText");
            if (found != null)
            {
                SJProgressText = found.GetComponent<TextMeshProUGUI>();
                return;
            }

            if (SJProgressBar == null)
            {
                return;
            }

            var parent = SJProgressBar.transform.parent != null
                ? SJProgressBar.transform.parent
                : transform;
            var go = new GameObject("SJProgressText", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -14f);
            rt.sizeDelta = new Vector2(56f, 16f);

            SJProgressText = go.GetComponent<TextMeshProUGUI>();
            if (NpcWillText != null)
            {
                SJProgressText.font = NpcWillText.font;
                SJProgressText.fontSharedMaterial = NpcWillText.fontSharedMaterial;
            }

            SJProgressText.alignment = TextAlignmentOptions.Center;
            SJProgressText.fontSize = 14f;
            SJProgressText.color = Color.white;
            SJProgressText.outlineWidth = 0.15f;
            SJProgressText.outlineColor = new Color32(0, 0, 0, 200);
            SJProgressText.raycastTarget = false;
        }

        static Color GetSjBarColor(int sjPercent)
        {
            if (sjPercent < 30)
            {
                return new Color(0.75f, 0.75f, 0.75f, 1f);
            }

            if (sjPercent < 60)
            {
                return new Color(1f, 0.85f, 0.2f, 1f);
            }

            if (sjPercent < 85)
            {
                return new Color(1f, 0.55f, 0.1f, 1f);
            }

            return new Color(1f, 0.25f, 0.2f, 1f);
        }
    }
}
