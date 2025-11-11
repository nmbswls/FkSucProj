using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldBootstrap : MonoBehaviour
{
    public WorldAreaInfo initialArea;


    void Start()
    {
        InitializeGame();
    }


    public void InitializeGame()
    {
         MainGameManager.Instance.InitStartGame("a", () =>
        {
            Debug.Log("InitializeGame finished");
        });
    }
}