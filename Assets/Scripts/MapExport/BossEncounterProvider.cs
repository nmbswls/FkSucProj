using System.Collections.Generic;
using UnityEngine;

namespace My.MapExport
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class BossEncounterProvider : MonoBehaviour
    {
        public string EncounterId = string.Empty;
        public string BossUniqName = string.Empty;
        public string DisplayName = string.Empty;
        public Transform ResetPoint;
        public float OutsideGraceTime = 0.75f;
        public float ReturnMoveSpeedRate = 1.4f;
        public float RecoverDuration = 3f;
        public float ReengageDelay = 1.5f;
        public bool InvulnerableWhileReturning = true;
        public List<BossEncounterPhaseExportInfo> Phases = new();

#if UNITY_EDITOR
        void OnValidate()
        {
            var collider = GetComponent<BoxCollider2D>();
            collider.isTrigger = true;
        }
#endif
    }
}
