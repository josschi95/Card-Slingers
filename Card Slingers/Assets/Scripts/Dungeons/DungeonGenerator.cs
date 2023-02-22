using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    public bool TEST_HALF_OFFSET;

    [SerializeField] private bool _generateAtStart;
    [SerializeField] private bool _usePresetSize;
    [SerializeField] private DungeonSize _dungeonSize;
    
    [Space]
    [Space]

    [SerializeField] private DungeonRoom[] dungeonRoomPrefabs;
    [SerializeField] private IntermediaryNode _intermediaryNode;
    [SerializeField] private DungeonRoom startRoomPrefab;

    [Space]

    [SerializeField] private GameObject _hallwayVertical;
    [SerializeField] private GameObject _hallwayHorizontal;
    [SerializeField] private GameObject _hallwayCorner;

    [Space]
    
    [SerializeField] private int minMainLineRooms;
    [SerializeField] private int maxMainLineRooms;

    [Space]

    [SerializeField] private int minBonusRooms;
    [SerializeField] private int maxBonusRooms;

    [Space]

    [SerializeField] private int _minimumOffset = 15;

    [Space]

    [SerializeField] private List<DungeonRoom> dungeonRooms;
    [SerializeField] private List<DungeonRoom> mainLineRooms;

    public enum DungeonSize { Small, Medium, Large }

    private static DungeonFeatures SMALL_DUNGEON;
    private static DungeonFeatures MEDIUM_DUNGEON;
    private static DungeonFeatures LARGE_DUNGEON;

    private void Awake()
    {
        SMALL_DUNGEON = new DungeonFeatures
        (
            4, //min main rooms
            7, //max main rooms
            3, //min side rooms
            4, //max side rooms
            3, //min num combats
            5 //max num combats
        );

        MEDIUM_DUNGEON = new DungeonFeatures
        (
            8, //min main rooms
            14, //max main rooms
            6, //min side rooms
            8, //max side rooms
            6, //min num combats
            10 //max num combats
        );

        LARGE_DUNGEON = new DungeonFeatures
        (
            15, //min main rooms
            28, //max main rooms
            15, //min side rooms
            20, //max side rooms
            10, //min num combats
            15 //max num combats
        );
    }

    private void Start()
    {
        if (_generateAtStart)
        {
            if (_usePresetSize) GenerateDungeon(_dungeonSize);
            else GenerateDungeon(minMainLineRooms, maxMainLineRooms, minBonusRooms, maxBonusRooms);
        }
    }

    public void GenerateDungeon(DungeonSize dungeonSize)
    {
        dungeonRooms = new List<DungeonRoom>();
        mainLineRooms = new List<DungeonRoom>();

        var features = SMALL_DUNGEON;
        if (dungeonSize == DungeonSize.Medium) features = MEDIUM_DUNGEON;
        else if (dungeonSize == DungeonSize.Large) features = LARGE_DUNGEON;

        int mainRooms = Random.Range(features.minMainRooms, features.maxMainRooms + 1);
        int bonusRooms = Random.Range(features.minSideRooms, features.maxSideRooms + 1);
        int combats = Random.Range(features.minCombats, features.maxCombats + 1);

        var startRoom = Instantiate(startRoomPrefab, Vector3.zero, Quaternion.identity);
        dungeonRooms.Add(startRoom);
        mainLineRooms.Add(startRoom);

        StartCoroutine(SpawnRooms(mainRooms, bonusRooms));

        Debug.LogWarning("Check Notes.");
        /*
         * Going to bed. last thing that I can think of is to build out hallways and nodes as I go along" +
            "rather than at the very end. e.g. When I place a room, I modify its position so that it lines up with whatever" +
            "offset I need for it to look pretty.

            Basically once I decide on the tentative placement for the room, then go and check what the hallway will look like, 
            Then modify the position accordingly



        ok changes have been made.
        however, I'm geting conflicting results for the number of hallways that need to be made by the crossbar.
        the crossbar hallway positions are still off, so I added isCrossBar in the create hallway function, use this along with
        the offset crom connect rooms to reposition thos crossbars

        */
    }

    public void GenerateDungeon(int minMain, int maxMain, int minBonus, int maxBonus)
    {
        dungeonRooms = new List<DungeonRoom>();
        mainLineRooms = new List<DungeonRoom>();

        int mainRoomsToSpawn = Random.Range(minMain, maxMain + 1);
        int bonusRoomsToSpawn = Random.Range(minBonus, maxBonus + 1);

        var startRoom = Instantiate(startRoomPrefab, Vector3.zero, Quaternion.identity);
        dungeonRooms.Add(startRoom);
        mainLineRooms.Add(startRoom);

        StartCoroutine(SpawnRooms(mainRoomsToSpawn, bonusRoomsToSpawn));
    }

    private IEnumerator SpawnRooms(int mainLineRoomsToSpawn, int bonusRooms)
    {
        //Create the main line of rooms
        while(mainLineRoomsToSpawn > 0)
        {
            var roomPrefab = dungeonRoomPrefabs[Random.Range(0, dungeonRoomPrefabs.Length)];

            var previousRoom = dungeonRooms[dungeonRooms.Count - 1];

            var newRoomOffset = Vector3.zero;
            newRoomOffset.x = previousRoom.RoomDimensions.x * 0.5f + roomPrefab.RoomDimensions.x * 0.5f + _minimumOffset;

            float vertChance = Random.value;
            var vertOffset = previousRoom.RoomDimensions.y * 0.5f + roomPrefab.RoomDimensions.y * 0.5f + _minimumOffset;

            //Adjust up or down so it doesn't skew too much in the same direction
            if (previousRoom.gameObject.transform.position.z > 50) vertChance = 0.1f;
            else if (previousRoom.gameObject.transform.position.z < -50) vertChance = 0.9f;
            
            if (vertChance <= 0.2f) newRoomOffset.z = -vertOffset;
            else if (vertChance <= 0.4f) newRoomOffset.z = -vertOffset * 0.5f;
            else if (vertChance >= 0.8) newRoomOffset.z = vertOffset;
            else if (vertChance >= 0.6) newRoomOffset.z = vertOffset * 0.5f;

            //Rounds it to a multiple of 5 for easy connection via tiles
            newRoomOffset = SnapPosition(newRoomOffset);

            var newRoom = Instantiate(roomPrefab, previousRoom.transform.position + newRoomOffset, Quaternion.identity);
            
            dungeonRooms.Add(newRoom);
            mainLineRooms.Add(newRoom);

            var offset = ConnectRoomsInLineNew(previousRoom, newRoom);
            newRoom.transform.position += offset;
            mainLineRoomsToSpawn--;

            yield return null;
        }

        int retries = 0;
        //Add on side rooms to the main line
        while (bonusRooms > 0)
        {
            if (retries > 10) yield break; //just stop any infinite loops

            var roomPrefab = dungeonRoomPrefabs[Random.Range(0, dungeonRoomPrefabs.Length)];

            //Don't add any bonus rooms to the start room
            var roomToConnectTo = mainLineRooms[Random.Range(1, mainLineRooms.Count)];

            var direction = Direction.Up;
            if (roomToConnectTo.ConnectedRooms[(int)direction] != null) direction = Direction.Down;
            if (roomToConnectTo.ConnectedRooms[(int)direction] != null) continue;

            var newRoomOffset = Vector3.zero;
            var minZ = roomToConnectTo.RoomDimensions.y * 0.5f + roomPrefab.RoomDimensions.y * 0.5f + _minimumOffset;
            if (direction == Direction.Up) newRoomOffset.z = minZ;
            else newRoomOffset.z = -minZ;

            float hertChance = Random.value;
            var hertOffset = roomToConnectTo.RoomDimensions.x * 0.5f + roomPrefab.RoomDimensions.x * 0.5f + _minimumOffset;

            if (hertChance <= 0.2f) newRoomOffset.x = -hertOffset;
            else if (hertChance <= 0.4f) newRoomOffset.x = -hertOffset * 0.5f;
            else if (hertChance >= 0.8) newRoomOffset.x = hertOffset;
            else if (hertChance >= 0.6) newRoomOffset.x = hertOffset * 0.5f;

            //Rounds it to a multiple of 5 for easy connection via tiles
            newRoomOffset = SnapPosition(newRoomOffset);

            var newPosition = roomToConnectTo.transform.position + newRoomOffset;
            var collidingRooms = Physics.OverlapBox(newPosition, new Vector3(roomPrefab.RoomDimensions.x * 0.5f, 5, roomPrefab.RoomDimensions.y * 0.5f));

            if (collidingRooms.Length > 0)
            {
                //Debug.Log("Overlap found. Retring.");
                retries++;
                continue;
            }

            var newRoom = Instantiate(roomPrefab, newPosition, Quaternion.identity);

            dungeonRooms.Add(newRoom);

            var offset = ConnectRoomsToNearest(roomToConnectTo, newRoom);
            newRoom.transform.position += offset;

            bonusRooms--;

            //Add a second bonus room onto the already existing bonus room
            if (Random.value < 0.15f)
            {

            }

            yield return null;
        }

        //ConnectWaypoints();

        //Close off unused entrances
        for (int i = 0; i < dungeonRooms.Count; i++)
        {
            dungeonRooms[i].OnConfirmLayout();
        }

        PlacePlayerStart();
    }

    private Vector3 ConnectRoomsToNearest(DungeonRoom roomA, DungeonRoom roomB)
    {
        float lowestDistance = int.MaxValue;
        int indexA = 0, indexB = 0;
        var offset = Vector3.zero;
        int mult = 1;

        for (int a = 0; a < roomA.Nodes.Length; a++)
        {
            for (int b = 0; b < roomB.Nodes.Length; b++)
            {
                var dist = Vector3.Distance(roomA.Nodes[a].position, roomB.Nodes[b].position);
                if (dist < lowestDistance)
                {
                    indexA = a;
                    indexB = b;
                    lowestDistance = dist;
                }
            }
        }

        //roomA.ConnectedRooms[indexA] = roomB;
        //roomB.ConnectedRooms[indexB] = roomA;
        ConnectWaypointsNew(roomA, roomB, (Direction)indexA, (Direction)indexB);
        if (roomA.transform.position.x == roomB.transform.position.x) return offset;
        if (roomA.transform.position.z == roomB.transform.position.z) return offset;
        if (indexA >= 2 && indexB >= 2) mult = 2;
        else if (indexA <= 1 && indexB <= 1) mult = 2;

        if (roomB.transform.position.x > roomA.transform.position.x) offset.x += 2.5f * mult;
        else offset.x -= 2.5f * mult;

        //room A is higher than room B
        if (roomA.transform.position.z > roomB.transform.position.z) offset.z -= 2.5f * mult;
        else offset.z += 2.5f * mult;



        return offset;
    }

    private Vector3 ConnectRoomsInLineNew(DungeonRoom roomA, DungeonRoom roomB)
    {
        var offset = Vector3.zero;

        //The rooms have the same z value
        if (roomA.transform.position.z == roomB.transform.position.z)
        {
            ConnectWaypointsNew(roomA, roomB, Direction.Right, Direction.Left);
            return offset;
        }

        //Do I need to check for same X as well? Probably

        //room B is to the right of room A
        Direction fromDirection;
        Direction toDirection;

        if (roomB.transform.position.x > roomA.transform.position.x)
        {
            toDirection = Direction.Left;
            offset.x += 2.5f;
        }
        else
        {
            toDirection = Direction.Right;
            offset.x -= 2.5f;
        }

        //room A is higher than room B
        if (roomA.transform.position.z > roomB.transform.position.z)
        {
            fromDirection = Direction.Down;
            offset.z -= 2.5f;
        }
        else
        {
            fromDirection = Direction.Up;
            offset.z += 2.5f;
        }

        ConnectWaypointsNew(roomA, roomB, fromDirection, toDirection);
        return offset;
    }


    private void ConnectWaypointsNew(DungeonRoom roomA, DungeonRoom roomB, Direction fromDirection, Direction toDirection)
    {
        roomA.ConnectedRooms[(int)fromDirection] = roomB;
        roomB.ConnectedRooms[(int)toDirection] = roomA;

        var pointA = roomA.RoomWaypoints[(int)fromDirection];
        var pointB = roomB.RoomWaypoints[(int)toDirection];

        if (pointA.ConnectedNode != null) Debug.LogWarning("Point A Already Connected");
        if (pointB.ConnectedNode != null) Debug.LogWarning("Point B Already Connected");

        var intermediaryPos = Vector3.zero;
        //Connection requires wrapping around the entire room
        if (fromDirection == toDirection) Debug.LogError("This should not happen.");
        //fromDirection is Up or Down, toDirection is Left or Right
        else if ((int)fromDirection <= 1 && (int)toDirection >= 2)
        {
            intermediaryPos.x = pointA.transform.position.x;
            intermediaryPos.z = pointB.transform.position.z;
        }
        //fromDirection is Left or Right, toDirection is Up or Down
        else if ((int)fromDirection >= 2 && (int)toDirection <= 1)
        {
            intermediaryPos.x = pointB.transform.position.x;
            intermediaryPos.z = pointA.transform.position.z;
        }
        //The two nodes are in a straight line Up/Down or Left/Right
        else if (pointA.transform.position.x == pointB.transform.position.x || pointA.transform.position.z == pointB.transform.position.z) //in a straight line
        {
            pointA.SetConnectedWaypoint(pointB);
            pointB.SetConnectedWaypoint(pointA);
            CreateHallways(pointA, pointB);
        }
        else //The two nodes are staggered
        {
            float crossBarXValue = (pointA.transform.position.z + pointB.transform.position.z) * 0.5f; //Find middle point
            float crossBarZValue = (pointA.transform.position.z + pointB.transform.position.z) * 0.5f; //Find middle point

            Vector3 firstIntermediatePos = SnapPosition(crossBarXValue, 0, crossBarZValue);
            Vector3 secondIntermediatePos = SnapPosition(crossBarXValue, 0, crossBarZValue);

            if (pointA.transform.position.x != pointB.transform.position.x) //Crossbar is Horizontal
            {
                firstIntermediatePos.x = pointA.transform.position.x;
                secondIntermediatePos.x = pointB.transform.position.x;
            }
            else if (pointA.transform.position.z != pointB.transform.position.z) //Crossbar is Vertical
            {
                firstIntermediatePos.z = pointA.transform.position.z;
                secondIntermediatePos.z = pointB.transform.position.z;
            }

            var firstIntermediate = Instantiate(_intermediaryNode, firstIntermediatePos, Quaternion.identity);
            var secondIntermediate = Instantiate(_intermediaryNode, secondIntermediatePos, Quaternion.identity);

            firstIntermediate.SetAsIntermediate(secondIntermediate, pointA);
            secondIntermediate.SetAsIntermediate(firstIntermediate, pointB);

            CreateHallways(firstIntermediate, secondIntermediate, true);
            CreateHallways(pointA, firstIntermediate);
            CreateHallways(pointB, secondIntermediate);
        }

        if (intermediaryPos != Vector3.zero)
        {
            var inter = Instantiate(_intermediaryNode, intermediaryPos, Quaternion.identity);
            inter.SetAsIntermediate(pointA, pointB);

            CreateHallways(pointA, inter);
            CreateHallways(pointB, inter);
        }
    }

    private void CreateHallways(Waypoint fromPoint, Waypoint toPoint, bool isCrossbar = false)
    {
        Vector3 spawnPos = Vector3.zero;
        float offset = 5;
        //waypoints are vertical
        if (fromPoint.transform.position.x == toPoint.transform.position.x)
        {
            //Get the absolute value for the difference in their position, then divide by 5
            int hallwaysToSpawn = Mathf.RoundToInt((Mathf.Abs(fromPoint.transform.position.z - toPoint.transform.position.z) * 0.2f));
            if (isCrossbar)
            {

            }
            // fromPoint.transform.position; //spawn from bottom to top
            //if (toPoint.transform.position.z < fromPoint.transform.position.z) spawnPos = toPoint.transform.position;
            if (toPoint.transform.position.z < fromPoint.transform.position.z)
            {
                spawnPos.z -= 5;
                offset = -5;
            }
            
            for (int i = 0; i < hallwaysToSpawn; i++)
            {
                //var hall = Instantiate(_hallwayVertical, spawnPos, Quaternion.identity, fromPoint.transform);
                var hall = Instantiate(_hallwayVertical, fromPoint.transform);
                hall.transform.localPosition = spawnPos;
                spawnPos.z += offset;
            }
        }
        //wayspoints are horizontal
        else
        {
            //Get the absolute value for the difference in their position, then divide by 5
            int hallwaysToSpawn = Mathf.RoundToInt((Mathf.Abs(fromPoint.transform.position.x - toPoint.transform.position.x) * 0.2f));
            if (isCrossbar)
            {

            }
            //var spawnPos = fromPoint.transform.position; //spawn from left to right
            //if (toPoint.transform.position.x < fromPoint.transform.position.x) spawnPos = toPoint.transform.position;
            if (toPoint.transform.position.x < fromPoint.transform.position.x)
            {
                spawnPos.x -= 5;
                offset = -5;
            }

            for (int i = 0; i < hallwaysToSpawn; i++)
            {
                var hall = Instantiate(_hallwayHorizontal, fromPoint.transform);
                hall.transform.localPosition = spawnPos;
                spawnPos.x += offset;
            }
        }
    }

    private void PlacePlayerStart()
    {
        var player = GameObject.Find("PlayerController");
        player.transform.position = transform.position;
        if (dungeonRooms[0].ConnectedRooms[0] != null) player.transform.eulerAngles = Vector3.zero;
        else if (dungeonRooms[0].ConnectedRooms[1] != null) player.transform.eulerAngles = new Vector3(0, 180, 0);
        else if (dungeonRooms[0].ConnectedRooms[2] != null) player.transform.eulerAngles = new Vector3(0, -90, 0);
        else if (dungeonRooms[0].ConnectedRooms[3] != null) player.transform.eulerAngles = new Vector3(0, 90, 0);
    }
    
    private Vector3 SnapPosition(Vector3 input, float factor = 5)
    {
        if (factor == 0) throw new UnityException("Cannot divide by 0!");

        float x = Mathf.Round(input.x / factor) * factor;
        float y = Mathf.Round(input.y / factor) * factor;
        float z = Mathf.Round(input.z / factor) * factor;

        return new Vector3(x, y, z);
    }
    
    private Vector3 SnapPosition(float x, float y, float z, float factor = 5)
    {
        if (factor == 0) throw new UnityException("Cannot divide by 0!");

        x = Mathf.Round(x / factor) * factor;
        y = Mathf.Round(y / factor) * factor;
        z = Mathf.Round(z / factor) * factor;

        return new Vector3(x, y, z);
    }

    ///////////////////////////////////

    /*private void ConnectRoomsInLine(DungeonRoom roomA, DungeonRoom roomB)
    {
        //The rooms have the same z value
        if (roomA.transform.position.z == roomB.transform.position.z)
        {
            ConnectWaypointsNew(roomA, roomB, Direction.Right, Direction.Left);
            //roomA.ConnectedRooms[(int)Direction.Right] = roomB;
            //roomB.ConnectedRooms[(int)Direction.Left] = roomA;
            return;
        }
        //Do I need to check for same X as well? Probably

        //room B is to the right of room A
        Direction fromDirection;
        Direction toDirection;
        if (roomB.transform.position.x > roomA.transform.position.x)
        {
            //roomB.ConnectedRooms[(int)Direction.Left] = roomA;
            toDirection = Direction.Left;
        }
        else
        {
            //roomB.ConnectedRooms[(int)Direction.Right] = roomA;
            toDirection = Direction.Right;
        }

        //room A is higher than room B
        if (roomA.transform.position.z > roomB.transform.position.z)
        {
            //roomA.ConnectedRooms[(int)Direction.Down] = roomB;
            fromDirection = Direction.Down;
        }
        else
        {
            //roomA.ConnectedRooms[(int)Direction.Up] = roomB;
            fromDirection = Direction.Up;
        }

        ConnectWaypointsNew(roomA, roomB, fromDirection, toDirection);
    }*/

    /*private void ConnectWaypoints()
    {
        foreach (DungeonRoom room in dungeonRooms)
        {
            for (int i = 0; i < room.ConnectedRooms.Length; i++)
            {
                //no room on that side
                if (room.ConnectedRooms[i] == null) continue;
                //already connected it in a previous iteration
                if (room.RoomWaypoints[i].ConnectedNode != null) continue;

                var connectedRoom = room.ConnectedRooms[i];

                Direction fromDirection = (Direction)i;
                Direction toDirection = Direction.Up;

                for (int r = 0; r < connectedRoom.ConnectedRooms.Length; r++)
                {
                    if (connectedRoom.ConnectedRooms[r] == room) toDirection = (Direction)r;
                }

                var intermediaryPos = Vector3.zero;
                var fromNode = room.Nodes[(int)fromDirection];
                var toNode = connectedRoom.Nodes[(int)toDirection];

                //Connection requires wrapping around the entire room
                if (fromDirection == toDirection) Debug.LogError("This should not happen.");

                //fromDirection is Up or Down, toDirection is Left or Right
                else if ((int)fromDirection <= 1 && (int)toDirection >= 2)
                {
                    intermediaryPos.x = fromNode.position.x;
                    intermediaryPos.z = toNode.position.z;
                }
                //fromDirection is Left or Right, toDirection is Up or Down
                else if ((int)fromDirection >= 2 && (int)toDirection <= 1)
                {
                    intermediaryPos.x = toNode.position.x;
                    intermediaryPos.z = fromNode.position.z;
                }
                else //Going between Up/Down or Left/Right
                {
                    if (fromNode.position.x == toNode.position.x || fromNode.position.z == toNode.position.z) //in a straight line
                    {
                        room.RoomWaypoints[(int)fromDirection].SetConnectedWaypoint(connectedRoom.RoomWaypoints[(int)toDirection]);
                        connectedRoom.RoomWaypoints[(int)toDirection].SetConnectedWaypoint(room.RoomWaypoints[(int)fromDirection]);
                        CreateHallways(room.RoomWaypoints[(int)fromDirection], connectedRoom.RoomWaypoints[(int)toDirection]);
                        continue; //Return to start of loop to ignore code below
                    }

                    //The two nodes are staggered
                    float crossBarXValue = (fromNode.position.z + toNode.position.z) * 0.5f; //Find middle point
                    float crossBarZValue = (fromNode.position.z + toNode.position.z) * 0.5f; //Find middle point

                    Vector3 firstIntermediatePos = SnapPosition(crossBarXValue, 0, crossBarZValue);
                    Vector3 secondIntermediatePos = SnapPosition(crossBarXValue, 0, crossBarZValue);

                    if (fromNode.position.x != toNode.position.x) //Crossbar is Horizontal
                    {
                        firstIntermediatePos.x = fromNode.position.x;
                        secondIntermediatePos.x = toNode.position.x;
                    }
                    else if (fromNode.position.z != toNode.position.z) //Crossbar is Vertical
                    {
                        firstIntermediatePos.z = fromNode.position.z;
                        secondIntermediatePos.z = toNode.position.z;
                    }

                    var firstIntermediate = Instantiate(_intermediaryNode, firstIntermediatePos, Quaternion.identity);
                    var secondIntermediate = Instantiate(_intermediaryNode, secondIntermediatePos, Quaternion.identity);

                    firstIntermediate.SetAsIntermediate(secondIntermediate, room.RoomWaypoints[(int)fromDirection]);
                    secondIntermediate.SetAsIntermediate(firstIntermediate, connectedRoom.RoomWaypoints[(int)toDirection]);

                    CreateHallways(firstIntermediate, secondIntermediate);
                    CreateHallways(firstIntermediate, room.RoomWaypoints[(int)fromDirection]);
                    CreateHallways(secondIntermediate, connectedRoom.RoomWaypoints[(int)toDirection]);
                }

                if (intermediaryPos != Vector3.zero)
                {
                    var inter = Instantiate(_intermediaryNode, intermediaryPos, Quaternion.identity);
                    inter.SetAsIntermediate(room.RoomWaypoints[(int)fromDirection], connectedRoom.RoomWaypoints[(int)toDirection]);

                    CreateHallways(inter, room.RoomWaypoints[(int)fromDirection]);
                    CreateHallways(inter, connectedRoom.RoomWaypoints[(int)toDirection]);
                }
            }
        }
    }*/


}

public enum Direction { Up, Down, Left, Right}

public struct DungeonFeatures
{
    public int minMainRooms;
    public int maxMainRooms;
    
    public int minSideRooms;
    public int maxSideRooms;

    public int minCombats;
    public int maxCombats;

    public DungeonFeatures(int minMain, int maxMain, int minSide, int maxSide, int minCombat, int maxCombat)
    {
        minMainRooms = minMain;
        maxMainRooms = maxMain;

        minSideRooms = minSide;
        maxSideRooms = maxSide;

        minCombats = minCombat;
        maxCombats = maxCombat;
    }
}