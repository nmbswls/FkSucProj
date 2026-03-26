using Map.Logic;
using My.Map.Drop;
using My.Map.Logic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;
using static UnityEditor.Progress;





namespace My.Map.Scene
{
    
    public class MapSceneDropManager : MonoBehaviour
    {

        public UniformGridIndex<long> GridIndex;


        // 可交互物 和 普通拾取是否共享
        private Dictionary<long, MapSceneDropInteractable> _spawnedInteractObjs = new Dictionary<long, MapSceneDropInteractable>();
        private Queue<MapSceneDropInteractable> _innerPool = new();
        public GameObject interactablePrefab;



        public GameObject autoPickPrefab;

        public void Start()
        {
            GridIndex = new(2);
        }


        public void OnGameInit()
        {
            MainGameManager.Instance.gameLogicManager.globalDropCollection.EvOnDropAdd += OnDropCreate;
            MainGameManager.Instance.gameLogicManager.globalDropCollection.EvOnDropRemove += OnDropRemoved;
        }


        public void Update()
        {
            CheckInteractWithDrops();

            TickRecycleOutInteract();

            TickRecycleDrops();
        }

        private List<long> cacheList = new();

        public float activateDistance = 16f;    // 距离阈值（米）

        private float _checkInteractTimer;
        public void CheckInteractWithDrops()
        {
            if (MainGameManager.Instance.playerScenePresenter == null)
            {
                return;
            }

            _checkInteractTimer -= Time.deltaTime;
            if(_checkInteractTimer > 0)
            {
                return;
            }

            _checkInteractTimer = 0.3f;
            if(_spawnedInteractObjs.Count > 100)
            {
                Debug.Log("Too Much Interact Drops.");
                return;
            }

            cacheList.Clear();
            Vector2 playerPos = MainGameManager.Instance.playerScenePresenter.GetWorldPosition();
            GridIndex.Query(playerPos, activateDistance * 1.1f, cacheList);

            int autoPicked = 0;
            foreach ( var item in cacheList)
            {
                var dropData = MainGameManager.Instance.gameLogicManager.globalDropCollection.FindDrop(item);
                if (dropData == null)
                {
                    continue;
                }
                var dropPos = dropData.Position; 
                float distSqr = (dropPos - playerPos).sqrMagnitude;

                if (!_spawnedInteractObjs.TryGetValue(item, out var obj))
                {
                    if (distSqr <= activateDistance * activateDistance)
                    {
                        // 生成交互物
                        obj = SpawnInteractable(dropData, null, dropData.AutoPick);
                        _spawnedInteractObjs[item] = obj;
                    }
                }

                if (dropData.AutoPick && !obj.IsFlying && LogicTime.time - dropData.CreateTime > 0.5f && distSqr < 1.0)
                {
                    obj.flyToPlayerMover.Init(MainGameManager.Instance.playerScenePresenter.transform, onArrived: () =>
                        {
                            Debug.Log("触发拾取道具添加");

                            MainGameManager.Instance.gameLogicManager.globalDropCollection.PickDrop(obj.DropData.Id);
                        });
                    //var go = Instantiate(autoPickPrefab, dropPos, Quaternion.identity);
                    //var mover = go.GetComponent<FlyToPlayerMover>();
                    //if (mover != null)
                    //{
                    //    mover.Init(MainGameManager.Instance.playerScenePresenter.transform, onArrived: () =>
                    //    {
                    //        Debug.Log("触发拾取道具添加");
                    //        GameObject.Destroy(go);
                    //    });

                    //    // 刷新显示的 sprite（从材质或你自己的配置来源）
                    //    var sr = go.GetComponentInChildren<SpriteRenderer>();
                    //    if (sr != null)
                    //    {
                    //        //sr.sprite = ParticleLayer.TryGetFirstSpriteFromTSA();
                    //    }
                    //}

                    //// 可选：生成后移除粒子避免重复
                    //OnDropRemoved(item);
                    ////spawned++;
                    ////if (spawned >= maxSpawnPerScan) break;
                }
                // 非自动拾取 生成交互物
                else
                {
                    
                }
            }

        }

        

        private float _checkInteractRecycleTimer;

        public void TickRecycleOutInteract()
        {
            if (MainGameManager.Instance.playerScenePresenter == null)
            {
                return;
            }

            _checkInteractRecycleTimer -= Time.deltaTime;
            if (_checkInteractRecycleTimer > 0)
            {
                return;
            }

            _checkInteractRecycleTimer = 1f;
            Vector2 playerPos = MainGameManager.Instance.playerScenePresenter.GetWorldPosition();
            foreach (var key in _spawnedInteractObjs.Keys.ToList())
            {
                var drop = MainGameManager.Instance.gameLogicManager.globalDropCollection.FindDrop(key);
                bool needRemove = false;
                if(drop == null)
                {
                    needRemove = true;
                }
                else
                {
                    var dropPos = drop.Position;
                    var o = _spawnedInteractObjs[key];
                    float distSqr = (dropPos - playerPos).sqrMagnitude;
                    if (distSqr > activateDistance * activateDistance)
                    {
                        _spawnedInteractObjs.Remove(key);

                        if (_innerPool.Count < 20)
                        {
                            o.DoRecycle();
                            _innerPool.Enqueue(o);
                        }
                        else
                        {
                            GameObject.Destroy(o.gameObject);
                        }
                    }
                }
            }
        }

        private MapSceneDropInteractable SpawnInteractable(DropData dropData, Vector3? srcPos, bool autoPick)
        {
            MapSceneDropInteractable interactObj = null;
            if (_innerPool.Count > 0)
            {
                interactObj = _innerPool.Dequeue();
                interactObj.gameObject.SetActive(true);
            }
            else
            {
                // 对象池可替换 Instantiate
                var go = Instantiate(interactablePrefab, dropData.Position, Quaternion.identity, transform);
                interactObj = go.GetComponent<MapSceneDropInteractable>();
                interactObj.gameObject.SetActive(true);
            }

            if(interactObj == null)
            {
                return null;
            }

            interactObj.InitFromDrop(dropData, srcPos, autoPick);
            return interactObj;
        }

        public void OnDropCreate(DropData newDrop, Vector2? srcPos)
        {
            Debug.Log("OnDropCreate " + newDrop.ItemId);
            //int particleIndex = ParticleLayer.EmitDrop(newDrop.Position, 0, 0.4f, Color.white);
            //DropParticleIndex[newDrop.Id] = particleIndex;
            GridIndex.AddOrMove(newDrop.Id, newDrop.Position);

            if (MainGameManager.Instance.playerScenePresenter == null) return;
            Vector2 playerPos = MainGameManager.Instance.playerScenePresenter.GetWorldPosition();
            if ((playerPos - srcPos.Value).sqrMagnitude < activateDistance * activateDistance)
            {
                if (_spawnedInteractObjs.ContainsKey(newDrop.Id)) return;

                // 生成交互物
                var go = SpawnInteractable(newDrop, srcPos, newDrop.AutoPick);
                _spawnedInteractObjs[newDrop.Id] = go;
            }

            
        }

        public void OnDropRemoved(long id)
        {
            //if(DropParticleIndex.TryGetValue(id, out var idx))
            //{
            //    //ParticleLayer.KillParticle(idx);
            //    DropParticleIndex.Remove(id);
            //}

            GridIndex.Remove(id);

            if(_spawnedInteractObjs.TryGetValue(id, out var spawnedInteract))
            {
                _spawnedInteractObjs.Remove(id);
                if(_innerPool.Count < 20)
                {
                    spawnedInteract.DoRecycle();
                    _innerPool.Enqueue(spawnedInteract);
                }
                    else
                {
                    GameObject.Destroy(spawnedInteract.gameObject);
                }
            }
        }

        private float _lastRecycleTime;
        private float _dropIt;
        public void TickRecycleDrops()
        {
            if (LogicTime.time < _lastRecycleTime + 30.0f)
            {
                return;
            }

            _lastRecycleTime = LogicTime.time;


        }
    }
}



