
using UnityEngine;

namespace My.Dialog
{

    public interface IDialogueActor
    {

        bool DialogControlled { get; set; }

        bool DialogMoveFinished { get; set; }

        void OnDialogStart();

        void OnDialogEnd();

        void DoDialogMove(Vector2 targetPos, float speed, Vector2? forcedStartPos);

        void DoDialogAnimation(string animName);
    }


}