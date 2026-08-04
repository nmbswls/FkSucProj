using cfg.demo;
using My.Input;
using My.Map;
using My.Map.Entity;
using My.UI;
using UnityEngine;

namespace My.Map.View
{
    // 玩家被推倒后的 H 倒地 Closeup（第一版：计时 + H 冲击结算，无 QTE）
    public class PauseCloseupKnockdownWindow : PanelBase, IInputConsumer
    {
        public const string ID = "PauseCloseupKnockdownWindow";

        public static PauseCloseupKnockdownWindow Show(long srcEntityId, float duration)
        {
            var panel = UIManager.Instance.ShowPanel(ID) as PauseCloseupKnockdownWindow;
            if (panel == null)
            {
                Debug.LogError("PauseCloseupKnockdownWindow err");
                return null;
            }

            panel.RefreshData(srcEntityId, duration);
            return panel;
        }

        public long SrcEntityId;
        public int HActId;
        public float Duration;

        float _timer;

        public void RefreshData(long srcEntityId, float duration)
        {
            SrcEntityId = srcEntityId;
            Duration = duration;

            var glm = MainGameManager.Instance.gameLogicManager;
            var player = glm.playerLogicEntity;
            HActId = PlayerGamePlayRule.RandomGetOneHAct("KnockDown", player.DesireLevel);
            if (HActId == 0)
            {
                Debug.LogError("PauseCloseupKnockdownWindow: no KnockDown HAct");
            }

            var npc = glm.GetLogicEntity(srcEntityId, false) as NpcUnitLogicEntity;
            npc?.HInteraction.Active.Begin(
                EBodyPart.FrontHole, EHInteractionSource.CloseupKnockdown, HActId);
        }

        public override void Show()
        {
            base.Show();
            _timer = 0;
            LogicTime.ReleasePause("PauseCloseupWindow");
            LogicTime.RequestPause("PauseCloseupWindow");
        }

        void Update()
        {
            _timer += Time.deltaTime;
            if (_timer > Duration)
            {
                HandleInteractFinish();
            }
        }

        void HandleInteractFinish()
        {
            var p = MainGameManager.Instance.gameLogicManager.playerLogicEntity;
            var target = MainGameManager.Instance.gameLogicManager.GetLogicEntity(SrcEntityId) as NpcUnitLogicEntity;
            if (p == null || target == null || HActId == 0)
            {
                UIManager.Instance.HidePanel(ID);
                return;
            }

            // Closeup 互动不派生 HP；接触部位与 Active 会话一致
            HActResolver.TryResolveAndApply(
                HActId, p, target, intensity: 1f, applyHpDamage: false,
                preferredContactPart: EBodyPart.FrontHole);

            p.ApplyResourceChange(AttrIdConsts.PlayerSanity, -4_000, false, Fight.FightStruct.EDmgFlag.None, srcEntityId: SrcEntityId);

            UIManager.Instance.HidePanel(ID);
        }

        public override void Hide()
        {
            base.Hide();
            LogicTime.ReleasePause("PauseCloseupWindow");
        }

        public bool OnConfirm() => true;
        public bool OnCancel() => true;
        public bool OnNavigate(Vector2 dir) => true;
        public bool OnHotkey(string keyName) => true;
        public bool OnScroll(float deltaY) => true;
        public bool OnClick(int button, Vector2 mousePos) => true;
        public bool OnHoldStart(string holdKey) => true;
        public bool OnHoldUpdate(string holdKey) => true;
        public bool OnHoldingEnd(string holdKey) => true;
    }
}
