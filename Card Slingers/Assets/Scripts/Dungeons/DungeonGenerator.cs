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

    private bool TryAddRoom(DungeonRoom oldRoom, DungeonRoom newRoomPrefab, Vector3 tentativePosition, out Direction fromDirection, out Direction toDirection, bool inLine = false)
    {
        
        fromDirection = Direction.Up;
        toDirection = Direction.Up;

        var colls = Physics.OverlapBox(tentativePosition, new Vector3(newRoomPrefab.RoomDimensions.x * 0.5f, 5, newRoomPrefab.RoomDimensions.y * 0.5f));
        if (colls.Length > 0) return false; //Cannot place this room here, there is another room or hallway

        var newRoom = Instantiate(newRoomPrefab, tentativePosition, Quaternion.identity);
        tentativePieces.Add(newRoom.gameObject);

        bool success;
        if (inLine) success = ConnectRoomsInLine(oldRoom, newRoomPrefab);
        else success = ConnectRoomsToNearest(oldRoom, newRoomPrefab);

        if (!success)
        {
            for (int i = 0; i < tentativePieces.Count; i++)
            {
                Destroy(tentativePieces[i]);
            }

            tentativePieces.Clear();
            return false;
        }
        //Now try to find out where the hallways would go
        //Either ConnectInLine or ConnectToNearest, don't bother with the offset here
        //This will just set what sides they are connected on

        //Once the sides have been decided, then try to create the hallways, 


        //Adjust the new room's position accordingly
        //If the hallways would overlap, just return false. I could probably try another connection method, but... meh

        dungeonRooms.Add(newRoom);
        mainLineRooms.Add(newRoom);

        return true;
    }

    private void PurgeAttempts()
    {
        for (int i = 0; i < tentativePieces.Count; i++)
        {
            Destroy(tentativePieces[i]);
        }
        tentativePieces.Clear();
    }

    private bool CanAddRoom(DungeonRoom newRoomPrefab, Vector3 tentativePosition)
    {
        var colls = Physics.OverlapBox(tentativePosition, new Vector3(newRoomPrefab.RoomDimensions.x * 0.5f, 5, newRoomPrefab.RoomDimensions.y * 0.5f));
        if (colls.Length > 0)
        {
            Debug.Log("Cannot place room at " + tentativePosition);
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
            yield return new WaitForSeconds(0.5f);

            tentativePieces.Clear();
            var roomPrefab = dungeonRoomPrefabs[Random.Range(0, dungeonRoomPrefabs.Length)];
            var previousRoom = dungeonRooms[dungeonRooms.Count - 1];

            var newRoomPosition = previousRoom.transform.position + GetRoomOffset(previousRoom, roomPrefab, Direction.Right);

            if (!CanAddRoom(roomPrefab, newRoomPosition))
            {
                Debug.Break();
                continue;
            }
            Debug.Log("Can place room.");

            var newRoom = Instantiate(roomPrefab, newRoomPosition, Quaternion.identity);
            dungeonRooms.Add(newRoom);
            mainLineRooms.Add(newRoom);
            tentativePieces.Add(newRoom.gameObject);
            
            Physics.SyncTransforms();

            if (!ConnectRoomsInLine(previousRoom, newRoom))
            {
                Debug.Break();
                //PurgeAttempts();
                continue;
            }

            mainLineRoomsToSpawn--;
            yield return null;
        }

        int retries = 0;
        //Add on side rooms to the main line
        while (bonusRooms > 0)
        {
            yield return new WaitForSeconds(0.5f);

            Debug.LogWarning("Everything seems to work okay until I get to this point.");
            if (retries > 10) yield break; //just stop any infinite loops
            tentativePieces.Clear();
            var roomPrefab = dungeonRoomPrefabs[Random.Range(0, dungeonRoomPrefabs.Length)];

            //Don't add any bonus rooms to the start room
            var roomToConnectTo = mainLineRooms[Random.Range(1, mainLineRooms.Count)];

            var direction = Direction.Up;

            //Only going Up/Down for right now
            if (roomToConnectTo.ConnectedRooms[(int)direction] != null) direction = Direction.Down;
            if (roomToConnectTo.ConnectedRooms[(int)direction] != null) continue;

            var newRoomPosition = roomToConnectTo.transform.position + GetRoomOffset(roomToConnectTo, roomPrefab, direction);
            
            if (!CanAddRoom(roomPrefab, newRoomPosition))
            {
                Debug.Log("Cannot place room at " + newRoomPosition);
                Debug.Break();
                retries++;
                continue;
            }

            var newRoom = Instantiate(roomPrefab, newRoomPosition, Quaternion.identity);
            dungeonRooms.Add(newRoom);
            tentativePieces.Add(newRoom.gameObject);

            if (!ConnectRoomsToNearest(roomToConnectTo, newRoom))
            {
                Debug.Log("Cannot place hallways.");
                Debug.Break();
                //PurgeAttempts();
                retries++;
                continue;
            }

            bonusRooms--;

            if (Random.value < 0.15f)
            {
                //Add a second bonus room onto the already existing bonus room
            }

            yield return null;
        }

        //Close off unused entrances
        for (int i = 0; i < dungeonRooms.Count; i++)
        {
            dungeonRooms[i].OnConfirmLayout();
        }

        PlacePlayerStart();
        Debug.LogWarning("Complete.");
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

        roomB.transform.position += offset; //Add offset to align room properly

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

        //if (indexA >= 2 && indexB >= 2) mult = 2;
        //else if (indexA <= 1 && indexB <= 1) mult = 2;

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
        if (fromDirection == toDirection) return false;

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
            //main room going down to a side passage, push it away a little
            if (toDirection == Direction.Up) 
            {
                //roomB.transform.position += new Vector3(0, 0, -2.5f);
            }
            float crossBarZValue = SnapFloat((pointA.transform.position.z + pointB.transform.position.z) * 0.5f); //Find middle point
            //So if I don't snap it, then the crossbar doesn't line up with anything
            //If I do snap it, then it doesn't align with Snapped rooms, but does align with non-snapped (2.5f) rooms
            
            Vector3 firstMidPos = new Vector3(pointA.Point.position.x, 0, crossBarZValue); //keep x value but snap the z
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

            //Need vertical from A to first intermediate
            if (!CreateVerticalHallway(pointA.Point, firstIntermediate.Point)) return false;
            //Need second vertical from B to second intermediate
            if (!CreateVerticalHallway(pointB.Point, secondIntermediate.Point)) return false;
            //Need horizontal between two intermediates. intermediate's point two is always left/right
            return CreateHorizontalHallway(firstIntermediate.PointTwo, secondIntermediate.PointTwo, true);
        }
        //The two nodes are in a staggered Left/Right
        else if ((int)fromDirection >= 2 && (int)toDirection >= 2)
        {
            Debug.LogWarning("This should not happen yet.");
            float crossBarXValue = SnapFloat((pointA.transform.position.z + pointB.transform.position.z) * 0.5f); //Find middle point
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
            if (!CreateHorizontalHallway(pointA.Point, firstIntermediate.PointTwo)) return false;
            //Need second horizontal from B to second intermediate
            if (!CreateHorizontalHallway(pointB.Point, secondIntermediate.PointTwo)) return false;
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
    private bool CreateVerticalHallway(Transform fromPoint, Transform toPoint, bool isCrossbar = false)
    {
        Physics.SyncTransforms();

        int numToSpawn = Mathf.FloorToInt((Mathf.Abs(fromPoint.position.z - toPoint.position.z) * 0.2f)) + 1;
        if (isCrossbar) numToSpawn--; //crossbar Points are slightly closer
        if (numToSpawn == 0)
        {
            Debug.Log("No rooms to spawn. " + fromPoint.position + ", " + toPoint.position);
            return true; //This should only happen with narrow Z hallways
        }

        for (int i = 0; i < numToSpawn; i++)
        {
            var pos = fromPoint.position + (fromPoint.localPosition * i * 2);
            if (isCrossbar) pos += fromPoint.localPosition;

            var coll = Physics.OverlapBox(pos, Vector3.one * 0.5f);
            if (coll.Length > 0)
            {
                for (int c = 0; c < coll.Length; c++)
                {
                    Debug.Log(i + " Intersected " + coll[c].name + " located at " + coll[c].gameObject.transform.position + " at " + pos);
                    DrawBox(pos + Vector3.up * 2.5f, Quaternion.identity, Vector3.one, Color.red);
                }
            }

            if (Physics.CheckBox(pos + Vector3.up * 2.5f, Vector3.one)) return false; //There is something here

            GameObject hall = Instantiate(_hallwayVertical, pos, Quaternion.identity);
            hall.gameObject.transform.SetParent(fromPoint);
            tentativePieces.Add(hall.gameObject);
        }
        return true;
    }

    //Creates the hallway objects, returns false if they would intersect with an existing object
    private bool CreateHorizontalHallway(Transform fromPoint, Transform toPoint, bool isCrossbar = false)
    {
        Physics.SyncTransforms();

        int numToSpawn = Mathf.FloorToInt((Mathf.Abs(fromPoint.position.x - toPoint.position.x) * 0.2f)) + 1;
        if (isCrossbar) numToSpawn--; //crossbar Points are slightly closer
        if (numToSpawn == 0)
        {
            Debug.Log("No rooms to spawn. " + fromPoint.position + ", " + toPoint.position);
            return true; //This should only happen with narrow Z hallways
        }

        for (int i = 0; i < numToSpawn; i++)
        {
            var pos = fromPoint.position + (fromPoint.localPosition * i * 2);
            if (isCrossbar) pos += fromPoint.localPosition;

            var coll = Physics.OverlapBox(pos + Vector3.up * 2.5f, Vector3.one * 0.5f);
            if (coll.Length > 0)
            {
                for (int c = 0; c < coll.Length; c++)
                {
                    Debug.Log(i + " Intersected " + coll[c].name + " located at " + coll[c].gameObject.transform.position + " at " + pos);
                    DrawBox(pos + Vector3.up * 2.5f, Quaternion.identity, Vector3.one, Color.red);
                }
            }

            if (Physics.CheckBox(pos + Vector3.up * 2.5f, Vector3.one)) return false; //There is something here

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

    //Return an int for Direction enum, use -1 for "null" if all are full
    private int GetOpenNode(DungeonRoom room)
    {
        for (int i = 0; i < room.ConnectedRooms.Length; i++)
        {
            if (room.ConnectedRooms[i] == null) return i;
        }
        return -1;
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