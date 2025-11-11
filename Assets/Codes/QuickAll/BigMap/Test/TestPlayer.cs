using System.Collections;
using System.Collections.Generic;
using Map.Entity;
using Map.Scene;
using UnityEngine;

public class TestPlayer : MonoBehaviour
{
    public SimpleCharacterController CharacterController;

    public void Update()
    {
        var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f) input.Normalize();

        CharacterController.DesiredVel = input * 3.0f;
    }
}
