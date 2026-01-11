using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldBootstrap : MonoBehaviour
{
    public WorldAreaInfo initialArea;


    async void Start()
    {
        await MainGameManager.Instance.InitStartGame("a", () =>
        {
            Debug.Log("InitializeGame finished");
        });
    }

}