using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldAreaRoot : MonoBehaviour
{
    public Grid Grid;
    
    public Tilemap[] TileGrounds;
    public Tilemap TileHole;

    public Transform PlayerBornPos;

    public Transform StaticPrefabRoot;
}
