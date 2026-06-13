using My;
using My.Map.Scene;
using UnityEngine;

public static class GrappleHookLineUtil
{
    // Unity LineRenderer Tile 模式：UV = 世界长度 × textureScale.x
    // 要让每节固定占 linkLengthWorld 米，textureScale.x 必须是常数 1/linkLength，与绳长无关
    public static float CalcChainTextureScale(float linkLengthWorld)
    {
        return 1f / Mathf.Max(linkLengthWorld, 0.01f);
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
