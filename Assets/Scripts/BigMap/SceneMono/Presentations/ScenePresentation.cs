using Animancer;
using Map.Entity;
using Map.Logic;
using My.Map;
using My.Map.Entity;
using My.UI;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace My
{
    public interface IScenePresentation
    {
        long Id { get; }
        void Bind(ILogicEntity logic);
        void Unbind();
        void ApplyState(object state);
        void SetVisible(bool visible);
        void Tick(float dt);

        ILogicEntity GetLogicEntity();

        Vector3 GetWorldPosition();

        bool CheckValid();

        void SetFadeAlpha(float fadeAlpha);

        Transform PivotHeader { get; }
    }

    public interface ISceneTargettable
    {
        Collider2D GetTargetCol();
    }

    public abstract class ScenePresentationBase<TLogic> : MonoBehaviour, IScenePresentation
        where TLogic : class, ILogicEntity
    {
        public long Id => _logic?.Id ?? 0;
        protected TLogic _logic;

        [Header("组件")]
        public Transform MainViewRt;
        [SerializeField]
        protected SpriteRenderer _shadowView;
        [SerializeField]
        protected SpriteRenderer[] _mainSpriteArr;
        private MaterialPropertyBlock _mpb;
        private static readonly int FadeProp = Shader.PropertyToID("_Fade"); // 缓存属性ID，性能更好

        protected AnimancerComponent _Animancer;

        private bool _visible;


        [Header("通用pivot")]
        [SerializeField] private Transform pivotHeader;
        public Transform PivotHeader { get { return pivotHeader; } }

        protected virtual void Awake()
        {
            //if(MainViewRt != null)
            //{
            //    _mainSpriteArr = MainViewRt.GetComponentsInChildren<SpriteRenderer>();
            //}

            _Animancer = GetComponentInChildren<AnimancerComponent>();

            // 2. 初始化属性块
            _mpb = new MaterialPropertyBlock();
        }

        [ContextMenu("Auto Collect Child Sprites")]
        private void CollectSprites()
        {
            _mainSpriteArr = MainViewRt.GetComponentsInChildren<SpriteRenderer>(true);

            Debug.Log($"已收集 {_mainSpriteArr.Length} 个 SpriteRenderer");

            // 标记对象已修改，确保 Unity 保存这个列表，否则重启后会丢失
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }


        public void Update()
        {
            if (_logic == null) return;

            Tick(LogicTime.deltaTime);
        }

        public virtual void Bind(ILogicEntity logic)
        {
            _logic = logic as TLogic;

            // 初始状态可能需要主动拉取或由逻辑层在 Bind 后立即推送

            transform.localPosition = MainGameManager.Instance.GetWorldPosFromLogicPos(_logic.Pos);

            RegisterEvents();

            // vieweventbus
            //SceneSmallIconLayerPanel.Instance?.OnScenePresentationBinded(this);
        }

        public virtual void Unbind()
        {
            SceneSmallIconLayerPanel.Instance?.OnScenePresentationUbbind(this);

            UnregisterEvents();

            _logic = null;
        }

        protected virtual void RegisterEvents()
        {
            _logic.EventOnEntityMove += OnEntityMove;
            _logic.EventOnDestroyed += OnEventEntityDestroyed;

        }
        protected virtual void UnregisterEvents()
        {
            _logic.EventOnEntityMove -= OnEntityMove;
            _logic.EventOnDestroyed -= OnEventEntityDestroyed;
        }


        protected virtual void OnEventEntityDestroyed(long entityId)
        {

        }

        public virtual void ApplyState(object state) { }

        protected virtual void OnLogicStateChanged(object payload) => ApplyState(payload);

        protected virtual void OnLogicAOIExit(object _)
        {
            SetVisible(false);
        }

        public virtual void SetVisible(bool visible)
        {
            _visible = visible;
            gameObject.SetActive(visible);
        }

        public virtual void Tick(float dt)
        {
            RefreshFadeState();
        }

        public void OnEntityMove(long entityId, Vector2 oldPos, Vector2 newPos)
        {
            transform.position = newPos;
            SceneAOIManager.Instance.MoveEntity(_logic, oldPos, newPos);
        }

        public ILogicEntity GetLogicEntity()
        {
            return _logic;
        }

        public Vector3 GetWorldPosition()
        {
            return transform.position;
        }

        protected virtual void LateUpdate()
        {
        }

        public bool CheckValid()
        {
            return _logic != null;
        }

        public virtual Collider2D GetMainCol()
        {
            return null;
        }

        protected float _targetFadeAlpha = 1;
        protected float _currFadeAlpha = 1;
        public void SetFadeAlpha(float alpha)
        {
            _targetFadeAlpha = alpha;
        }

        protected void RefreshFadeState()
        {
            if (Math.Abs(_targetFadeAlpha - _currFadeAlpha) > 1e-2)
            {
                _currFadeAlpha = Mathf.Lerp(_currFadeAlpha, _targetFadeAlpha, 2 * LogicTime.deltaTime);
                OnFadeStateUpdate();
            }
        }

        protected virtual void OnFadeStateUpdate()
        {
            // 优化：所有部件共用同一个值，所以只要设置一次 MPB
            _mpb.SetFloat(FadeProp, 1 - _currFadeAlpha);

            // 3. 遍历应用
            for (int i = 0; i < _mainSpriteArr.Length; i++)
            {
                // 获取当前可能已有的属性（防止覆盖其他属性）
                _mainSpriteArr[i].GetPropertyBlock(_mpb);
                // 更新 Fade 值
                _mpb.SetFloat(FadeProp, 1 - _currFadeAlpha);
                // 应用回去
                _mainSpriteArr[i].SetPropertyBlock(_mpb);
            }
        }

        #region 头顶气泡

        #endregion
        public void AddHeadTalkBubble(string content, float duration)
        {

        }
    }

}


