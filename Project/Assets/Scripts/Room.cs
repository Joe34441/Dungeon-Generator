using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    [SerializeField] GameObject northDoor = null;
    [SerializeField] GameObject eastDoor = null;
    [SerializeField] GameObject southDoor = null;
    [SerializeField] GameObject westDoor = null;

    [SerializeField] List<GameObject> middleTiles = new List<GameObject>();
    [SerializeField] List<GameObject> northTiles = new List<GameObject>();
    [SerializeField] List<GameObject> eastTiles = new List<GameObject>();
    [SerializeField] List<GameObject> southTiles = new List<GameObject>();
    [SerializeField] List<GameObject> westTiles = new List<GameObject>();

    private int layer = -1;
    public int GetLayer() { return layer; }
    public void SetLayer(int _layer) { layer = _layer; }

    public void DestroyDoor(Cell.Direction direction)
    {
        if (direction == Cell.Direction.North && northDoor != null) DynamicDestroy(northDoor);
        if (direction == Cell.Direction.East && eastDoor != null) DynamicDestroy(eastDoor);
        if (direction == Cell.Direction.South && southDoor != null) DynamicDestroy(southDoor);
        if (direction == Cell.Direction.West && westDoor != null) DynamicDestroy(westDoor);
    }

    public GameObject GetDoor(Cell.Direction direction)
    {
        if (direction == Cell.Direction.North && northDoor != null) return northDoor;
        if (direction == Cell.Direction.East && eastDoor != null) return eastDoor;
        if (direction == Cell.Direction.South && southDoor != null) return southDoor;
        if (direction == Cell.Direction.West && westDoor != null) return westDoor;
        
        return null;
    }

    public List<GameObject> GetTiles(Cell.Direction direction)
    {
        if (direction == Cell.Direction.Middle) return middleTiles;
        if (direction == Cell.Direction.North) return northTiles;
        if (direction == Cell.Direction.East) return eastTiles;
        if (direction == Cell.Direction.South) return southTiles;
        if (direction == Cell.Direction.West) return westTiles;

        return null;
    }

    private void DynamicDestroy(GameObject go)
    {
        if (Application.isPlaying) Destroy(go);
        else DestroyImmediate(go);
    }
}
