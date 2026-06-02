using System.Collections.Generic;
using System.Linq;
using My.Map;
using My.Map.Scene;
using TMPro;
using UnityEngine;

public class QuickDebugShow : MonoBehaviour
{
    public Canvas TopCanvas;

    public GameObject HpValPrefab;

    public class HpBarStruct
    {
        public GameObject Go;
        public TextMeshProUGUI Val;
        public SceneUnitPresenter bindingUnit;
    }

    public Dictionary<long, HpBarStruct> hpBars = new Dictionary<long, HpBarStruct>();

    [SerializeField]
    float logicHeightScreenOffsetY = 24f;

    void Start()
    {
        TopCanvas = GetComponentInParent<Canvas>();
    }

    public void Clear()
    {
        foreach (var k in hpBars.Keys.ToList())
        {
            GameObject.Destroy(hpBars[k].Go);
        }

        hpBars.Clear();
    }

    public void Update()
    {
        foreach (var k in hpBars.Keys.ToList())
        {
            if (hpBars[k].bindingUnit == null || !hpBars[k].bindingUnit.CheckValid())
            {
                GameObject.Destroy(hpBars[k].Go);
                hpBars.Remove(k);
                continue;
            }

            var entity = hpBars[k].bindingUnit.UnitEntity;
            hpBars[k].Val.text = entity.LogicY.ToString("F2");

            var anchor = hpBars[k].bindingUnit.PivotHeader != null
                ? hpBars[k].bindingUnit.PivotHeader.position
                : hpBars[k].bindingUnit.GetWorldPosition();
            Vector3 screenPos = Camera.main.WorldToScreenPoint(anchor);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent as RectTransform,
                screenPos,
                TopCanvas.worldCamera,
                out Vector2 uiLocalPos
            );

            uiLocalPos += Vector2.up * logicHeightScreenOffsetY;
            hpBars[k].Go.transform.localPosition = uiLocalPos;
        }

        foreach (var p in SceneAOIManager.Instance.GetAllActivePresentation())
        {
            if (p is not SceneUnitPresenter unitPresent)
            {
                continue;
            }

            if (unitPresent.UnitEntity.Type != EEntityType.Player)
            {
                continue;
            }

            if (!hpBars.ContainsKey(p.Id))
            {
                var newStruct = new HpBarStruct
                {
                    Go = GameObject.Instantiate(HpValPrefab, transform),
                    bindingUnit = unitPresent,
                };
                newStruct.Go.SetActive(true);
                newStruct.Val = newStruct.Go.GetComponentInChildren<TextMeshProUGUI>();
                hpBars[p.Id] = newStruct;
            }
        }
    }
}

