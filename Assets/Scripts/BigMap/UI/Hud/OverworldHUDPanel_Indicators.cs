using System.Collections.Generic;
using My.Map;
using My.Map.Entity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static My.GameLogicManager;

namespace My.UI
{
    public class OverworldHudExposeSkillIndicator : MonoBehaviour
    {
        public enum ExposeSkillGlowShape
        {
            Circle = 0,
            Square = 1,
        }

        const string EnterExposeSkillId = "player_enter_expose";
        const string ReturnDisguiseSkillId = "player_return_disguise";
        const float PressedScale = 0.94f;

        static readonly int HoldProgressId = Shader.PropertyToID("_HoldProgress");
        static readonly int ShapeHalfWidthId = Shader.PropertyToID("_ShapeHalfWidth");
        static readonly int ShapeHalfHeightId = Shader.PropertyToID("_ShapeHalfHeight");
        static readonly int CornerRadiusId = Shader.PropertyToID("_CornerRadius");
        static readonly int RectWidthPxId = Shader.PropertyToID("_RectWidthPx");
        static readonly int RectHeightPxId = Shader.PropertyToID("_RectHeightPx");

        [SerializeField] ExposeSkillGlowShape _pressedGlowShape = ExposeSkillGlowShape.Square;
        [Tooltip("火光描边形状半宽/半高（像素），不填 icon 时生效")]
        [SerializeField] Vector2 _pressedGlowSize = new Vector2(48f, 48f);
        [SerializeField] float _pressedGlowCornerRadius = 0.06f;

        RectTransform _viewRoot;
        RectTransform _iconRect;
        GameObject _pressedOverlay;
        ExposeSkillFireOutlineGraphic _pressedGraphic;
        Image _iconImage;
        TextMeshProUGUI _skillNameText;
        Material _pressedMaterial;
        CanvasGroup _viewCanvasGroup;

        public void BindView()
        {
            var viewTr = transform.Find("view");
            if (viewTr != null)
            {
                _viewRoot = viewTr as RectTransform;
                _viewCanvasGroup = viewTr.GetComponent<CanvasGroup>();
                if (_viewCanvasGroup == null)
                {
                    _viewCanvasGroup = viewTr.gameObject.AddComponent<CanvasGroup>();
                }
                _viewCanvasGroup.blocksRaycasts = false;

                var iconTr = viewTr.Find("icon");
                if (iconTr != null)
                {
                    _iconRect = iconTr as RectTransform;
                    _iconImage = iconTr.GetComponent<Image>();
                }

                var pressedTr = viewTr.Find("Pressed");
                if (pressedTr != null)
                {
                    _pressedOverlay = pressedTr.gameObject;
                    _pressedGraphic = pressedTr.GetComponent<ExposeSkillFireOutlineGraphic>();
                    if (_pressedGraphic != null)
                    {
                        _pressedGraphic.raycastTarget = false;
                        if (_pressedGraphic.material != null)
                        {
                            _pressedMaterial = _pressedGraphic.material;
                        }
                    }
                }
            }

            SyncGlowShapeMaterial();

            var skillNameTr = transform.Find("SkillName");
            if (skillNameTr != null)
            {
                _skillNameText = skillNameTr.GetComponent<TextMeshProUGUI>();
            }

            if (_pressedOverlay != null)
            {
                _pressedOverlay.SetActive(false);
            }

            if (_viewRoot != null)
            {
                _viewRoot.localScale = Vector3.one;
            }
        }

        public void RefreshView()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            var player = glm?.playerLogicEntity;
            if (player == null || glm == null)
            {
                gameObject.SetActive(false);
                return;
            }

            bool canShow = !glm.PlayerHumanMode && player.DisguiseIfPossible;
            if (!canShow)
            {
                gameObject.SetActive(false);
                SetActiveVisual(false);
                return;
            }

            gameObject.SetActive(true);

            bool isExposed = player.IsExposed;
            string activeSkillId = isExposed ? ReturnDisguiseSkillId : EnterExposeSkillId;
            ApplySkillPresentation(activeSkillId, isExposed);

            bool showActiveVisual = false;
            float progress = 0f;
            bool usePressedScale = false;
            if (player.ablilityManager != null
                && player.ablilityManager.TryGetActiveHoldViewState(out var holdState)
                && holdState.SkillId == activeSkillId
                && holdState.IsActive)
            {
                if (!isExposed && holdState.IsKeyHeld)
                {
                    showActiveVisual = true;
                    progress = holdState.Progress01;
                    usePressedScale = true;
                }
                else if (isExposed)
                {
                    showActiveVisual = true;
                    progress = holdState.Progress01;
                }
            }

            SetActiveVisual(showActiveVisual, progress, usePressedScale);
        }

        void ApplySkillPresentation(string skillId, bool isExposed)
        {
            var cfg = SkillLibrary.GetSkillConfig(skillId);
            if (_skillNameText != null)
            {
                if (cfg != null && !string.IsNullOrEmpty(cfg.Desc))
                {
                    _skillNameText.text = cfg.Desc;
                }
                else
                {
                    _skillNameText.text = isExposed ? "返回伪装" : "蓄力暴露";
                }
            }

            if (_iconImage != null && cfg != null && !string.IsNullOrEmpty(cfg.IconPath))
            {
                var iconSprite = SimpleResManager.Load<Sprite>($"Sprites/Skill/{cfg.IconPath}");
                if (iconSprite != null)
                {
                    _iconImage.sprite = iconSprite;
                }
            }
        }

        void SetActiveVisual(bool active, float progress = 0f, bool usePressedScale = false)
        {
            if (_pressedOverlay != null)
            {
                _pressedOverlay.SetActive(active);
            }

            if (_pressedMaterial != null)
            {
                SyncGlowShapeMaterial();
                if (active)
                {
                    _pressedMaterial.SetFloat(HoldProgressId, Mathf.Clamp01(progress));
                }
            }

            if (_viewRoot != null)
            {
                _viewRoot.localScale = active && usePressedScale ? Vector3.one * PressedScale : Vector3.one;
            }
        }

        void SyncGlowShapeMaterial()
        {
            if (_pressedMaterial == null || _pressedGraphic == null)
            {
                return;
            }

            var pressedRect = _pressedGraphic.rectTransform.rect;
            float rectW = Mathf.Max(pressedRect.width, 1f);
            float rectH = Mathf.Max(pressedRect.height, 1f);

            Vector2 glowSize = _pressedGlowSize;
            if (_iconRect != null)
            {
                var iconSize = _iconRect.rect.size;
                if (iconSize.x > 0f && iconSize.y > 0f)
                {
                    glowSize = iconSize;
                }
            }

            if (_pressedGlowShape == ExposeSkillGlowShape.Circle)
            {
                _pressedMaterial.EnableKeyword("_SHAPETYPE_CIRCLE");
                _pressedMaterial.DisableKeyword("_SHAPETYPE_SQUARE");
                float radius = Mathf.Min(glowSize.x, glowSize.y) * 0.5f;
                glowSize = new Vector2(radius * 2f, radius * 2f);
            }
            else
            {
                _pressedMaterial.EnableKeyword("_SHAPETYPE_SQUARE");
                _pressedMaterial.DisableKeyword("_SHAPETYPE_CIRCLE");
            }

            _pressedMaterial.SetFloat(ShapeHalfWidthId, Mathf.Clamp(glowSize.x * 0.5f / rectW, 0.01f, 0.5f));
            _pressedMaterial.SetFloat(ShapeHalfHeightId, Mathf.Clamp(glowSize.y * 0.5f / rectH, 0.01f, 0.5f));
            _pressedMaterial.SetFloat(CornerRadiusId, _pressedGlowCornerRadius);
            _pressedMaterial.SetFloat(RectWidthPxId, rectW);
            _pressedMaterial.SetFloat(RectHeightPxId, rectH);
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

        private int cachedPlayerEstrusLevel = -1;

        public void Init()
        {
            MainPs = transform.Find("MainPs").GetComponent<ParticleSystem>();
        }

        public void CheckEstrusUpdate()
        {
            var player = MainGameManager.Instance.gameLogicManager.playerLogicEntity;
            if (player == null)
            {
                return;
            }

            int level = (int)(player.GetAttr(AttrIdConsts.PlayerEstrusProgrss) / 1000 / 20);
            if (cachedPlayerEstrusLevel == level)
            {
                return;
            }

            cachedPlayerEstrusLevel = level;

            switch (cachedPlayerEstrusLevel)
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
