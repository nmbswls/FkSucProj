using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GP/Dialog/DIalogueCharacterDatabase")]
public class DIalogueCharacterDatabase : ScriptableObject
{
    [Serializable]
    public class Character
    {
        public string id;
        public List<Expression> expressions = new List<Expression>();
    }
    [Serializable]
    public class Expression
    {
        public string id;
        public Sprite sprite;
    }

    public List<Character> characters = new List<Character>();

    public Sprite LoadSprite(string charId, string exprId)
    {
        var c = characters.Find(x => x.id == charId);
        if (c == null) return null;
        var e = c.expressions.Find(x => x.id == exprId);
        if (e == null) return null;
        return e.sprite;
    }
}