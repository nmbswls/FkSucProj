using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.BodyPart
{
    // 空洞骑士式装备点数槽：每个槽代表 1 点容量
    public sealed class GearPointNotchBarView : MonoBehaviour
    {
        static readonly Color UsedNotchColor = new Color(0.88f, 0.74f, 0.32f, 1f);
        static readonly Color FreeNotchColor = new Color(0.32f, 0.3f, 0.4f, 0.75f);

        [SerializeField] Transform notchRoot;
        [SerializeField] Image notchTemplate;
        [SerializeField] TextMeshProUGUI summaryText;

        readonly List<Image> _spawned = new();

        public void Refresh(int used, int cap)
        {
            used = Mathf.Max(0, used);
            cap = Mathf.Max(0, cap);

            if (summaryText != null)
            {
                summaryText.text = $"装备点数 {used}/{cap}";
            }

            ClearSpawned();
            if (notchRoot == null || notchTemplate == null || cap <= 0)
            {
                return;
            }

            for (int i = 0; i < cap; i++)
            {
                var go = Instantiate(notchTemplate.gameObject, notchRoot);
                go.SetActive(true);
                var img = go.GetComponent<Image>();
                if (img != null)
                {
                    img.color = i < used ? UsedNotchColor : FreeNotchColor;
                    _spawned.Add(img);
                }
            }
        }

        void ClearSpawned()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                {
                    Destroy(_spawned[i].gameObject);
                }
            }

            _spawned.Clear();
            if (notchRoot == null || notchTemplate == null)
            {
                return;
            }

            for (int i = notchRoot.childCount - 1; i >= 0; i--)
            {
                var child = notchRoot.GetChild(i);
                if (child == notchTemplate.transform)
                {
                    continue;
                }

                Destroy(child.gameObject);
            }
        }
    }
}
