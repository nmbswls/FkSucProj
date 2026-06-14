using System.Collections.Generic;
using My.Map;
using My.Map.Entity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static My.GameLogicManager;

namespace My.UI
{
    // 暴露技能槽：按下蓄力时显示 Pressed 特效，松开时隐藏；其余样式由 prefab 配置。
    public class OverworldHudExposeSkillIndicator : MonoBehaviour
    {
        const string EnterExposeSkillId = "player_enter_expose";
        const string ReturnDisguiseSkillId = "player_return_disguise";
        static readonly int HoldProgressId = Shader.PropertyToID("_HoldProgress");

        GameObject _pressedOverlay;
        Material _pressedMaterial;
        Image _iconImage;
        TextMeshProUGUI _skillNameText;

        public void BindView()
        {
            var pressedTr = transform.Find("Pressed");
            if (pressedTr != null)
            {
                _pressedOverlay = pressedTr.gameObject;
                var graphic = pressedTr.GetComponent<Graphic>();
                if (graphic != null)
                {
                    _pressedMaterial = graphic.material;
                }

                _pressedOverlay.SetActive(false);
            }

            var iconTr = transform.Find("view/icon");
            if (iconTr != null)
            {
                _iconImage = iconTr.GetComponent<Image>();
            }

            var skillNameTr = transform.Find("SkillName");
            if (skillNameTr != null)
            {
                _skillNameText = skillNameTr.GetComponent<TextMeshProUGUI>();
            }
        }

        public void RefreshView()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            var player = glm?.playerLogicEntity;
            if (player == null || glm == null || glm.PlayerHumanMode || !player.DisguiseIfPossible)
            {
                gameObject.SetActive(false);
                SetChargeVisual(false, 0);
                return;
            }

            gameObject.SetActive(true);

            bool isExposed = player.IsExposed;
            string skillId = isExposed ? ReturnDisguiseSkillId : EnterExposeSkillId;
            RefreshSkillLabel(skillId, isExposed);

            bool showCharge = false;
            float progress = 0f;
            if (!isExposed
                && player.ablilityManager != null
                && player.ablilityManager.TryGetActiveHoldViewState(out var holdState)
                && holdState.IsActive
                && holdState.SkillId == EnterExposeSkillId
                && holdState.IsKeyHeld)
            {
                showCharge = true;
                progress = holdState.Progress01;
            }

            SetChargeVisual(showCharge, progress);
        }

        void RefreshSkillLabel(string skillId, bool isExposed)
        {
            var cfg = SkillLibrary.GetSkillConfig(skillId);
            if (_skillNameText != null)
            {
                _skillNameText.text = cfg != null && !string.IsNullOrEmpty(cfg.Desc)
                    ? cfg.Desc
                    : (isExposed ? "返回伪装" : "蓄力暴露");
            }

            if (_iconImage == null || cfg == null || string.IsNullOrEmpty(cfg.IconPath))
            {
                return;
            }

            var iconSprite = SimpleResManager.Load<Sprite>($"Sprites/Skill/{cfg.IconPath}");
            if (iconSprite != null)
            {
                _iconImage.sprite = iconSprite;
            }
        }

        void SetChargeVisual(bool active, float progress)
        {
            if (_pressedOverlay != null)
            {
                _pressedOverlay.SetActive(active);
            }

            if (_pressedMaterial == null)
            {
                return;
            }

            _pressedMaterial.SetFloat(HoldProgressId, active ? Mathf.Clamp01(progress) : 0f);
        }
    }

    public class OverworldHudDayPeriodIndicator : MonoBehaviour
    {
        public TextMeshProUGUI PeriodText;

        public void RefreshView(GameLogicManager glm)
        {

        }
    }

    public class OverworldWantedIndicator : MonoBehaviour
    {
        public TextMeshProUGUI WantedValText;
        public Transform MarkContainer;
        public List<GameObject> WantedMarkList = new();

        public void BindView()
        {
            WantedValText = transform.Find("TxtLevel").GetComponent<TextMeshProUGUI>();

            MarkContainer = transform.Find("Marks");
            WantedMarkList.Clear();
            for (int i = 0; i < MarkContainer.childCount; i++)
            {
                WantedMarkList.Add(MarkContainer.GetChild(i).gameObject);
            }
        }

        public void RefreshView()
        {
            var wantedLevel = MainGameManager.Instance.gameLogicManager.WantedManager.GetWantedStarLevel();
            WantedValText.text = wantedLevel.ToString();

            for (int i = 0; i < wantedLevel; i++)
            {
                WantedMarkList[i].gameObject.SetActive(true);
            }

            for (int i = wantedLevel; i < WantedMarkList.Count; i++)
            {
                WantedMarkList[i].gameObject.SetActive(false);
            }
        }
    }

    public class OverworldHudEstrusIndicator : MonoBehaviour
    {
        public ParticleSystem MainPs;

        private int cachedPlayerDesireLevel = -1;

        public void Init()
        {
            MainPs = transform.Find("MainPs").GetComponent<ParticleSystem>();
        }

        public void CheckEstrusUpdate()
        {
            var player = MainGameManager.Instance.gameLogicManager.LocalPlayerEntity;
            if (player == null)
            {
                return;
            }

            int level = player.DesireLevel;
            if (cachedPlayerDesireLevel == level)
            {
                return;
            }

            cachedPlayerDesireLevel = level;

            switch (cachedPlayerDesireLevel)
            {
                case 0:
                    ModifyParticleStyle(null, 0.7f, 0, 0, 2.0f);
                    break;
                case 1:
                    ModifyParticleStyle(null, 0.7f, 0, 2, 2.0f);
                    break;
                case 2:
                    ModifyParticleStyle(null, 0.7f, 0, 4, 2.0f);
                    break;
                case 3:
                    ModifyParticleStyle(null, 0.7f, 0, 6, 2.0f);
                    break;
                case 4:
                    ModifyParticleStyle(null, 0.7f, 0, 7, 2.0f);
                    break;
                case 5:
                    ModifyParticleStyle(null, 0.7f, 0, 10, 2.0f);
                    break;
            }
        }

        public void ModifyParticleStyle(Texture2D newTexture, float alpha, float startSpeed, float density, float duration)
        {
            if (MainPs == null)
            {
                Debug.LogError("未绑定 ParticleSystem!");
                return;
            }
            MainPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var mainModule = MainPs.main;
            mainModule.duration = duration;
            mainModule.startSpeed = startSpeed;

            Color currentColor = mainModule.startColor.color;
            currentColor.a = Mathf.Clamp01(alpha);
            mainModule.startColor = currentColor;

            var emissionModule = MainPs.emission;
            emissionModule.rateOverTime = density;

            if (newTexture != null)
            {
                ParticleSystemRenderer psRenderer = MainPs.GetComponent<ParticleSystemRenderer>();

                if (psRenderer != null && psRenderer.material != null)
                {
                    psRenderer.material.SetTexture("_MainTex", newTexture);
                }
            }

            MainPs.Clear();
            MainPs.Play();
        }
    }

    public class OverworldHudAlertHintIndicator : MonoBehaviour
    {
        public TextMeshProUGUI AlertValText;
        public Image FilledAlertBar;
        public Image TempAlertBar;

        GameObject _floatTextPrefab;

        public void BindView()
        {
            AlertValText = transform.Find("Val").GetComponent<TextMeshProUGUI>();
            FilledAlertBar = transform.Find("AlertValBar").GetComponent<Image>();
            TempAlertBar = transform.Find("ContentBar").GetComponent<Image>();

            var floatTextTr = transform.Find("OntCostHint");
            if (floatTextTr != null)
            {
                _floatTextPrefab = floatTextTr.gameObject;
            }
        }

        public void RefreshView()
        {
            var areaManager = MainGameManager.Instance?.gameLogicManager?.AreaManager;
            if (areaManager == null)
            {
                return;
            }

            AlertValText.text = areaManager.AreaAlertValue.ToString();

            var tempVal = areaManager.GetTempAlertValue();
            var filledVal = areaManager.AreaAlertValue;

            var filledRate = filledVal * 1.0f / areaManager.MaxAlertValue;
            filledRate = Mathf.Clamp(filledRate, 0, 1);

            var totalRate = (filledVal + tempVal) * 1.0f / areaManager.MaxAlertValue;
            totalRate = Mathf.Clamp(totalRate, 0, 1);

            FilledAlertBar.fillAmount = filledRate;
            TempAlertBar.fillAmount = totalRate;
        }

        public void DoPendingAlertReduce(long val)
        {
            if (_floatTextPrefab == null)
            {
                return;
            }

            GameObject go = Instantiate(_floatTextPrefab, transform.position, Quaternion.identity, transform);
            go.SetActive(true);

            HudSimpleFloatingText popup = go.GetComponent<HudSimpleFloatingText>();
            popup.Setup("-" + val, transform.position, Color.black);
        }
    }

    public class OverworldHudRetreatHintIndicator : MonoBehaviour
    {
        Image _barContent;
        TextMeshProUGUI _valText;

        public void BindView()
        {
            _barContent = transform.Find("BarContent")?.GetComponent<Image>();
            _valText = transform.Find("Val")?.GetComponent<TextMeshProUGUI>();
        }

        public void RefreshView()
        {
            var player = MainGameManager.Instance?.gameLogicManager?.playerLogicEntity;
            if (player == null)
            {
                gameObject.SetActive(false);
                return;
            }

            if (!player.IsRetreating)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            float progress = Mathf.Clamp01(
                (LogicTime.time - player.RetreatingStartTime) / PlayerLogicEntity.RetreatDuration);

            if (_barContent != null)
            {
                _barContent.fillAmount = progress;
            }

            if (_valText != null)
            {
                _valText.text = Mathf.RoundToInt(progress * 100f).ToString();
            }
        }
    }
}
