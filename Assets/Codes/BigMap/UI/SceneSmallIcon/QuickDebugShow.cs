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


    public void Clear()
    {
        foreach(var k in hpBars.Keys.ToList())
        {
            var go = hpBars[k].Go;
            GameObject.Destroy(go);
        }

        hpBars.Clear();
    }


    // Update is called once per frame
    public void Update()
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
            //var attracted = hpBars[k].bindingUnit.UnitEntity.CheckAttractState();
            //if(attracted)
            //{
            //    hpBars[k].Val.text += " a";
            //}

            if(hpBars[k].bindingUnit.UnitEntity.combatStateComp.CombatState == My.Map.EntityCombatStateComp.ECombatState.InCombat)
            {
                hpBars[k].Val.text += " b";
            }
            else if(hpBars[k].bindingUnit.UnitEntity.combatStateComp.CombatState == My.Map.EntityCombatStateComp.ECombatState.CombatRecover)
            {
                hpBars[k].Val.text += " r";
            }

            try
            {
                var seePlayer = hpBars[k].bindingUnit.UnitEntity.IsTargetVisible(MainGameManager.Instance.playerScenePresenter.PlayerEntity.Id);
                if(seePlayer)
                {
                    hpBars[k].Val.text += " s";
                }
            }
            catch { }

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
            if(unitPresent.UnitEntity.Type == My.Map.EEntityType.Player)
            {
                continue;
            }

            if(!hpBars.ContainsKey(p.Id))
            {
                HpBarStruct newStruct = new();
                newStruct.Go = GameObject.Instantiate(HpValPrefab, transform);
                newStruct.Go.SetActive(true);
                newStruct.Val = newStruct.Go.GetComponentInChildren<TextMeshProUGUI>();
                newStruct.bindingUnit = unitPresent;
                hpBars[p.Id] = newStruct;
            }
        }
    }
}
