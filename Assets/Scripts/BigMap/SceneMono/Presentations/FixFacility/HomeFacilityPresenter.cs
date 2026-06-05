
using System.Collections.Generic;
using My.Map;
using My.Map.Entity;
using My.Map.Scene;
using UnityEngine;

namespace My
{
    public class HomeFacilityPresenter : ScenePresentationBase<HomeFacilityLogicEntity>, ISubInteractHolder
    {

        public HomeFacilityLogicEntity FacilityEntity { get { return (HomeFacilityLogicEntity)_logic; } }

        public Transform ViewRoot;
        [SerializeField]
        private SpriteRenderer[] _sprites;

        public SubInteractHandle[] Handles;

        private HomeFacility _homeFacility;
        private readonly List<HomeBgNpc> _bgWorkers = new();

        [SerializeField]
        private float workPhaseSeconds = 4f;

        [SerializeField]
        private float restPhaseSeconds = 3f;

        private Dictionary<int, float> _interactCdTimer = new();

        protected override void Awake()
        {
            base.Awake();

            if (Handles != null)
            {
                foreach (var handle in Handles)
                {
                    handle.Owner = this;
                }
            }
        }

        public override void Bind(ILogicEntity logic)
        {
            base.Bind(logic);
            _homeFacility = GetComponentInChildren<HomeFacility>(true);
            RefreshWorkforceVisuals();
        }

        public override void Unbind()
        {
            ReleaseWorkforceVisuals();
            _homeFacility = null;
            base.Unbind();
        }

        public void RefreshWorkforceVisuals()
        {
            ReleaseWorkforceVisuals();
            if (_logic == null || FacilityEntity?.InnerFacilityRef == null)
            {
                return;
            }

            var inst = FacilityEntity.InnerFacilityRef;
            int n = Mathf.Max(0, inst.ArrangePeopleNum);
            if (n == 0 || inst.CfgRef == null || !inst.CfgRef.SupportsWorkforceAssignment)
            {
                return;
            }

            if (_homeFacility == null)
            {
                _homeFacility = GetComponentInChildren<HomeFacility>(true);
            }

            var workSpots = new List<HomeActionSpot>();
            var restSpots = new List<HomeActionSpot>();
            if (_homeFacility != null)
            {
                foreach (var s in _homeFacility.GetComponentsInChildren<HomeActionSpot>(true))
                {
                    if (s.Type == HomeActionSpot.SpotType.Work)
                    {
                        workSpots.Add(s);
                    }
                    else if (s.Type == HomeActionSpot.SpotType.Social)
                    {
                        restSpots.Add(s);
                    }
                }
            }

            Transform parent = ViewRoot != null ? ViewRoot : transform;
            for (int i = 0; i < n; i++)
            {
                Vector3 wPos = ResolveSpotPos(workSpots, i);
                Vector3 rPos = restSpots.Count > 0
                    ? ResolveSpotPos(restSpots, i)
                    : wPos + new Vector3(0.6f, -0.2f, 0f);

                var npc = HomeBgNpcPool.Rent(i % HomeBgNpcPool.StyleCount, parent);
                if (npc == null)
                {
                    continue;
                }

                npc.transform.position = wPos;
                npc.BeginFacilityWorkRestLoop(wPos, rPos, workPhaseSeconds, restPhaseSeconds);
                _bgWorkers.Add(npc);
            }
        }

        private Vector3 ResolveSpotPos(List<HomeActionSpot> spots, int workerIndex)
        {
            if (spots == null || spots.Count == 0)
            {
                return transform.position + new Vector3(0.35f * (workerIndex % 4), -0.15f * (workerIndex / 4), 0f);
            }

            var spot = spots[workerIndex % spots.Count];
            int slot = workerIndex / spots.Count;
            return spot.GetApproximateSlotWorldPosition(slot);
        }

        private void ReleaseWorkforceVisuals()
        {
            foreach (var npc in _bgWorkers)
            {
                HomeBgNpcPool.Return(npc);
            }

            _bgWorkers.Clear();
        }

        public bool CanSubInteractEnable(int subIdx)
        {
            _interactCdTimer.TryGetValue(subIdx, out var lastCd);
            if (lastCd != 0 && LogicTime.time - lastCd < 1.0f)
            {
                return false;
            }

            var innerCfg = FacilityEntity.InnerFacilityRef.CfgRef;
            if (innerCfg == null)
            {
                return false;
            }

            var func = innerCfg.SubFuncInfos.Find(item => item.SubHandleIdx == subIdx);
            if (func == null)
            {
                return false;
            }

            return true;
        }


        public List<SceneInteractSelection> GetSubInteractSelections(int subIdx)
        {
            var ret = new List<SceneInteractSelection>();

            ret.Add(new SceneInteractSelection()
            {
                SelectId = 1,
                SelectContent = "查看",
            });

            return ret;
        }

        public bool SubTriggerInteract(int subIdx, int selectionId, int playerId)
        {
            MainGameManager.Instance.ShowMapSpeachBubble(MainGameManager.Instance.playerScenePresenter.Id, $"我是{FacilityEntity.InnerFacilityRef.Id}。", 1f);
            _interactCdTimer[subIdx] = LogicTime.time;
            return true;
        }
    }
}
