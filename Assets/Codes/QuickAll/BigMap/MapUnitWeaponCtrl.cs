using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.InputSystem.HID;

public class MapUnitWeaponCtrl : MonoBehaviour
{
    // Start is called before the first frame update
    public SceneUnitPresenter UnitPresenter;

    public MapUnitWeaponOne WeaponOne;

    public void Awake()
    {
        UnitPresenter = GetComponentInParent<SceneUnitPresenter>();

        for(int i=0;i< transform.childCount;i++)
        {

        }
    }


    void Update()
    {
    }

    /// <summary>
    ///  ¿º“
    /// </summary>
    /// <param name="weaponName"></param>
    /// <param name="hitId"></param>
    public void ApplyUseWeapon(string weaponName, long hitId, float duration)
    {
        WeaponOne.ShowWeapon(hitId, duration);
    }

    public void OnHitWindowClear(long hitId)
    {
        WeaponOne.ClearWeapon();
    }

    public void OnWeaponTriggerHit(long hitId, long entityId)
    {
        if(entityId == UnitPresenter.UnitEntity.Id)
        {
            return;
        }
        Debug.Log("OnWeaponTriggerHit hit with id " + entityId);
        UnitPresenter.OnWeaponHitCallback(hitId, entityId);
    }
}
