using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Settings", menuName = "ScriptableObjects/Settings", order = 1)]
public class ScriptableSettings : ScriptableObject
{
    public Settings dungeonSettings;
}

[Serializable]
public class Settings
{
    [Header("Dungeon")]
    public int mazeSize;
    public GameObject mazeEntrancePosition;
    public string markerTag;
    public GameObject deadendMarker;
    public Material longestPathFloorMaterial;

    [Header("Rooms")]
    public string roomTag;
    public Material floorMaterial;
    public GameObject stairRoom;
    public List<GameObject> normalRoomVariations;
    public List<int> variationWeighting;

    [Header("Tiles")]
    public GameObject floorTile;
    public int floorTilesPerLength;
    public GameObject wallTile;
    public int wallTilesPerHeight;

    [Header("Example Rooms")]
    public GameObject exampleRoom;
    public int exampleFloorTilesPerLength;
    public GameObject exampleFloorTile;

    [Header("Generation")]
    public bool useRandomSeed;
    public int seed;
    public bool useExampleRoom;
    public bool markDeadends;
    public bool markLongestPath;
    public bool cutOff;
    public int cutOffPoint;

    [Header("Runtime")]
    public bool infiniteVerticalGeneration = false;

}