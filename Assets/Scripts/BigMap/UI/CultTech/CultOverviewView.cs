using System.Collections.Generic;
using My.Player;
using TMPro;
using UnityEngine;

namespace My.UI.CultTech
{
    public sealed class CultOverviewView : MonoBehaviour
    {
        readonly List<TextMeshProUGUI> _valueTexts = new();
        DemonCultSystem _cult;
        TextMeshProUGUI _influenceValue;

        public void Bind(DemonCultSystem cult)
        {
            _cult = cult;
            ResolveLayout();
            Refresh();
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void Refresh()
        {
            if (_cult == null || _valueTexts.Count < 4) return;
            _valueTexts[0].text = _cult.Faith.ToString();
            _valueTexts[1].text = _cult.GetUnlockedTechCount().ToString();
            _valueTexts[2].text = _cult.UnlockedSeatCount.ToString();
            _valueTexts[3].text = _cult.UnlockedSeatTechNodeCount.ToString();
            if (_influenceValue != null) _influenceValue.text = "???";
        }

        void ResolveLayout()
        {
            _valueTexts.Clear();
            _valueTexts.Add(FindText("FaithValue"));
            _valueTexts.Add(FindText("DoctrineValue"));
            _valueTexts.Add(FindText("SeatValue"));
            _valueTexts.Add(FindText("SeatTechValue"));
            _influenceValue = FindText("InfluenceValue");
            for (var index = 0; index < _valueTexts.Count; index++)
            {
                if (_valueTexts[index] == null)
                    Debug.LogError($"CultOverviewView requires value text {index} in its prefab.");
            }
            if (_influenceValue == null)
                Debug.LogError("CultOverviewView requires InfluenceValue in its prefab.");
        }

        TextMeshProUGUI FindText(string name)
        {
            foreach (var text in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text.name == name) return text;
            }
            return null;
        }
    }
}
