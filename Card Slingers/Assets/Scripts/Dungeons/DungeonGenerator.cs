using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    #region - Variables -
    public bool TEST_HALF_OFFSET;

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

    [SerializeField] private int _minimumOffset = 5;

    [Space]

    [SerializeField] private List<DungeonRoom> dungeonRooms;
    [SerializeField] private List<DungeonRoom> mainLineRooms;
    private List<GameObject> tentativePieces;

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
        //Debug.LogError("You're not allowed to work on this at work. Just set up some more character models.");
        if (_generateAtStart)
        {
            if (_usePresetSize) GenerateDungeon(_dungeonSize);
            else GenerateDungeon(minMainLineRooms, maxMainLineRooms, minBonusRooms, maxBonusRooms);
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

    public void GenerateDungeon(DungeonSize dungeonSize)
    {
        dungeonRooms = new List<DungeonRoom>();
        mainLineRooms = new List<DungeonRoom>();
        tentativePieces = new List<GameObject>();

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

    private void PurgeAttempts()
    {
        for (int i = 0; i < tentativePieces.Count; i++)
        {
            Destroy(tentativePieces[i]);
        }
        tentativePieces.Clear();
        Debug.LogWarning("Purge Performed.");
    }

    private bool CanAddRoom(DungeonRoom newRoomPrefab, Vector3 tentativePosition)
    {
        var colls = Physics.OverlapBox(tentativePosition, new Vector3(newRoomPrefab.RoomDimensions.x * 0.5f, 5, newRoomPrefab.RoomDimensions.y * 0.5f));
        if (colls.Length > 0)
        {
            //Debug.Log("Cannot place room at " + tentativePosition);
            return false; //Cannot place this room here, there is another room or hallway
        }
        return true;
    }

    private IEnumerator SpawnRooms(int mainLineRoomsToSpawn, int bonusRooms)
    {
        Physics.autoSyncTransforms = true;
        //Create the main line of rooms
        while(mainLineRoomsToSpawn > 0)
        {
            yield return new WaitForSeconds(0.25f);

            tentativePieces.Clear();
            var roomPrefab = dungeonRoomPrefabs[Random.Range(0, dungeonRoomPrefabs.Length)];
            var previousRoom = dungeonRooms[dungeonRooms.Count - 1];

            var newRoomPosition = previousRoom.transform.position + GetRoomOffset(previousRoom, roomPrefab, Direction.Right);

            if (!CanAddRoom(roomPrefab, newRoomPosition)) continue;

            var newRoom = Instantiate(roomPrefab, newRoomPosition, Quaternion.identity);
            tentativePieces.Add(newRoom.gameObject);
            
            Physics.SyncTransforms();

            if (!ConnectRoomsInLine(previousRoom, newRoom))
            {
                Debug.Break();
                PurgeAttempts();
                continue;
            }

            dungeonRooms.Add(newRoom);
            mainLineRooms.Add(newRoom);
            mainLineRoomsToSpawn--;
            yield return null;
        }

        int retries = 0;
        //Add on side rooms to the main line
        while (bonusRooms > 0)
        {
            yield return new WaitForSeconds(0.25f);

            if (retries > 10) bonusRooms = 0; //just stop any infinite loops
            tentativePieces.Clear();
            var roomPrefab = dungeonRoomPrefabs[Random.Range(0, dungeonRoomPrefabs.Length)];

            //Don't add any bonus rooms to the start room
            var roomToConnectTo = mainLineRooms[Random.Range(1, mainLineRooms.Count)];

            var direction = Direction.Up; //Sure ain't pretty, but it works I think?
            if (roomToConnectTo.ConnectedRooms[(int)direction] != null) direction = Direction.Down;
            if (roomToConnectTo.ConnectedRooms[(int)direction] != null) direction = Direction.Left;
            if (roomToConnectTo.ConnectedRooms[(int)direction] != null) direction = Direction.Right;
            if (roomToConnectTo.ConnectedRooms[(int)direction] != null) continue;

            var newRoomPosition = roomToConnectTo.transform.position + GetRoomOffset(roomToConnectTo, roomPrefab, direction);
            
            if (!CanAddRoom(roomPrefab, newRoomPosition))
            {
                retries++;
                continue;
            }

            var newRoom = Instantiate(roomPrefab, newRoomPosition, Quaternion.identity);
            tentativePieces.Add(newRoom.gameObject);

            if (!ConnectRoomsToNearest(roomToConnectTo, newRoom))
            {
                Debug.Log("Cannot place hallways.");
                Debug.Break();
                PurgeAttempts();
                retries++;
                continue;
            }

            bonusRooms--;

            if (Random.value < 0.15f)
            {
                //Add a second bonus room onto the already existing bonus room
            }
            dungeonRooms.Add(newRoom);

            yield return null;
        }

        //Close off unused entrances
        for (int i = 0; i < dungeonRooms.Count; i++)
        {
            dungeonRooms[i].OnConfirmLayout();
        }

        PlacePlayerStart();
        Physics.autoSyncTransforms = false;
        Debug.Log("Complete.");
    }

    private bool ConnectRoomsInLine(DungeonRoom roomA, DungeonRoom roomB)
    {
        var offset = Vector3.zero;

        //The rooms have the same z value
        if (roomA.transform.position.z == roomB.transform.position.z) //horizontal
        {
            return ConnectWaypoints(roomA, roomB, Direction.Right, Direction.Left);
        }
        else if (roomA.transform.position.x == roomB.transform.position.x) //vertical
        {
            return ConnectWaypoints(roomA, roomB, Direction.Up, Direction.Down);
        }

        Direction fromDirection, toDirection;

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

        //roomB.transform.position += offset; //Add offset to align room properly

        return ConnectWaypoints(roomA, roomB, fromDirection, toDirection);
    }

    private bool ConnectRoomsToNearest(DungeonRoom roomA, DungeonRoom roomB)
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

        if (indexA >= 2 && indexB >= 2) mult = 2;
        else if (indexA <= 1 && indexB <= 1) mult = 2;

        if (roomB.transform.position.x > roomA.transform.position.x) offset.x += 2.5f * mult;
        else offset.x -= 2.5f * mult;

        //room A is higher than room B
        if (roomA.transform.position.z > roomB.transform.position.z) offset.z -= 2.5f * mult;
        else offset.z += 2.5f * mult;

        //Rooms are in a straight line, no need to modify their offset
        if (roomA.transform.position.x == roomB.transform.position.x) offset = Vector3.zero;
        if (roomA.transform.position.z == roomB.transform.position.z) offset = Vector3.zero;

        //roomB.transform.position += offset;

        return ConnectWaypoints(roomA, roomB, (Direction)indexA, (Direction)indexB);
    }
    
    //Create hallways and connect nodes for given directions
    //This method will return false if any of the created hallways would intersect with an existing object
    private bool ConnectWaypoints(DungeonRoom roomA, DungeonRoom roomB, Direction fromDirection, Direction toDirection)
    {
        roomA.ConnectedRooms[(int)fromDirection] = roomB;
        roomB.ConnectedRooms[(int)toDirection] = roomA;

        var pointA = roomA.RoomWaypoints[(int)fromDirection];
        var pointB = roomB.RoomWaypoints[(int)toDirection];

        if (pointA.ConnectedNode != null)
        {
            Debug.LogWarning("Point A Connected");
            return false;
        }
        else if (pointB.ConnectedNode != null)
        {
            Debug.LogWarning("Point B Already Connected");
            return false;
        }
        if (fromDirection == toDirection)
        {
            Debug.LogWarning("Cannot connect rooms from same directions.");
            return false;
        }

        var intermediaryPos = Vector3.zero;

        //Nodes are vertically aligned
        if (pointA.transform.position.x == pointB.transform.position.x)
        {
            //Should be no need to adjust the positions of the two rooms

            pointA.SetConnectedWaypoint(pointB);
            pointB.SetConnectedWaypoint(pointA);
            return CreateVerticalHallway(pointA.Point, pointB.Point);
        }
        //Nodes are horizontally aligned
        else if (pointA.transform.position.z == pointB.transform.position.z)
        {
            //Should be no need to adjust the positions of the two rooms

            pointA.SetConnectedWaypoint(pointB);
            pointB.SetConnectedWaypoint(pointA);
            return CreateHorizontalHallway(pointA.Point, pointB.Point);
        }
        //fromDirection is Up/Down, toDirection is Left/Right
        else if ((int)fromDirection <= 1 && (int)toDirection >= 2)
        {
            //Will need to adjust position of Room B
            AdjustRoomPosition(roomA, roomB);

            intermediaryPos.x = roomA.transform.position.x;
            intermediaryPos.z = roomB.transform.position.z;

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
            AdjustRoomPosition(roomA, roomB);

            intermediaryPos.x = roomB.transform.position.x;
            intermediaryPos.z = roomA.transform.position.z;

            var inter = Instantiate(GetCorner(fromDirection, toDirection), intermediaryPos, Quaternion.identity);
            inter.SetAsIntermediate(pointA, pointB);
            tentativePieces.Add(inter.gameObject);

            if (!CreateHorizontalHallway(pointA.Point, inter.PointTwo)) return false; //intermediate's point two is always left/right
            return CreateVerticalHallway(pointB.Point, inter.Point);
        }
        //The two nodes are in a staggered Up/Down
        else if ((int)fromDirection <= 1 && (int)toDirection <= 1)
        {
            //AdjustRoomPosition(roomA, roomB);
            if (roomA.transform.position.z > roomB.transform.position.z) roomB.transform.position += Vector3.back * 2.5f;
            else roomB.transform.position += Vector3.forward * 2.5f;// offset.z += 2.5f;

            float dist = (Mathf.Abs(pointA.Point.position.z - pointB.Point.position.z));
            if (dist != SnapFloat(dist))
            {
                if (fromDirection == Direction.Up) roomB.transform.position += Vector3.forward * 2.5f;
                else roomB.transform.position += Vector3.back * 2.5f;
            }

            //Base the crossBarZValue on the location of pointA.Point
            int numHalls = Mathf.FloorToInt((Mathf.Abs(pointA.Point.position.z - pointB.Point.position.z) * 0.2f)) + 1;

            float crossBarZValue = pointA.Point.position.z; //Place initial position to be even with the start point for pointA
            crossBarZValue +=  (pointA.Point.localPosition.z * 2) * Mathf.RoundToInt(numHalls * 0.5f);
            
            Vector3 firstMidPos = new Vector3(pointA.Point.position.x, 0, crossBarZValue);
            Vector3 secondMidPos = new Vector3(pointB.Point.position.x, 0, crossBarZValue);

            IntermediaryNode firstCorner;
            IntermediaryNode secondCorner;
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
            //Will need to adjust position of Room B
            if (roomB.transform.position.x > roomA.transform.position.x) roomB.transform.position += Vector3.right * 2.5f;
            else roomB.transform.position += Vector3.left * 2.5f;

            float dist = (Mathf.Abs(pointA.Point.position.x - pointB.Point.position.x));
            if (dist != SnapFloat(dist))
            {
                if (fromDirection == Direction.Left) roomB.transform.position += Vector3.left * 2.5f;
                else roomB.transform.position += Vector3.right * 2.5f;
            }

            //Base the crossBarXValue on the location of pointA.Point
            int numHalls = Mathf.FloorToInt((Mathf.Abs(pointA.Point.position.x - pointB.Point.position.x) * 0.2f)) + 1;

            float crossBarXValue = pointA.Point.position.x; //Place initial position to be even with the start point for pointA
            crossBarXValue += (pointA.Point.localPosition.x * 2) * Mathf.RoundToInt(numHalls * 0.5f);

            Vector3 firstMidPos = new Vector3(crossBarXValue, 0, pointA.Point.position.z); //keep z value but snap x
            Vector3 secondMidPos = new Vector3(crossBarXValue, 0, pointB.Point.position.z);

            IntermediaryNode firstCorner;
            IntermediaryNode secondCorner;
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
        else
        {
            Debug.LogWarning("I don't know what I missed.");
            return false;
        }
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

            if (Physics.CheckBox(pos + Vector3.up * 2.5f, Vector3.one))
            {
                Debug.Log(i + "(crossbar: " + isCrossbar + ")" + " Intersected at " + pos);
                DrawBox(pos + Vector3.up * 2.5f, Quaternion.identity, Vector3.one, Color.red);
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

            if (Physics.CheckBox(pos + Vector3.up * 2.5f, Vector3.one))
            {
                Debug.Log(i + "(crossbar: " + isCrossbar + ")" + " Intersected at " + pos);
                DrawBox(pos + Vector3.up * 2.5f, Quaternion.identity, Vector3.one, Color.red);
                return false; //There is something here
            }

            GameObject hall = Instantiate(_hallwayHorizontal, pos, Quaternion.identity) as GameObject;
            hall.gameObject.transform.SetParent(fromPoint);
            tentativePieces.Add(hall.gameObject);
        }
        return true;
    }

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

    private void AdjustRoomPosition(DungeonRoom roomA, DungeonRoom roomB)
    {
        var offset = Vector3.zero;
        //Will need to adjust position of Room B
        if (roomB.transform.position.x > roomA.transform.position.x) offset.x += 2.5f;
        else offset.x -= 2.5f;

        //room A is higher than room B
        if (roomA.transform.position.z > roomB.transform.position.z) offset.z -= 2.5f;
        else offset.z += 2.5f;
        roomB.transform.position += offset;
    }

    /// <summary>
    /// Returns a minimum offset in the given direction plus a random offset in a parallel direction
    /// </summary>
    private Vector3 GetRoomOffset(DungeonRoom roomA, DungeonRoom roomB, Direction direction)
    {
        var offset = Vector3.zero;

        //Sets the minimum offset between the two rooms
        switch (direction)
        {
            case Direction.Up:
                offset.z = roomA.RoomDimensions.y * 0.5f + roomB.RoomDimensions.y * 0.5f + _minimumOffset;
                break;
            case Direction.Down:
                offset.z = -(roomA.RoomDimensions.y * 0.5f + roomB.RoomDimensions.y * 0.5f + _minimumOffset);
                break;
            case Direction.Left:
                offset.x = -(roomA.RoomDimensions.x * 0.5f + roomB.RoomDimensions.x * 0.5f + _minimumOffset);
                break;
            case Direction.Right:
                offset.x = roomA.RoomDimensions.x * 0.5f + roomB.RoomDimensions.x * 0.5f + _minimumOffset;
                break;
        }

        offset += GetRandomOffset(roomA, roomB, direction);
        return SnapPosition(offset);
    }

    private Vector3 GetRandomOffset(DungeonRoom roomA, DungeonRoom roomB, Direction direction)
    {
        var offset = Vector3.zero;
        var offsetChance = Random.value;

        if ((int)direction <= 1) //Up or Down : Modify the X value of the offset
        {
            var offsetX = roomA.RoomDimensions.x * 0.5f + roomB.RoomDimensions.x * 0.5f + _minimumOffset;
            if (offsetChance <= 0.2f) offset.x -= offsetX;
            else if (offsetChance <= 0.4f) offset.x -= offsetX * 0.5f;
            //20% chance of having no offset
            else if (offsetChance >= 0.8f) offset.x += offsetX;
            else if (offsetChance >= 0.6f) offset.x += offsetX * 0.5f;
        }
        else //Left/Right : Modify the Z value of the offset
        {
            var offsetZ = roomA.RoomDimensions.y * 0.5f + roomB.RoomDimensions.y * 0.5f + _minimumOffset;
            if (offsetChance <= 0.2f) offset.z -= offsetZ;
            else if (offsetChance <= 0.4f) offset.z -= offsetZ * 0.5f;
            //20% chance of having no offset
            else if (offsetChance >= 0.8f) offset.z += offsetZ;
            else if (offsetChance >= 0.6f) offset.z += offsetZ * 0.5f;
        }

        return offset;
    }

    private bool CanAddToRoom(DungeonRoom room)
    {
        if (room == dungeonRooms[0]) return false;

        for (int i = 0; i < room.ConnectedRooms.Length; i++)
        {
            if (room.ConnectedRooms[i] == null) return true;
        }
        return false;
    }
    
    //Return an int for Direction enum, use -1 for "null" if all are full
    private Direction GetOpenNode(DungeonRoom room)
    {
        int d = Random.Range(0, 4);
        if (room.ConnectedRooms[d] == null) return (Direction)d;

        d = Random.Range(0, 4);
        if (room.ConnectedRooms[d] == null) return (Direction)d;

        for (int i = 0; i < room.ConnectedRooms.Length; i++)
        {
            if (room.ConnectedRooms[i] == null) return (Direction)i;
        }

        throw new UnityException("This is not a valid room!");
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

    private float SnapFloat(float f, float factor = 5)
    {
        if (factor == 0) throw new UnityException("Cannot divide by 0!");
        f = Mathf.Round(f / factor) * factor;
        return f;
    }


    private void DrawBox(Vector3 pos, Quaternion rot, Vector3 scale, Color c)
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

        Debug.DrawLine(point1, point2, c);
        Debug.DrawLine(point2, point3, c);
        Debug.DrawLine(point3, point4, c);
        Debug.DrawLine(point4, point1, c);

        Debug.DrawLine(point5, point6, c);
        Debug.DrawLine(point6, point7, c);
        Debug.DrawLine(point7, point8, c);
        Debug.DrawLine(point8, point5, c);

        Debug.DrawLine(point1, point5, c);
        Debug.DrawLine(point2, point6, c);
        Debug.DrawLine(point3, point7, c);
        Debug.DrawLine(point4, point8, c);
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