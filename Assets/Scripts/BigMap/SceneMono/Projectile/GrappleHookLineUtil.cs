using My;
using My.Map.Scene;
using UnityEngine;

public static class GrappleHookLineUtil
{
    public static float CalcChainTextureScale(float ropeLengthWorld, float linkLengthWorld)
    {
        return ropeLengthWorld / Mathf.Max(linkLengthWorld, 0.01f);
    }

    public static void ApplyLineAlpha(LineRenderer lr, float alpha)
    {
        if (lr == null)
        {
            return;
        }

        var c = new Color(1f, 1f, 1f, alpha);
        lr.startColor = c;
        lr.endColor = c;
    }

    public static bool TryResolveAnchorWorld(long casterEntityId, out Vector2 anchorWorld)
    {
        anchorWorld = default;

        SceneUnitPresenter sup = TryGetPresenter(casterEntityId);
        if (sup == null)
        {
            return false;
        }

        if (sup.TryGetThrowGrappleLogicPos(string.Empty, out var logicPos))
        {
            anchorWorld = (Vector2)MainGameManager.Instance.GetWorldPosFromLogicPos(logicPos);
            return true;
        }

        Transform t = sup.HitPivot != null ? sup.HitPivot : sup.transform;
        anchorWorld = t.position;
        return true;
    }

    static SceneUnitPresenter TryGetPresenter(long casterEntityId)
    {
        var pres = SceneAOIManager.Instance?.GetActivePresentation(casterEntityId);
        if (pres is SceneUnitPresenter fromAoi)
        {
            return fromAoi;
        }

        var playerPres = MainGameManager.Instance?.playerScenePresenter;
        if (playerPres != null && playerPres.Id == casterEntityId)
        {
            return playerPres;
        }

        return null;
    }
}
