using System.Collections.Generic;
using cfg.demo;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 秘密基地书房剪贴板：浏览已解锁的人类知识 EventGrant
    public sealed class KnowledgeClipboardPanel : PanelWithInput
    {
        public const string Pid = "KnowledgeClipboardPanel";

        [SerializeField] Button closeButton;
        [SerializeField] RectTransform noteBoard;
        [SerializeField] KnowledgeClipboardNoteView noteTemplate;
        [SerializeField] TMP_Text emptyHintText;
        [SerializeField] TMP_Text titleText;

        readonly List<EventGrant> _grants = new();
        readonly List<KnowledgeClipboardNoteView> _spawned = new();

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = Pid;
            }

            layer = UILayer.Popup;
            closeButton?.onClick.AddListener(Close);
            if (noteTemplate != null)
            {
                noteTemplate.gameObject.SetActive(false);
            }
        }

        public override void Show()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null || !glm.IsInSecretBaseContext())
            {
                UIManager.Instance.HidePanel(Pid);
                return;
            }

            base.Show();
            Refresh();
        }

        public override void Hide()
        {
            ClearSpawned();
            base.Hide();
        }

        public override bool OnCancel()
        {
            Close();
            return true;
        }

        void Close()
        {
            UIManager.Instance.HidePanel(Pid);
        }

        void Refresh()
        {
            ClearSpawned();

            var grants = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.EventGrantSystem;
            if (grants == null)
            {
                SetEmptyVisible(true);
                return;
            }

            grants.CollectUnlockedKnowledgeGrants(_grants);
            SetEmptyVisible(_grants.Count == 0);
            if (_grants.Count == 0 || noteBoard == null || noteTemplate == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutNotes();
        }

        void LayoutNotes()
        {
            float boardW = noteBoard.rect.width;
            float boardH = noteBoard.rect.height;
            if (boardW < 8f)
            {
                boardW = 640f;
            }

            if (boardH < 8f)
            {
                boardH = 360f;
            }

            const float noteW = 200f;
            const float noteH = 120f;
            int cols = Mathf.Max(1, Mathf.FloorToInt((boardW - 40f) / (noteW + 24f)));
            float originX = -boardW * 0.5f + 28f + noteW * 0.5f;
            float originY = boardH * 0.5f - 28f - noteH * 0.5f;
            float stepX = noteW + 28f;
            float stepY = noteH + 24f;

            for (int i = 0; i < _grants.Count; i++)
            {
                var grant = _grants[i];
                int seed = StableHash(grant.Id);
                int col = i % cols;
                int row = i / cols;
                float jitterX = ((seed & 0xff) / 255f - 0.5f) * 36f;
                float jitterY = (((seed >> 8) & 0xff) / 255f - 0.5f) * 28f;
                float angle = (((seed >> 16) & 0xff) / 255f - 0.5f) * 10f;

                var note = Instantiate(noteTemplate, noteBoard);
                note.gameObject.SetActive(true);
                note.name = "Note_" + grant.Id;
                note.Bind(grant.Name, grant.Desc, seed);

                var rt = note.transform as RectTransform;
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(noteW, noteH);
                    rt.anchoredPosition = new Vector2(
                        originX + col * stepX + jitterX,
                        originY - row * stepY + jitterY);
                    rt.localRotation = Quaternion.Euler(0f, 0f, angle);
                }

                _spawned.Add(note);
            }
        }

        void ClearSpawned()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                {
                    Destroy(_spawned[i].gameObject);
                }
            }

            _spawned.Clear();
        }

        void SetEmptyVisible(bool visible)
        {
            if (emptyHintText != null)
            {
                emptyHintText.gameObject.SetActive(visible);
                if (visible)
                {
                    emptyHintText.text = "剪贴板还是空的。\n去世界里多听听传闻、多看看风景吧。";
                }
            }
        }

        static int StableHash(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return 0;
            }

            unchecked
            {
                int h = 23;
                for (int i = 0; i < s.Length; i++)
                {
                    h = h * 31 + s[i];
                }

                return h;
            }
        }
    }
}
