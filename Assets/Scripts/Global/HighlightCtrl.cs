

using System.Collections.Generic;
using System.Linq;
using HighlightPlus;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace My
{

    public class HighlightCtrl : MonoBehaviour
    {

        public HashSet<string> HighlightReason = new HashSet<string>();

        public HighlightEffect[] ControlledEffects;

        private void OnEnable()
        {
            GlobalHighlightManager.OnClearHighlightByReason += HandleClearByReason;
        }

        private void OnDisable()
        {
            GlobalHighlightManager.OnClearHighlightByReason -= HandleClearByReason;
        }

        private void HandleClearByReason(string reason)
        {
            if (HighlightReason.Contains(reason))
            {
                HighlightReason.Remove(reason);

                if (HighlightReason.Count > 0)
                {
                    if (ControlledEffects != null)
                    {
                        foreach (var e in ControlledEffects)
                        {
                            e.highlighted = true;
                        }
                    }
                }
                else
                {
                    if (ControlledEffects != null)
                    {
                        foreach (var e in ControlledEffects)
                        {
                            e.highlighted = false;
                        }
                    }
                }
            }
        }

        



        public void SetHighlightStatus(bool isHighlight, string highlightReason)
        {
            if(isHighlight)
            {
                if (!HighlightReason.Contains(highlightReason))
                {
                    HighlightReason.Add(highlightReason);
                }
            }
            else
            {
                HighlightReason.Remove(highlightReason);
            }
            
            if (this == null || gameObject == null || gameObject.IsDestroyed())
            {
                return;
            }

            if(HighlightReason.Count > 0)
            {
                if (ControlledEffects != null)
                {
                    foreach (var e in ControlledEffects)
                    {
                        e.highlighted = true;
                    }
                }
            }
            else
            {
                if (ControlledEffects != null)
                {
                    foreach (var e in ControlledEffects)
                    {
                        e.highlighted = false;
                    }
                }
            }
        }
    }
}

