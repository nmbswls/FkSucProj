using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldBootstrap : MonoBehaviour
{
    public WorldAreaInfo initialArea;


    void Start()
    {
        InitializeWorldArea();
    }


    public void InitializeWorldArea()
    {
        MainGameManager.Instance.gameLogicManager.OnPlayerEnterArea("1");
    }
}