using Animancer;
using cfg.demo;
using My.Input;
using My.Map;
using My.Map.Entity;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.Map.View
{
    // ������ͣ�� H ���ര�ڣ�5�C8s��ʱͣ���ȶ� + ����˫�ᣩ
    public class PauseCloseupHTangleWindow : PanelBase, IInputConsumer
    {
        public const string ID = "PauseCloseupHTangleWindow";

        struct SettlementSnapshot
        {
            public long SanCost;
            public long PlayerImpulseApply;
            public long EnemyImpulseApply;
            public long Score;
            public int DeeperLayers;
        }

        public static PauseCloseupHTangleWindow Show(long srcEntityId)
        {
            var panel = UIManager.Instance.ShowPanel(ID) as PauseCloseupHTangleWindow;
            if (panel == null)
            {
                Debug.LogError("PauseCloseupHTangleWindow err");
                return null;
            }

            panel.RefreshData(srcEntityId);
            return panel;
        }

        [Header("UI")]
        [SerializeField] Image showPic;
        [SerializeField] Image progressBar;
        [SerializeField] SoloAnimation mainBtnSoloAnimation;
        [SerializeField] TMP_Text scoreText;

        [Header("HTangle Timing")]
        const float CfgDuration = 5f;
        const float CfgActPulseInterval = 1f; // ��������

        [Header("HTangle Heat")]
        const float heatNaturalRate = 0.13f;
        const float heatHoldRate = 0.45f;
        const float heatMax = 1f;

        const float holdAnimSpeedBase = 1f;
        const float holdAnimSpeedBoost = 1.5f;

        const float playerSettleWeight = 0.15f;

        const long sanBase = 3000;
        const long sanPerHoldSecond = 1600;

        [Header("HTangle Settlement")]
        [SerializeField] long scorePerDeeperLayer = 500;
        [SerializeField] int deeperLayerCap = 40;

        public long SrcEntityId { get; private set; }

        float _heat;
        float _holdDuration;
        float _elapsed;
        float _duration;
        float _pulseTimer;
        long _accEnemyImpulse;
        long _accPlayerImpulse;
        int _currentActId;
        bool _gameFinished;
        bool _isHoldingSpace;
        bool _settled;

        long Score => _accEnemyImpulse;

        public Image ShowPic
        {
            get => showPic;
        }

        public Image ProgressBar
        {
            get => progressBar;
        }

        void Awake()
        {
            
        }

        void ResetGameState()
        {
            _gameFinished = false;
            _pulseTimer = 0f;
            _heat = 0f;
            _holdDuration = 0f;
            _elapsed = 0f;
            _accEnemyImpulse = 0;
            _accPlayerImpulse = 0;
            _duration = CfgDuration;
            _currentActId = PickActIdForHeat(_heat);
        }

        bool TickGame(float dt, GameLogicManager glm)
        {
            if (_gameFinished)
            {
                return true;
            }

            _elapsed += dt;

            var heatDelta = heatNaturalRate * dt;
            if (_isHoldingSpace)
            {
                heatDelta += heatHoldRate * dt;
                _holdDuration += dt;
            }

            _heat = Mathf.Min(heatMax, _heat + heatDelta);

            _pulseTimer += dt;
            while (_pulseTimer >= CfgActPulseInterval)
            {
                _pulseTimer -= CfgActPulseInterval;
                TryActPulse(glm);
            }

            if (_elapsed >= _duration)
            {
                _gameFinished = true;
                return true;
            }

            return false;
        }

        SettlementSnapshot BuildSettlement()
        {
            var deeper = scorePerDeeperLayer > 0
                ? (int)Mathf.Min(Score / scorePerDeeperLayer, deeperLayerCap)
                : 0;

            return new SettlementSnapshot
            {
                SanCost = sanBase + (long)(_holdDuration * sanPerHoldSecond),
                PlayerImpulseApply = (long)(_accPlayerImpulse * playerSettleWeight),
                EnemyImpulseApply = _accEnemyImpulse,
                Score = Score,
                DeeperLayers = deeper,
            };
        }

        static int HeatToDesireTier(float heat)
        {
            if (heat < 0.25f)
            {
                return 0;
            }

            if (heat < 0.5f)
            {
                return 1;
            }

            if (heat < 0.75f)
            {
                return 2;
            }

            return 3;
        }

        int PickActIdForHeat(float heat)
        {
            return PlayerGamePlayRule.RandomGetOneHAct("Charmed", HeatToDesireTier(heat));
        }

        void TryActPulse(GameLogicManager glm)
        {
            if (glm?.playerLogicEntity == null)
            {
                return;
            }

            _currentActId = PickActIdForHeat(_heat);

            var player = glm.playerLogicEntity;
            var npc = glm.GetLogicEntity(SrcEntityId, false) as NpcUnitLogicEntity;
            float intensity = 1f + _heat;
            // 缠绵接触穴：显式传入，与 Receive 会话一致
            if (!HActResolver.TryResolve(
                    _currentActId, player, npc, intensity, out var resolved, EBodyPart.FrontHole))
            {
                Debug.LogWarning("[PauseCloseupHTangleWindow] ResolveHActParams failed for act " + _currentActId);
                return;
            }

            _accEnemyImpulse += resolved.ImpulseOnEnemy;
            _accPlayerImpulse += resolved.ImpulseOnPlayer;
            if (npc != null)
            {
                npc.HInteraction.Receive.NoteAct(
                    _currentActId,
                    resolved.ContactPart != EBodyPart.None ? resolved.ContactPart : EBodyPart.FrontHole,
                    EHInteractionSource.CloseupHTangle);
            }
        }

        void Update()
        {
            if (_settled)
            {
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                return;
            }

            if (TickGame(Time.deltaTime, glm))
            {
                HandleInteractFinish();
            }

            RefreshUI();
        }

        void RefreshUI()
        {
            if (progressBar != null)
            {
                progressBar.fillAmount = _heat;
            }

            if (scoreText != null)
            {
                scoreText.text = $"Score {Score}";
            }

            UpdateMainBtnAnimSpeed();
        }

        void UpdateMainBtnAnimSpeed()
        {
            if (mainBtnSoloAnimation == null)
            {
                return;
            }

            mainBtnSoloAnimation.Speed = _isHoldingSpace
                ? holdAnimSpeedBase + holdAnimSpeedBoost * _heat
                : holdAnimSpeedBase;
        }

        public void RefreshData(long srcEntityId)
        {
            SrcEntityId = srcEntityId;
            _settled = false;
            _isHoldingSpace = false;
            ResetGameState();
            RefreshUI();
        }

        public override void Show()
        {
            base.Show();

            LogicTime.ReleasePause("PauseCloseupWindow");
            LogicTime.RequestPause("PauseCloseupWindow");

            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                return;
            }

            // 缠绵默认接触「穴」；写 NPC.Receive + 拉长 fcked，便于射精条满时内射
            var npc = glm.GetLogicEntity(SrcEntityId, false) as NpcUnitLogicEntity;
            npc?.HInteraction.Receive.Begin(
                EBodyPart.FrontHole, EHInteractionSource.CloseupHTangle, _currentActId);
            glm.globalBuffManager.AddBuff(
                SrcEntityId, "fcked_marked", 1, overrideDuration: HInteractionSlot.DefaultHoldSeconds);
            glm.globalBuffManager.AddBuff(glm.playerLogicEntity.Id, "charm_fck_bonus", 1, overrideDuration: 0.5f);
        }

        void HandleInteractFinish()
        {
            if (_settled)
            {
                return;
            }

            _settled = true;
            ApplySettlement(BuildSettlement());
            UIManager.Instance.HidePanel(ID);
        }

        void ApplySettlement(SettlementSnapshot settlement)
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            var player = glm?.playerLogicEntity;
            var npc = glm?.GetLogicEntity(SrcEntityId, false) as NpcUnitLogicEntity;
            if (player == null)
            {
                return;
            }

            if (settlement.SanCost > 0)
            {
                player.ApplyResourceChange(
                    AttrIdConsts.PlayerSanity, -settlement.SanCost, false,
                    Fight.FightStruct.EDmgFlag.None, null);
                player.ForceCommitAttribute();
            }

            if (settlement.PlayerImpulseApply > 0)
            {
                // 缠绵结束一次落地冲击；高潮权重取最后一次脉冲对应的 HAct
                float climaxWeight = 1f;
                var act = My.Config.CfgMgr.Cfgs?.TbHActInfo?.GetOrDefault(_currentActId);
                if (act != null && act.PlayerClimaxWeight > 0f)
                {
                    climaxWeight = act.PlayerClimaxWeight;
                }

                player.ApplyHImpulseDirectly(settlement.PlayerImpulseApply, null, climaxWeight);
            }

            if (npc != null && settlement.EnemyImpulseApply > 0)
            {
                npc.ApplyNpcHImpulse(settlement.EnemyImpulseApply);
                npc.HInteraction.Receive.NoteAct(
                    _currentActId, EBodyPart.FrontHole, EHInteractionSource.CloseupHTangle);
                glm.globalBuffManager?.AddBuff(
                    SrcEntityId, "fcked_marked", 1, overrideDuration: HInteractionSlot.DefaultHoldSeconds);
            }

            if (npc != null && settlement.DeeperLayers > 0)
            {
                glm.globalBuffManager.AddBuff(SrcEntityId, "charm_fck_deeper", settlement.DeeperLayers);
            }

            Debug.Log(
                $"[PauseCloseupHTangleWindow] Settled score={settlement.Score} san={settlement.SanCost} " +
                $"playerH={settlement.PlayerImpulseApply} enemyH={settlement.EnemyImpulseApply} " +
                $"deeper={settlement.DeeperLayers} hold={_holdDuration:F2}s");
        }

        public override void Hide()
        {
            base.Hide();
            LogicTime.ReleasePause("PauseCloseupWindow");
            _isHoldingSpace = false;
            if (mainBtnSoloAnimation != null)
            {
                mainBtnSoloAnimation.Speed = holdAnimSpeedBase;
            }
        }

        bool IsSpaceHoldKey(string keyName) =>
            keyName == EInputKey.Space.ToString();

        public bool OnConfirm() => true;

        public bool OnCancel() => true;

        public bool OnNavigate(Vector2 dir) => true;

        public bool OnHotkey(string keyName) => true;

        public bool OnScroll(float deltaY) => true;

        public bool OnClick(int button, Vector2 mousePos) => true;

        public bool OnHoldStart(string holdKey)
        {
            if (IsSpaceHoldKey(holdKey))
            {
                _isHoldingSpace = true;
            }

            return true;
        }

        public bool OnHoldUpdate(string holdKey)
        {
            if (IsSpaceHoldKey(holdKey))
            {
                _isHoldingSpace = true;
            }

            return true;
        }

        public bool OnHoldingEnd(string holdKey)
        {
            if (IsSpaceHoldKey(holdKey))
            {
                _isHoldingSpace = false;
            }

            return true;
        }
    }
}
