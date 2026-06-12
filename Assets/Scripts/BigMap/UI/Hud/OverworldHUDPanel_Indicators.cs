using System.Collections.Generic;
using My.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static My.GameLogicManager;

namespace My.UI
{
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
}
