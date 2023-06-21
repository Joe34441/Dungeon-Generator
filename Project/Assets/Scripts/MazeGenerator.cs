using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;

public class MazeGenerator : MonoBehaviour
{
#region Variables

    [SerializeField] private ScriptableSettings savedSettings;
    [SerializeField] ScriptableSettings objectToSaveTo;

    [Header("Dungeon")]
    [Range(2, 200)] [SerializeField] private int mazeSize = 2;
    [SerializeField] private GameObject mazeEntrancePosition;
    [SerializeField] private string markerTag = "Marker";
    [SerializeField] private GameObject deadendMarker;
    [SerializeField] private Material longestPathFloorMaterial;
    public static int sMazeSize;
    private bool generate = false;
    private Vector2 entranceRoomPosition = new Vector2();

    [Header("Rooms")]
    [SerializeField] private string roomTag = "Room";
    [SerializeField] private Material floorMaterial;
    [SerializeField] private GameObject stairRoom;
    [SerializeField] private GameObject floorStartRoom;
    [SerializeField] public List<GameObject> normalRoomVariations;
    [Range(1, 100)] [SerializeField] public List<int> variationWeighting = new List<int>(100);

    [Header("Tiles")]
    [SerializeField] private GameObject floorTile;
    [SerializeField] private int floorTilesPerLength;
    [SerializeField] private GameObject wallTile;
    [SerializeField] private int wallTilesPerHeight;

    [Header("Example Rooms")]
    [SerializeField] private GameObject exampleRoom;
    [SerializeField] private GameObject exampleFloorTile;
    [SerializeField] private int exampleFloorTilesPerLength;
    [SerializeField] private int exampleWallTilesPerHeight;

    [Header("Generation")]
    [SerializeField] private bool useRandomSeed = true;
    [Range(1, 100000)] [SerializeField] private int seed = 1;
    [SerializeField] private bool useExampleRoom = true;
    [SerializeField] private bool markDeadends = true;
    [SerializeField] private bool markLongestPath = true;
    [SerializeField] private bool cutOff = false;
    [SerializeField] [Range(1, 100)] private int cutOffPoint = 1;
    [HideInInspector] public List<GameObject> bottomGrid = new List<GameObject>();
    [HideInInspector] public List<GameObject> middleGrid = new List<GameObject>();
    [HideInInspector] public List<GameObject> topGrid = new List<GameObject>();
    [HideInInspector] private List<Vector2> endRoomPositions = new List<Vector2>();
    public static Layer currentLayer = new Layer();
    public static bool sUseRandomSeed;
    public static int sSeed;
    private List<GameObject> deadendMarkers = new List<GameObject>();
    private List<GameObject> longestPathFloorTiles = new List<GameObject>();
    private bool backtracking = false;
    private bool cutOffActivated = false;
    private int numOfVisitedCells;
    public static int sNumOfVisitedCells;

    [Header("Runtime")]
    [SerializeField] private bool infiniteVerticalGeneration = false;
    private int layerNum = 0;


    //int breakcount = 0;

#endregion Variables

#region Button Events

    private void Start()
    {
        //***************************************************************************************************************
        //Internal unity error, force unloading of current assembly to allow restarting of assembly next run //**********
        // - (Stops unity freezing when running play mode more than once after entering an infinite          //**********
        //    loop with a static variable as a condition, where the static variable is null)                 //**********
        AppDomain.CurrentDomain.DomainUnload += (var, var2) =>                                               //**********
        {                                                                                                    //**********
            Debug.Log("Forced AppDomain Unload! (internal unity error)");                                    //**********
        };                                                                                                   //**********
        //***************************************************************************************************************

        //generate first 3 layers
        if (infiniteVerticalGeneration)
        {
            //destroy rooms currently in the scene
            DestroyAll();
            DestroyRemainingRooms();

            //layer 0 / initial bottom layer
            layerNum = 0;
            Generate();
            bottomGrid = new List<GameObject>();
            foreach (Cell cell in currentLayer.grid) bottomGrid.Add(cell.room);

            //layer 1 / initial middle layer
            currentLayer = new Layer();
            layerNum = 1;
            Generate();
            middleGrid = new List<GameObject>();
            foreach (Cell cell in currentLayer.grid) middleGrid.Add(cell.room);

            //layer 2 / initial top layer
            currentLayer = new Layer();
            layerNum = 2;
            Generate();
            topGrid = new List<GameObject>();
            foreach (Cell cell in currentLayer.grid) topGrid.Add(cell.room);

            currentLayer = new Layer();
            layerNum = 1;
        }
    }

    public void Generate()
    {
        //reset control variables
        if (!generate) generate = true;
        //if infinite generation, dont use the example room and dont place the deadend markers
        if (infiniteVerticalGeneration)
        {
            useExampleRoom = false;
            markDeadends = false;
        }
        //seed random
        SeedRandom();
        //set statics
        sMazeSize = mazeSize;
        sUseRandomSeed = useRandomSeed;
        sSeed = seed;
        //reset flags
        numOfVisitedCells = 0;
        cutOffActivated = false;
        //get the new entrance position
        GetEntrancePosition();
        //perform the generator logic
        GeneratorLogic();
        //refresh markers
        if (markDeadends || markLongestPath) RefreshMarkers();
    }

    public void SaveCurrent()
    {
        SaveToScriptableObject();
    }

    public void GenerateSaved()
    {
        if (LoadSaved()) Generate();
    }

    public void RefreshMarkers()
    {
        RemoveMarkers();
        CreateMarkers();
    }

    public void DestroyAll()
    {
        DestroyRooms();
        DestroyMarkers();
        ClearLists();
    }

    public void UpdateLayer(int newLayer)
    {
        if (newLayer <= 0) return;

        try
        {
            if (newLayer == layerNum)  return; //already generated

            else if (newLayer > layerNum)
            {
                //bottom layer = middle layer
                if (bottomGrid != null && bottomGrid.Count > 1) foreach (GameObject obj in bottomGrid) DynamicDestroy(obj);
                bottomGrid = new List<GameObject>();
                foreach (GameObject obj in middleGrid)
                {
                    if (!obj) continue;

                    GameObject newRoom = Instantiate(obj);
                    bottomGrid.Add(newRoom);
                }

                //middle layer = top layer
                if (middleGrid != null && middleGrid.Count > 1) foreach (GameObject obj in middleGrid) DynamicDestroy(obj);
                middleGrid = new List<GameObject>();
                foreach (GameObject obj in topGrid)
                {
                    if (!obj) continue;

                    GameObject newRoom = Instantiate(obj);
                    middleGrid.Add(newRoom);
                }

                //generate current layer
                entranceRoomPosition = endRoomPositions[layerNum];
                layerNum = newLayer + 1;
                Generate();
                layerNum = newLayer;

                //top layer = current layer
                if (topGrid != null & topGrid.Count > 1) foreach(GameObject obj in topGrid) DynamicDestroy(obj);
                topGrid = new List<GameObject>();
                foreach (Cell cell in currentLayer.grid) topGrid.Add(cell.room);

                currentLayer = new Layer();
            }
            else if (newLayer < layerNum)
            {
                //top layer = middle layer
                if (topGrid != null && topGrid.Count > 1)
                {
                    foreach (GameObject obj in topGrid)
                    {
                        DynamicDestroy(obj);
                    }
                }

                topGrid = new List<GameObject>();
                foreach (GameObject obj in middleGrid)
                {
                    if (!obj) continue;

                    GameObject newRoom = Instantiate(obj);
                    topGrid.Add(newRoom);
                }

                //middle layer = bottom layer
                if (middleGrid != null && middleGrid.Count > 1) foreach (GameObject obj in middleGrid) DynamicDestroy(obj);
                middleGrid = new List<GameObject>();
                foreach (GameObject obj in bottomGrid)
                {
                    if (!obj) continue;

                    GameObject newRoom = Instantiate(obj);
                    middleGrid.Add(newRoom);
                }

                //genenerate current layer
                entranceRoomPosition = endRoomPositions[layerNum];
                layerNum = newLayer - 1;
                Generate();
                layerNum = newLayer;

                //bottom layer = current layer
                if (bottomGrid != null & bottomGrid.Count > 1) foreach (GameObject obj in bottomGrid) DynamicDestroy(obj);
                bottomGrid = new List<GameObject>();
                foreach (Cell cell in currentLayer.grid) bottomGrid.Add(cell.room);

                currentLayer = new Layer();
            }
        }
        catch (System.Exception e) { Debug.Log(e); }

        return;
    }

#endregion Button Events

#region Core Logic
    private void GeneratorLogic()
    {
        if (generate)
        {
            if (!(infiniteVerticalGeneration && Application.isPlaying))
            {
                //destroy everything generated
                DestroyAll();
            }

            //setup new grid
            SetupGrid();

            //looking for a valid cell to move to from the entrance room
            while (currentLayer.stack.Count == 0) 
            {
                MazeLogic();
                generate = false;
            }
        }

        //while not back to the start of the stack, and cut off has not been activated
        while (currentLayer.stack.Count > 0 && !cutOffActivated)
        {
            MazeLogic();
        }

        if (cutOffActivated) //if cut off was activated
        {
            //destroy unvisited rooms
            DestroyUnvisitedRooms();
        }

        if (infiniteVerticalGeneration)
        {
            //add in the connecting stair and start room
            AddStairRoom();
            AddStartRoom(layerNum);
        }
    }

    private void SetupGrid()
    {
        //get the tile offsets
        float tileWidthOffset = GetTileWidthOffset();
        float tileHeightOffset = GetTileHeightOffset();

        //for every position in the grid
        for (int i = 0; i < mazeSize; i++)
        {
            for (int j = 0; j < mazeSize; j++)
            {
                //create new cell instance
                Cell cell = new Cell();
                cell.xPos = j;
                cell.zPos = i;
                //add new cell to grid list 
                if (currentLayer != null) currentLayer.grid.Add(cell);
            }
        }

        //for each cell in the grid
        for (int i = 0; i < currentLayer.grid.Count; i++)
        {
            //instantiate a new room for this cell in the grid
            if (useExampleRoom || normalRoomVariations.Count == 0) //useExampleRoom is true, or no room variations have been provided
            {
                //instantiate the example room
                currentLayer.grid[i].room = Instantiate(exampleRoom, Vector3.zero, Quaternion.identity, transform);
            }
            else //room variations have been provided
            {
                if (normalRoomVariations.Count == 0)
                {
                    //use the only variation
                    currentLayer.grid[i].room = Instantiate(normalRoomVariations[0], Vector3.zero, Quaternion.identity, transform);
                }
                else //multiple variations have been provided
                {
                    //get a random number
                    int randNum = GetRandomRoomVariation();
                    //use a random variation
                    currentLayer.grid[i].room = Instantiate(normalRoomVariations[randNum], Vector3.zero, Quaternion.identity, transform);
                }
            }

            //calculate the room world position multipliers based on grid index and tile offset
            Vector3 newPos = mazeEntrancePosition.transform.position;
            newPos.x += currentLayer.grid[i].xPos * tileWidthOffset;
            newPos.y += layerNum * tileHeightOffset;
            newPos.z += (currentLayer.grid[i].zPos * tileWidthOffset);

            if (layerNum == 0) newPos.z -= entranceRoomPosition.y * tileWidthOffset;
            else newPos.z -= tileWidthOffset;

            //set room position, name and tag
            currentLayer.grid[i].room.transform.position = newPos;
            currentLayer.grid[i].room.gameObject.name = i.ToString();
            currentLayer.grid[i].room.gameObject.tag = roomTag;
        }

        if (layerNum == 0)
        {
            //for each cell in the grid
            for (int i = 0; i < currentLayer.grid.Count; ++i)
            {
                if (currentLayer.grid[i].xPos == entranceRoomPosition.x && currentLayer.grid[i].zPos == entranceRoomPosition.y)
                {
                    //this cell is the entrance room
                    //recursive backtrack will start at this cell
                    currentLayer.current = currentLayer.grid[i];
                    //destroy the entrance door
                    currentLayer.grid[i].room.GetComponent<Room>().DestroyDoor(Cell.Direction.South);
                    //break as we've found the entrance cell
                    break;
                }
            }
        }
        else
        {
            //calculate index of new start room in whole grid
            int xIndex = (int)(endRoomPositions[layerNum - 1].x);
            int yIndex = (int)(endRoomPositions[layerNum - 1].y);
            //recursive backtrack will start at this cell
            currentLayer.current = currentLayer.grid[(mazeSize * yIndex) + xIndex];
        }
    }

    private void MazeLogic()
    {
        //increment number of visited cells
        numOfVisitedCells++;
        //set bool visited on this cell to true
        currentLayer.current.visited = true;
        //get a random, unvisited neighboring cell
        currentLayer.next = currentLayer.current.CheckNeighbors();
        //reset list of neighbors
        currentLayer.current.neighbors = new List<Cell>();
        if (currentLayer.next != null)
        {
            //not backtracking as moving into next cell
            backtracking = false;
            //set visited bool of cell we're moving into to true
            currentLayer.next.visited = true;
            //add cell into top of stack
            currentLayer.stack.Add(currentLayer.current);
            //remove the wall to create the doorway between these rooms
            RemoveWalls(currentLayer.current, currentLayer.next);
            //next iteration start from the new cell
            currentLayer.current = currentLayer.next;
        }
        else if (currentLayer.stack.Count > 0)
        {
            //at a deadend and havent returned to the start of the dungeon
            if (!backtracking)//if we weren't backtracking,
            {
                //set backtracking to true
                backtracking = true;
                //add current cell to list of deadends
                currentLayer.deadends.Add(currentLayer.current);
                //check if this is now the longest path
                CheckPath();
                //if cutOff has been set to true, check if we should cutoff
                if (cutOff) CutOff();
            }
            //backtrack to the previous cell
            currentLayer.current = currentLayer.stack[currentLayer.stack.Count - 1];
            //remove cell from stack
            currentLayer.stack.RemoveAt(currentLayer.stack.Count - 1);
            //decrement number of visited cells
            numOfVisitedCells--;
        }
        else
        {
            //delete rooms in list of cells to delete
            foreach (GameObject go in currentLayer.deleteCell)
            {
                DynamicDestroy(go);
            }
        }
        //set static value
        sNumOfVisitedCells = numOfVisitedCells;
    }

    private void RemoveWalls(Cell a, Cell b)
    {
        //check which doorways to be destroyed going from cell a to b
        //e.g. if going north from cell a to b, destroy north wall in a and south wall in b

        float x = a.xPos - b.xPos;
        if (x == 1)
        {
            a.doorways[2] = true;
            b.doorways[0] = true;
        }
        else if (x == -1)
        {
            a.doorways[0] = true;
            b.doorways[2] = true;
        }

        float z = a.zPos - b.zPos;
        if (z == 1)
        {
            a.doorways[1] = true;
            b.doorways[3] = true;
        }
        else if (z == -1)
        {
            a.doorways[3] = true;
            b.doorways[1] = true;
        }

        //destroy the corresponding wall in cells a and b
        a.CalculateWalls();
        b.CalculateWalls();
    }

    private void DestroyRooms(List<GameObject> grid = null)
    {
        if (infiniteVerticalGeneration && Application.isPlaying)
        {
            //if infitinite generation and application is playing (not in object mode)
            if (grid != null)
            {
                //if grid list is not null
                foreach (GameObject go in grid)
                {
                    //destroy all game objects in list
                    DynamicDestroy(go);
                }
            }
        }
        else
        {
            //destroy all child game objects with the room tag
            DestroyRemainingRooms();
        }
    }

    private void DestroyRemainingRooms()
    {
        List<GameObject> objectsToDestroy = new List<GameObject>();
        foreach (Transform child in transform)
        {
            //foreach child with a transform
            if (child.gameObject.CompareTag(roomTag))
            {
                //if game object has the rom tag, add to ToDestroy list 
                objectsToDestroy.Add(child.gameObject);
            }
        }

        foreach (GameObject go in objectsToDestroy)
        {
            //destroy the rooms
            DynamicDestroy(go);
        }
    }

    private void DestroyUnvisitedRooms()
    {
        List<GameObject> objectsToDestroy = new List<GameObject>();
        foreach (Cell cell in currentLayer.grid)
        {
            //foreach cell in the grid
            if (!cell.visited)
            {
                //if the cell is unvisited, add to ToDestroy list
                objectsToDestroy.Add(cell.room);
            }
        }
        
        foreach (GameObject go in objectsToDestroy)
        {
            //destroy all unvisited, generated rooms in children
            DynamicDestroy(go);
        }
    }

    private void CheckPath()
    {
        if (currentLayer.longestPath.Count < currentLayer.stack.Count)
        {
            //must be new list as RemoveAt on stack also removes from longestPath as they use the same references
            currentLayer.longestPath = new List<Cell>(currentLayer.stack);
            //set longest deadend as current longest deadend
            currentLayer.endRoom = currentLayer.current;
            //add empty value to end room positions list if it's full
            if (endRoomPositions.Count < layerNum + 1) endRoomPositions.Add(Vector2.zero);
            //set end room position to current longest list
            endRoomPositions[layerNum] = new Vector2(currentLayer.endRoom.xPos, currentLayer.endRoom.zPos);
        }
    }

    private void ClearLists()
    {
        if (currentLayer != null)
        {
            //if not null, clear the lists
            currentLayer.grid.Clear();
            currentLayer.stack.Clear();
            currentLayer.longestPath.Clear();
            currentLayer.deadends.Clear();
            currentLayer.deleteCell.Clear();
        }
    }

    private void CutOff()
    {
        if (!cutOffActivated)
        {
            if (((float)(numOfVisitedCells) / (sMazeSize * sMazeSize)) * 100 >= cutOffPoint)
            {
                //if we've visited the specified percent of cells, set flag flag to true
                cutOffActivated = true;
            }
        }
    }

    private void AddStairRoom()
    {
        //get old room's position
        Vector3 newPos = currentLayer.endRoom.room.transform.position;
        newPos.x += 10.0f;
        //destroy current deadend room
        DynamicDestroy(currentLayer.endRoom.room);

        //instantiate the connecting stair room
        currentLayer.endRoom.room = Instantiate(stairRoom, newPos, Quaternion.identity, transform);
        currentLayer.endRoom.room.GetComponent<StairRoom>().SetLayer(layerNum);

        //rotate the room so that the entrance faces the correct direction
        Vector3 newRotation = new Vector3(0, 0, 0);
        if (currentLayer.endRoom.doorways[0]) newRotation.y = 180;
        else if (currentLayer.endRoom.doorways[1]) newRotation.y = 270;
        else if (currentLayer.endRoom.doorways[2]) newRotation.y = 0;
        else if (currentLayer.endRoom.doorways[3]) newRotation.y = 90;

        //set the rotation
        currentLayer.endRoom.room.transform.eulerAngles = newRotation;
        //set room tag
        currentLayer.endRoom.room.gameObject.tag = roomTag;
    }

    private void AddStartRoom(int _layerNum)
    {
        //guard clause; dont be checking for layer -1 as this is the entrance room
        if (_layerNum < 1) return;

        //calculate index of new start room in whole grid
        int xIndex = (int)(endRoomPositions[_layerNum - 1].x);
        int yIndex = (int)(endRoomPositions[_layerNum - 1].y);
        int gridIndex = (mazeSize * yIndex) + xIndex;

        //remember which doorways to destroy
        bool destroyNorth = false;
        bool destroyEast = false;
        bool destroySouth = false;
        bool destroyWest = false;

        if (currentLayer.grid[gridIndex].doorways[0]) destroyNorth = true;
        if (currentLayer.grid[gridIndex].doorways[1]) destroyEast = true;
        if (currentLayer.grid[gridIndex].doorways[2]) destroySouth = true;
        if (currentLayer.grid[gridIndex].doorways[3]) destroyWest = true;

        //get old room's position and rotation
        Vector3 newPos = currentLayer.grid[gridIndex].room.transform.position;
        Quaternion newRotation = currentLayer.grid[gridIndex].room.transform.rotation;
        //instantiate the new start room for this layer
        GameObject newRoom = Instantiate(floorStartRoom, newPos, newRotation, transform);
        newRoom.GetComponent<StartRoom>().SetLayer(layerNum);

        //destroy the doors
        if (destroyNorth) newRoom.GetComponent<Room>().DestroyDoor(Cell.Direction.North);
        if (destroyEast) newRoom.GetComponent<Room>().DestroyDoor(Cell.Direction.East);
        if (destroySouth) newRoom.GetComponent<Room>().DestroyDoor(Cell.Direction.South);
        if (destroyWest) newRoom.GetComponent<Room>().DestroyDoor(Cell.Direction.West);

        //destroy current start room
        DynamicDestroy(currentLayer.grid[gridIndex].room);

        //set to new start room
        currentLayer.grid[gridIndex].room = newRoom;
        //set room tag
        currentLayer.grid[gridIndex].room.tag = roomTag;
    }

    #endregion Core Logic

    #region Markers

    private void CreateMarkers()
    {
        if (markDeadends)
        {
            foreach (Cell cell in currentLayer.deadends)
            {
                //instantiate marker
                GameObject marker = Instantiate(deadendMarker, GetNewMarkerPosition(cell, GetTileWidthOffset()), Quaternion.identity, transform);
                marker.tag = markerTag;
                //add marker to list
                deadendMarkers.Add(marker);
            }
        }

        if (markLongestPath)
        {
            for (int i = 0; i < currentLayer.longestPath.Count - 1; i++)
            {
                //get current room position
                Vector3 currentRoomPos = currentLayer.longestPath[i].room.transform.position;
                //get next room position
                Vector3 nextRoomPos = currentLayer.longestPath[i + 1].room.transform.position;

                //add the rooms middle tile
                AddLongestPathTiles(i, Cell.Direction.Middle, Cell.Direction.Middle);

                //next room is to the north
                if (currentRoomPos.x < nextRoomPos.x) AddLongestPathTiles(i, Cell.Direction.North, Cell.Direction.South);
                //next room is to the south
                else if (currentRoomPos.x > nextRoomPos.x) AddLongestPathTiles(i, Cell.Direction.South, Cell.Direction.North);
                //next room is to the west
                else if (currentRoomPos.z < nextRoomPos.z) AddLongestPathTiles(i, Cell.Direction.West, Cell.Direction.East);
                //next room is to the east
                else if (currentRoomPos.z > nextRoomPos.z) AddLongestPathTiles(i, Cell.Direction.East, Cell.Direction.West);
            }

            foreach (GameObject go in longestPathFloorTiles)
            {
                if (!go) continue; //guard clause

                //change the floor tiles' material
                go.GetComponent<MeshRenderer>().material = longestPathFloorMaterial;
            }
        }
    }

    private void AddLongestPathTiles(int index, Cell.Direction forwardDirection, Cell.Direction backwardDirection)
    {
        if (index >= currentLayer.longestPath.Count) return; //guard clause

        //get the corresponding tiles for this room
        List<GameObject> newTiles = new List<GameObject>(currentLayer.longestPath[index].room.GetComponent<Room>().GetTiles(forwardDirection));

        if (newTiles.Count == 0) return; //guard clause

        foreach (GameObject go in newTiles)
        {
            //add tile GameObject to list
            longestPathFloorTiles.Add(go);
        }

        if (backwardDirection == Cell.Direction.Middle) return; //guard clause

        //get the corresponding tiles for next room
        newTiles = new List<GameObject>(currentLayer.longestPath[index + 1].room.GetComponent<Room>().GetTiles(backwardDirection));

        if (newTiles.Count == 0) return; //guard clause

        foreach (GameObject go in newTiles)
        {
            //add tile GameObject to list
            longestPathFloorTiles.Add(go);
        }
    }

    private void DestroyMarkers()
    {

        foreach (GameObject go in longestPathFloorTiles)
        {
            if (!go) continue;
            //reset material
            go.GetComponent<MeshRenderer>().material = floorMaterial;
        }

        //destroy all generated markers in children
        List<GameObject> objectsToDestroy = new List<GameObject>();
        foreach (Transform child in transform)
        {
            //foreach child of game object
            if (child.gameObject.CompareTag(markerTag))
            {
                //if it has the marker tag, add to ToDestroy list
                objectsToDestroy.Add(child.gameObject);
            }
        }

        foreach (GameObject go in objectsToDestroy)
        {
            //destroy the game objects
            DynamicDestroy(go);
        }
    }

    private void RemoveMarkers()
    {
        foreach (GameObject go in longestPathFloorTiles)
        {
            if (!go) continue;
            //reset material
            go.GetComponent<MeshRenderer>().material = floorMaterial;
        }

        foreach (GameObject go in deadendMarkers)
        {
            //destroy game objects in list of deadend markers
            DynamicDestroy(go);
        }
    }

#endregion Markers

#region Utility

    private void SeedRandom()
    {
        if (useRandomSeed)
        {
            //seed random using current total frame count
            UnityEngine.Random.InitState(Time.frameCount);
            //get a max 6 digit int to use as new seed
            seed = UnityEngine.Random.Range(1, 100000);
        }

        //seed random with generated or given seed
        UnityEngine.Random.InitState(seed * (layerNum + 1));
    }

    public int GetRandomInt(int min, int max)
    {
        //return a unity generated random int between given min and max
        return UnityEngine.Random.Range(min, max);
    }

    private int GetRandomRoomVariation()
    {
        List<int> variationWeights = new List<int>();
        int currentTotal = 0;
        int numOfVariations = normalRoomVariations.Count;

        for (int i = 0; i < numOfVariations; ++i)
        {
            //add this variations weighting to total
            currentTotal += variationWeighting[i];
            //add current total to list
            variationWeights.Add(currentTotal);
        }

        //get a random number between 0 and the total of the weightings
        int randNum = GetRandomInt(0, currentTotal);

        for (int i = 0; i < variationWeights.Count; ++i)
        {
            if (variationWeights[i] >= randNum)
            {
                //return this variation as random number is within this rooms range
                return i;
            }
        }

        return 0;
    }

    private float GetTileWidthOffset()
    {
        float tileOffset;
        //calculate width offset for the rooms
        if (useExampleRoom) tileOffset = exampleFloorTile.GetComponent<Renderer>().bounds.size.z * exampleFloorTilesPerLength;
        else tileOffset = floorTile.GetComponent<Renderer>().bounds.size.z * floorTilesPerLength;

        return tileOffset;
    }

    private float GetTileHeightOffset()
    {
        float tileOffset;
        //calculate height offset for the rooms
        if (useExampleRoom) tileOffset = exampleFloorTile.GetComponent<Renderer>().bounds.size.x * exampleWallTilesPerHeight;
        else tileOffset = wallTile.GetComponent<Renderer>().bounds.size.x * wallTilesPerHeight;

        return tileOffset;
    }

    private void GetEntrancePosition()
    {
        if (layerNum == 0)
        {
            //if its the first layer, user has set the start position
            float yPos = (mazeSize % 2 == 0) ? (mazeSize / 2) - 1.0f : (mazeSize - 1) / 2;
            entranceRoomPosition = new Vector2(0.0f, yPos);
        }
        else
        {
            //get start position from list
            if (endRoomPositions.Count < layerNum + 1) endRoomPositions.Add(Vector2.zero);
            entranceRoomPosition = endRoomPositions[layerNum - 1];
        }
    }

    private Vector3 GetNewMarkerPosition(Cell cell, float tileOffset)
    {
        //calculate world space position to place marker
        Vector3 position = new Vector3(cell.xPos * tileOffset, 0.0f, cell.zPos * tileOffset);
        position += mazeEntrancePosition.transform.position;
        position.z -= (tileOffset * entranceRoomPosition.y);
        position.x += 10.0f;

        return position;
    }

    private void DynamicDestroy(GameObject go)
    {
        //if application is playing, use normal destroy
        //If in editor in object mode, use destroy immediate
        if (Application.isPlaying) Destroy(go);
        else DestroyImmediate(go);
    }

    private bool LoadSaved()
    {
        if (!savedSettings)
        {
            //inform user that they havent set a scriptable object to load from
            Debug.Log("No supplied scriptable object to load from.");
            return false;
        }

        //Dungeon
        mazeSize = savedSettings.dungeonSettings.mazeSize;
        mazeEntrancePosition = savedSettings.dungeonSettings.mazeEntrancePosition;
        markerTag = savedSettings.dungeonSettings.markerTag;
        deadendMarker = savedSettings.dungeonSettings.deadendMarker;
        longestPathFloorMaterial = savedSettings.dungeonSettings.longestPathFloorMaterial;
        //Rooms
        roomTag = savedSettings.dungeonSettings.roomTag;
        floorMaterial = savedSettings.dungeonSettings.floorMaterial;
        stairRoom = savedSettings.dungeonSettings.stairRoom;
        normalRoomVariations = savedSettings.dungeonSettings.normalRoomVariations;
        variationWeighting = savedSettings.dungeonSettings.variationWeighting;
        //Tiles
        floorTile = savedSettings.dungeonSettings.floorTile;
        floorTilesPerLength = savedSettings.dungeonSettings.floorTilesPerLength;
        wallTile = savedSettings.dungeonSettings.wallTile;
        wallTilesPerHeight = savedSettings.dungeonSettings.wallTilesPerHeight;
        //Example Rooms
        exampleRoom = savedSettings.dungeonSettings.exampleRoom;
        exampleFloorTile = savedSettings.dungeonSettings.exampleFloorTile;
        exampleFloorTilesPerLength = savedSettings.dungeonSettings.exampleFloorTilesPerLength;
        //Generation
        useRandomSeed = savedSettings.dungeonSettings.useRandomSeed;
        seed = savedSettings.dungeonSettings.seed;
        useExampleRoom = savedSettings.dungeonSettings.useExampleRoom;
        markDeadends = savedSettings.dungeonSettings.markDeadends;
        markLongestPath = savedSettings.dungeonSettings.markLongestPath;
        cutOff = savedSettings.dungeonSettings.cutOff;
        cutOffPoint = savedSettings.dungeonSettings.cutOffPoint;
        infiniteVerticalGeneration = savedSettings.dungeonSettings.infiniteVerticalGeneration;

        return true;
    }

    private void SaveToScriptableObject()
    {
        if (!objectToSaveTo)
        {
            //inform user that they havent set a scriptable object to save to
            Debug.Log("No supplied scriptable object to save to.");
            return;
        }

        //Dungeon
        objectToSaveTo.dungeonSettings.mazeSize = mazeSize;
        objectToSaveTo.dungeonSettings.mazeEntrancePosition = mazeEntrancePosition;
        objectToSaveTo.dungeonSettings.markerTag = markerTag;
        objectToSaveTo.dungeonSettings.deadendMarker = deadendMarker;
        objectToSaveTo.dungeonSettings.longestPathFloorMaterial = longestPathFloorMaterial;
        //Rooms
        objectToSaveTo.dungeonSettings.roomTag = roomTag;
        objectToSaveTo.dungeonSettings.floorMaterial = floorMaterial;
        objectToSaveTo.dungeonSettings.stairRoom = stairRoom;
        objectToSaveTo.dungeonSettings.normalRoomVariations = normalRoomVariations;
        objectToSaveTo.dungeonSettings.variationWeighting = variationWeighting;
        //Tiles
        objectToSaveTo.dungeonSettings.floorTile = floorTile;
        objectToSaveTo.dungeonSettings.floorTilesPerLength = floorTilesPerLength;
        objectToSaveTo.dungeonSettings.wallTile = wallTile;
        objectToSaveTo.dungeonSettings.wallTilesPerHeight = wallTilesPerHeight;
        //Example Rooms
        objectToSaveTo.dungeonSettings.exampleRoom = exampleRoom;
        objectToSaveTo.dungeonSettings.exampleFloorTilesPerLength = exampleFloorTilesPerLength;
        objectToSaveTo.dungeonSettings.exampleFloorTile = exampleFloorTile;
        //Generation
        objectToSaveTo.dungeonSettings.useRandomSeed = false; //do not want to be using a random seed
        objectToSaveTo.dungeonSettings.seed = seed;
        objectToSaveTo.dungeonSettings.useExampleRoom = useExampleRoom;
        objectToSaveTo.dungeonSettings.markDeadends = markDeadends;
        objectToSaveTo.dungeonSettings.markLongestPath = markLongestPath;
        objectToSaveTo.dungeonSettings.cutOff = cutOff;
        objectToSaveTo.dungeonSettings.cutOffPoint = cutOffPoint;
        objectToSaveTo.dungeonSettings.infiniteVerticalGeneration = infiniteVerticalGeneration;
    }

    #endregion

}


public class Layer
{
    public List<Cell> grid = new List<Cell>();
    public List<Cell> stack = new List<Cell>();
    public List<Cell> longestPath = new List<Cell>();
    public List<Cell> deadends = new List<Cell>();
    public List<GameObject> deleteCell = new List<GameObject>();

    public Cell endRoom;
    public Cell current;
    public Cell next;
}


public class Cell
{
    public int xPos;
    public int zPos;
    public const int numOfWalls = 4;
    public bool[] doorways = new bool[numOfWalls] { false, false, false, false };
    public bool visited = false;

    public GameObject room;
    public List<Cell> neighbors = new List<Cell>();
    public Cell[] directions = new Cell[numOfWalls];
    
    public enum Direction
    {
        Middle,
        North,
        East,
        South,
        West
    }

    public void CalculateWalls()
    {
        int gridIndex = (zPos == 0) ? xPos : (zPos * MazeGenerator.sMazeSize) + xPos;

        //guard clauses
        if (gridIndex == -1) return;
        if (MazeGenerator.currentLayer.grid[gridIndex].room == null) return;
        
        //destroy the given doorways
        if (doorways[0])
        {
            MazeGenerator.currentLayer.deleteCell.Add(MazeGenerator.currentLayer.grid[gridIndex].room.GetComponent<Room>().GetDoor(Cell.Direction.North));
            MazeGenerator.currentLayer.grid[gridIndex].room.GetComponent<Room>().DestroyDoor(Cell.Direction.North);
        }
        if (doorways[1])
        {
            MazeGenerator.currentLayer.deleteCell.Add(MazeGenerator.currentLayer.grid[gridIndex].room.GetComponent<Room>().GetDoor(Cell.Direction.East));
            MazeGenerator.currentLayer.grid[gridIndex].room.GetComponent<Room>().DestroyDoor(Cell.Direction.East);
        }
        if (doorways[2])
        {
            MazeGenerator.currentLayer.deleteCell.Add(MazeGenerator.currentLayer.grid[gridIndex].room.GetComponent<Room>().GetDoor(Cell.Direction.South));
            MazeGenerator.currentLayer.grid[gridIndex].room.GetComponent<Room>().DestroyDoor(Cell.Direction.South);
        }
        if (doorways[3])
        {
            MazeGenerator.currentLayer.deleteCell.Add(MazeGenerator.currentLayer.grid[gridIndex].room.GetComponent<Room>().GetDoor(Cell.Direction.West));
            MazeGenerator.currentLayer.grid[gridIndex].room.GetComponent<Room>().DestroyDoor(Cell.Direction.West);
        }
    }

    public int Index(int i, int j)
    {
        if (i < 0 ||
            i > MazeGenerator.sMazeSize - 1 ||
            j < 0 ||
            j > MazeGenerator.sMazeSize - 1)
        {
            //if checking outside the grid, return -1
            return -1;
        }

        return i + j * MazeGenerator.sMazeSize; 
    }

    public Cell CheckNeighbors()
    {
        //add neighbor cells to directions list
        if (Index(xPos, zPos - 1) != -1)
        {
            directions[0] = MazeGenerator.currentLayer.grid[Index(xPos, zPos - 1)];
        }
        if (Index(xPos + 1, zPos) != -1)
        {
            directions[1] = MazeGenerator.currentLayer.grid[Index(xPos + 1, zPos)];
        }
        if (Index(xPos, zPos + 1) != -1)
        {
            directions[2] = MazeGenerator.currentLayer.grid[Index(xPos, zPos + 1)];
        }
        if (Index(xPos - 1, zPos) != -1)
        {
            directions[3] = MazeGenerator.currentLayer.grid[Index(xPos - 1, zPos)];
        }

        for (int i = 0; i < numOfWalls; i++)
        {
            //for each neighbor
            if (directions[i] != null && directions[i].visited == false)
            {
                //if not null and unvisted, add neighbor cell to list
                neighbors.Add(directions[i]);
            }
        }

        if (neighbors.Count > 0)
        {
            //get a random number between 0 and number of valid neighbors
            int iRand = GetRandomInt(0, neighbors.Count);
            //return the randomly selected cell
            return neighbors[iRand];
        }
        else
        {
            //no neighbors, return null
            return null;
        }
    }

    public int GetRandomInt(int min, int max)
    {
        //return a unity generated random int between given min and max
        return UnityEngine.Random.Range(min, max);
    }
}
