using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using My.UI;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public interface ISceneInteractable
{
    long Id { get; }

    string ShowName { get; }

    Vector2 Pos { get; }

    bool CanInteractEnable(float dist);
    void TriggerInteract(int selectionId);

    Vector3 GetHintAnchorPosition();

    List<SceneInteractSelection> GetInteractSelections(float dist);
}

public class SceneInteractSelection
{
    public int SelectId;
    public string SelectContent;
    public bool Selectable = true;
}

public class SceneInteractSystem
{

    private float _checkRadius = 3f;
    private float _checkAngle = 60f;

    private float _interactTimer = 0f;


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
    private readonly List<IntResultItem> candidates = new List<IntResultItem>(64);
    private List<IntResultItem> currInteractPoints = new();
    public List<long> closeUnitCache = new();
    //public ISceneInteractable? currnteractObj;

    public void Tick(float dt)
    {
        _interactTimer -= dt;
        if (_interactTimer > 0)
        {
            return;
        }

        _interactTimer = 0.2f;
        UpdateNormalInteractRangeObjs();

        bool allSame = true;

        if (currInteractPoints.Count == candidates.Count)
        {
            for(int i=0;i<currInteractPoints.Count;i++)
            {
                if (currInteractPoints[i].interactable != candidates[i].interactable)
                {
                    allSame = false;
                }
            }
        }
        else
        {
            allSame = false;
        }

        if(allSame)
        {
            return;
        }
        currInteractPoints.Clear();
        foreach(var one in candidates)
        {
            currInteractPoints.Add(one);
        }

        SceneInteractMenuPanel.Instance?.RefreshInteractObjs(currInteractPoints); 
    }


    public void UpdateNormalInteractRangeObjs()
    {
        candidates.Clear();

        var presenter = MainGameManager.Instance.playerScenePresenter;
        if (presenter == null || presenter.GetLogicEntity() == null)
        {
            return;
        }

        if(MainGameManager.Instance.dialoguePlayer.IsPlaying)
        {
            return;
        }
        if (MainGameManager.Instance.gameLogicManager.IsBalancing)
        {
            return;
        }

        Vector2 center = presenter.transform.position;
        int count = Physics2D.OverlapCircleNonAlloc(center, _checkRadius, hits, 1 << LayerMask.NameToLayer("MapTarget"));

        // 遍历命中，筛选实现了接口的对象
        for (int i = 0; i < count; i++)
        {
            var col = hits[i];
            if (col == null) continue;

            // 在 Collider 或其父节点上寻找接口
            // 注意：GetComponentInParent 会产生少量 GC，若极致无 GC，可预缓存或自定义映射
            var interactable = col.GetComponentInParent<ISceneInteractable>();
            if (interactable == null) continue;

            Vector2 diff = (Vector2)col.transform.position - center;
            var dist = diff.magnitude;

            bool canInt = false;
            if (dist < 0.3f)
            {
                canInt = true;
            }
            else
            {
                var angle = Vector2.Angle(diff, presenter.PlayerEntity.FaceDir);
                if (angle < _checkAngle * 0.5f)
                {
                    canInt = true;
                }
            }

            if (!canInt)
            {
                continue;
            }

            //// 计算距离（以角色位置 center 为基准）
            //// 距离点可以用碰撞体最近点，能更准确反映“与角色的最短距离”
            //Vector2 nearest = col.ClosestPoint(center);
            //float dist = (nearest - center).sqrMagnitude;

            if (!interactable.CanInteractEnable(dist))
            {
                continue;
            }

            candidates.Add(new IntResultItem
            {
                interactable = interactable,
                distance = dist,
                pos = col.transform.position,
            });
        }

        // 根据距离从近到远排序
        candidates.Sort((a, b) => a.distance.CompareTo(b.distance));
    }
    
}
