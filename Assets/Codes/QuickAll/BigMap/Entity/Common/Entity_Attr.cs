using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Map.Entity.Attr
{
    public enum SourceType
    {
        AbilityActive,
        Buff,
        BuffTrigger,
        BuffEffect,
        Item, 
        Env,
        Aura,
        AreaEffect,
        Bullet,
        Mechanism,
        Throw,
    }

    public enum EAttrType
    {
        Invalid,
        Num,
        Resource,
        State,
    }

    public static class AttrUtils
    {
        public static EAttrType GetAttrType(string attrId)
        {
            switch (attrId)
            {
                case AttrIdConsts.Unmovable:
                case AttrIdConsts.LockFace:
                case AttrIdConsts.Stun:
                case AttrIdConsts.ForbidOp:
                case AttrIdConsts.NoSelect:
                case AttrIdConsts.HidingMask:
                case AttrIdConsts.UnitDizzy:
                case AttrIdConsts.StatUnstoppable:
                    return EAttrType.State;

                case AttrIdConsts.Attack:
                case AttrIdConsts.HP_MAX:
                    return EAttrType.Num;

                case AttrIdConsts.HP:
                case AttrIdConsts.PlayerYinNeng:
                case AttrIdConsts.PlayerHunger:
                case AttrIdConsts.PlayerClothes:
                case AttrIdConsts.PlayerNaiLi:
                case AttrIdConsts.UnitEnterHVal:
                    return EAttrType.Resource;

                default:
                    Debug.LogError("Unknown attr " + attrId);
                    return EAttrType.Invalid;
            }
        }
    }


    public  struct SourceKey : IEquatable<SourceKey>
    {
        public SourceType type;

        public long entityId;
        public long buffId;
        public string buffName;
        public string abilityName;
        public string sourceId;
        public long bulletId;

        public bool Equals(SourceKey other) => type == other.type && entityId == other.entityId && buffId == other.buffId && buffName == other.buffName && abilityName == other.abilityName && sourceId == other.sourceId;
        public override int GetHashCode() => HashCode.Combine((int)type, entityId, buffId, buffName, abilityName, sourceId, bulletId);
    }

    public sealed class Modifier
    {
        public long instId;
        public  SourceKey source;
        public  string attrId;
        public  bool isOverride;
        public  long value;      // 可扩展为曲线/表达式
        public  int priority;     // Override/互斥控制
    }

    public sealed class NumericEntry
    {
        public string attrId;
        public long baseValue;               // Base 阶段可直接缓存或来自模板
        public readonly List<Modifier> addMods = new();
        public readonly List<Modifier> mulMods = new();

        public readonly List<Modifier> overrideMods = new(); // 按 priority 排序
                                                             // 索引：来源 -> 修饰器集合（用于 O(1) 过期）
        public readonly Dictionary<SourceKey, List<Modifier>> bySource = new();
        // 聚合缓存
        public long addSum = 0;
        public long mulProduct = 10000;         // 乘法统一存为 Π(1+rate) 或直接乘 value
        public long? overrideValue = null;
        public long finalValue;
        public bool dirty = true;
        public int version = 0;
    }

    public enum MaxChangePolicy { KeepRatio, KeepDelta, ClampOnly, ResetOnBuffEnd }

    public sealed class ResourceEntry
    {
        public string resourceId;    // e.g., "HP", "MP", "Shield"
                                              // 上限是一个数值类属性（允许被修饰与依赖）
        public NumericEntry max;     // HP.Max, MP.Max ...
                                              // 当前值状态
        public long current;
        public long regenPerSec;             // 可作为 NumericEntry 或常量
        public long drainPerSec;             // 同上
        public MaxChangePolicy onMaxChange = MaxChangePolicy.KeepRatio;
        public bool canHeal = true;
        public bool canSpend = true;

        public long cacheMaxVal;
        public bool dirty = true;             // 当前值是否需要刷新（因上限或策略改变）
        public int version = 0;

        public int ToZeroSrc;

        public List<ResourceDeltaIntent> pendingDelta = new();
    }

    public class ResourceDeltaIntent
    {
        public long delta;
        public SourceKey? srcKey;
        public int deltaFlags;
        public Dictionary<string, long> extraAttrs = null;

        public long finalDelta;
    }

    public sealed class AttrCalcContext
    {
        private readonly Func<string, long> getter;
        public AttrCalcContext(Func<string, long> getter) => this.getter = getter;
        public long Get(string id) => getter(id);
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class AttributeStore
    {
        public IEntityAttributeOwner Owner;
        public static long ModifierIdCounter = 10000;

        // 数值类与资源类的注册表
        private readonly Dictionary<string, NumericEntry> numerics = new();
        private readonly Dictionary<string, ResourceEntry> resources = new(); // key: "HP", "MP"... (Current通过资源管理)
                                                                              // 来源索引（实体级别）：来源 -> (attrId, modifier) 列表，便于 Buff 结束时批量过期
        private readonly Dictionary<SourceKey, List<(string attrId, Modifier mod)>> sourceIndex = new();

        private readonly List<(string resourceId, float delta, bool isDamage, SourceKey source)> _resourceChanges = new();

        // 依赖图
        private readonly float epsilon = 1e-5f;

        // 脏队列：按 level 分桶
        private readonly SortedDictionary<int, Queue<string>> dirtyQueues = new();

        public event Action<string, bool> EvOnStatusAttrChanged;
        public event Action<string /*attrId*/, long /*before*/, long /*after*/, ResourceDeltaIntent /*intent*/> EvOnResourceAttrChanged;

        public AttributeStore(IEntityAttributeOwner owner)
        {
            this.Owner = owner;
        }


        // 注册数值属性（可选初始 Base 值与默认 Clamp）
        public NumericEntry RegisterNumeric(string attrId, long initialBase = 0)
        {
            if (numerics.ContainsKey(attrId))
                throw new InvalidOperationException($"Numeric attr already registered: {attrId}");

            var e = new NumericEntry { attrId = attrId, baseValue = initialBase, finalValue = initialBase, dirty = true };
            numerics[attrId] = e;

            return e;
        }

        // 注册资源（绑定 Max 到某个数值属性，例如 "HP.Max"）
        public ResourceEntry RegisterResource(string resourceId, string maxAttrId, long initialCurrent = 0, MaxChangePolicy policy = MaxChangePolicy.KeepRatio)
        {
            if (resources.ContainsKey(resourceId))
                throw new InvalidOperationException($"Resource already registered: {resourceId}");

            // 确保 Max 数值属性已注册
            var maxEntry = numerics.TryGetValue(maxAttrId, out var e)
                ? e
                : RegisterNumeric(maxAttrId, initialBase: 0); // 或抛异常，按你的设计选择

            var r = new ResourceEntry
            {
                resourceId = resourceId,
                current = Math.Min(initialCurrent, maxEntry.finalValue),
                max = maxEntry,
                onMaxChange = policy,
                cacheMaxVal = maxEntry.finalValue,
                dirty = true
            };
            resources[resourceId] = r;
            return r;
        }

        /// <summary>
        /// 增加modifier
        /// </summary>
        /// <param name="m"></param>
        public Modifier AddModifier(SourceKey source, string attrId, long val)
        {
            var m = new Modifier()
            {
                instId = ++ModifierIdCounter,
                source = source,
                attrId = attrId,
                value = val
            };

            if(!numerics.ContainsKey(attrId))
            {
                RegisterNumeric(attrId);
            }
            var e = numerics[m.attrId];

            // 如果是override 需要更新覆盖策略
            if(m.isOverride)
            {
                InsertOverride(e, m);
                e.overrideValue = e.overrideMods[0].value;
            }
            else
            {
                e.addMods.Add(m);
                e.addSum += m.value;
            }
            // 索引
            if (!e.bySource.TryGetValue(m.source, out var list))
            {
                list = new List<Modifier>(); e.bySource[m.source] = list;
            }
            list.Add(m);
            if (!sourceIndex.TryGetValue(m.source, out var gl))
            {
                gl = new List<(string, Modifier)>(); sourceIndex[m.source] = gl;
            }
            gl.Add((m.attrId, m));

            MarkDirty(m.attrId);

            return m; 
        }

        /// <summary>
        /// 增加modifier
        /// </summary>
        /// <param name="m"></param>
        public void UpdateModifier(Modifier m)
        {
            var e = numerics[m.attrId];

            // 如果是override 需要更新覆盖策略
            if (m.isOverride)
            {
                e.overrideValue = e.overrideMods[0].value;
            }
            else
            {
                e.addSum = e.addMods.Sum(item=>item.value);
            }
            

            MarkDirty(m.attrId);
        }

        private static void InsertOverride(NumericEntry e, Modifier m)
        {
            var idx = e.overrideMods.BinarySearch(m, Comparer<Modifier>.Create((a, b) => b.priority.CompareTo(a.priority)));
            if (idx < 0) idx = ~idx;
            e.overrideMods.Insert(idx, m);
        }

        public void ExpireBySource(SourceKey sk)
        {
            if (!sourceIndex.TryGetValue(sk, out var list)) return;
            foreach (var (attrId, mod) in list)
            {
                var e = numerics[attrId];

                if(mod.isOverride)
                {
                    e.overrideMods.Remove(mod);
                    e.overrideValue = e.overrideMods.Count > 0 ? e.overrideMods[0].value : (long?)null;
                }
                else
                {
                    e.addMods.Remove(mod);
                    e.addSum -= mod.value;
                }
                // 从 per-attr 索引移除
                if (e.bySource.TryGetValue(sk, out var col))
                {
                    col.Remove(mod);
                    if (col.Count == 0) e.bySource.Remove(sk);
                }
                MarkDirty(attrId);
            }
            sourceIndex.Remove(sk);
        }

        private void MarkDirty(string attrId)
        {
            var attrNode = UnitAttrSystemUnits.GetAttrNode(attrId);
            if (attrNode == null)
            {
                // 数值可能未参与图（纯局部属性），也要重算其 finalValue
                if (numerics.ContainsKey(attrId)) RecomputeNumeric(attrId);
                return;
            }
            Enqueue(attrNode.level, attrId);
        }

        private void Enqueue(int level, string attrId)
        {
            if (!dirtyQueues.TryGetValue(level, out var q))
            {
                q = new Queue<string>(); 
                dirtyQueues[level] = q;
            }
            q.Enqueue(attrId);
        }


        public void Commit()
        {
            // 层级推进
            foreach (var kv in dirtyQueues.ToArray())
            {
                var level = kv.Key; var q = kv.Value;
                while (q.Count > 0)
                {
                    var attrId = q.Dequeue();
                    var changed = RecomputeNumeric(attrId);
                    if (!changed) continue;
                    // 向后继传播

                    var attrNode = UnitAttrSystemUnits.GetAttrNode(attrId);
                    if(attrNode != null)
                    {
                        foreach (var dep in attrNode.outs) Enqueue(dep.level, dep.attrId);
                    }
                }
                dirtyQueues.Remove(level);
            }

            // 资源类的“上限变化联动”：若 Max 变动，调整 current
            foreach (var r in resources.Values)
            {
                // max.finalValue 已在上一步更新，比较前后（此处可缓存上次值以对比）
                // 这里演示：当 Numeric 重算时即可标注 r.dirty
                if (r.dirty)
                {
                    AdjustCurrentOnMaxChange(r);
                    r.dirty = false; 
                    r.version++;
                }
            }

            CommitResources();
        }

        private bool RecomputeNumeric(string attrId)
        {
            if (!numerics.TryGetValue(attrId, out var e)) return false;

            // 先执行公式（如依赖其他属性），否则用聚合
            long baseComputed = e.baseValue;
            var attrNode = UnitAttrSystemUnits.GetAttrNode(attrId);
            if (attrNode != null && attrNode.Eval != null)
            {
                var ctx = new AttrCalcContext(GetFinal);
                baseComputed = attrNode.Eval(ctx);
            }
            long final;
            if (e.overrideValue.HasValue) final = e.overrideValue.Value;
            else
            {
                final = (baseComputed + e.addSum) * e.mulProduct;
            }

            Debug.Log($"entity {Owner.Id} RecomputeNumeric {attrId} update {final}");


            if (Math.Abs(final - e.finalValue) > epsilon)
            {
                var old = e.finalValue;
                e.finalValue = final;
                e.version++;
                // 如果这是某个资源的 Max，标注该资源需要联动
                TryMarkResourceOnMaxChanged(attrId, old, final);
                return true;
            }
            return false;
        }


        // 3) 聚合资源变化
        public void ApplyResourceChange(string resourceId, long delta, bool isDamage, SourceKey? source, Dictionary<string, long> extraAttrs = null)
        {
            if (!resources.TryGetValue(resourceId, out var r)) return;

            r.pendingDelta.Add(new ResourceDeltaIntent()
            {
                delta = delta,
                srcKey = source,
                deltaFlags = isDamage ? 1 : 0,
                extraAttrs = extraAttrs,
            });
        }

        /// <summary>
        /// 更新
        /// </summary>
        public void CommitResources()
        {
            foreach (var r in resources.Values)
            {
                foreach(var pending in r.pendingDelta)
                {
                    long before = r.current;

                    var delta = Owner.CalculateResourceCostAmount(pending);
                    pending.finalDelta = delta;
                    r.current += delta;

                    EvOnResourceAttrChanged?.Invoke(r.resourceId, before, r.current, pending);
                    r.version++;
                }

                r.pendingDelta.Clear();

                // 4.4 最终钳制到 [0, newMax]
                if (r.current < 0f) r.current = 0;
                if (r.current > r.cacheMaxVal) r.current = r.cacheMaxVal;
            }
        }


        private long GetFinal(string id)
        {
            // 查询依赖时读取当前已稳定的 finalValue
            if (numerics.TryGetValue(id, out var e)) return e.finalValue;
            // 支持直接读取资源当前或上限
            if (id.EndsWith(".Max"))
            {
                var rid = id[..^4]; // 去掉 ".Max"
                if (resources.TryGetValue(rid, out var r)) return r.max.finalValue;
            }
            throw new KeyNotFoundException($"Attr not found: {id}");
        }

        private void TryMarkResourceOnMaxChanged(string attrId, float oldMax, float newMax)
        {
            if (!attrId.EndsWith(".Max")) return;
            var rid = attrId[..^4];
            if (resources.TryGetValue(rid, out var r) && MathF.Abs(oldMax - newMax) > epsilon)
            {
                r.dirty = true;
            }
        }

        private void AdjustCurrentOnMaxChange(ResourceEntry r)
        {
            //float oldMax = r.max.finalValue;
            long oldMax = r.cacheMaxVal;
            long newMax = r.max.finalValue;
            // 若需要旧值，请在 ResourceEntry 中加 lastMax 并在标脏时赋值
            switch (r.onMaxChange)
            {
                case MaxChangePolicy.KeepRatio:
                    if (oldMax > 0) r.current = Math.Min(newMax, r.current * (newMax / oldMax));
                    else r.current = Math.Min(newMax, r.current);
                    break;
                case MaxChangePolicy.KeepDelta:
                    var delta = newMax - oldMax;
                    r.current = Math.Min(newMax, r.current + delta);
                    break;
                case MaxChangePolicy.ClampOnly:
                    r.current = Math.Min(newMax, r.current);
                    break;
                case MaxChangePolicy.ResetOnBuffEnd:
                    // 需要 Buff 生命周期回调：在 Buff 结束时将上限回退并按策略重整
                    r.current = Math.Min(newMax, r.current);
                    break;
            }

            r.cacheMaxVal = newMax;
        }

        public void TickResourceAutoRecover(float dt)
        {
            foreach (var r in resources.Values)
            {
                long delta = (long)((r.regenPerSec * dt) - (r.drainPerSec * dt));
                if (MathF.Abs(delta) < 1e-6f) continue;
                long newVal = Math.Clamp(r.current + delta, 0, r.max.finalValue);
                if (MathF.Abs(newVal - r.current) > epsilon)
                {
                    r.current = newVal; r.version++;
                }
            }
        }

        [Flags]
        public enum DamageFlags
        {
            None = 0,
        }


        #region 对外接口

        public long GetAttr(string attrId)
        {
            switch(attrId)
            {
                case AttrIdConsts.HP:
                case AttrIdConsts.PlayerNaiLi:
                case AttrIdConsts.PlayerYinNeng:
                case AttrIdConsts.PlayerClothes:
                    {
                        return GetResourceCurrent(attrId);
                    }
                default:
                    {
                        return GetNumericAttr(attrId);
                    }
            }
        }


        private long GetNumericAttr(string attrId)
        {
            if (numerics.TryGetValue(attrId, out var e)) return e.finalValue;
            return 0;
        }


        private long GetResourceCurrent(string resourceId)
        {
            if (resources.TryGetValue(resourceId, out var r)) return r.current;
            return 0;
        }

        public bool CheckHasState(string attrId)
        {
            numerics.TryGetValue(attrId, out var e);
            return e.finalValue > 0;
        }
        #endregion
    }
}

