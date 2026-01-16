using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using My.Map.Scene;
using UnityEngine;
using UnityEngine.InputSystem.HID;

public class MapUnitWeaponCtrl : MonoBehaviour
{
    // Start is called before the first frame update
    public SceneUnitPresenter UnitPresenter;

    public List<MapUnitWeaponOne> WeaponOnes = new();

    public void Awake()
    {
        UnitPresenter = GetComponentInParent<SceneUnitPresenter>();

        for(int i=0;i< transform.childCount;i++)
        {
            var go = transform.GetChild(i);
            var weaponOne = go.GetComponent<MapUnitWeaponOne>();
            if(weaponOne == null)
            {
                Debug.LogError("MapUnitWeaponCtrl init fail");
                continue;
            }    
            WeaponOnes.Add(weaponOne);
            go.gameObject.SetActive(false);

            weaponOne.WeaponCtrl = this;
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
        var findIt = WeaponOnes.Find((item)=>item.gameObject.name == weaponName);
        if(findIt == null)
        {
            Debug.LogError($"ApplyUseWeapon {weaponName} not found");
            return;
        }
        findIt.ShowWeapon(hitId, duration);
    }

    public void OnHitWindowClear(string weaponName, long hitId)
    {
        var findIt = WeaponOnes.Find((item) => item.gameObject.name == weaponName);
        if (findIt == null)
        {
            Debug.LogError($"ApplyUseWeapon {weaponName} not found");
            return;
        }
        findIt.ClearWeapon(hitId);
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
