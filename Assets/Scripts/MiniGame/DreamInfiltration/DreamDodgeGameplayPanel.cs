using System.Collections;
using System.Collections.Generic;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.MiniGame.Dream
{
    public class DreamDodgeGameplayPanel : PanelWithInput
    {
        [Header("Prefab layout（可空，则按子节点名解析 / 再退回代码生成）")]
        [SerializeField] private RectTransform dimOverlay;
        [SerializeField] private RectTransform playAreaLayout;
        [SerializeField] private RectTransform playerMarker;
        [SerializeField] private TextMeshProUGUI hudText;

        private RectTransform _rootRt;
        private RectTransform _playArea;
        private RectTransform _playerRt;
        private TextMeshProUGUI _hudTmp;
        private DreamGameplayContext _ctx;

        private readonly List<DreamSkyEntity> _entities = new();
        private float _spawnBadTimer;
        private float _spawnGoodTimer;
        private int _hp;
        private int _force;
        private int _soothe;
        private int _trick;
        private bool _frozen;
        private bool _ended;
        private bool _layoutBuilt;

        public override int FocusPriority => 830;

        private sealed class DreamSkyEntity
        {
            public RectTransform Rt;
            public Vector2 Velocity;
            public bool IsBad;
            public DreamTendencyKind PickupKind;
        }

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            _rootRt = GetComponent<RectTransform>();
            layer = UILayer.Overlay;
            ResolveLayoutOrBuild();
        }

        private void ResolveLayoutOrBuild()
        {
            if (_layoutBuilt) return;

            _playArea = playAreaLayout != null ? playAreaLayout : _rootRt.Find("PlayArea") as RectTransform;
            _playerRt = playerMarker != null ? playerMarker : _playArea != null ? _playArea.Find("Player") as RectTransform : null;
            var hudTr = _rootRt.Find("Hud");
            _hudTmp = hudText != null ? hudText : hudTr != null ? hudTr.GetComponent<TextMeshProUGUI>() : null;


            if (_playArea != null && _playerRt != null && _hudTmp != null)
            {
                _layoutBuilt = true;
                var dimRt = dimOverlay != null ? dimOverlay : _rootRt.Find("Dim") as RectTransform;
                if (dimRt != null) DreamUISpriteUtil.EnsureWhiteSprite(dimRt.GetComponent<Image>());
                DreamUISpriteUtil.EnsureWhiteSprite(_playArea.GetComponent<Image>());
                DreamUISpriteUtil.EnsureWhiteSprite(_playerRt.GetComponent<Image>());
                return;
            }

            BuildLayoutIfNeeded();
        }

        private void BuildLayoutIfNeeded()
        {
            if (_layoutBuilt) return;
            _layoutBuilt = true;

            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(_rootRt, false);
            var dimRt = (RectTransform)dim.transform;
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().sprite = DreamUISpriteUtil.WhiteSprite();
            dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

            _playArea = new GameObject("PlayArea", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _playArea.SetParent(_rootRt, false);
            _playArea.anchorMin = _playArea.anchorMax = new Vector2(0.5f, 0.5f);
            _playArea.sizeDelta = new Vector2(560f, 320f);
            _playArea.anchoredPosition = Vector2.zero;
            var paImg = _playArea.GetComponent<Image>();
            paImg.sprite = DreamUISpriteUtil.WhiteSprite();
            paImg.color = new Color(0.12f, 0.1f, 0.18f, 1f);

            _playerRt = new GameObject("Player", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _playerRt.SetParent(_playArea, false);
            _playerRt.anchorMin = _playerRt.anchorMax = new Vector2(0.5f, 0.5f);
            _playerRt.sizeDelta = new Vector2(28f, 28f);
            _playerRt.anchoredPosition = Vector2.zero;
            var pImg = _playerRt.GetComponent<Image>();
            pImg.sprite = DreamUISpriteUtil.WhiteSprite();
            pImg.color = new Color(0.45f, 0.95f, 0.55f, 1f);

            var hudGo = new GameObject("Hud", typeof(RectTransform), typeof(TextMeshProUGUI));
            hudGo.transform.SetParent(_rootRt, false);
            var hudRt = (RectTransform)hudGo.transform;
            hudRt.anchorMin = new Vector2(0f, 1f);
            hudRt.anchorMax = new Vector2(1f, 1f);
            hudRt.pivot = new Vector2(0.5f, 1f);
            hudRt.anchoredPosition = new Vector2(0f, -8f);
            hudRt.sizeDelta = new Vector2(-40f, 72f);
            _hudTmp = hudGo.GetComponent<TextMeshProUGUI>();
            _hudTmp.fontSize = 18;
            _hudTmp.alignment = TextAlignmentOptions.Top;
            _hudTmp.color = new Color(0.9f, 0.9f, 0.95f);
        }

        public override void Setup(object data = null)
        {
            _ctx = data as DreamGameplayContext ?? new DreamGameplayContext();
            _hp = _ctx.MaxHp;
            _force = _soothe = _trick = 0;
            _frozen = false;
            _ended = false;
            _spawnBadTimer = 0.35f;
            _spawnGoodTimer = 1.1f;
            ClearEntities();
            if (_playerRt != null) _playerRt.anchoredPosition = Vector2.zero;
            RefreshHud();
        }

        public override void Show()
        {
            base.Show();
            _frozen = false;
            _ended = false;
        }

        private void Update()
        {
            if (!IsVisible || _frozen || _ended) return;

            var half = GetPlayAreaHalfExtents();
            var p = _playerRt.anchoredPosition;
            var move = new Vector2(UnityEngine.Input.GetAxisRaw("Horizontal"), UnityEngine.Input.GetAxisRaw("Vertical"));
            if (move.sqrMagnitude > 1f) move.Normalize();
            p += move * (_ctx.PlayerMoveSpeed * Time.deltaTime);
            p.x = Mathf.Clamp(p.x, -half.x + 20f, half.x - 20f);
            p.y = Mathf.Clamp(p.y, -half.y + 20f, half.y - 20f);
            _playerRt.anchoredPosition = p;

            _spawnBadTimer -= Time.deltaTime;
            if (_spawnBadTimer <= 0f)
            {
                SpawnBad();
                _spawnBadTimer = Random.Range(0.35f, 0.75f);
            }

            _spawnGoodTimer -= Time.deltaTime;
            if (_spawnGoodTimer <= 0f)
            {
                SpawnGood();
                _spawnGoodTimer = Random.Range(1.0f, 2.2f);
            }

            TickEntities(p);
            RefreshHud();
            CheckWinLose();
        }

        private Vector2 GetPlayAreaHalfExtents()
        {
            if (_playArea == null) return Vector2.zero;
            var r = _playArea.rect;
            if (r.width > 2f && r.height > 2f)
                return new Vector2(r.width * 0.5f, r.height * 0.5f);
            var sd = _playArea.sizeDelta;
            return new Vector2(Mathf.Abs(sd.x) * 0.5f, Mathf.Abs(sd.y) * 0.5f);
        }

        private static Vector2 GetEntityHalfExtents(RectTransform rt)
        {
            if (rt == null) return Vector2.one * 8f;
            var s = rt.rect.size;
            return new Vector2(Mathf.Max(s.x * 0.5f, 0.5f), Mathf.Max(s.y * 0.5f, 0.5f));
        }

        private static bool IsEntityOutsidePlayArea(Vector2 center, Vector2 playHalf, Vector2 entityHalf)
        {
            return center.x - entityHalf.x > playHalf.x
                || center.x + entityHalf.x < -playHalf.x
                || center.y - entityHalf.y > playHalf.y
                || center.y + entityHalf.y < -playHalf.y;
        }

        private void SpawnBad()
        {
            var go = new GameObject("Bullet", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_playArea, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(16f, 16f);
            var edge = Random.Range(0, 4);
            var half = GetPlayAreaHalfExtents();
            if (edge == 0) rt.anchoredPosition = new Vector2(-half.x, Random.Range(-half.y, half.y));
            else if (edge == 1) rt.anchoredPosition = new Vector2(half.x, Random.Range(-half.y, half.y));
            else if (edge == 2) rt.anchoredPosition = new Vector2(Random.Range(-half.x, half.x), -half.y);
            else rt.anchoredPosition = new Vector2(Random.Range(-half.x, half.x), half.y);

            var img = go.GetComponent<Image>();
            img.sprite = DreamUISpriteUtil.WhiteSprite();
            img.color = new Color(0.95f, 0.25f, 0.2f, 1f);

            var toCenter = (-rt.anchoredPosition).normalized;
            var speed = Random.Range(90f, 170f);
            _entities.Add(new DreamSkyEntity
            {
                Rt = rt,
                Velocity = toCenter * speed,
                IsBad = true,
                PickupKind = DreamTendencyKind.Force,
            });
        }

        private void SpawnGood()
        {
            var go = new GameObject("Pickup", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_playArea, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(20f, 20f);
            var half = GetPlayAreaHalfExtents() - Vector2.one * 30f;
            half.x = Mathf.Max(8f, half.x);
            half.y = Mathf.Max(8f, half.y);
            rt.anchoredPosition = new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y));

            var kind = (DreamTendencyKind)Random.Range(0, 3);
            var img = go.GetComponent<Image>();
            img.sprite = DreamUISpriteUtil.WhiteSprite();
            img.color = kind switch
            {
                DreamTendencyKind.Force => new Color(1f, 0.45f, 0.2f, 1f),
                DreamTendencyKind.Soothing => new Color(0.45f, 0.75f, 1f, 1f),
                _ => new Color(0.85f, 0.55f, 1f, 1f),
            };

            _entities.Add(new DreamSkyEntity
            {
                Rt = rt,
                Velocity = Random.insideUnitCircle * 25f,
                IsBad = false,
                PickupKind = kind,
            });
        }

        private void TickEntities(Vector2 playerPos)
        {
            const float hitR = 22f;
            for (var i = _entities.Count - 1; i >= 0; i--)
            {
                var e = _entities[i];
                if (e.Rt == null)
                {
                    _entities.RemoveAt(i);
                    continue;
                }

                e.Rt.anchoredPosition += e.Velocity * Time.deltaTime;
                var d = e.Rt.anchoredPosition - playerPos;
                if (d.sqrMagnitude < hitR * hitR)
                {
                    if (e.IsBad)
                    {
                        _hp -= _ctx.BulletDamage;
                        Destroy(e.Rt.gameObject);
                    }
                    else
                    {
                        switch (e.PickupKind)
                        {
                            case DreamTendencyKind.Force:
                                _force += _ctx.PickupScore;
                                break;
                            case DreamTendencyKind.Soothing:
                                _soothe += _ctx.PickupScore;
                                break;
                            default:
                                _trick += _ctx.PickupScore;
                                break;
                        }

                        Destroy(e.Rt.gameObject);
                    }

                    _entities.RemoveAt(i);
                    continue;
                }

                var playHalf = GetPlayAreaHalfExtents();
                var pos = e.Rt.anchoredPosition;
                var eh = GetEntityHalfExtents(e.Rt);
                if (IsEntityOutsidePlayArea(pos, playHalf, eh))
                {
                    Destroy(e.Rt.gameObject);
                    _entities.RemoveAt(i);
                }
            }
        }

        private void CheckWinLose()
        {
            if (_ended) return;
            if (_hp <= 0)
            {
                _ended = true;
                StartCoroutine(EndSequence(false));
                return;
            }

            var t = _ctx.VictoryScoreThreshold;
            if (_force >= t || _soothe >= t || _trick >= t)
            {
                _ended = true;
                StartCoroutine(EndSequence(true));
            }
        }

        private IEnumerator EndSequence(bool won)
        {
            _frozen = true;
            ClearEntities();
            if (_playArea != null)
            {
                var flash = new GameObject("VictoryFlash", typeof(RectTransform), typeof(Image));
                flash.transform.SetParent(_playArea, false);
                var frt = (RectTransform)flash.transform;
                frt.anchorMin = Vector2.zero;
                frt.anchorMax = Vector2.one;
                frt.offsetMin = Vector2.zero;
                frt.offsetMax = Vector2.zero;
                var fimg = flash.GetComponent<Image>();
                fimg.sprite = DreamUISpriteUtil.WhiteSprite();
                fimg.color = won ? new Color(1f, 1f, 1f, 0f) : new Color(0.2f, 0.05f, 0.05f, 0f);
                for (var i = 0; i < 12; i++)
                {
                    var a = i / 12f;
                    fimg.color = won
                        ? new Color(1f, 1f, 1f, Mathf.Lerp(0f, 0.35f, a))
                        : new Color(0.4f, 0.05f, 0.05f, Mathf.Lerp(0f, 0.45f, a));
                    yield return null;
                }

                for (var i = 0; i < 18; i++) yield return null;
                Destroy(flash);
            }

            UIManager.Instance?.HidePanel(DreamInfiltrationIds.GameplayPanel);
            var payload = new DreamSettlementPayload
            {
                Won = won,
                ThemeDisplayName = _ctx.ThemeDisplayName,
                ForceScore = _force,
                SoothingScore = _soothe,
                TrickScore = _trick,
            };
            UIManager.Instance?.ShowPanel(DreamInfiltrationIds.SettlementPanel, payload, UILayer.Overlay);
        }

        private void ClearEntities()
        {
            foreach (var e in _entities)
            {
                if (e.Rt != null) Destroy(e.Rt.gameObject);
            }

            _entities.Clear();
        }

        private void RefreshHud()
        {
            if (_hudTmp == null) return;
            _hudTmp.text =
                $"{_ctx.ThemeDisplayName}  |  HP {_hp}/{_ctx.MaxHp}\n" +
                $"暴力 {_force}  安抚 {_soothe}  计谋 {_trick}  / 目标 {_ctx.VictoryScoreThreshold}\n" +
                "WASD 移动  |  躲红块 吃彩块";
        }

        public override void Teardown()
        {
            ClearEntities();
            base.Teardown();
        }

        public override bool OnCancel()
        {
            if (_ended) return true;
            _ended = true;
            UIManager.Instance?.HidePanel(DreamInfiltrationIds.GameplayPanel);
            UIManager.Instance?.ShowPanel(DreamInfiltrationIds.EntryPanel, null, UILayer.Overlay);
            return true;
        }

        public override bool CapturesNavigateAxisForWorld => true;
        public override bool OnNavigate(Vector2 dir)
        {
            return true;
        }
    }

    // 与玩法面板同文件，避免仅新增小 .cs 时 Unity 偶发未编译导致类型找不到
    public sealed class DreamGameplayContext
    {
        public string ThemeId = "";
        public string ThemeDisplayName = "";
        public int VictoryScoreThreshold = 100;
        public float PlayerMoveSpeed = 220f;
        public int MaxHp = 100;
        public int BulletDamage = 15;
        public int PickupScore = 12;
    }
}
