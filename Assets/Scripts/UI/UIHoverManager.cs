

using My.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.UI
{
    [Serializable]
    public class HoverTipParams
    {
        public EHoverTipType TipType;
        public Vector3 BindPos;

        public int Param1;
    }

    public interface IHoverInfoProvider
    {
        // 返回用于显示的文本，如果不可提示则返回 null 或空串
        //void OnRefreshTipInfo(TooltipController controller);
        // 可选：返回用于锚点的屏幕坐标（默认使用鼠标位置或命中点）
        Vector2? GetCustomScreenPos(Camera uiCamera);

        RectTransform GetHoverUIRange();
        Vector2 TooltipPosition { get; }

        HoverTipParams? GetSimpleTipInfo();


        void OnEnterHovered();

        void OnLeaveHovered();
    }

    /// <summary>
    /// hover tip类型
    /// </summary>
    public enum EHoverTipType
    {
        Invalid,
        Main3Ball,
        PlayerBuff,
        Item,
        Talent,
    }


    public interface IHoverTipPanel
    {
        void OnHoverTipUpdate(HoverTipParams tipParams, IHoverInfoProvider provider);
    }

    [Serializable]
    public class UIHoverTipDesc
    {
        public EHoverTipType TipType;
        public string TipPanelName;
    }

    public class UIHoverManager : MonoBehaviour
    {
        // 必需引用
        //public TooltipController tooltip;
        public GraphicRaycaster raycaster;          // Canvas 的 GraphicRaycaster
        public Camera mainCamera;                   // 

        public bool With3D = false;

        // 简单开关
        public bool updateOnlyOnMouseMove = true;   // 仅鼠标移动时更新（默认省性能）

        private EventSystem _eventSystem;
        private PointerEventData _ped;
        private Vector2 _lastMouse;

        private IHoverInfoProvider currHoverOne;
        private IHoverInfoProvider? prevHoverOne;

        public List<UIHoverTipDesc> HoverDescList = new();

        public class UIHoverTipRuntime
        {
            public EHoverTipType TipType;
            public bool IsVisible;
            public GameObject Root;
            public IHoverTipPanel TipPanel;
        }

        private Dictionary<EHoverTipType, UIHoverTipRuntime> _uiHoverTips = new();

        void Awake()
        {
            if (!mainCamera) mainCamera = Camera.main;
        }

        private void Start()
        {
            _eventSystem = EventSystem.current;
            _ped = new PointerEventData(_eventSystem);

            foreach(var desc in HoverDescList)
            {
                var runtimeState = new UIHoverTipRuntime();
                runtimeState.TipType = desc.TipType;
                runtimeState.Root = null;

                _uiHoverTips[runtimeState.TipType] = runtimeState;
            }
        }

        void Update()
        {
            Vector2 mouse = UnityEngine.Input.mousePosition;

            if (updateOnlyOnMouseMove && mouse == _lastMouse) return;
            _lastMouse = mouse;

            do
            {
                // 先查 UI
                if (TryGetUIHover(mouse, out currHoverOne, out Vector2 uiPos))
                {
                    break;
                }

                // 再查场景
                if (TryGetWorldHover(mouse, out currHoverOne, out Vector2 worldPos))
                {
                    break;
                }

            } while (false);

            bool hasTip = false;
            if (currHoverOne != null && (prevHoverOne == null || prevHoverOne != currHoverOne))
            {

                OnLeaveHover();

                prevHoverOne = currHoverOne; // 更新悬浮者
                                             //var tipInfo = currHoverOne.GetSimpleTipInfo();
                                             //if (tipInfo != null)
                                             //{
                                             //    tooltip.OnRefreshSimpleTipInfo(tipInfo);
                                             //}

                OnHoverOneUpdate(currHoverOne);
            }
            else if (currHoverOne == null && prevHoverOne != null)
            {
                OnLeaveHover();
            }
        }


        private bool TryGetUIHover(Vector2 mouseScreen, out IHoverInfoProvider tipProvider, out Vector2 screenPos)
        {
            tipProvider = null;
            screenPos = mouseScreen;
            if (raycaster == null || _eventSystem == null) return false;

            _ped.position = mouseScreen;
            var results = new List<RaycastResult>();
            raycaster.Raycast(_ped, results);

            // 结果已按绘制深度排序，取第一个带 ITipProvider 的
            foreach (var r in results)
            {
                var tip = r.gameObject.GetComponentInParent<IHoverInfoProvider>();
                if (tip == null)
                {
                    continue;
                }

                if (tip.GetSimpleTipInfo() == null)
                {
                    continue;
                }

                tipProvider = tip;

                Vector2? custom = tip.GetCustomScreenPos(mainCamera);
                screenPos = custom ?? mouseScreen;
                return true;
            }
            return false;
        }

        private bool TryGetWorldHover(Vector2 mouseScreen, out IHoverInfoProvider tipProvider, out Vector2 screenPos)
        {
            tipProvider = null;
            screenPos = mouseScreen;
            if (mainCamera == null) return false;

            Ray ray = mainCamera.ScreenPointToRay(mouseScreen);

            // 先 3D
            if (With3D)
            {
                if (Physics.Raycast(ray, out var hit3D))
                {
                    var tip = hit3D.collider.GetComponentInParent<IHoverInfoProvider>();
                    if (tip != null)
                    {
                        tipProvider = tip;

                        Vector2? custom = tip.GetCustomScreenPos(mainCamera);
                        screenPos = custom ?? RectTransformUtility.WorldToScreenPoint(mainCamera, hit3D.point);
                        return true;
                    }
                }
            }


            // 再 2D（可选，不命中 3D 时尝试）
            RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray);
            if (hit2D.collider != null)
            {
                var tip = hit2D.collider.GetComponentInParent<IHoverInfoProvider>();
                if (tip != null)
                {
                    tipProvider = tip;
                    Vector2? custom = tip.GetCustomScreenPos(mainCamera);
                    screenPos = custom ?? RectTransformUtility.WorldToScreenPoint(mainCamera, hit2D.point);
                    return true;
                }
            }

            return false;
        }

        public void OnHoverOneUpdate(IHoverInfoProvider newHovered)
        {
            newHovered.OnEnterHovered();

            var tipInfo = newHovered.GetSimpleTipInfo();
            if (tipInfo != null)
            {
                RequestShowTip(tipInfo, newHovered);
            }
        }

        public void OnLeaveHover()
        {
            if (prevHoverOne != null)
            {
                prevHoverOne.OnLeaveHovered();
                var tipInfo = prevHoverOne.GetSimpleTipInfo();
                if (tipInfo != null)
                {
                    RequestHideTip(tipInfo);
                }

            }
            prevHoverOne = null;

        }

        private int showRequestVersion;
        private int hideRequestVersion;

        private float hideDelayMs = 100;
        private float showDelayMs = 100;

        // 对外接口：请求显示
        public void RequestShowTip(HoverTipParams tipParams, IHoverInfoProvider provider)
        {
            // 新的显示请求到来，提升版本号并取消隐藏协程的效力
            int myVersion = ++showRequestVersion;
            hideRequestVersion++; // 使当前正在等待隐藏的协程失效

            StopCoroutineSafe(nameof(ShowDelayRoutine));
            StopCoroutineSafe(nameof(HideDelayRoutine));
            StartCoroutine(ShowDelayRoutine(tipParams, provider, myVersion));
        }

        public void RequestHideTip(HoverTipParams tipParams)
        {
            // 新的隐藏请求到来，提升版本号并取消显示协程的效力
            int myVersion = ++hideRequestVersion;
            showRequestVersion++; // 使当前正在等待显示的协程失效

            StopCoroutineSafe(nameof(ShowDelayRoutine));
            StopCoroutineSafe(nameof(HideDelayRoutine));
            StartCoroutine(HideDelayRoutine(tipParams, myVersion));
        }

        private IEnumerator ShowDelayRoutine(HoverTipParams tipParams, IHoverInfoProvider provider, int version)
        {
            // 延迟等待
            if (!_uiHoverTips.TryGetValue(tipParams.TipType, out var hoverTip))
            {
                yield break;
            }

            if(hoverTip.Root == null)
            {

                var p = HoverDescList.Find(item=>item.TipType == tipParams.TipType);
                if (p == null || string.IsNullOrEmpty(p.TipPanelName))
                {
                    Debug.LogError($"UIHoverManager: no HoverDescList entry for {tipParams.TipType}");
                    yield break;
                }

                var request = Resources.LoadAsync<GameObject>($"UI/Prefabs/Tips/{p.TipPanelName}");
                while(!request.isDone)
                {
                    yield return null;
                }

                var a = GameObject.InstantiateAsync<GameObject>((GameObject)request.asset, UIManager.Instance.GetLayerRoot(UILayer.Overlay));
                while(!a.isDone)
                {
                    yield return null;
                }

                hoverTip.Root = a.Result[0];
                hoverTip.TipPanel = hoverTip.Root.GetComponent<IHoverTipPanel>();
                if (hoverTip.TipPanel == null)
                {
                    Debug.LogError($"UIHoverManager: tip prefab '{p.TipPanelName}' has no IHoverTipPanel on root.");
                }
            }

            if (hoverTip.Root == null)
            {
                Debug.LogError("resource not found");
                yield break;
            }

            float wait = showDelayMs / 1000f;
            float t = 0f;

            while (t < wait)
            {
                // 若有新的请求使本协程失效，立即退出
                if (version != showRequestVersion) yield break;
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            // 再次确认仍然是最新的显示请求
            if (version != showRequestVersion) yield break;

            // 真正显示（你已有的显示实现可以放在这里）
            SetTipVisible(tipParams, provider, true);
        }

        private IEnumerator HideDelayRoutine(HoverTipParams tipParams, int version)
        {
            float wait = hideDelayMs / 1000f;
            float t = 0f;
            while (t < wait)
            {
                // 若有新的请求使本协程失效（例如又要显示），立即退出
                if (version != hideRequestVersion) yield break;
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            // 仍是最新隐藏请求
            if (version != hideRequestVersion) yield break;

            SetTipVisible(tipParams, null, false);
        }

        private void SetTipVisible(HoverTipParams tipParams, IHoverInfoProvider provider, bool visible)
        {
            if (!_uiHoverTips.TryGetValue(tipParams.TipType, out var hoverTip))
            {
                return;
            }
            hoverTip.IsVisible = visible;

            if (hoverTip.TipPanel != null)
            {
                hoverTip.Root.SetActive(visible);

                if (visible)
                {
                    hoverTip.TipPanel.OnHoverTipUpdate(tipParams, provider);
                }
            }
        }


        private void StopCoroutineSafe(string routineName)
        {
            // 避免 StopCoroutine(null) 报错；可选
            try { StopCoroutine(routineName); } catch { }
        }
    }

}

