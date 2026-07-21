using System.Collections.Generic;
using My.Map;
using UnityEngine;

namespace My.Farm
{
    // 单格作物表现 + 成熟时可交互收获
    public sealed class FarmCropCellView : MonoBehaviour, ISceneInteractable
    {
        string _plotId;
        string _logicAreaId;
        FarmCellPersist _cell;
        FarmSystem _farm;
        SpriteRenderer _sr;

        public string ShowName
        {
            get
            {
                if (_cell == null || string.IsNullOrEmpty(_cell.CropId))
                {
                    return "空地";
                }

                var crop = FarmCatalog.GetCrop(_cell.CropId);
                return crop?.DisplayName ?? _cell.CropId;
            }
        }

        public Vector2 Pos => transform.position;
        public bool WithInteractDetail => true;
        public bool InteractFocused { get; set; }
        public bool IsInteractDetail { get; set; }

        public void Bind(
            string plotId,
            FarmCellPersist cell,
            float cellSize,
            Vector2 origin,
            Color color,
            FarmSystem farm,
            string logicAreaId)
        {
            _plotId = plotId;
            _cell = cell;
            _farm = farm;
            _logicAreaId = logicAreaId;

            transform.position = new Vector3(
                origin.x + (cell.Cx + 0.5f) * cellSize,
                origin.y + (cell.Cy + 0.5f) * cellSize,
                0f);

            EnsureRenderer();
            EnsureCollider(cellSize);
            _sr.color = color;
            float s = Mathf.Clamp(cellSize * 0.85f, 0.35f, 1.2f);
            transform.localScale = new Vector3(s, s, 1f);
            gameObject.SetActive(true);
        }

        void EnsureRenderer()
        {
            if (_sr != null)
            {
                return;
            }

            _sr = gameObject.GetComponent<SpriteRenderer>();
            if (_sr == null)
            {
                _sr = gameObject.AddComponent<SpriteRenderer>();
            }

            if (_sr.sprite == null)
            {
                _sr.sprite = CreateWhiteSprite();
            }

            _sr.sortingOrder = 20;
        }

        void EnsureCollider(float cellSize)
        {
            var col = GetComponent<BoxCollider2D>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider2D>();
            }

            col.isTrigger = true;
            col.size = Vector2.one * 0.9f;
            gameObject.layer = LayerMask.NameToLayer("MapTarget");
        }

        static Sprite _white;

        static Sprite CreateWhiteSprite()
        {
            if (_white != null)
            {
                return _white;
            }

            var tex = Texture2D.whiteTexture;
            _white = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16f);
            return _white;
        }

        public bool CanInteractEnable()
        {
            return _farm != null && _cell != null && !string.IsNullOrEmpty(_cell.CropId);
        }

        public Vector3 GetHintAnchorPosition() => transform.position;

        public float GetHintOffsetInfos() => 0.35f;

        public bool IsAutoInteract() => false;

        public List<SceneInteractSelection> GetInteractSelections()
        {
            var list = new List<SceneInteractSelection>();
            if (_farm == null || _cell == null || string.IsNullOrEmpty(_cell.CropId))
            {
                return list;
            }

            var crop = FarmCatalog.GetCrop(_cell.CropId);
            if (crop == null)
            {
                return list;
            }

            if (FarmCatalog.IsMature(crop, _cell.GrowProgress))
            {
                list.Add(new SceneInteractSelection
                {
                    SelectId = 1,
                    SelectContent = "收获 " + crop.DisplayName,
                });
            }
            else if (!FarmCatalog.IsSprouted(crop, _cell.GrowProgress))
            {
                list.Add(new SceneInteractSelection
                {
                    SelectId = 2,
                    SelectContent = "种子：" + crop.DisplayName,
                    Selectable = false,
                });
            }
            else
            {
                list.Add(new SceneInteractSelection
                {
                    SelectId = 3,
                    SelectContent = crop.DisplayName + " 生长中",
                    Selectable = false,
                });
            }

            if (!_cell.Watered)
            {
                list.Add(new SceneInteractSelection
                {
                    SelectId = 10,
                    SelectContent = "浇水",
                });
            }

            if (!_cell.Fertilized)
            {
                list.Add(new SceneInteractSelection
                {
                    SelectId = 11,
                    SelectContent = "施肥",
                });
            }

            return list;
        }

        public bool TriggerInteract(int selectionId, int playerId)
        {
            if (_farm == null || _cell == null)
            {
                return false;
            }

            switch (selectionId)
            {
                case 1:
                    return _farm.TryHarvestCell(_logicAreaId, _plotId, _cell.Cx, _cell.Cy, fromPlayerInteract: true);
                case 10:
                    return _farm.TryWaterCell(_logicAreaId, _plotId, _cell.Cx, _cell.Cy);
                case 11:
                    return _farm.TryFertilizeCell(_logicAreaId, _plotId, _cell.Cx, _cell.Cy);
                default:
                    return true;
            }
        }
    }
}
