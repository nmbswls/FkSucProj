using System.Collections;
using System.Collections.Generic;
using System.Linq;
using My.Map.Entity;
using My.Map.Scene;
using TMPro;
using UnityEngine;

public class QuickDebugShow : MonoBehaviour
{
    public Canvas TopCanvas;
    // Start is called before the first frame update
    void Start()
    {
        TopCanvas = GetComponentInParent<Canvas>();
    }

    public GameObject HpValPrefab;
    public class HpBarStruct
    {
        public GameObject Go;
        public TextMeshProUGUI Val;
        public SceneUnitPresenter bindingUnit;
    }
    
    public Dictionary<long, HpBarStruct> hpBars = new Dictionary<long, HpBarStruct>();

    // Update is called once per frame
    void Update()
    {
        foreach (var k in hpBars.Keys.ToList())
        {
            if(hpBars[k].bindingUnit == null || !hpBars[k].bindingUnit.CheckValid())
            {
                GameObject.Destroy(hpBars[k].Go);
                hpBars.Remove(k);
                continue;
            }

            hpBars[k].Val.text = hpBars[k].bindingUnit.UnitEntity.GetAttr(AttrIdConsts.HP).ToString();
            var attracted = hpBars[k].bindingUnit.UnitEntity.CheckAttractState();
            if(attracted)
            {
                hpBars[k].Val.text += " a";
            }

            var worldPos = hpBars[k].bindingUnit.GetWorldPosition();
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            Vector2 uiLocalPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent as RectTransform,
                screenPos,   
                TopCanvas.worldCamera,
                out uiLocalPos
            );

            //someUiElement.anchoredPosition = uiLocalPos;

            hpBars[k].Go.transform.localPosition = uiLocalPos;
        }

        foreach(var p in SceneAOIManager.Instance.GetAllActivePresentation())
        {
            if(p is not SceneUnitPresenter unitPresent)
            {
                continue;
            }
            if(!hpBars.ContainsKey(p.Id))
            {
                HpBarStruct newStruct = new();
                newStruct.Go = GameObject.Instantiate(HpValPrefab, transform);
                newStruct.Val = newStruct.Go.GetComponentInChildren<TextMeshProUGUI>();
                newStruct.bindingUnit = unitPresent;
                hpBars[p.Id] = newStruct;
            }
        }
    }
}
