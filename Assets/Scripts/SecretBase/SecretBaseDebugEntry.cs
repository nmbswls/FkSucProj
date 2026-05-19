using My;
using UnityEngine;

namespace My.SecretBase
{
    // 开发测试：挂到任意物体，按 F9 进入隐秘据点，F10 离开。
    public class SecretBaseDebugEntry : MonoBehaviour
    {
        void Update()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                return;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.F9))
            {
                glm.EnterSecretBase(SecretBaseDefs.DefaultSpawnPoint);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.F10))
            {
                glm.ExitSecretBase();
            }
        }
    }
}
