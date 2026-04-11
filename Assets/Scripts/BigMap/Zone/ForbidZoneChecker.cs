using System;
using System.Collections;
using System.Collections.Generic;
using cfg.demo;
using UnityEngine;

namespace My
{

    public class ForbidZoneChecker : MonoBehaviour
    {

        public Collider2D InnerCol;
        public Collider2D OuterCol;
        
        public List<CommonCheckCond> EnableCondition = new();
    }
}


