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
        [Header("Prefab layout（序列化引用，缺失则按子节点名解析）")]
        [SerializeField] private RectTransform dimOverlay;
        [SerializeField] private RectTransform playAreaLayout;
        [SerializeField] private RectTransform playerMarker;
        [SerializeField] private RectTransform coreMarker;
        [SerializeField] private TextMeshProUGUI hudText;

        private RectTransform _rootRt;
        private RectTransform _playArea;
        private RectTransform _playerRt;
        private RectTransform _coreRt;
        private Image _coreImg;
        private TextMeshProUGUI _hudTmp;
        private DreamGameplayContext _ctx;

        private readonly List<DreamSkyEntity> _entities = new();
        private float _spawnBadTimer;
        private float _spawnGoodTimer;
        private float _spawnAoeTimer;
        private int _hp;
        private int _coreHp;
        private int _forceDamage;
        private int _sootheDamage;
        private int _trickDamage;
        private bool _frozen;
        private bool _ended;
        private bool _layoutBuilt;
        private Vector2 _moveInput;

        public override int FocusPriority => 830;

        private enum EntityKind
        {
            BulletStraight,
            AoeWarning,
            PickupForce,
            PickupSoothing,
            PickupTrick,
            ArcProjectile,
        }

        private sealed class DreamSkyEntity
        {
            public RectTransform Rt;
            public Vector2 Velocity;
            public bool IsBad;
            public EntityKind Kind;
            public DreamTendencyKind PickupKind;
            // AOE 专用
            public float AoeTimer;
            public bool AoeActivated;
            public float AoeActiveTimer;
            public float AoeRadius;
            // 弧形子弹专用
            public Vector2 ArcP0;
            public Vector2 ArcP1;
            public Vector2 ArcP2;
            public float ArcT;
            public float ArcDuration;
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
            _playerRt = playerMarker != null ? playerMarker
                : _playArea != null ? _playArea.Find("Player") as RectTransform : null;
            _coreRt = coreMarker != null ? coreMarker
                : _playArea != null ? _playArea.Find("Core") as RectTransform : null;
            var hudTr = _rootRt.Find("Hud");
            _hudTmp = hudText != null ? hudText
                : hudTr != null ? hudTr.GetComponent<TextMeshProUGUI>() : null;

            if (_playArea != null && _playerRt != null && _hudTmp != null)
            {
                _layoutBuilt = true;
                var dimRt = dimOverlay != null ? dimOverlay : _rootRt.Find("Dim") as RectTransform;
                if (dimRt != null) DreamUISpriteUtil.EnsureWhiteSprite(dimRt.GetComponent<Image>());
                DreamUISpriteUtil.EnsureWhiteSprite(_playArea.GetComponent<Image>());
                DreamUISpriteUtil.EnsureWhiteSprite(_playerRt.GetComponent<Image>());
                if (_coreRt != null)
                {
                    _coreImg = _coreRt.GetComponent<Image>();
                    // 若 prefab 中已挂载了 sprite，保留；否则用白块兜底
                    if (_coreImg != null && _coreImg.sprite == null)
                        DreamUISpriteUtil.EnsureWhiteSprite(_coreImg);
                }
                return;
            }

            BuildFallbackLayout();
        }

        // 仅在 prefab 引用完全缺失时才走此分支
        private void BuildFallbackLayout()
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
            var dimImg = dim.GetComponent<Image>();
            dimImg.sprite = DreamUISpriteUtil.WhiteSprite();
            dimImg.color = new Color(0f, 0f, 0f, 0.45f);

            _playArea = new GameObject("PlayArea", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _playArea.SetParent(_rootRt, false);
            _playArea.anchorMin = _playArea.anchorMax = new Vector2(0.5f, 0.5f);
            _playArea.sizeDelta = new Vector2(780f, 480f);
            _playArea.anchoredPosition = Vector2.zero;
            var paImg = _playArea.GetComponent<Image>();
            paImg.sprite = DreamUISpriteUtil.WhiteSprite();
            paImg.color = new Color(0.12f, 0.1f, 0.18f, 1f);

            // 核心先加，渲染在玩家下层
            _coreRt = new GameObject("Core", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _coreRt.SetParent(_playArea, false);
            _coreRt.anchorMin = _coreRt.anchorMax = new Vector2(0.5f, 0.5f);
            _coreRt.sizeDelta = new Vector2(64f, 64f);
            _coreRt.anchoredPosition = Vector2.zero;
            _coreImg = _coreRt.GetComponent<Image>();
            _coreImg.sprite = DreamUISpriteUtil.WhiteSprite();
            _coreImg.color = Color.white;

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
            _coreHp = _ctx.CoreMaxHp;
            _forceDamage = _sootheDamage = _trickDamage = 0;
            _frozen = false;
            _ended = false;
            _spawnBadTimer = 0.35f;
            _spawnGoodTimer = 1.1f;
            _spawnAoeTimer = 3f;
            ClearEntities();
            if (_playerRt != null) _playerRt.anchoredPosition = Vector2.zero;
            // 延迟解析 Core 引用（应对 prefab 在 Awake 前还未完全初始化的情况）
            if (_coreRt == null && _playArea != null)
            {
                _coreRt = _playArea.Find("Core") as RectTransform;
                if (_coreRt != null) _coreImg = _coreRt.GetComponent<Image>();
            }
            UpdateCoreVisual();
            RefreshHud();
        }

        public override void Show()
        {
            base.Show();
            _frozen = false;
            _ended = false;
            _moveInput = Vector2.zero;
        }

        private void Update()
        {
            if (!IsVisible)
            {
                _moveInput = Vector2.zero;
                return;
            }
            if (_frozen || _ended) return;

            MovePlayer();
            TickSpawnTimers();
            TickEntities(_playerRt.anchoredPosition);
            UpdateCoreVisual();
            RefreshHud();
            CheckWinLose();
        }

        private void MovePlayer()
        {
            var half = GetPlayAreaHalfExtents();
            var p = _playerRt.anchoredPosition;
            var move = _moveInput;
            if (move.sqrMagnitude > 1f) move.Normalize();
            p += move * (_ctx.PlayerMoveSpeed * Time.deltaTime);
            p.x = Mathf.Clamp(p.x, -half.x + 16f, half.x - 16f);
            p.y = Mathf.Clamp(p.y, -half.y + 16f, half.y - 16f);
            _playerRt.anchoredPosition = p;
        }

        private void TickSpawnTimers()
        {
            _spawnBadTimer -= Time.deltaTime;
            if (_spawnBadTimer <= 0f)
            {
                SpawnBulletStraight();
                _spawnBadTimer = Random.Range(0.35f, 0.75f);
            }

            _spawnAoeTimer -= Time.deltaTime;
            if (_spawnAoeTimer <= 0f)
            {
                SpawnAoeWarning();
                _spawnAoeTimer = Random.Range(2.5f, 5f);
            }

            _spawnGoodTimer -= Time.deltaTime;
            if (_spawnGoodTimer <= 0f)
            {
                SpawnPickup(_playerRt.anchoredPosition);
                _spawnGoodTimer = Random.Range(1.0f, 2.2f);
            }
        }

        // 从四边随机生成，方向略偏向中心
        private void SpawnBulletStraight()
        {
            var go = new GameObject("Bullet", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_playArea, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(16f, 16f);
            var half = GetPlayAreaHalfExtents();
            var edge = Random.Range(0, 4);
            switch (edge)
            {
                case 0: rt.anchoredPosition = new Vector2(-half.x, Random.Range(-half.y, half.y)); break;
                case 1: rt.anchoredPosition = new Vector2(half.x, Random.Range(-half.y, half.y)); break;
                case 2: rt.anchoredPosition = new Vector2(Random.Range(-half.x, half.x), -half.y); break;
                default: rt.anchoredPosition = new Vector2(Random.Range(-half.x, half.x), half.y); break;
            }
            var img = go.GetComponent<Image>();
            img.sprite = DreamUISpriteUtil.WhiteSprite();
            img.color = new Color(0.95f, 0.25f, 0.2f, 1f);

            var baseDir = (-rt.anchoredPosition).normalized;
            // 轻微随机偏转，避免全部笔直穿越中心
            var dir = (baseDir + Random.insideUnitCircle * 0.3f).normalized;
            _entities.Add(new DreamSkyEntity
            {
                Rt = rt,
                Velocity = dir * Random.Range(90f, 170f),
                IsBad = true,
                Kind = EntityKind.BulletStraight,
            });
        }

        // 预警 AOE：先显示红色警告圈，倒计时结束后闪烁并造成伤害
        private void SpawnAoeWarning()
        {
            var half = GetPlayAreaHalfExtents() - Vector2.one * 55f;
            half.x = Mathf.Max(20f, half.x);
            half.y = Mathf.Max(20f, half.y);
            var pos = new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y));
            var radius = Random.Range(50f, 85f);

            var go = new GameObject("AoeWarning", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_playArea, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(radius * 2f, radius * 2f);
            rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            img.sprite = DreamUISpriteUtil.WhiteSprite();
            img.color = new Color(1f, 0.1f, 0.1f, 0.2f);

            _entities.Add(new DreamSkyEntity
            {
                Rt = rt,
                IsBad = true,
                Kind = EntityKind.AoeWarning,
                AoeTimer = _ctx.AoeWarningDuration,
                AoeActivated = false,
                AoeActiveTimer = 0.35f,
                AoeRadius = radius,
            });
        }

        // 按 1/3 概率分配三种拾取物
        private void SpawnPickup(Vector2 playerPos)
        {
            var kind = (DreamTendencyKind)Random.Range(0, 3);
            switch (kind)
            {
                case DreamTendencyKind.Force:   SpawnPickupForce(); break;
                case DreamTendencyKind.Soothing: SpawnPickupSoothing(); break;
                default:                         SpawnPickupTrick(playerPos); break;
            }
        }

        // 红色：从核心向随机方向发射出去
        private void SpawnPickupForce()
        {
            var corePos = _coreRt != null ? _coreRt.anchoredPosition : Vector2.zero;
            var angle = Random.Range(0f, Mathf.PI * 2f);
            var go = new GameObject("Pickup", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_playArea, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(20f, 20f);
            rt.anchoredPosition = corePos;
            var img = go.GetComponent<Image>();
            img.sprite = DreamUISpriteUtil.WhiteSprite();
            img.color = new Color(1f, 0.45f, 0.2f, 1f);
            _entities.Add(new DreamSkyEntity
            {
                Rt = rt,
                Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(80f, 130f),
                IsBad = false,
                Kind = EntityKind.PickupForce,
                PickupKind = DreamTendencyKind.Force,
            });
        }

        // 蓝色：全场随机位置，轻微漂移
        private void SpawnPickupSoothing()
        {
            var half = GetPlayAreaHalfExtents() - Vector2.one * 30f;
            half.x = Mathf.Max(8f, half.x);
            half.y = Mathf.Max(8f, half.y);
            var go = new GameObject("Pickup", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_playArea, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(20f, 20f);
            rt.anchoredPosition = new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y));
            var img = go.GetComponent<Image>();
            img.sprite = DreamUISpriteUtil.WhiteSprite();
            img.color = new Color(0.45f, 0.75f, 1f, 1f);
            _entities.Add(new DreamSkyEntity
            {
                Rt = rt,
                Velocity = Random.insideUnitCircle * 22f,
                IsBad = false,
                Kind = EntityKind.PickupSoothing,
                PickupKind = DreamTendencyKind.Soothing,
            });
        }

        // 紫色：在玩家周围随机距离生成，逐渐向玩家靠近
        private void SpawnPickupTrick(Vector2 playerPos)
        {
            var angle = Random.Range(0f, Mathf.PI * 2f);
            var dist = Random.Range(80f, 130f);
            var spawnPos = playerPos + new Vector2(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist);
            var half = GetPlayAreaHalfExtents() - Vector2.one * 15f;
            spawnPos.x = Mathf.Clamp(spawnPos.x, -half.x, half.x);
            spawnPos.y = Mathf.Clamp(spawnPos.y, -half.y, half.y);

            var go = new GameObject("Pickup", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_playArea, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(20f, 20f);
            rt.anchoredPosition = spawnPos;
            var img = go.GetComponent<Image>();
            img.sprite = DreamUISpriteUtil.WhiteSprite();
            img.color = new Color(0.85f, 0.55f, 1f, 1f);
            _entities.Add(new DreamSkyEntity
            {
                Rt = rt,
                Velocity = Vector2.zero,
                IsBad = false,
                Kind = EntityKind.PickupTrick,
                PickupKind = DreamTendencyKind.Trick,
            });
        }

        // 拾取后发射弧形子弹（二次贝塞尔曲线）
        private void SpawnArcProjectile(Vector2 fromPos, DreamTendencyKind kind)
        {
            var corePos = _coreRt != null ? _coreRt.anchoredPosition : Vector2.zero;
            var go = new GameObject("ArcProj", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_playArea, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(12f, 12f);
            rt.anchoredPosition = fromPos;
            var img = go.GetComponent<Image>();
            img.sprite = DreamUISpriteUtil.WhiteSprite();
            img.color = kind switch
            {
                DreamTendencyKind.Force    => new Color(1f, 0.55f, 0.1f, 1f),
                DreamTendencyKind.Soothing => new Color(0.3f, 0.8f, 1f, 1f),
                _                          => new Color(0.9f, 0.4f, 1f, 1f),
            };

            // 垂直于连线方向随机偏移控制点，形成弧形轨迹
            var delta = corePos - fromPos;
            var mid = (fromPos + corePos) * 0.5f;
            var perp = delta.sqrMagnitude > 0.01f
                ? new Vector2(-delta.y, delta.x).normalized
                : Vector2.right;
            var ctrl = mid + perp * Random.Range(-90f, 90f);

            _entities.Add(new DreamSkyEntity
            {
                Rt = rt,
                Kind = EntityKind.ArcProjectile,
                PickupKind = kind,
                ArcP0 = fromPos,
                ArcP1 = ctrl,
                ArcP2 = corePos,
                ArcT = 0f,
                ArcDuration = Random.Range(0.5f, 0.75f),
            });
        }

        private void TickEntities(Vector2 playerPos)
        {
            const float hitR = 22f;
            var half = GetPlayAreaHalfExtents();

            for (var i = _entities.Count - 1; i >= 0; i--)
            {
                var e = _entities[i];
                if (e.Rt == null)
                {
                    _entities.RemoveAt(i);
                    continue;
                }

                if (e.Kind == EntityKind.AoeWarning)
                {
                    if (!e.AoeActivated)
                    {
                        e.AoeTimer -= Time.deltaTime;
                        // 警告期间 alpha 脉冲闪烁
                        var alpha = 0.15f + 0.12f * Mathf.Sin(Time.time * 7f);
                        var img = e.Rt.GetComponent<Image>();
                        if (img != null) img.color = new Color(1f, 0.15f, 0.1f, alpha);
                        if (e.AoeTimer <= 0f)
                        {
                            e.AoeActivated = true;
                            if (img != null) img.color = new Color(1f, 0.25f, 0.2f, 0.75f);
                            // 激活瞬间判断玩家是否在范围内
                            var dist = (e.Rt.anchoredPosition - playerPos).magnitude;
                            if (dist < e.AoeRadius) _hp -= _ctx.AoeDamage;
                        }
                    }
                    else
                    {
                        e.AoeActiveTimer -= Time.deltaTime;
                        if (e.AoeActiveTimer <= 0f)
                        {
                            Destroy(e.Rt.gameObject);
                            _entities.RemoveAt(i);
                        }
                    }
                    continue;
                }

                if (e.Kind == EntityKind.ArcProjectile)
                {
                    e.ArcT += Time.deltaTime / e.ArcDuration;
                    if (e.ArcT >= 1f)
                    {
                        var dmg = _ctx.ProjectileDamage;
                        _coreHp = Mathf.Max(0, _coreHp - dmg);
                        switch (e.PickupKind)
                        {
                            case DreamTendencyKind.Force:    _forceDamage   += dmg; break;
                            case DreamTendencyKind.Soothing: _sootheDamage  += dmg; break;
                            default:                          _trickDamage   += dmg; break;
                        }
                        Destroy(e.Rt.gameObject);
                        _entities.RemoveAt(i);
                        continue;
                    }
                    // 二次贝塞尔插值
                    float t = e.ArcT, u = 1f - t;
                    e.Rt.anchoredPosition = u * u * e.ArcP0 + 2f * u * t * e.ArcP1 + t * t * e.ArcP2;
                    continue;
                }

                // 紫色拾取物每帧更新朝玩家方向的速度
                if (e.Kind == EntityKind.PickupTrick)
                {
                    var toPlayer = playerPos - e.Rt.anchoredPosition;
                    if (toPlayer.sqrMagnitude > 0.1f)
                        e.Velocity = toPlayer.normalized * _ctx.TrickApproachSpeed;
                }

                e.Rt.anchoredPosition += e.Velocity * Time.deltaTime;

                var delta = e.Rt.anchoredPosition - playerPos;
                if (delta.sqrMagnitude < hitR * hitR)
                {
                    if (e.IsBad)
                    {
                        _hp -= _ctx.BulletDamage;
                    }
                    else
                    {
                        SpawnArcProjectile(_playerRt.anchoredPosition, e.PickupKind);
                    }
                    Destroy(e.Rt.gameObject);
                    _entities.RemoveAt(i);
                    continue;
                }

                var pos = e.Rt.anchoredPosition;
                var eh = GetEntityHalfExtents(e.Rt);
                if (IsEntityOutsidePlayArea(pos, half, eh))
                {
                    Destroy(e.Rt.gameObject);
                    _entities.RemoveAt(i);
                }
            }
        }

        // 根据核心剩余血量调整颜色（满血白色/高亮，低血红色）
        private void UpdateCoreVisual()
        {
            if (_coreImg == null) return;
            var ratio = _ctx.CoreMaxHp > 0 ? (float)_coreHp / _ctx.CoreMaxHp : 0f;
            _coreImg.color = Color.Lerp(new Color(0.8f, 0.1f, 0.1f, 1f), Color.white, ratio);
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
            if (_coreHp <= 0)
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
                var flash = new GameObject("Flash", typeof(RectTransform), typeof(Image));
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
            var totalScore = _forceDamage + _sootheDamage + _trickDamage;
            var finalWon = won;
            if (finalWon
                && _ctx != null
                && _ctx.EntrySource == DreamEntrySourceKind.AbstractGroupEntry
                && _ctx.RequiredScore > 0
                && totalScore < _ctx.RequiredScore)
            {
                finalWon = false;
            }

            var payload = new DreamSettlementPayload
            {
                Won = finalWon,
                ThemeDisplayName = _ctx.ThemeDisplayName,
                ForceScore    = _forceDamage,
                SoothingScore = _sootheDamage,
                TrickScore    = _trickDamage,
                EntrySource = _ctx.EntrySource,
                SpotId = _ctx.SpotId,
                CharacterKey = _ctx.CharacterKey,
                CharDreamEntryId = _ctx.CharDreamEntryId,
                AbstractGroupId = _ctx.AbstractGroupId,
                AbstractGroupStage = _ctx.AbstractGroupStage,
                VictoryTendency = finalWon
                    ? DreamVictoryTendencyResolver.Resolve(_forceDamage, _sootheDamage, _trickDamage)
                    : null,
                AdvanceDayAfterClose = true,
            };
            UIManager.Instance?.ShowPanel(DreamInfiltrationIds.SettlementPanel, payload, UILayer.Overlay);
        }

        private void ClearEntities()
        {
            foreach (var e in _entities)
                if (e.Rt != null) Destroy(e.Rt.gameObject);
            _entities.Clear();
        }

        private void RefreshHud()
        {
            if (_hudTmp == null) return;
            _hudTmp.text =
                $"{_ctx.ThemeDisplayName}  |  HP {_hp}/{_ctx.MaxHp}  |  核心 {_coreHp}/{_ctx.CoreMaxHp}\n" +
                $"力 {_forceDamage}  安 {_sootheDamage}  谋 {_trickDamage}  (已对核心造成伤害)\n" +
                "WASD 移动 | 躲红块/AOE | 拾彩色物 → 弧形炮弹攻击核心";
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
            if (!IsVisible || _frozen || _ended)
            {
                _moveInput = Vector2.zero;
                return true;
            }
            _moveInput = dir;
            if (_moveInput.sqrMagnitude > 1f) _moveInput.Normalize();
            return true;
        }
    }

    public sealed class DreamGameplayContext
    {
        public string ThemeId = "";
        public string ThemeDisplayName = "";
        public int CoreMaxHp = 200;
        public float PlayerMoveSpeed = 220f;
        public int MaxHp = 100;
        public int BulletDamage = 15;
        public int AoeDamage = 20;
        public float AoeWarningDuration = 1.5f;
        public int ProjectileDamage = 20;
        public float TrickApproachSpeed = 65f;
        public int RequiredScore;

        public DreamEntrySourceKind EntrySource = DreamEntrySourceKind.FacilitySpot;
        public string SpotId = "";
        public string CharacterKey = "";
        public int CharDreamEntryId;
        public string AbstractGroupId = "";
        public int AbstractGroupStage;
    }
}
