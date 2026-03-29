using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuickAIUseSkill : MonoBehaviour
{
    public QuickAIConfig config;
    public QuickAIBlackboard bb;
    public System.Action OnAttackPerformed;

    public bool IsInRange()
    {
        if (bb.target == null) return false;
        return Vector2.Distance(transform.position, bb.target.position) <= config.attackRange;
    }

    public IEnumerator DoAttackCoroutine()
    {
        // 前摇
        yield return new WaitForSeconds(config.attackWindup);

        // 执行攻击
        if (config.projectilePrefab == null)
        {
            // 近战：可用OverlapCircle触发伤害
            var hits = Physics2D.OverlapCircleAll(transform.position + transform.right * (config.attackRange * 0.6f), config.attackRange * 0.5f, bb != null && bb.target != null ? (1 << bb.target.gameObject.layer) : ~0);
            // 实战中应使用更安全的 Layer/Tag 检测 + 伤害接口
        }
        else
        {
            // 远程：实例化子弹
            var proj = Instantiate(config.projectilePrefab, transform.position, Quaternion.identity);
            Vector2 dir = (bb.target.position - transform.position).normalized;
            proj.transform.right = dir;
            // 让子弹脚本自行处理速度/伤害
        }

        OnAttackPerformed?.Invoke();

        // 后摇
        yield return new WaitForSeconds(config.attackWinddown);
    }

    public void CheckUseSkillEnd()
    {

    }
}