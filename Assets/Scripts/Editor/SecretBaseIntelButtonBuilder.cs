using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using My.UI;

namespace My.EditorTools
{
    public static class SecretBaseIntelButtonBuilder
    {
        const string PrefabPath = "Assets/Resources/UI/Prefabs/SecretBaseHudPanel.prefab";

        [MenuItem("Tools/UI/Ensure Secretbase Intel Button")]
        public static void EnsureButton()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var panel = root.GetComponent<SecretBaseHudPanel>();
                var topBar = root.transform.Find("TopRightBar");
                if (panel == null || topBar == null)
                    throw new System.InvalidOperationException("SecretBaseHudPanel or TopRightBar is missing.");

                var buttonTransform = topBar.Find("BtnRumorIntel");
                Button button;
                if (buttonTransform == null)
                {
                    var source = topBar.Find("BtnBuild");
                    if (source == null)
                        throw new System.InvalidOperationException("BtnBuild is missing.");

                    var clone = Object.Instantiate(source.gameObject, topBar);
                    clone.name = "BtnRumorIntel";
                    var rect = clone.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(1f, 1f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(1f, 1f);
                    rect.anchoredPosition = new Vector2(-8f, 0f);
                    rect.sizeDelta = new Vector2(96f, 36f);
                    button = clone.GetComponent<Button>();
                    var label = clone.GetComponentInChildren<TMP_Text>(true);
                    if (label != null) label.text = "情报";
                }
                else
                {
                    button = buttonTransform.GetComponent<Button>();
                }

                var serialized = new SerializedObject(panel);
                serialized.FindProperty("btnRumorIntel").objectReferenceValue = button;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[SecretBaseIntelButtonBuilder] Secretbase intel button is ready.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
