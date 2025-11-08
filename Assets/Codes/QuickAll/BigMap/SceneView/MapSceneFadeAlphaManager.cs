using Map.Scene;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSceneFadeAlphaManager : MonoBehaviour
{
    // Start is called before the first frame update

    public Dictionary<string, MapRoomProvider> SceneRoomCeilDict = new();
    private MapRoomProvider? CurrFadeCeil;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// «¯”Ú≥ı ºªØ
    /// </summary>
    /// <param name="sceneRoot"></param>
    public void OnEnterArea(GameObject sceneRoot)
    {
        var roomRoot = sceneRoot.transform.Find("RoomRoot");
        for(int i=0;i<roomRoot.childCount;i++)
        {
            var child = roomRoot.GetChild(i);
            var roomInfo = child.GetComponent<MapRoomProvider>();
            if (roomInfo != null)
            {
                SceneRoomCeilDict.Add(roomInfo.RoomId, roomInfo);
            }
        }
    }

    public void RefreshCeilFadeEffect(string? currRoom)
    {
        if(CurrFadeCeil != null)
        {
            CurrFadeCeil.ShowFadeCeil();
        }

        if(string.IsNullOrEmpty(currRoom))
        {
            CurrFadeCeil = null;
        }
        else if(SceneRoomCeilDict.TryGetValue(currRoom, out var roomProvider))
        {
            CurrFadeCeil = roomProvider;
            roomProvider.HideFadeCeil();
        }
        else
        {
            CurrFadeCeil = null;
        }
    }



    public void OnLeaveArea()
    {
        SceneRoomCeilDict.Clear();
        CurrFadeCeil = null;
    }
}
