using DamageNumbersPro;
using Map.Scene.UI;
using My.Config;
using My.Dialog;
using My.Map;
using My.Map.Entity;
using My.Map.Fight;
using My.Map.Scene;
using My.Map.SmallGame.Zha;
using My.Map.View;
using My.MiniGame;
using My.Player.Bag;
using My.Saving;
using My.UI;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace My
{
    public partial class MainGameManager
    {
        public void ShowBottomProgress(string hintText, float duration, float phaseStartLogicTime)
        {
            OverworldHUDPanel.Instance?.ShowBottomProgress(hintText, duration, phaseStartLogicTime);
        }

        public void TryCancelButtomProgress(float phaseStartLogicTime)
        {
            OverworldHUDPanel.Instance?.TryCancelProgressComplete(phaseStartLogicTime);
        }

        public void ShowFakeFxEffect(string hintText, Vector2 logicPos)
        {
            var worldPos = GetWorldPosFromLogicPos(logicPos);
            FakeHintTextManager.ShowWorld(hintText, worldPos);
        }

        public void ShowSceneFxEffect(string effectName, Vector2 pos, Vector2 dir)
        {
            var worldPos = GetWorldPosFromLogicPos(pos);

            var fxCtx = MapSceneEffectManager.Instance.ShowSceneEffect(worldPos, 1, effectName, null, dir: dir);
            if (fxCtx == null)
            {
                Debug.LogError("ShowSceneFxEffect faaa " + effectName);
            }
        }

        public void ShowNoiseEffect(float intensity, Vector2 logicPos)
        {
            var worldPos = GetWorldPosFromLogicPos(logicPos);

            var fxCtx = MapSceneEffectManager.Instance.ShowSceneEffect(worldPos, 1, "ChamEffect", null);
            if (fxCtx == null)
            {
                return;
            }

            var ring = fxCtx.EffectGo.GetComponent<MapNoiseRing>();
            ring.gameObject.SetActive(true);
            ring.Play(Mathf.Clamp01(intensity), worldPos);
        }

        public void ShowClickkkWindow(string windowType, Vector2 showPos, float duration)
        {
            ShowClickkkUI.Instance.OpenClickkkHint(windowType, showPos, duration);
        }

        public void CloseClickkkWindow(string windowType, bool isInterrupt)
        {
            ShowClickkkUI.Instance.CloseClickkkWindow(windowType, isInterrupt);
        }

        public void ShowHTangleCloseupWindow(long srcEntityId)
        {
            PauseCloseupHTangleWindow.Show(srcEntityId);
        }

        public void ShowKaiYouCloseupWindow(long srcEntityId, string showName, float duration)
        {
            PauseCloseupKaiYouWindow.Show(srcEntityId, showName, duration);
        }

        public void ShowKnockdownCloseupWindow(long srcEntityId, float duration)
        {
            PauseCloseupKnockdownWindow.Show(srcEntityId, duration);
        }

        public void ShowGcCloseupWindow(string showName, float duration)
        {
            PauseCloseupWindow.Show(showName, duration);
        }

        public void DoDeepZhaquSmallGame(long targetUnitId, object extraParam)
        {
            LogicTime.ReleasePause("deep");

            var retPanel = UIManager.Instance.ShowPanel("DeepZhaQuMiniGame") as DeepZhaQuMiniGamePanel;
            if (retPanel != null)
            {
                retPanel.InitializeGame(targetUnitId, 0.2f, 4f);
            }
        }

        public void OnSmallGameFinish(long targetUnitId, bool success, object resultInfo)
        {
            LogicTime.ReleasePause("deep");
            if (success)
            {
                Debug.Log("OnSmallGameFinish " + targetUnitId + " success.");

                var entity = gameLogicManager.GetLogicEntity(targetUnitId);
                if (entity != null && entity is BaseUnitLogicEntity unitEntity)
                {
                    unitEntity.ApplyResourceChange(AttrIdConsts.DeepZhaChance, -1, true, FightStruct.EDmgFlag.None, null);
                    gameLogicManager.globalDropCollection.CreateDrop("jinghua", 3, unitEntity.Pos + new Vector2(0.3f, 0.3f), true, unitEntity.Pos);
                    gameLogicManager.globalDropCollection.CreateDrop("jinghua", 3, unitEntity.Pos + new Vector2(-0.3f, 0.1f), true, unitEntity.Pos);
                    gameLogicManager.globalDropCollection.CreateDrop("jinghua", 3, unitEntity.Pos + new Vector2(-0.1f, 0.6f), true, unitEntity.Pos);

                    if (UnityEngine.Random.Range(0, 10000) < 2000)
                    {
                        Debug.Log("OnSmallGameFinish deep zha create dig.");
                        gameLogicManager.AreaManager.CreateOneDig(entity.Pos, "dig_01", 100);
                    }
                }
            }

            UIManager.Instance.HidePanel("DeepZhaQuMiniGame");
        }

        public int ShowRangeWarnEffect(FightStruct.Shape shape, Vector2 centerPos, Vector2 dir, float duration, Vector2 offset)
        {
            switch (shape.Type)
            {
                case FightStruct.EShapeType.Circle:
                    return SceneRangeWarnManager.ShowSceneWarnRangeCircle(centerPos, dir, shape.Radius, duration, offset);
                case FightStruct.EShapeType.Square:
                    return SceneRangeWarnManager.ShowSceneWarnRangeRect(centerPos, dir, shape.Width, shape.Length, duration, offset);
            }

            return 0;
        }

        public void UpdateRangeWarnEffect(int eId, Vector2 pos, Vector2 dir)
        {
            SceneRangeWarnManager.UpdateSceneWarnRangeRect(eId, pos, dir);
        }

        public int ShowProgressSceneEffect(string effectName, Vector2 pos, long bindingUnitId)
        {
            var fxCtx = MapSceneEffectManager.Instance.ShowSceneEffect(pos, -1f, effectName, bindingUnitId);
            return fxCtx != null ? fxCtx.UniqId : 0;
        }

        public void UpdateSceneEffectProgress(int effectId, float progress01)
        {
            MapSceneEffectManager.Instance.SetEffectProgress(effectId, progress01);
        }

        public void DestroySceneFxEffect(int effectId)
        {
            MapSceneEffectManager.Instance.ForceDestroy(effectId);
        }

        public void DoPlayerRelocate(PlayerRelocateSpec spec, Action onComplete = null)
        {
            if (playerScenePresenter == null)
            {
                onComplete?.Invoke();
                return;
            }

            playerScenePresenter.PlayRelocate(spec, onComplete);
        }

        SaveData BuildRuntimeSaveSnapshot()
        {
            var data = new SaveData();
            data.Meta.SaveTime = System.DateTime.Now.ToString("o");
            data.Meta.Version = Application.version;

            if (gameLogicManager?.AreaManager != null &&
                !string.IsNullOrEmpty(gameLogicManager.AreaManager.AreaOverlayId))
            {
                data.CurrentMapId = gameLogicManager.AreaManager.AreaOverlayId;
            }

            if (gameLogicManager?.playerLogicEntity != null)
            {
                data.CurrentPos = gameLogicManager.playerLogicEntity.Pos;
            }

            gameLogicManager?.AppendRuntimePersistenceToSaveData(data);

            SaveData.EnsureHydrated(data);

            return data;
        }

        public async Task OnSaveClicked()
        {
            if (SaveSystem.IsBusy)
            {
                return;
            }

            SaveData dataToSave = BuildRuntimeSaveSnapshot();
            await SaveSystem.SaveAsync(SaveSystem.DefaultSaveFileName, dataToSave);

            Debug.Log("UI: save flow finished");
        }

        public bool PlayDialog(string dialogId, long? srcEntityId = null, bool pause = false, Action onDialogEnd = null, DialogueSessionContext sessionContext = null)
        {
            var dialogMetaInfo = CfgMgr.Cfgs.TbDialogMetaInfo.Get(dialogId);
            if (dialogMetaInfo == null)
            {
                Debug.LogError($"PlayDialog dialog not found {dialogId}.");
                return false;
            }

            var dialogAsset = Resources.Load<TextAsset>($"Dialogue/output/{dialogMetaInfo.JsonDataName}");
            if (dialogAsset == null)
            {
                Debug.LogError("PlayerDialog not found dialog " + dialogId + "asset " + dialogMetaInfo.JsonDataName);
                return false;
            }

            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            };

            var dialogData = JsonConvert.DeserializeObject<DialogueData>(dialogAsset.text, settings);

            var dialogPanel = UIManager.Instance.ShowPanel("DialoguePanel") as DialogueUI;

            var runtime = new DialogueRuntime
            {
                ui = dialogPanel,
                driver = dialoguePlayer.GetComponent<DialogueTimeDriver>(),
                JumpTo = label => dialoguePlayer.JumpToStep(label),
                SrcEntityId = srcEntityId,
                SessionContext = sessionContext,
            };

            dialoguePlayer.ui = dialogPanel;
            dialoguePlayer.PlayFromData(dialogMetaInfo, dialogData, runtime, () =>
            {
                LogicTime.ClearPauseSource("Dialog");
                UIManager.Instance.HidePanel("DialoguePanel");
                onDialogEnd?.Invoke();
            });

            if (pause)
            {
                LogicTime.RequestPause("Dialog");
            }

            return true;
        }

        public void StartLoot(ILootableObj lootObj)
        {
            UIOrchestrator.Instance.TryEnterLootDetailMode(lootObj);
        }

        public void StartHitStop(float duration)
        {
            HitStopManager.Instance.TriggerHitStop(duration);

            VcamInpulseSource.GenerateImpulse(0.06f);
        }

        public void ShowMapSpeachBubble(long entityId, string content, float duration, int priority = 1, float extraInteval = 0)
        {
            var pres = SceneAOIManager.Instance.GetActivePresentation(entityId);
            if (pres == null)
            {
                return;
            }

            MapSpeechBubbleManager.Instance.Say(pres, content, duration, priority);
        }

        public bool TryGetThrowGrappleLogicPos(long targetEntityId, string socketPath, out Vector2 logicPos)
        {
            logicPos = default;
            var pres = SceneAOIManager.Instance?.GetActivePresentation(targetEntityId);
            if (pres is not SceneUnitPresenter sup)
            {
                return false;
            }

            return sup.TryGetThrowGrappleLogicPos(socketPath, out logicPos);
        }

        public void ShowDamageNumber(Vector2 worldPos, string content, Transform bindTrans = null)
        {
            var dmgResource = SimpleResManager.Load<DamageNumber>("SceneEffect/JumpText/Damage_01");
            if (bindTrans == null)
            {
                dmgResource.Spawn(worldPos, content);
            }
            else
            {
                dmgResource.Spawn(worldPos, content, bindTrans);
            }
        }
    }
}
