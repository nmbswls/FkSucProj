using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HighlightPlus;
using My;
using My.Map;
using My.Map.Entity;
using My.Map.Scene;
using My.MiniGame;
using My.UI;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public interface ISceneInteractable
{
    string ShowName { get; }

    Vector2 Pos { get; }

    //bool IsInteractFocus { get; set; }

    bool InteractFocused { get; set; }
    bool IsInteractDetail { get; set; }

    bool CanInteractEnable();

    bool TriggerInteract(int selectionId);

    Vector3 GetHintAnchorPosition();

    float GetHintOffsetInfos();

    List<SceneInteractSelection> GetInteractSelections();

    bool IsAutoInteract();
}

public class SceneInteractSelection
{
    public int SelectId;
    public string SelectContent;
    public bool Selectable = true;
}

public class SceneInteractSystem
{
    public static float CheckInterval = 0.1f;

    private float _normalCheckRadius = 0.4f;
    private float _checkAngle = 90f;

    private float _interactTimer = 0f;
    private float _executeCheckRadius = 3f;

    private float _maxCheckableRadius = 5.0f;

    public SceneInteractSystem()
    {
        hits = new Collider2D[16];
    }

    private Collider2D[] hits;
    public struct IntResultItem
    {
        public ISceneInteractable interactable;
        public float distance;
        public Vector2 pos;
    }
    private readonly List<IntResultItem> normalCandidates = new List<IntResultItem>(64);
    private readonly HashSet<ISceneInteractable> normalCandidatedSet = new HashSet<ISceneInteractable>();

    private List<IntResultItem> currInteractPoints = new();
    public List<long> closeUnitCache = new();
    //public ISceneInteractable? currnteractObj;

    private float _pauseInteractTimer;


    /// <summary>
    /// 当前可处决对象
    /// </summary>
    private SceneNpcPresenter? currExecuteTarget = null;
    private readonly List<SceneNpcPresenter> executeCandidates = new List<SceneNpcPresenter>(64);


    public void Tick(float dt)
    {
        TickNormalInteract(dt);  
    }

    public void SetInteractPause(float duration = 0.8f)
    {
        _pauseInteractTimer = LogicTime.time + duration;
    }

    public bool IsInteractEnableState()
    {
        var presenter = MainGameManager.Instance.playerScenePresenter;
        if (presenter == null || presenter.GetLogicEntity() == null)
        {
            return false;
        }

        if (MainGameManager.Instance.dialoguePlayer.IsPlaying)
        {
            return false;
        }
        if (MainGameManager.Instance.gameLogicManager.IsBalancing)
        {
            return false;
        }

        if(MainGameManager.Instance.gameLogicManager.MainStage != GameLogicManager.EMainGameStage.Running)
        {
            return false;
        }


        if(LootPointUIPanel.Instance != null)
        {
            return false;
        }
        if (DeepAbsorbPanel.Instance != null)
        {
            return false;
        }

        if(LogicTime.time < _pauseInteractTimer)
        {
            return false;
        }

        return true;
    }

    protected void TickNormalInteract(float dt)
    {
        _interactTimer -= dt;
        if (_interactTimer > 0)
        {
            return;
        }
        _interactTimer = CheckInterval;


        if(!IsInteractEnableState())
        {
            SceneInteractMenuPanel.Instance?.RefreshActiveInteractableObjs(new());
            SceneInteractMenuPanel.Instance?.RefreshExecuteTarget(null);
            currInteractPoints.Clear();
            currExecuteTarget = null;


            return;
        }

        var presenter = MainGameManager.Instance.playerScenePresenter;

        normalCandidates.Clear();
        normalCandidatedSet.Clear();
        executeCandidates.Clear();

        Vector2 center = presenter.transform.position;
        int count = Physics2D.OverlapCircleNonAlloc(center, _maxCheckableRadius, hits, 1 << LayerMask.NameToLayer("MapTarget"));

        // 遍历命中，筛选实现了接口的对象
        for (int i = 0; i < count; i++)
        {
            var col = hits[i];
            if (col == null) continue;
            // 在 Collider 或其父节点上寻找接口
            // 注意：GetComponentInParent 会产生少量 GC，若极致无 GC，可预缓存或自定义映射
            var interactable = col.GetComponentInParent<ISceneInteractable>();
            if (interactable == null) continue;

            if (normalCandidatedSet.Contains(interactable))
            {
                continue;
            }

            Vector2 diff = (Vector2)col.transform.position - center;
            var dist = diff.magnitude;

            bool canInt = false;
            if (dist < 0.3f)
            {
                canInt = true;
            }
            else
            {
                var angle = Vector2.Angle(diff, presenter.PlayerEntity.CurrentLook);
                if (angle < _checkAngle * 0.5f)
                {
                    canInt = true;
                }
            }

            // 角度不满足 一切交互都不进行
            if (!canInt)
            {
                continue;
            }

            // 优先判断处决
            do
            {
                if (interactable is not SceneNpcPresenter npcPresenter)
                {
                    break;
                }

                if(dist > _executeCheckRadius)
                {
                    break;
                }

                if(!npcPresenter.NpcEntity.CheckCanExecute())
                {
                    break;
                }

                executeCandidates.Add(npcPresenter);

                continue;
            }
            while (false);
            
            if (!interactable.CanInteractEnable())
            {
                continue;
            }

            var closest = col.ClosestPoint(center);
            var closestDist = (closest - center).magnitude;
            // 检查普通交互
            if (closestDist > _normalCheckRadius)
            {
                continue;
            }

            // 自动触发 直接自动触发
            if (interactable.IsAutoInteract())
            {
                Debug.Log("trigger auto interact.");
                interactable.TriggerInteract(1);
                continue;
            }

            normalCandidatedSet.Add(interactable);
            normalCandidates.Add(new IntResultItem
            {
                interactable = interactable,
                distance = dist,
                pos = col.transform.position,
            });
        }

        // 根据距离从近到远排序
        // 还要考虑角度权重？
        normalCandidates.Sort((a, b) => a.distance.CompareTo(b.distance));

        //executeCandidates.Sort((a, b) => {
        //    return 1;
        //});

        bool withExecute = false;
        if (executeCandidates.Count > 0)
        {
            withExecute = true;
            currExecuteTarget = executeCandidates.First();

        }
        else
        {
            currExecuteTarget = null;
        }


        bool allSame = true;

        if (currInteractPoints.Count == normalCandidates.Count)
        {
            for (int i = 0; i < currInteractPoints.Count; i++)
            {
                if (currInteractPoints[i].interactable != normalCandidates[i].interactable)
                {
                    allSame = false;
                }
            }
        }
        else
        {
            allSame = false;
        }

        if (!allSame)
        {
            currInteractPoints.Clear();
            foreach (var one in normalCandidates)
            {
                currInteractPoints.Add(one);
            }

            SceneInteractMenuPanel.Instance?.RefreshActiveInteractableObjs(currInteractPoints);
        }

        SceneInteractMenuPanel.Instance?.RefreshExecuteTarget(currExecuteTarget);
    }
    

    protected bool CheckSelfInteract()
    {
        return false;
    }

    public void UpdateNormalInteractRangeObjs()
    {
        
    }
}
