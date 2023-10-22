using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Drawing;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityMovementAI;
//using static UnityEditor.PlayerSettings;

/*
 * This Code is a modified version of the cellular automata tutorial by Sebastian Lague
 * 
 * The Map generation has been heavily modified and repurposed for Unity 2D
 * The checking for regions and connectivity have been followed faithfully to the tutorial
 * 
 * I have also referred to a tutorial by Sunny valley studios for the TileMapVisualizer
*/
public class MapGenerator : MonoBehaviour
{
    public int width;
    public int height;

    public string seed;
    public bool useRandomSeed;

    public bool useBSP;

    [Range(1,5)]
    public int noOfSplits;

    [Range(0, 10)]
    public int iterations;

    //[Range(2, 8)]
    //public int cellularAutomataNeighbours;

    [Range(0, 100)]
    public int randomFillPercent;

    [SerializeField]
    private TileMapVisualizer tileMapVisualizer;

    int[,] map;
    HashSet<Vector2Int> floorMapHash;
    HashSet<Vector2Int> wallMapHash;

    void Start()
    {
        generateMap();
        //Debug.DrawLine(new Vector3(0,0,0), new Vector3(width,height,0),Color.cyan);

    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            generateMap();
        }
    }
    // Map and 2D tilemap generation
    void generateMap()
    {
        tileMapVisualizer.Clear();
        ClearEntities();
        map = new int[width, height];
        if (useRandomSeed)
        {
            seed = Time.time.ToString();
        }
        randomFillMap();
        if (useBSP)
        {
            BinarySpacePartition();
        }
        cellularAutomata();
        Map2Hash();
        SpawnEntities();
        tileMapVisualizer.PaintFloorTiles(floorMapHash);
        tileMapVisualizer.PaintWallTiles(wallMapHash);

    }

    private void ClearEntities()
    {
        var objects = GameObject.FindGameObjectsWithTag("Troll");
        
        foreach ( GameObject o in objects)
        {
            Destroy(o);
        }
        objects = GameObject.FindGameObjectsWithTag("Torch");
        
        foreach ( GameObject o in objects)
        {
            Destroy(o);
        }
        objects = GameObject.FindGameObjectsWithTag("Gem");
        foreach ( GameObject o in objects)
        {
            Destroy(o);
        }
        objects = GameObject.FindGameObjectsWithTag("Goal");
        foreach (GameObject o in objects)
        {
            Destroy(o);
        }
        objects = GameObject.FindGameObjectsWithTag("Thief");
        foreach ( GameObject o in objects)
        {
            Destroy(o);
        }
        objects = GameObject.FindGameObjectsWithTag("TrollChief");
        foreach ( GameObject o in objects)
        {
            Destroy(o);
        }
    }

    // Code block for adding entities into the procedurally generated map

    public Transform thief,troll, trollChief,gem,torch,goal;
    [Range(1, 5)]
    public int noOfTorches;
    [Range(1, 5)]
    public int noOfTrolls;

    public Vector2 objectSizeRange = new Vector2(1, 2);
    public bool randomizeOrientation = false;

    public List<GameObject> objs = new List<GameObject>();

    private void SpawnEntities()
    {
        for(int i = 0; i < noOfTorches; i++){ SpawnEntity(torch); }
        for (int i = 0;i < noOfTrolls;i++) { SpawnEntity(troll);}
        SpawnEntity(gem);
        SpawnEntity(goal);
        SpawnEntity(trollChief);
        SpawnEntity(thief);

    }

    private void SpawnEntity(Transform obj)
    {
        System.Random rand = new System.Random(seed.GetHashCode());
        bool placed = false;
        while (placed == false)
        {
            Vector2Int randomTile = floorMapHash.ElementAt(rand.Next(floorMapHash.Count));
            int neighboursCount = getSurroundingWallCount(randomTile.x, randomTile.y);
            if (neighboursCount == 0 && map[randomTile.x, randomTile.y] != 1)
            {
                Vector3 pos = new Vector3(0.5f + randomTile.x, 0.5f + randomTile.y, 0);

                Transform t = Instantiate(obj, pos, Quaternion.identity) as Transform;


                if (randomizeOrientation)
                {
                    Vector3 euler = transform.eulerAngles;
                    euler.z = rand.Next(0, 360);
                    transform.eulerAngles = euler;
                }
                objs.Add(t.GetComponent<GameObject>());
                map[randomTile.x, randomTile.y] = 1; //changing value in map so that it doesnt place another object here
                placed = true;
            }
        }
    }
    // Main code block for Procedural Map generation
    void BinarySpacePartition()
    {
        int currentSplits = 0;
        List<BSPRoom> rooms = new List<BSPRoom>();
        rooms.Add(new BSPRoom(new Vector2Int(0,height), new Vector2Int(width,0)));
        
        while(currentSplits < noOfSplits)
        {

            System.Random rand = new System.Random(seed.GetHashCode());
            int removeRoomIndex = 0; 

            //Choose room for split
            if (rooms.Count > 1) {
                rooms.Sort();
                int countBigRooms = 0;
                int maxSize = rooms.ElementAt(0).GetRoomSize();
                for (int i = 0; i<rooms.Count; i++)
                {
                    if (maxSize - 25 < rooms[i].GetRoomSize())
                    {
                        countBigRooms++;
                    }
                    else
                        break;
                }
                removeRoomIndex = rand.Next(0, countBigRooms);
            }
            BSPRoom splitRoom = rooms.ElementAt(removeRoomIndex);
            rooms.RemoveAt(removeRoomIndex);

            //Split chosen Room
            bool direction = false; 
            int axis;
            direction = rand.Next(0, 100) < 50 ;
            if (direction) // HorizontalSplit
            {
                axis = (splitRoom.GetBottomRight().y + splitRoom.GetTopLeft().y) / 2;
                for (int i = splitRoom.GetTopLeft().x; i < splitRoom.GetBottomRight().x; i++)
                {
                    map[i, axis] = 1;
                }
                rooms.Add(new BSPRoom(splitRoom.GetTopLeft(),new Vector2Int(splitRoom.GetBottomRight().x,axis)));
                rooms.Add(new BSPRoom(new Vector2Int(splitRoom.GetTopLeft().x,axis),splitRoom.GetBottomRight()));
                Debug.Log("Horizontal split");
            }
            else // VerticalSplit
            {
                axis = (splitRoom.GetTopLeft().x + splitRoom.GetBottomRight().x) / 2;
                for (int i = splitRoom.GetBottomRight().y; i < splitRoom.GetTopLeft().y; i++)
                {
                    map[axis, i] = 1;
                }
                rooms.Add(new BSPRoom(splitRoom.GetTopLeft(), new Vector2Int(axis, splitRoom.GetBottomRight().y)));
                rooms.Add(new BSPRoom(new Vector2Int(axis, splitRoom.GetTopLeft().y), splitRoom.GetBottomRight()));
                Debug.Log("Vertical split");
            }
            currentSplits++;
        }
    }
    struct BSPRoom : IComparable<BSPRoom>
    {
        Vector2Int topLeft;
        Vector2Int bottomRight;
        int roomSize;
        public BSPRoom(Vector2Int topLeft, Vector2Int bottomRight)
        {
            this.topLeft = topLeft;
            this.bottomRight = bottomRight;

            roomSize = (bottomRight.x - topLeft.x) * (topLeft.y - bottomRight.y);
        }
        public int CompareTo(BSPRoom other)
        {
            return other.roomSize.CompareTo(roomSize);
        }
        public int GetRoomSize()
        {
            return roomSize;
        }
        public Vector2Int GetTopLeft()
        {
            return topLeft;
        }public Vector2Int GetBottomRight()
        {
            return bottomRight;
        }
    }
    void cellularAutomata()
    {
        for (int i = 0; i < iterations; i++)
        {
            cellularAutomataIterations();
        }
        ProcessMap();
    }

    void randomFillMap()
    {
        
        System.Random rand = new System.Random(seed.GetHashCode());
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (i == 0 || i == width - 1 || j == 0 || j == height - 1)
                {
                    map[i, j] = 1;
                }
                else
                {
                    map[i, j] = rand.Next(0, 100) < randomFillPercent ? 1 : 0;
                }
            }
        }
    }
    // Cellular automata rules
    void cellularAutomataIterations()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                int neighbourWallTiles = getSurroundingWallCount(i, j);

                if (neighbourWallTiles > 4)
                {
                    map[i, j] = 1;
                }
                else if (neighbourWallTiles < 4)
                {
                    map[i, j] = 0;
                }
            }
        }
    }
    // CA helper code
    int getSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
        {
            for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
            {
                if (IsInMapRange(neighbourX,neighbourY))
                {

                    if (neighbourX != gridX || neighbourY != gridY)
                    {
                        wallCount += map[neighbourX, neighbourY];
                    }
                }
                else
                {
                    wallCount++;
                }
            }
        }
        return wallCount;
    }
    // Code post here is faithfully followed from a tutorial by Sebastian Lague for Connecting disjoint rooms in Cellular automata
    struct Coord
    {
        public int tileX;
        public int tileY;

        public Coord(int x, int y)
        {
            tileX = x; 
            tileY = y;
        }
    }

    // Getting region through floodfill algorithm
    List<Coord> GetRegionTiles(int startX, int startY)
    {
        List<Coord> tiles = new List<Coord>();
        int[,] mapFlags = new int[width, height];
        int tileType = map[startX, startY];

        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(startX, startY));
        mapFlags[startX, startY] = 1;

        while (queue.Count > 0)
        {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);

            for(int x = tile.tileX - 1; x<= tile.tileX + 1; x++)
            {
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                {
                    if(IsInMapRange(x, y) && (y==tile.tileY ||x ==tile.tileX))
                    {
                        if (mapFlags[x, y] == 0 && map[x,y] == tileType)
                        {
                            mapFlags[x, y] = 1;
                            queue.Enqueue(new Coord(x, y));
                        }
                    }
                }

            }
        }
        return tiles;
    }

    // Returns a list of all regions of a specific tileType
    List<List<Coord>> GetRegions(int tileType)
    {
        List<List<Coord>> regions = new List<List<Coord>>();
        int[,] mapFlags = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapFlags[x, y] == 0 && map[x,y] == tileType)
                {
                    List<Coord> newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);

                    foreach(Coord tile in newRegion)
                    {
                        mapFlags[tile.tileX, tile.tileY] = 1;
                    }
                }
            }
        }
        return regions;
    }

    // Removes small groups of walls or rooms
    void ProcessMap()
    {
        List<List<Coord>> wallRegions = GetRegions(1);
        int wallThresholdSize = 50;
        foreach (List<Coord> wallRegion in wallRegions)
        {
            if(wallRegion.Count < wallThresholdSize)
            {
                foreach (Coord tile in wallRegion)
                {
                    map[tile.tileX, tile.tileY] = 0;
                }
            }
        }
        List<List<Coord>> roomRegions = GetRegions(0);
        int roomThresholdSize = 50;
        List<Room> survivingRooms = new List<Room>();

        foreach (List<Coord> roomRegion in roomRegions)
        {
            if (roomRegion.Count < roomThresholdSize)
            {
                foreach (Coord tile in roomRegion)
                {
                    map[tile.tileX, tile.tileY] = 1;
                }
            }
            else
            {
                survivingRooms.Add(new Room(roomRegion,map));
            }
        }
        survivingRooms.Sort();
        survivingRooms[0].isMainRoom = true;
        survivingRooms[0].isAccessibleFromMainRoom = true;
        ConnectClosestRooms(survivingRooms);
    }

    void ConnectClosestRooms(List<Room> allRooms, bool forceAccessibilityFromMainRoom = false)
    {
        List<Room> roomListA = new List<Room>();
        List<Room> roomListB = new List<Room>();
        if(forceAccessibilityFromMainRoom)
        {
            foreach (Room room in allRooms)
            {
                if (room.isAccessibleFromMainRoom)
                {
                    roomListB.Add(room);
                }
                else { roomListA.Add(room); }
            }
        }
        else
        {
            roomListA = allRooms;
            roomListB = allRooms;
        }
        int bestDistance = 0;
        Coord bestTileA = new Coord();
        Coord bestTileB = new Coord();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();
        bool possibleConnectionFound = false;

        foreach(Room roomA in roomListA)
        {
            if (!forceAccessibilityFromMainRoom)
            {
                possibleConnectionFound = false;
                if(roomA.connectedRooms.Count > 0) { continue; }
            }
            foreach (Room roomB in roomListB)
            {
                if(roomA==roomB || roomA.isConnected(roomB)) { continue; }
                for(int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++)
                {
                    for (int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++)
                    {
                        Coord tileA = roomA.edgeTiles[tileIndexA];
                        Coord tileB = roomB.edgeTiles[tileIndexB];
                        int distanceBetweenRooms = (int)(Mathf.Pow(tileA.tileX - tileB.tileX, 2) + Mathf.Pow(tileA.tileY - tileB.tileY, 2));

                        if(distanceBetweenRooms < bestDistance || !possibleConnectionFound)
                        {
                            bestDistance = distanceBetweenRooms;
                            possibleConnectionFound = true;
                            bestTileA = tileA; 
                            bestTileB = tileB;
                            bestRoomA = roomA;
                            bestRoomB = roomB;
                        }
                    }

                }
            }
            if (possibleConnectionFound && !forceAccessibilityFromMainRoom)
            {
                CreatePassage(bestRoomA,bestRoomB,bestTileA,bestTileB);
            }
        }
        if (possibleConnectionFound && forceAccessibilityFromMainRoom)
        {
            CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            ConnectClosestRooms(allRooms, true);
        }
        if (!forceAccessibilityFromMainRoom)
        {
            ConnectClosestRooms(allRooms, true);
        }
    }

    void CreatePassage(Room roomA,Room roomB, Coord tileA, Coord tileB)
    {
        Room.ConnectRooms(roomA, roomB);
        List<Coord> line = GetLine(tileA, tileB);
        foreach(Coord c in line)
        {
            DrawCircle(c, 1);
        }
    }
    void DrawCircle(Coord c, int r)
    {
        for (int x = -r; x <= r; x++)
        {
            for(int y = -r; y <= r; y++)
            {
                if(x*x + y*y<= r * r)
                {
                    int drawX = c.tileX + x;
                    int drawY = c.tileY + y;
                    if(IsInMapRange(drawX, drawY))
                    {
                        map[drawX, drawY] = 0;
                    }
                }
            }
        }
    }
    List<Coord> GetLine(Coord from, Coord to)
    {
        List<Coord> line = new List<Coord>();
        int x = from.tileX;
        int y = from.tileY;

        int dx = to.tileX - from.tileX;
        int dy = to.tileY - from.tileY;

        bool inverted = false;
        int step = Math.Sign(dx);
        int gradientStep = Math.Sign(dy);

        int longest = Mathf.Abs(dx);
        int shortest = Mathf.Abs(dy);

        if (longest < shortest)
        {
            inverted = true;
            longest = Mathf.Abs(dy);
            shortest = Mathf.Abs(dx);

            step = Math.Sign(dy);
            gradientStep = Math.Sign(dx);
        }
        int gradientAccumulation = longest / 2;
        for(int i = 0;i<longest;i++)
        {
            line.Add(new Coord(x,y));

            if(inverted) y += step;
            else x += step;

            gradientAccumulation += shortest;
            if(gradientAccumulation >= longest)
            {
                if (inverted) x += gradientStep;
                else y += gradientStep;
                gradientAccumulation -= longest;
            }
        }
        return line;
    }
    Vector3 CoordToWorldPoint(Coord tile)
    {
        return new Vector3( 0.5f + tile.tileX, 2, 0.5f + tile.tileY);
    }

    class Room : IComparable<Room>
    {
        public List<Coord> tiles;
        public List<Coord> edgeTiles;
        public List<Room> connectedRooms;

        public int roomSize;

        public bool isAccessibleFromMainRoom;
        public bool isMainRoom;

        public Room()
        {}
        public Room(List<Coord> roomTiles, int[,] map)
        {
            tiles = roomTiles;
            roomSize = tiles.Count;
            connectedRooms = new List<Room>();

            edgeTiles = new List<Coord>();
            foreach(Coord tile in tiles)
            {
                for(int x = tile.tileX-1; x<=tile.tileX+1; x++)
                {
                    for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                    {
                        if(x== tile.tileX || y== tile.tileY)
                        {
                            if (map[x, y] == 1)
                            {
                                edgeTiles.Add(tile);
                            }
                        }
                    }

                }
            }
        }
        public void SetAccessibleFromMainRoom()
        {
            if (!isAccessibleFromMainRoom)
            {
                isAccessibleFromMainRoom = true;
                foreach(Room room in connectedRooms)
                {
                    room.isAccessibleFromMainRoom=true;
                }
            }
        }
        public static void ConnectRooms(Room roomA, Room roomB)
        {
            if (roomA.isAccessibleFromMainRoom)
            {
                roomB.SetAccessibleFromMainRoom();
            }
            else if (roomB.isAccessibleFromMainRoom)
            {
                roomA.SetAccessibleFromMainRoom();
            }
            roomA.connectedRooms.Add(roomB);
            roomB.connectedRooms.Add(roomA);
        }
        public bool isConnected(Room otherRoom)
        {
            return connectedRooms.Contains(otherRoom);
        }
        public int CompareTo(Room other)
        {
            return other.roomSize.CompareTo(roomSize);
        }
    }
    bool IsInMapRange(int x, int y)
    {
        return (x >= 0 && x < width && y >= 0 && y < height);
    }
    void Map2Hash()
    {
        floorMapHash = new HashSet<Vector2Int> ();
        for(int i = 0; i < width; i++)
        {
            for(int j = 0; j < height; j++)
            {
                if (map[i, j] == 0)
                {
                    floorMapHash.Add(new Vector2Int(i, j));
                }
            }
        }
        wallMapHash = new HashSet<Vector2Int> ();

        List<List<Coord>> floorRegions = GetRegions(0);

        foreach (List<Coord> floorRegion in floorRegions)
        {
            foreach (Coord floor in floorRegion)
            {
                for (int x = floor.tileX - 1; x <= floor.tileX + 1; x++)
                {
                    for (int y = floor.tileY - 1; y <= floor.tileY + 1; y++)
                    {
                        if (IsInMapRange(x, y))
                        {
                            if (map[x, y] == 1)
                            {
                                wallMapHash.Add(new Vector2Int(x, y));
                            }
                        }
                    }

                }
            }
        }
    }
}