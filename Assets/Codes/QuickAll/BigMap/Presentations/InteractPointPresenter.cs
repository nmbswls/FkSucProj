using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class InteractPointPresenter : ScenePresentationBase<InteractPointLogic>, ISceneInteractable
{
    [SerializeField] private SpriteRenderer icon;
    [SerializeField] private GameObject highlightFx;

    public event Action<bool> EventOnInteractStateChanged;

    public Vector3 GetHintAnchorPosition()
    {
        return GetWorldPosition();
    }

    public void SetInteractExpandStatus(bool expanded)
    {
        EventOnInteractStateChanged?.Invoke(expanded);
    }

    public void TriggerInteract(string interactSelection)
    {
        throw new System.NotImplementedException();
    }

    public List<string> GetInteractSelections()
    {
        return new() { "Int" };
    }

    public bool CanInteractEnable()
    {
        return true;
    }
}
