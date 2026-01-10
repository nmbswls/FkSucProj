using UnityEngine;

[CreateAssetMenu(menuName = "GP/Area/WorldAreaInfo", fileName = "WorldAreaInfo")]
public class WorldAreaInfo : ScriptableObject
{
    [Tooltip("世界名称（用于日志/存档）")]
    public string worldName;

    [Tooltip("该世界包含的子场景名称（需添加到 Build Settings）")]
    public string[] subScenes;

    [Tooltip("主激活场景（可选），用于设为 ActiveScene 的子场景名")]
    public string activeSubScene;
}