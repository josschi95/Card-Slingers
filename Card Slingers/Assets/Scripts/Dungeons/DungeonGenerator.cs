using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    #region - Variables -
    private const int MINIMUM_OFFSET = 15; //Anything less will cause issues with hallways

    [SerializeField] private bool _generateAtStart;
    [SerializeField] private bool _usePresetSize;
    [SerializeField] private DungeonSize _dungeonSize;

    [Space]
    [Space]

    [SerializeField] private DungeonRoom startRoomPrefab;
    [SerializeField] private DungeonRoom[] dungeonRoomPrefabs;

    [Space]

    [SerializeField] private GameObject _hallwayVertical;
    [SerializeField] private GameObject _hallwayHorizontal;
    [Tooltip("Up/Left, Up/Right, Down/Left, Down/Right")]
    [SerializeField] private IntermediaryNode[] _corners;

    [Space]

    [SerializeField] private int minMainLineRooms;
    [SerializeField] private int maxMainLineRooms;

    [Space]

    [SerializeField] private int minBonusRooms;
    [SerializeField] private int maxBonusRooms;

    [Space]

    [SerializeField] private List<DungeonRoom> dungeonRooms;
    [SerializeField] private List<GameObject> tentativePieces;

    public enum DungeonSize { Small, Medium, Large }

    private static DungeonFeatures SMALL_DUNGEON;
    private static DungeonFeatures MEDIUM_DUNGEON;
    private static DungeonFeatures LARGE_DUNGEON;
    #endregion

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
            7, //min main rooms
            12, //max main rooms
            6, //min side rooms
            8, //max side rooms
            6, //min num combats
            10 //max num combats
        );

        LARGE_DUNGEON = new DungeonFeatures
        (
            10, //min main rooms
            16, //max main rooms
            10, //min side rooms
            15, //max side rooms
            10, //min num combats
            15 //max num combats
        );
    }

    private void Start()
    {
        //Debug.LogError("You're not allowed to work on this at work. Just set up some more character models.");
        if (_generateAtStart)
        {
            if (_usePresetSize) GenerateDungeon(_dungeonSize);
            else GenerateDungeon(minMainLineRooms, maxMainLineRooms, minBonusRooms, maxBonusRooms);
        }
    }

    public void GenerateDungeon(DungeonSize dungeonSize)
    {
        dungeonRooms = new List<DungeonRoom>();
        tentativePieces = new List<GameObject>();

        var features = SMALL_DUNGEON;
        if (dungeonSize == DungeonSize.Medium) features = MEDIUM_DUNGEON;
        else if (dungeonSize == DungeonSize.Large) features = LARGE_DUNGEON;

        int mainRooms = Random.Range(features.minMainRooms, features.maxMainRooms + 1);
        int bonusRooms = Random.Range(features.minSideRooms, features.maxSideRooms + 1);
        int combats = Random.Range(features.minCombats, features.maxCombats + 1);

        var startRoom = Instantiate(startRoomPrefab, Vector3.zero, Quaternion.identity);
        dungeonRooms.Add(startRoom);

        //StartCoroutine(SpawnRooms(mainRooms, bonusRooms));
        StartCoroutine(SpawnRooms(mainRooms));
    }

    public void GenerateDungeon(int minMain, int maxMain, int minBonus, int maxBonus)
    {
        dungeonRooms = new List<DungeonRoom>();

        int mainRoomsToSpawn = Random.Range(minMain, maxMain + 1);
        int bonusRoomsToSpawn = Random.Range(minBonus, maxBonus + 1);

        var startRoom = Instantiate(startRoomPrefab, Vector3.zero, Quaternion.identity);
        dungeonRooms.Add(startRoom);

        //StartCoroutine(SpawnRooms(mainRoomsToSpawn, bonusRoomsToSpawn));
        StartCoroutine(SpawnRooms(mainRoomsToSpawn));
    }
    
    private IEnumerator SpawnRooms(int roomsToSpawn)
    {
        Physics.autoSyncTransforms = true;
        //Add on side rooms to the main line
        while (roomsToSpawn > 0)
        {
            tentativePieces.Clear();
            yield return new WaitForSeconds(0.25f);

            var roomPrefab = dungeonRoomPrefabs[Random.Range(0, dungeonRoomPrefabs.Length)];

            DungeonRoom roomToConnectTo;
            if (dungeonRooms.Count == 1) roomToConnectTo = dungeonRooms[0];
            else roomToConnectTo = dungeonRooms[Random.Range(1, dungeonRooms.Count)];

            if (!CanAddToRoom(roomToConnectTo)) continue;

            var fromDirection = GetOpenNode(roomToConnectTo);
            var newRoomPosition = roomToConnectTo.transform.position + GetRoomOffset(roomToConnectTo, roomPrefab, fromDirection);

            if (!CanPlaceRoom(roomPrefab, newRoomPosition)) continue;

            var newRoom = Instantiate(roomPrefab, newRoomPosition, Quaternion.identity);
            tentativePieces.Add(newRoom.gameObject);

            Direction toDirection = GetDirectionToNearest(roomToConnectTo, newRoom, fromDirection);

            if (!ConnectWaypoints(roomToConnectTo, newRoom, fromDirection, toDirection))
            {
                PurgeAttempts();
                continue;
            }

            dungeonRooms.Add(newRoom);
            roomToConnectTo.ConnectedRooms[(int)fromDirection] = newRoom;
            newRoom.ConnectedRooms[(int)toDirection] = roomToConnectTo;
            roomsToSpawn--;

            yield return null;
        }

        TryConnectLoops();
        OnDungeonComplete();
    }

    //connect waypoints and build out hallways
    private bool ConnectWaypoints(DungeonRoom roomA, DungeonRoom roomB, Direction fromDirection, Direction toDirection, bool canAdjust = true)
    {
        if (fromDirection == toDirection) return false; //Can't wrap around a room

        var pointA = roomA.Waypoints[(int)fromDirection];
        var pointB = roomB.Waypoints[(int)toDirection];

        if (pointA.ConnectedNode != null)
        {
            Debug.LogWarning("Point A at " + pointA.transform.position + " is already connected to node at " + pointA.ConnectedNode.transform.position);
            Debug.LogWarning("Was trying to connect to node at " + pointB.transform.position);
            Debug.Break();
            return false;
        }
        else if (pointB.ConnectedNode != null)
        {
            Debug.LogWarning("Point B at " + pointB.transform.position + " is already connected to node at " + pointB.ConnectedNode.transform.position);
            Debug.LogWarning("Was trying to connect to node at " + pointA.transform.position);
            Debug.Break();
            return false;
        }

        var intermediaryPos = Vector3.zero;

        //Nodes are vertically aligned
        if (pointA.transform.position.x == pointB.transform.position.x)
        {
            pointA.SetConnectedWaypoint(pointB);
            pointB.SetConnectedWaypoint(pointA);
            return CreateVerticalHallway(pointA.Point, pointB.Point);
        }
        //Nodes are horizontally aligned
        else if (pointA.transform.position.z == pointB.transform.position.z)
        {
            pointA.SetConnectedWaypoint(pointB);
            pointB.SetConnectedWaypoint(pointA);
            return CreateHorizontalHallway(pointA.Point, pointB.Point);
        }
        //fromDirection is Up/Down, toDirection is Left/Right
        else if ((int)fromDirection <= 1 && (int)toDirection >= 2)
        {
            //Will need to adjust position of Room B
            if (canAdjust) AdjustRoomPosition(roomA, roomB);
            if (!CanPlaceRoom(roomB, roomB.transform.position, true)) return false;

            intermediaryPos.x = roomA.transform.position.x;
            intermediaryPos.z = roomB.transform.position.z;

            if (!CanPlaceHallway(intermediaryPos)) return false; //There is something here

            var inter = Instantiate(GetCorner(fromDirection, toDirection), intermediaryPos, Quaternion.identity);
            inter.SetAsIntermediate(pointA, pointB);
            tentativePieces.Add(inter.gameObject);

            if (!CreateVerticalHallway(pointA.Point, inter.Point)) return false;
            return CreateHorizontalHallway(pointB.Point, inter.PointTwo); //intermediate's point two is always left/right
        }
        //fromDirection is Left/Right, toDirection is Up/Down
        else if ((int)fromDirection >= 2 && (int)toDirection <= 1)
        {
            //Will need to adjust position of Room B
            if (canAdjust) AdjustRoomPosition(roomA, roomB);
            if (!CanPlaceRoom(roomB, roomB.transform.position, true)) return false;

            intermediaryPos.x = roomB.transform.position.x;
            intermediaryPos.z = roomA.transform.position.z;

            if (!CanPlaceHallway(intermediaryPos)) return false; //There is something here

            var inter = Instantiate(GetCorner(fromDirection, toDirection), intermediaryPos, Quaternion.identity);
            inter.SetAsIntermediate(pointA, pointB);
            tentativePieces.Add(inter.gameObject);

            if (!CreateHorizontalHallway(pointA.Point, inter.PointTwo)) return false; //intermediate's point two is always left/right
            return CreateVerticalHallway(pointB.Point, inter.Point);
        }
        //The two nodes are in a staggered Up/Down
        else if ((int)fromDirection <= 1 && (int)toDirection <= 1)
        {
            if (canAdjust)
            {
                if (roomA.transform.position.z > roomB.transform.position.z) roomB.transform.position += Vector3.back * 2.5f;
                else roomB.transform.position += Vector3.forward * 2.5f;

                float dist = (Mathf.Abs(pointA.Point.position.z - pointB.Point.position.z));
                if (dist != SnapFloat(dist))
                {
                    if (fromDirection == Direction.Up) roomB.transform.position += Vector3.forward * 2.5f;
                    else roomB.transform.position += Vector3.back * 2.5f;
                }
                if (!CanPlaceRoom(roomB, roomB.transform.position, true)) return false;
            }

            //Base the crossBarZValue on the location of pointA.Point
            int numHalls = Mathf.FloorToInt((Mathf.Abs(pointA.Point.position.z - pointB.Point.position.z) * 0.2f)) + 1;
            float crossBarZValue = pointA.Point.position.z; //Place initial position to be even with the start point for pointA
            crossBarZValue += (pointA.Point.localPosition.z * 2) * Mathf.RoundToInt(numHalls * 0.5f);

            Vector3 firstMidPos = new Vector3(pointA.Point.position.x, 0, crossBarZValue);
            Vector3 secondMidPos = new Vector3(pointB.Point.position.x, 0, crossBarZValue);
            if (!CanPlaceHallway(firstMidPos)) return false; //There is something here
            if (!CanPlaceHallway(secondMidPos)) return false; //There is something here

            IntermediaryNode firstCorner, secondCorner;
            if (firstMidPos.x < secondMidPos.x)
            {
                firstCorner = GetCorner(fromDirection, Direction.Left);
                secondCorner = GetCorner(Direction.Right, toDirection);
            }
            else
            {
                firstCorner = GetCorner(fromDirection, Direction.Right);
                secondCorner = GetCorner(Direction.Left, toDirection);
            }

            var firstIntermediate = Instantiate(firstCorner, firstMidPos, Quaternion.identity);
            var secondIntermediate = Instantiate(secondCorner, secondMidPos, Quaternion.identity);
            tentativePieces.Add(firstIntermediate.gameObject);
            tentativePieces.Add(secondIntermediate.gameObject);

            firstIntermediate.SetAsIntermediate(secondIntermediate, pointA);
            secondIntermediate.SetAsIntermediate(firstIntermediate, pointB);

            //Need vertical from A to first intermediate, but only if hallway length is at least 1
            if (firstIntermediate.transform.position != pointA.Point.position)
            {
                if (!CreateVerticalHallway(pointA.Point, firstIntermediate.Point, false, true)) return false;
            }
            //Need second vertical from B to second intermediate, but only if hallway length is at least 1
            if (secondIntermediate.transform.position != pointB.Point.position)
            {
                if (!CreateVerticalHallway(pointB.Point, secondIntermediate.Point)) return false;
            }
            //Need horizontal between two intermediates. intermediate's point two is always left/right
            return CreateHorizontalHallway(firstIntermediate.PointTwo, secondIntermediate.PointTwo, true);
        }
        //The two nodes are in a staggered Left/Right
        else if ((int)fromDirection >= 2 && (int)toDirection >= 2)
        {
            if (canAdjust)
            {
                //Will need to adjust position of Room B
                if (roomB.transform.position.x > roomA.transform.position.x) roomB.transform.position += Vector3.right * 2.5f;
                else roomB.transform.position += Vector3.left * 2.5f;

                float dist = (Mathf.Abs(pointA.Point.position.x - pointB.Point.position.x));
                if (dist != SnapFloat(dist))
                {
                    if (fromDirection == Direction.Left) roomB.transform.position += Vector3.left * 2.5f;
                    else roomB.transform.position += Vector3.right * 2.5f;
                }
                if (!CanPlaceRoom(roomB, roomB.transform.position, true)) return false;
            }

            //Base the crossBarXValue on the location of pointA.Point
            int numHalls = Mathf.FloorToInt((Mathf.Abs(pointA.Point.position.x - pointB.Point.position.x) * 0.2f)) + 1;
            float crossBarXValue = pointA.Point.position.x; //Place initial position to be even with the start point for pointA
            crossBarXValue += (pointA.Point.localPosition.x * 2) * Mathf.RoundToInt(numHalls * 0.5f);

            Vector3 firstMidPos = new Vector3(crossBarXValue, 0, pointA.Point.position.z); //keep z value but snap x
            Vector3 secondMidPos = new Vector3(crossBarXValue, 0, pointB.Point.position.z);
            if (!CanPlaceHallway(firstMidPos)) return false; //There is something here
            if (!CanPlaceHallway(secondMidPos)) return false; //There is something here

            IntermediaryNode firstCorner, secondCorner;
            if (firstMidPos.z < secondMidPos.z)
            {
                firstCorner = GetCorner(fromDirection, Direction.Down);
                secondCorner = GetCorner(Direction.Up, toDirection);
            }
            else
            {
                firstCorner = GetCorner(fromDirection, Direction.Up);
                secondCorner = GetCorner(Direction.Down, toDirection);
            }

            var firstIntermediate = Instantiate(firstCorner, firstMidPos, Quaternion.identity);
            var secondIntermediate = Instantiate(secondCorner, secondMidPos, Quaternion.identity);
            tentativePieces.Add(firstIntermediate.gameObject);
            tentativePieces.Add(secondIntermediate.gameObject);

            firstIntermediate.SetAsIntermediate(secondIntermediate, pointA);
            secondIntermediate.SetAsIntermediate(firstIntermediate, pointB);

            //Need horizontal from A to first intermediate
            if (firstIntermediate.transform.position != pointA.Point.position)
            {
                if (!CreateHorizontalHallway(pointA.Point, firstIntermediate.PointTwo)) return false;
            }
            //Need second horizontal from B to second intermediate
            if (secondIntermediate.transform.position != pointB.Point.position)
            {
                if (!CreateHorizontalHallway(pointB.Point, secondIntermediate.PointTwo)) return false;
            }
            //Need vertical between two intermediates
            return CreateVerticalHallway(firstIntermediate.Point, secondIntermediate.Point, true);
        }

        Debug.LogWarning("I don't know what I missed.");
        return false;
    }

    //Creates the hallway objects, returns false if they would intersect with an existing object
    private bool CreateVerticalHallway(Transform fromPoint, Transform toPoint, bool isCrossbar = false, bool toCrossbar = false)
    {
        Physics.SyncTransforms();

        int numToSpawn = Mathf.FloorToInt((Mathf.Abs(fromPoint.position.z - toPoint.position.z) * 0.2f));
        if (!isCrossbar || toCrossbar) numToSpawn++; //crossbar Points are slightly closer
        if (numToSpawn == 0) return true; //This should only happen with narrow Z hallways

        for (int i = 0; i < numToSpawn; i++)
        {
            var pos = fromPoint.position + (fromPoint.localPosition * i * 2);
            if (isCrossbar) pos += fromPoint.localPosition;

            if (!CanPlaceHallway(pos))
            {
                Debug.Log(i + "(crossbar: " + isCrossbar + ")" + " Intersected at " + pos);
                return false; //There is something here
            }

            GameObject hall = Instantiate(_hallwayVertical, pos, Quaternion.identity);
            hall.gameObject.transform.SetParent(fromPoint);
            tentativePieces.Add(hall.gameObject);
        }
        return true;
    }

    //Creates the hallway objects, returns false if they would intersect with an existing object
    private bool CreateHorizontalHallway(Transform fromPoint, Transform toPoint, bool isCrossbar = false, bool toCrossbar = false)
    {
        Physics.SyncTransforms();

        //if (isCrossbar || toCrossbar) numToSpawn--; //crossbar Points are slightly closer
        //if (numToSpawn == 0) return true; //This should only happen with narrow Z hallways
        int numToSpawn = Mathf.FloorToInt((Mathf.Abs(fromPoint.position.x - toPoint.position.x) * 0.2f));
        if (!isCrossbar || toCrossbar) numToSpawn++; //crossbar Points are slightly closer
        if (numToSpawn == 0) return true; //This should only happen with narrow Z hallways

        for (int i = 0; i < numToSpawn; i++)
        {
            var pos = fromPoint.position + (fromPoint.localPosition * i * 2);
            if (isCrossbar) pos += fromPoint.localPosition;

            if (!CanPlaceHallway(pos))
            {
                Debug.Log(i + "(crossbar: " + isCrossbar + ")" + " Intersected at " + pos);
                return false; //There is something here
            }

            GameObject hall = Instantiate(_hallwayHorizontal, pos, Quaternion.identity) as GameObject;
            hall.gameObject.transform.SetParent(fromPoint);
            tentativePieces.Add(hall.gameObject);
        }
        return true;
    }

    private void TryConnectLoops()
    {
        foreach (DungeonRoom room in dungeonRooms)
        {
            if (room == dungeonRooms[0]) continue;

            for (int i = 0; i < room.ConnectedRooms.Length; i++)
            {
                if (room.ConnectedRooms[i] == null)
                {
                    CheckForNearbyNode(room.Waypoints[i]);
                }
            }
        }
    }

    private void CheckForNearbyNode(Waypoint point)
    {
        var otherPoints = Physics.OverlapBox(point.Point.position, Vector3.one * 30);
        //DrawDebugBox(point.Point.position, Quaternion.identity, Vector3.one * 30, Color.green);

        for (int i = 0; i < otherPoints.Length; i++)
        {
            tentativePieces.Clear();

            var newPoint = otherPoints[i].GetComponent<Waypoint>();
            if (newPoint == null || newPoint.ConnectedNode != null) continue; //is null or taken
            if (newPoint.Room == point.Room || newPoint.Room == dungeonRooms[0]) continue; //same room or start 
            if (RoomsLineUp(newPoint.Point.position, point.Point.position)) //points need to be on same offset
            {
                if (ConnectWaypoints(point.Room, newPoint.Room, point.direction, newPoint.direction, false))
                {
                    Debug.DrawLine(point.Point.position, newPoint.Point.position, Color.green, int.MaxValue);
                    point.Room.ConnectedRooms[(int)point.direction] = newPoint.Room;
                    newPoint.Room.ConnectedRooms[(int)newPoint.direction] = point.Room;
                    Debug.Log("Success!");
                }
                else
                {
                    Debug.DrawLine(point.Point.position, newPoint.Point.position, Color.red, int.MaxValue);
                    PurgeAttempts();
                }
            }
        }
    }

    //Confirm layout and orient player
    private void OnDungeonComplete()
    {
        //Close off unused entrances
        for (int i = 0; i < dungeonRooms.Count; i++)
        {
            dungeonRooms[i].OnConfirmLayout();
        }

        var player = GameObject.Find("PlayerController").transform;
        player.position = transform.position;
        if (dungeonRooms[0].ConnectedRooms[0] != null) player.eulerAngles = Vector3.zero;
        else if (dungeonRooms[0].ConnectedRooms[1] != null) player.eulerAngles = new Vector3(0, 180, 0);
        else if (dungeonRooms[0].ConnectedRooms[2] != null) player.eulerAngles = new Vector3(0, 270, 0);
        else if (dungeonRooms[0].ConnectedRooms[3] != null) player.eulerAngles = new Vector3(0, 90, 0);

        Physics.autoSyncTransforms = false;
        Debug.Log("Complete.");
    }

    private void PurgeAttempts()
    {
        for (int i = 0; i < tentativePieces.Count; i++)
        {
            Destroy(tentativePieces[i].gameObject);
        }
        tentativePieces.Clear();
        //Debug.LogWarning("Invalid Placement. Purge Performed.");
    }

    private bool CanPlaceRoom(DungeonRoom newRoomPrefab, Vector3 tentativePosition, bool readjustment = false)
    {
        if (readjustment) newRoomPrefab.gameObject.SetActive(false);

        if (Physics.CheckBox(tentativePosition, new Vector3(newRoomPrefab.RoomDimensions.x * 0.5f, 5, newRoomPrefab.RoomDimensions.y * 0.5f)))
        {
            if (readjustment) newRoomPrefab.gameObject.SetActive(true);
            DrawDebugBox(tentativePosition, Quaternion.identity, new Vector3(newRoomPrefab.RoomDimensions.x * 0.5f, 5, newRoomPrefab.RoomDimensions.y * 0.5f), Color.red);
            return false;
        }
        if (readjustment) newRoomPrefab.gameObject.SetActive(true);
        return true;
    }

    private bool CanPlaceHallway(Vector3 position)
    {
        if (Physics.CheckBox(position + Vector3.up * 2.5f, Vector3.one * 2))
        {
            DrawDebugBox(position + Vector3.up * 2.5f, Quaternion.identity, Vector3.one * 2, Color.red);
            return false; //There is something here
        }
        return true;
    }

    private Direction GetDirectionToNearest(DungeonRoom roomA, DungeonRoom roomB, Direction fromDirection)
    {
        float lowestDistance = int.MaxValue;
        int index = 0;

        for (int i = 0; i < roomB.Nodes.Length; i++)
        {
            var dist = Vector3.Distance(roomA.Nodes[(int)fromDirection].position, roomB.Nodes[i].position);
            if (dist < lowestDistance)
            {
                lowestDistance = dist;
                index = i;
            }
        }

        return (Direction)index;
    }

    //Returns a corner piece given the two directions
    private IntermediaryNode GetCorner(Direction fromDirection, Direction toDirection)
    {
        //One node is coming down, so I need a corner with its top open
        if (fromDirection == Direction.Down || toDirection == Direction.Down)
        {
            if (fromDirection == Direction.Right || toDirection == Direction.Right)
            {
                return _corners[0]; //Coming from right, so need left
            }
            else
            {
                return _corners[1]; //coming from left, so need right
            }
        }
        else //One Node is going up, so I need a corner with its bottom open
        {
            if (fromDirection == Direction.Right || toDirection == Direction.Right)
            {
                return _corners[2]; //Coming from right, so need left
            }
            else
            {
                return _corners[3]; //coming from left, so need right
            }
        }
    }

    //Adjusts the position of roomB so that hallways can be appropriately placed
    private void AdjustRoomPosition(DungeonRoom roomA, DungeonRoom roomB)
    {
        var offset = Vector3.zero;

        if (roomB.transform.position.x > roomA.transform.position.x) offset.x += 2.5f;
        else offset.x -= 2.5f;

        //room A is higher than room B
        if (roomA.transform.position.z > roomB.transform.position.z) offset.z -= 2.5f;
        else offset.z += 2.5f;
        roomB.transform.position += offset;
    }

    // Returns a minimum offset in the given direction plus a random offset in a parallel direction
    private Vector3 GetRoomOffset(DungeonRoom roomA, DungeonRoom roomB, Direction direction, bool clamp = false)
    {
        var offset = Vector3.zero;

        //Sets the minimum offset between the two rooms
        //This is based off the dimensions of the rooms and the minimum offset (15)
        switch (direction)
        {
            case Direction.Up:
                offset.z = roomA.RoomDimensions.y * 0.5f + roomB.RoomDimensions.y * 0.5f + MINIMUM_OFFSET;
                break;
            case Direction.Down:
                offset.z = -(roomA.RoomDimensions.y * 0.5f + roomB.RoomDimensions.y * 0.5f + MINIMUM_OFFSET);
                break;
            case Direction.Left:
                offset.x = -(roomA.RoomDimensions.x * 0.5f + roomB.RoomDimensions.x * 0.5f + MINIMUM_OFFSET);
                break;
            case Direction.Right:
                offset.x = roomA.RoomDimensions.x * 0.5f + roomB.RoomDimensions.x * 0.5f + MINIMUM_OFFSET;
                break;
        }

        offset += GetRandomOffset(roomA, roomB, direction, clamp);
        return SnapPosition(offset);
    }

    private Vector3 GetRandomOffset(DungeonRoom roomA, DungeonRoom roomB, Direction direction, bool clamp)
    {
        var offset = Vector3.zero;
        var offsetChance = Random.value;

        if ((int)direction <= 1) //Up or Down : Modify the X value of the offset
        {
            var offsetX = roomA.RoomDimensions.x * 0.5f + roomB.RoomDimensions.x * 0.5f + MINIMUM_OFFSET;
            if (offsetChance <= 0.15f) offset.x -= offsetX;
            else if (offsetChance <= 0.4f) offset.x -= offsetX * 0.5f;
            //20% chance of having no offset
            else if (offsetChance >= 0.85f) offset.x += offsetX;
            else if (offsetChance >= 0.6f) offset.x += offsetX * 0.5f;
        }
        else //Left/Right : Modify the Z value of the offset
        {
            if (clamp && roomA.transform.position.z >= 75) offsetChance = 0f; //clamp main line skew
            else if (clamp && roomA.transform.position.z <= -75) offsetChance = 1f;

            var offsetZ = roomA.RoomDimensions.y * 0.5f + roomB.RoomDimensions.y * 0.5f + MINIMUM_OFFSET;
            if (offsetChance <= 0.15f) offset.z -= offsetZ;
            else if (offsetChance <= 0.4f) offset.z -= offsetZ * 0.5f;
            //20% chance of having no offset
            else if (offsetChance >= 0.85f) offset.z += offsetZ;
            else if (offsetChance >= 0.6f) offset.z += offsetZ * 0.5f;
        }

        return offset;
    }

    //Returns true if the room has at least one unclaimed node
    private bool CanAddToRoom(DungeonRoom room)
    {
        //if (room == dungeonRooms[0]) return false;

        for (int i = 0; i < room.ConnectedRooms.Length; i++)
        {
            if (room.ConnectedRooms[i] == null) return true;
        }
        return false;
    }

    //Grabs a random open node from the room
    private Direction GetOpenNode(DungeonRoom room)
    {
        int dir = Random.Range(0, 4);
        if (room.ConnectedRooms[dir] == null) return (Direction)dir;

        dir = Random.Range(0, 4);
        if (room.ConnectedRooms[dir] == null) return (Direction)dir;

        for (int i = 0; i < room.ConnectedRooms.Length; i++)
        {
            if (room.ConnectedRooms[i] == null) return (Direction)i;
        }

        throw new UnityException("This is not a valid room!");
    }

    //Returns true if both positions are at an even or both at an odd position (5 or 2.5)
    private bool RoomsLineUp(Vector3 roomA, Vector3 roomB)
    {
        bool offsetA = (roomA == SnapPosition(roomA));
        bool offsetB = (roomB == SnapPosition(roomB));
        return offsetA == offsetB;
    }

    private Vector3 SnapPosition(Vector3 input, float factor = 5)
    {
        if (factor == 0) throw new UnityException("Cannot divide by 0!");

        float x = Mathf.Round(input.x / factor) * factor;
        float y = Mathf.Round(input.y / factor) * factor;
        float z = Mathf.Round(input.z / factor) * factor;

        return new Vector3(x, y, z);
    }

    private float SnapFloat(float f, float factor = 5)
    {
        if (factor == 0) throw new UnityException("Cannot divide by 0!");
        f = Mathf.Round(f / factor) * factor;
        return f;
    }

    private void DrawDebugBox(Vector3 pos, Quaternion rot, Vector3 scale, Color c, float duration = 5f)
    {
        // create matrix
        Matrix4x4 m = new Matrix4x4();
        m.SetTRS(pos, rot, scale);

        var point1 = m.MultiplyPoint(new Vector3(-0.5f, -0.5f, 0.5f));
        var point2 = m.MultiplyPoint(new Vector3(0.5f, -0.5f, 0.5f));
        var point3 = m.MultiplyPoint(new Vector3(0.5f, -0.5f, -0.5f));
        var point4 = m.MultiplyPoint(new Vector3(-0.5f, -0.5f, -0.5f));

        var point5 = m.MultiplyPoint(new Vector3(-0.5f, 0.5f, 0.5f));
        var point6 = m.MultiplyPoint(new Vector3(0.5f, 0.5f, 0.5f));
        var point7 = m.MultiplyPoint(new Vector3(0.5f, 0.5f, -0.5f));
        var point8 = m.MultiplyPoint(new Vector3(-0.5f, 0.5f, -0.5f));

        Debug.DrawLine(point1, point2, c, duration);
        Debug.DrawLine(point2, point3, c, duration);
        Debug.DrawLine(point3, point4, c, duration);
        Debug.DrawLine(point4, point1, c, duration);

        Debug.DrawLine(point5, point6, c, duration);
        Debug.DrawLine(point6, point7, c, duration);
        Debug.DrawLine(point7, point8, c, duration);
        Debug.DrawLine(point8, point5, c, duration);

        Debug.DrawLine(point1, point5, c, duration);
        Debug.DrawLine(point2, point6, c, duration);
        Debug.DrawLine(point3, point7, c, duration);
        Debug.DrawLine(point4, point8, c, duration);
    }
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

/* Graveyard
 * 
private IEnumerator SpawnRooms(int mainLineRoomsToSpawn, int bonusRooms)
    {
        Physics.autoSyncTransforms = true;
        //Create the main line of rooms
        while (mainLineRoomsToSpawn > 0)
        {
            yield return new WaitForSeconds(0.25f);

            tentativePieces.Clear();
            var roomPrefab = dungeonRoomPrefabs[Random.Range(0, dungeonRoomPrefabs.Length)];
            var previousRoom = dungeonRooms[dungeonRooms.Count - 1];

            //Get random position for the new room, clamp it to prevent the main line from skewing too much in one direction
            var newRoomPosition = previousRoom.transform.position + GetRoomOffset(previousRoom, roomPrefab, Direction.Right, true);

            if (!CanPlaceRoom(roomPrefab, newRoomPosition)) continue;

            var newRoom = Instantiate(roomPrefab, newRoomPosition, Quaternion.identity);
            tentativePieces.Add(newRoom.gameObject);

            Physics.SyncTransforms();

            Direction fromDirection, toDirection;
            GetDirectionsInLine(previousRoom, newRoom, out fromDirection, out toDirection);

            if (!CanConnectWaypoints(previousRoom, newRoom, fromDirection, toDirection))
            {
                PurgeAttempts();
                continue;
            }

            previousRoom.ConnectedRooms[(int)fromDirection] = newRoom;
            newRoom.ConnectedRooms[(int)toDirection] = previousRoom;
            dungeonRooms.Add(newRoom);
            mainLineRoomsToSpawn--;
            yield return null;
        }

        int maxAttempts = bonusRooms + 15;
        //Add on side rooms to the main line
        while (bonusRooms > 0 && maxAttempts > 0)
        {
            maxAttempts--; //just stop any infinite loops
            tentativePieces.Clear();
            yield return new WaitForSeconds(0.25f);

            var roomPrefab = dungeonRoomPrefabs[Random.Range(0, dungeonRoomPrefabs.Length)];
            //Don't add any bonus rooms to the start room
            var roomToConnectTo = dungeonRooms[Random.Range(1, dungeonRooms.Count)];
            if (!CanAddToRoom(roomToConnectTo)) continue;

            var fromDirection = GetOpenNode(roomToConnectTo);

            var newRoomPosition = roomToConnectTo.transform.position + GetRoomOffset(roomToConnectTo, roomPrefab, fromDirection);

            if (!CanPlaceRoom(roomPrefab, newRoomPosition)) continue;

            var newRoom = Instantiate(roomPrefab, newRoomPosition, Quaternion.identity);
            tentativePieces.Add(newRoom.gameObject);

            Direction toDirection = GetDirectionToNearest(roomToConnectTo, newRoom, fromDirection);


            //This is where the error is coming from. the direction that I'm initially passing is different from the one
            //That I am then getting from GetDirectionsToNearest

            if (!CanConnectWaypoints(roomToConnectTo, newRoom, fromDirection, toDirection))
            {
                PurgeAttempts();
                continue;
            }

            dungeonRooms.Add(newRoom);
            roomToConnectTo.ConnectedRooms[(int)fromDirection] = newRoom;
            newRoom.ConnectedRooms[(int)toDirection] = roomToConnectTo;
            bonusRooms--;

            yield return null;
        }

        OnDungeonComplete();
    }

    private Vector3 SnapPosition(float x, float y, float z, float factor = 5)
    {
        if (factor == 0) throw new UnityException("Cannot divide by 0!");

        x = Mathf.Round(x / factor) * factor;
        y = Mathf.Round(y / factor) * factor;
        z = Mathf.Round(z / factor) * factor;

        return new Vector3(x, y, z);
    }
    
    private bool ConnectRoomsInLine(DungeonRoom roomA, DungeonRoom roomB)
    {
        //The rooms have the same z value
        if (roomA.transform.position.z == roomB.transform.position.z) //horizontal
        {
            return CanConnectWaypoints(roomA, roomB, Direction.Right, Direction.Left);
        }
        else if (roomA.transform.position.x == roomB.transform.position.x) //vertical
        {
            return CanConnectWaypoints(roomA, roomB, Direction.Up, Direction.Down);
        }

        Direction fromDirection, toDirection;

        if (roomB.transform.position.x > roomA.transform.position.x) toDirection = Direction.Left;
        else toDirection = Direction.Right;

        //room A is higher than room B
        if (roomA.transform.position.z > roomB.transform.position.z) fromDirection = Direction.Down;
        else fromDirection = Direction.Up;

        return CanConnectWaypoints(roomA, roomB, fromDirection, toDirection);
    }

    private bool ConnectRoomsToNearest(DungeonRoom roomA, DungeonRoom roomB)
    {
        float lowestDistance = int.MaxValue;
        int indexA = 0, indexB = 0;

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

        return CanConnectWaypoints(roomA, roomB, (Direction)indexA, (Direction)indexB);
    }

    private void GetDirectionsToNearest(DungeonRoom roomA, DungeonRoom roomB, out Direction fromDirection, out Direction toDirection)
    {
        float lowestDistance = int.MaxValue;
        int indexA = 0, indexB = 0;

        for (int a = 0; a < roomA.Nodes.Length; a++)
        {
            for (int b = 0; b < roomB.Nodes.Length; b++)
            {
                var dist = Vector3.Distance(roomA.Nodes[a].position, roomB.Nodes[b].position);
                if (dist < lowestDistance)
                {
                    lowestDistance = dist;
                    indexA = a;
                    indexB = b;
                }
            }
        }

        fromDirection = (Direction)indexA;
        toDirection = (Direction)indexB;
    }

    private void GetDirectionsInLine(DungeonRoom roomA, DungeonRoom roomB, out Direction fromDirection, out Direction toDirection)
    {
        if (roomA.transform.position.z == roomB.transform.position.z) //horizontal
        {
            fromDirection = Direction.Right;
            toDirection = Direction.Left;
        }
        else if (roomA.transform.position.x == roomB.transform.position.x) //vertical
        {
            fromDirection = Direction.Up;
            toDirection = Direction.Down;
        }
        else
        {
            if (roomB.transform.position.x > roomA.transform.position.x) toDirection = Direction.Left;
            else toDirection = Direction.Right;

            //room A is higher than room B
            if (roomA.transform.position.z > roomB.transform.position.z) fromDirection = Direction.Down;
            else fromDirection = Direction.Up;
        }
    }

 */