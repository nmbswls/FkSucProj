using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MapUnitWeaponCtrl;
using UnityEngine.InputSystem.HID;

public class MapUnitWeaponOne : MonoBehaviour
{
    public Animator weaponAnim;
    public MapUnitWeaponCtrl WeaponCtrl;

    public void OnTriggerEnter2D(Collider2D other)
    {
        var unitComp = other.GetComponentInParent<SceneUnitPresenter>();
        if (unitComp == null) return;

        WeaponCtrl.OnWeaponTriggerHit(unitComp.GetLogicEntity().Id);
    }

    public void ShowWeapon(float duration)
    {
        gameObject.SetActive(true);

        // 先尝试获取 clip 长度（简单版：按 clip 名匹配）
        float clipLenSec = -1f;
        var rac = weaponAnim.runtimeAnimatorController;
        if (rac != null)
        {
            foreach (var clip in rac.animationClips)
            {
                if (clip != null && clip.name == "Attack")
                {
                    clipLenSec = clip.length;
                    break;
                }
            }
        }
        weaponAnim.speed = 1.0f;
        if (clipLenSec != -1)
        {
            var speed = clipLenSec / duration;
            weaponAnim.speed = speed;
        }
        weaponAnim.Play("Attack", 0, 0f);
    }

    public void ClearWeapon()
    {
        gameObject.SetActive(false);
    }
}
