using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using My.Map;
using My.Map.Entity;
using My.Map.Scene;
using UnityEngine;
using UnityEngine.InputSystem.HID;

public class MapUnitWeaponCtrl : MonoBehaviour
{
    // Start is called before the first frame update
    public SceneUnitPresenter UnitPresenter;

    public List<MapUnitWeaponOne> WeaponOnes = new();
    public YSortOrder ySortOrder;

    public string CurrEquipWeapon = string.Empty;

    public void Awake()
    {
        UnitPresenter = GetComponentInParent<SceneUnitPresenter>();

        for(int i=0;i< transform.childCount;i++)
        {
            var go = transform.GetChild(i);
            var weaponOne = go.GetComponent<MapUnitWeaponOne>();
            if(weaponOne == null)
            {
                //Debug.LogError("MapUnitWeaponCtrl init fail");
                continue;
            }    
            WeaponOnes.Add(weaponOne);
            if(weaponOne.name != CurrEquipWeapon)
            {
                go.gameObject.SetActive(false);
            }

            weaponOne.WeaponCtrl = this;
        }
    }


    void Update()
    {
        
        bool isFacingUp = UnitPresenter.UnitEntity.CurrentLook.y > 0.1f;
        if(ySortOrder != null)
        {
            // 1. 处理层级
            ySortOrder.baseOrder = isFacingUp ?
                -1 :
                +1;
        }

        foreach (var weaponOne in WeaponOnes)
        {
            weaponOne.OnWeaponAimDirUpdate(UnitPresenter.UnitEntity.CurrentLook);
        }
    }

    /// <summary>
    /// 设置常驻武器显示
    /// </summary>
    /// <param name="weaponName"></param>
    public void SetAlwaysShowWeapon(string weaponName)
    {
        CurrEquipWeapon = weaponName;

        foreach(var weaponOne in WeaponOnes)
        {
            weaponOne.IsShown = false;
        }
    }

    /// <summary>
    /// 世家
    /// </summary>
    /// <param name="weaponName"></param>
    /// <param name="hitId"></param>
    public void HandleUseWeapon(string weaponName, long hitId, float duration, string weaponAnimName)
    {
        var findIt = WeaponOnes.Find((item)=>item.gameObject.name == weaponName);
        if(findIt == null)
        {
            Debug.LogError($"ApplyUseWeapon {weaponName} not found");
            return;
        }
        findIt.ShowWeapon(hitId, duration, weaponAnimName);
    }

    public void HandleClearWeapon(string weaponName)
    {
        var findIt = WeaponOnes.Find((item) => item.gameObject.name == weaponName);
        if (findIt == null)
        {
            Debug.LogError($"ApplyUseWeapon {weaponName} not found");
            return;
        }
        findIt.ClearWeapon();
    }

    public void OnWeaponTriggerHit(long hitId, ILogicEntity logicEntity)
    {
        if(logicEntity == null || logicEntity.Id == UnitPresenter.UnitEntity.Id)
        {
            return;
        }
        
        Debug.Log("OnWeaponTriggerHit hit with id " + logicEntity.Id);

        if(logicEntity.GetAttr(AttrIdConsts.NoSelect) > 0)
        {
            return;
        }

        if(logicEntity.GetAttr(AttrIdConsts.HP) <= 0)
        {
            return;
        }

        UnitPresenter.OnWeaponHitCallback(hitId, logicEntity.Id);
    }
}
