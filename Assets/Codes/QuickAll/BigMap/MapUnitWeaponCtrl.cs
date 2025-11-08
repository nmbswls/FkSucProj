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

    public class WeaponHitCtx
    { 
        public long hitId;
        public string weaponName;
        public float hitDuration;
    }

    public WeaponHitCtx? CurrentHitCtx = null;

    public void Awake()
    {
        UnitPresenter = GetComponentInParent<SceneUnitPresenter>();

        for(int i=0;i< transform.childCount;i++)
        {

        }
    }


    void Update()
    {
        if(CurrentHitCtx != null)
        {
            CurrentHitCtx.hitDuration -= Time.deltaTime;
            if(CurrentHitCtx.hitDuration <= 0)
            {
                //Debug.Log("MapUnitWeaponCtrl clsoe hit window " + CurrentHitCtx.hitId);
                OnHitWindowClear(CurrentHitCtx.hitId);
            }
        }
    }

    /// <summary>
    ///  ¿º“
    /// </summary>
    /// <param name="weaponName"></param>
    /// <param name="hitId"></param>
    public void ApplyUseWeapon(string weaponName, long hitId, float duration)
    {
        //Debug.Log("ApplyActiveUseWeapon " + weaponName + " " + hitId + " " + duration);
        CurrentHitCtx = new WeaponHitCtx()
        {
            hitId = hitId,
            weaponName = weaponName,
            hitDuration = duration,
        };

        WeaponOne.ShowWeapon(duration);
    }

    public void OnHitWindowClear(long hitId)
    {
        if(CurrentHitCtx != null)
        {
            if(CurrentHitCtx.hitId == hitId)
            {
                WeaponOne.ClearWeapon();
                CurrentHitCtx = null;
            }
        }
    }

    public void OnWeaponTriggerHit(long entityId)
    {
        if (CurrentHitCtx == null)
        {
            return;
        }
        if(entityId == UnitPresenter.UnitEntity.Id)
        {
            return;
        }
        Debug.Log("OnWeaponTriggerHit hit with id " + entityId);
        UnitPresenter.OnWeaponHitCallback(CurrentHitCtx.hitId, entityId);
    }
}
