using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    #region - Fields -
    private const int MINIMUM_OFFSET = 15; //Anything less will cause issues with hallways

    [Space]

    [SerializeField] private CombatGenerator combatGenerator;
    [SerializeField] private ObstacleGenerator obstacleGenerator;
    [SerializeField] private MiniMapController miniMap;

    [Space]

    [Space]

    [SerializeField] private List<DungeonRoom> dungeonRooms;
    private List<IntermediaryNode> _corners = new List<IntermediaryNode>();
    private List<GameObject> tentativePieces;

    private DungeonPresets _preset;
    private DungeonFeatures _currentDungeonFeature;

    private static DungeonFeatures[] _dungeonFeatures;

    //EXTRA_SMALL_DUNGEON
    private static DungeonFeatures SMALL_DUNGEON;
    private static DungeonFeatures MEDIUM_DUNGEON;
    private static DungeonFeatures LARGE_DUNGEON;
    //EXTRA_LARGE_DUNGEON
    #endregion

    private void Awake()
    {
        _dungeonFeatures = new DungeonFeatures[System.Enum.GetNames(typeof(DungeonSize)).Length];

        SMALL_DUNGEON = new DungeonFeatures(4, 7, 3, 5);
        MEDIUM_DUNGEON = new DungeonFeatures(7, 12, 6, 10);
        LARGE_DUNGEON = new DungeonFeatures(10, 16, 10, 15);

        _dungeonFeatures[0] = SMALL_DUNGEON;
        _dungeonFeatures[1] = MEDIUM_DUNGEON;
        _dungeonFeatures[2] = LARGE_DUNGEON;
    }

    public void BeginGeneration(DungeonPresets dungeonPreset, DungeonSize size)
    {
        _preset = dungeonPreset;
        dungeonRooms = new List<DungeonRoom>();
        tentativePieces = new List<GameObject>();

        _currentDungeonFeature = _dungeonFeatures[(int)size];

        int rooms = Random.Range(_currentDungeonFeature.minRooms, _currentDungeonFeature.maxRooms + 1);
        int combats = Random.Range(_currentDungeonFeature.minCombats, _currentDungeonFeature.maxCombats + 1);

        var startRoom = Instantiate(_preset.StartRoomPrefab, Vector3.zero, Quaternion.identity, gameObject.transform);
        dungeonRooms.Add(startRoom);

        //Will have to switch out the references for all of the ones in the preset
        StartCoroutine(GenerateDungeon(rooms, combats));
    }
    
    private IEnumerator GenerateDungeon(int rooms, int combats)
    {
        yield return StartCoroutine(SpawnRooms(rooms));

        FindDungeonBounds();

        obstacleGenerator.GenerateObstacles(_preset.Obstacles, dungeonRooms);

        yield return StartCoroutine(combatGenerator.PlaceCombats(_preset.Encounters, dungeonRooms.ToArray(), combats)); 

        OnDungeonComplete();
    }

    private IEnumerator SpawnRooms(int roomsToSpawn)
    {
        while (roomsToSpawn > 0)
        {
            tentativePieces.Clear();
            //yield return new WaitForSeconds(0.25f);

            if (_preset == null) Debug.Log("Preset is null.");
            var roomPrefab = _preset.RoomPrefabs[Random.Range(0, _preset.RoomPrefabs.Length)];
            //var roomPrefab = dungeonRoomPrefabs[Random.Range(0, dungeonRoomPrefabs.Length)];

            DungeonRoom fromRoom = dungeonRooms[0];
            if (dungeonRooms.Count > 1) fromRoom = GetOpenRoom();

            var fromDirection = GetOpenNode(fromRoom);
            var newRoomPosition = fromRoom.Transform.position + GetRoomOffset(fromRoom, roomPrefab, fromDirection);

            if (!CanPlaceRoom(roomPrefab, newRoomPosition)) continue;

            var newRoom = Instantiate(roomPrefab, newRoomPosition, Quaternion.identity, gameObject.transform);
            tentativePieces.Add(newRoom.gameObject);

            Direction toDirection = GetDirectionToNearest(fromRoom, newRoom, fromDirection);

            if (!ConnectWaypoints(fromRoom.Nodes[(int)fromDirection], newRoom.Nodes[(int)toDirection]))
            {
                PurgeAttempts();
                continue;
            }

            dungeonRooms.Add(newRoom);

            fromRoom.ConnectedRooms[(int)fromDirection] = newRoom;
            newRoom.ConnectedRooms[(int)toDirection] = fromRoom;

            roomsToSpawn--;



            yield return null;
        }

        TryConnectLoops();

        for (int i = 0; i < dungeonRooms.Count; i++)
        {
            dungeonRooms[i].CloseEmptyEntrances();
        }
    }

    //connect waypoints and build out hallways
    private bool ConnectWaypoints(PathNode fromNode, PathNode toNode, bool canAdjust = false) //was true. should never be true
    {
        if (fromNode.direction == toNode.direction) return false;

        if (fromNode.ConnectedNode != null)
        {
            Debug.LogWarning("Point A at " + fromNode.transform.position + " is already connected to node at " + fromNode.ConnectedNode.transform.position + " " +
                "Was trying to connect to node at " + toNode.transform.position);
            return false;
        }
        else if (toNode.ConnectedNode != null)
        {
            Debug.LogWarning("Point B at " + toNode.transform.position + " is already connected to node at " + toNode.ConnectedNode.transform.position + " " +
                "Was trying to connect to node at " + fromNode.transform.position);
            return false;
        }

        var intermediaryPos = Vector3.zero;
        //Nodes are vertically aligned
        if (fromNode.Transform.position.x == toNode.Transform.position.x)
        {
            if (CreateHallway(fromNode.Point, toNode.Point, _preset.Hallway_Vert))
            {
                fromNode.ConnectedNode = toNode;
                toNode.ConnectedNode = fromNode;
                return true;
            }
            return false;
        }
        //Nodes are horizontally aligned
        else if (fromNode.Transform.position.z == toNode.Transform.position.z)
        {
            if (CreateHallway(fromNode.Point, toNode.Point, _preset.Hallway_Horz))
            {
                fromNode.ConnectedNode = toNode;
                toNode.ConnectedNode = fromNode;
                return true;
            }
            return false;
        }
        //fromDirection is Up/Down, toDirection is Left/Right
        else if ((int)fromNode.direction <= 1 && (int)toNode.direction >= 2)
        {
            //Will need to adjust position of Room B
            if (!CanPlaceRoom(toNode.Room, toNode.Room.Transform.position, true)) return false;

            intermediaryPos.x = fromNode.Point.position.x;
            intermediaryPos.z = toNode.Point.position.z;

            if (!CanPlaceHallway(intermediaryPos)) return false; //There is something here

            var inter = Instantiate(GetCorner(fromNode.direction, toNode.direction), intermediaryPos, Quaternion.identity, gameObject.transform);
            tentativePieces.Add(inter.gameObject);

            if (!CreateHallway(fromNode.Point, inter.Point, _preset.Hallway_Vert)) return false;
            if (!CreateHallway(toNode.Point, inter.PointTwo, _preset.Hallway_Horz)) return false;
            _corners.Add(inter);
            inter.SetAsIntermediate(fromNode, toNode);
            return true;
        }
        //fromDirection is Left/Right, toDirection is Up/Down
        else if ((int)fromNode.direction >= 2 && (int)toNode.direction <= 1)
        {
            //Will need to adjust position of Room B
            if (!CanPlaceRoom(toNode.Room, toNode.Room.Transform.position, true)) return false;

            intermediaryPos.x = toNode.Point.position.x;
            intermediaryPos.z = fromNode.Point.position.z;

            if (!CanPlaceHallway(intermediaryPos)) return false; //There is something here

            var inter = Instantiate(GetCorner(fromNode.direction, toNode.direction), intermediaryPos, Quaternion.identity, gameObject.transform);
            tentativePieces.Add(inter.gameObject);

            if (!CreateHallway(fromNode.Point, inter.PointTwo, _preset.Hallway_Horz)) return false; //intermediate's point two is always left/right
            if (!CreateHallway(toNode.Point, inter.Point, _preset.Hallway_Vert)) return false;
            _corners.Add(inter);
            inter.SetAsIntermediate(fromNode, toNode);
            return true;
        }
        //The two nodes are in a staggered Up/Down
        else if ((int)fromNode.direction <= 1 && (int)toNode.direction <= 1)
        {
            //Base the crossBarZValue on the location of pointA.Point
            int numHalls = Mathf.FloorToInt((Mathf.Abs(fromNode.Point.position.z - toNode.Point.position.z) * 0.2f)) + 1;
            float crossBarZValue = fromNode.Point.position.z; //Place initial position to be even with the start point for pointA
            crossBarZValue += (fromNode.Point.localPosition.z * 2) * Mathf.RoundToInt(numHalls * 0.5f);

            Vector3 firstMidPos = new Vector3(fromNode.Point.position.x, 0, crossBarZValue);
            Vector3 secondMidPos = new Vector3(toNode.Point.position.x, 0, crossBarZValue);
            if (!CanPlaceHallway(firstMidPos)) return false; //There is something here
            if (!CanPlaceHallway(secondMidPos)) return false; //There is something here

            IntermediaryNode firstCorner, secondCorner;
            if (firstMidPos.x < secondMidPos.x)
            {
                firstCorner = GetCorner(fromNode.direction, Direction.Left);
                secondCorner = GetCorner(Direction.Right, toNode.direction);
            }
            else
            {
                firstCorner = GetCorner(fromNode.direction, Direction.Right);
                secondCorner = GetCorner(Direction.Left, toNode.direction);
            }

            var firstIntermediate = Instantiate(firstCorner, firstMidPos, Quaternion.identity, gameObject.transform);
            var secondIntermediate = Instantiate(secondCorner, secondMidPos, Quaternion.identity, gameObject.transform);
            tentativePieces.Add(firstIntermediate.gameObject);
            tentativePieces.Add(secondIntermediate.gameObject);

            //Need vertical from A to first intermediate, but only if hallway length is at least 1
            if (firstIntermediate.transform.position != fromNode.Point.position)
            {
                if (!CreateHallway(fromNode.Point, firstIntermediate.Point, _preset.Hallway_Vert)) return false;
            }
            //Need second vertical from B to second intermediate, but only if hallway length is at least 1
            if (secondIntermediate.transform.position != toNode.Point.position)
            {
                if (!CreateHallway(toNode.Point, secondIntermediate.Point, _preset.Hallway_Vert)) return false;
            }
            if (!CreateHallway(firstIntermediate.PointTwo, secondIntermediate.PointTwo, _preset.Hallway_Horz, true)) return false;

            _corners.Add(firstIntermediate);
            _corners.Add(secondIntermediate);
            firstIntermediate.SetAsIntermediate(secondIntermediate, fromNode);
            secondIntermediate.SetAsIntermediate(firstIntermediate, toNode);
            return true;
        }
        //The two nodes are in a staggered Left/Right
        else if ((int)fromNode.direction >= 2 && (int)toNode.direction >= 2)
        {
            //Base the crossBarXValue on the location of pointA.Point
            int numHalls = Mathf.FloorToInt((Mathf.Abs(fromNode.Point.position.x - toNode.Point.position.x) * 0.2f)) + 1;
            float crossBarXValue = fromNode.Point.position.x; //Place initial position to be even with the start point for pointA
            crossBarXValue += (fromNode.Point.localPosition.x * 2) * Mathf.RoundToInt(numHalls * 0.5f);

            Vector3 firstMidPos = new Vector3(crossBarXValue, 0, fromNode.Point.position.z); //keep z value but snap x
            Vector3 secondMidPos = new Vector3(crossBarXValue, 0, toNode.Point.position.z);
            if (!CanPlaceHallway(firstMidPos)) return false; //There is something here
            if (!CanPlaceHallway(secondMidPos)) return false; //There is something here

            IntermediaryNode firstCorner, secondCorner;
            if (firstMidPos.z < secondMidPos.z)
            {
                firstCorner = GetCorner(fromNode.direction, Direction.Down);
                secondCorner = GetCorner(Direction.Up, toNode.direction);
            }
            else
            {
                firstCorner = GetCorner(fromNode.direction, Direction.Up);
                secondCorner = GetCorner(Direction.Down, toNode.direction);
            }

            var firstIntermediate = Instantiate(firstCorner, firstMidPos, Quaternion.identity, gameObject.transform);
            var secondIntermediate = Instantiate(secondCorner, secondMidPos, Quaternion.identity, gameObject.transform);
            tentativePieces.Add(firstIntermediate.gameObject);
            tentativePieces.Add(secondIntermediate.gameObject);

            //Need horizontal from A to first intermediate
            if (firstIntermediate.transform.position != fromNode.Point.position)
            {
                if (!CreateHallway(fromNode.Point, firstIntermediate.PointTwo, _preset.Hallway_Horz)) return false;
            }
            //Need second horizontal from B to second intermediate
            if (secondIntermediate.transform.position != toNode.Point.position)
            {
                if (!CreateHallway(toNode.Point, secondIntermediate.PointTwo, _preset.Hallway_Horz)) return false;
            }
            if (!CreateHallway(firstIntermediate.Point, secondIntermediate.Point, _preset.Hallway_Vert, true)) return false;

            _corners.Add(firstIntermediate);
            _corners.Add(secondIntermediate);
            firstIntermediate.SetAsIntermediate(secondIntermediate, fromNode);
            secondIntermediate.SetAsIntermediate(firstIntermediate, toNode);
            return true;
        }

        return false;
    }

    private bool CreateHallway(Transform fromPoint, Transform toPoint, GameObject prefab, bool isCrossbar = false)
    {
        Physics.SyncTransforms();

        int numToSpawn = Mathf.FloorToInt((Mathf.Abs(fromPoint.position.z - toPoint.position.z) * 0.2f));
        if (prefab == _preset.Hallway_Horz) numToSpawn = Mathf.FloorToInt((Mathf.Abs(fromPoint.position.x - toPoint.position.x) * 0.2f));
        if (!isCrossbar) numToSpawn++; //crossbar Points are slightly closer

        for (int i = 0; i < numToSpawn; i++)
        {
            var pos = fromPoint.position + (fromPoint.localPosition * i * 2);
            if (isCrossbar) pos += fromPoint.localPosition;

            if (!CanPlaceHallway(pos)) return false; //There is something here

            GameObject hall = Instantiate(prefab, pos, Quaternion.identity, fromPoint);
            tentativePieces.Add(hall.gameObject);
        }
        return true;
    }

    private void TryConnectLoops()
    {
        //Debug.Log("Starting Loop Check.");
        foreach (DungeonRoom room in dungeonRooms)
        {
            if (room == dungeonRooms[0]) continue;

            for (int i = 0; i < room.ConnectedRooms.Length; i++)
            {
                if (room.Nodes[i].ConnectedNode == null)
                {
                    CheckForNearbyNode(room.Nodes[i]);
                }
            }
        }
    }

    private void CheckForNearbyNode(PathNode node)
    {
        if (node.ConnectedNode != null)
        {
            Debug.LogWarning("Still passing a connected node!");
            return; //Don't know why I need this, but I guess I do.
        }

        var otherNodes = Physics.OverlapBox(node.Point.position, Vector3.one * 30);

        for (int i = 0; i < otherNodes.Length; i++)
        {
            tentativePieces.Clear();

            var newNode = otherNodes[i].GetComponent<PathNode>();
            if (newNode == null || newNode.ConnectedNode != null) continue; //is null or taken
            if (newNode is IntermediaryNode) continue; //Cannot connect to an intermediary. Checking its room throws an error
            if (newNode.Room == node.Room || newNode.Room == dungeonRooms[0]) continue; //same room or start 
            //if (!NodesLineUp(newNode.Point.position, node.Point.position)) continue; //points need to be on same offset

            if (ConnectWaypoints(node, newNode))
            {
                Debug.DrawLine(node.Point.position, newNode.Point.position, Color.green, int.MaxValue);
                node.Room.ConnectedRooms[(int)node.direction] = newNode.Room;
                newNode.Room.ConnectedRooms[(int)newNode.direction] = node.Room;
                //Debug.Log("Loop Created!");
            }
            else
            {
                Debug.DrawLine(node.Point.position, newNode.Point.position, Color.red, int.MaxValue);
                PurgeAttempts();
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
        for (int i = 0; i < _corners.Count; i++)
        {
            //_corners[i].OnComplete();
        }

        miniMap.SetBounds(dungeonRooms.ToArray());
        
        DungeonManager.instance.DungeonIsReady = true;        
    }

    private void PurgeAttempts()
    {
        for (int i = 0; i < tentativePieces.Count; i++)
        {
            Destroy(tentativePieces[i].gameObject);
        }
        tentativePieces.Clear();
    }

    private bool CanPlaceRoom(DungeonRoom newRoomPrefab, Vector3 tentativePosition, bool readjustment = false)
    {
        if (readjustment) newRoomPrefab.gameObject.SetActive(false);

        if (Physics.CheckBox(tentativePosition, new Vector3(newRoomPrefab.RoomDimensions.x * 0.5f, 5, newRoomPrefab.RoomDimensions.y * 0.5f)))
        {
            if (readjustment) newRoomPrefab.gameObject.SetActive(true);
            DrawDebugBox(tentativePosition + Vector3.up * 2.5f, Quaternion.identity, new Vector3(newRoomPrefab.RoomDimensions.x, 5, newRoomPrefab.RoomDimensions.y), Color.red);
            return false;
        }
        if (readjustment) newRoomPrefab.gameObject.SetActive(true);
        return true;
    }

    private bool CanPlaceHallway(Vector3 position)
    {
        if (Physics.CheckBox(position + Vector3.up * 2.5f, Vector3.one * 2f))
        {
            DrawDebugBox(position + Vector3.up * 2.5f, Quaternion.identity, Vector3.one * 5f, Color.red);
            return false; //There is something here
        }
        return true;
    }

    private Direction GetDirectionToNearest(DungeonRoom roomA, DungeonRoom roomB, Direction fromDirection)
    {
        float lowestDistance = int.MaxValue;
        int index = 0;
        var nodePos = roomA.Nodes[(int)fromDirection].Point.position;
        for (int i = 0; i < roomB.Nodes.Length; i++)
        {
            var dist = Vector3.Distance(nodePos, roomB.Nodes[i].Point.position);
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
                return _preset.Corners[0];
                //return _corners[0]; //Coming from right, so need left
            }
            else
            {
                return _preset.Corners[1];
                //return _corners[1]; //coming from left, so need right
            }
        }
        else //One Node is going up, so I need a corner with its bottom open
        {
            if (fromDirection == Direction.Right || toDirection == Direction.Right)
            {
                return _preset.Corners[2];
                //return _corners[2]; //Coming from right, so need left
            }
            else
            {
                return _preset.Corners[3];
                //return _corners[3]; //coming from left, so need right
            }
        }
    }

    // Returns a minimum offset in the given direction plus a random offset in a parallel direction
    private Vector3 GetRoomOffset(DungeonRoom roomA, DungeonRoom roomB, Direction direction)
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

        offset += GetRandomOffset(roomA, roomB, direction);
        return SnapPosition(offset);
    }

    private Vector3 GetRandomOffset(DungeonRoom roomA, DungeonRoom roomB, Direction direction)
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
            var offsetZ = roomA.RoomDimensions.y * 0.5f + roomB.RoomDimensions.y * 0.5f + MINIMUM_OFFSET;
            if (offsetChance <= 0.15f) offset.z -= offsetZ;
            else if (offsetChance <= 0.4f) offset.z -= offsetZ * 0.5f;
            //20% chance of having no offset
            else if (offsetChance >= 0.85f) offset.z += offsetZ;
            else if (offsetChance >= 0.6f) offset.z += offsetZ * 0.5f;
        }

        return offset;
    }

    //Returns a dungeon room with at least one unclaimed node
    private DungeonRoom GetOpenRoom()
    {
        var room = dungeonRooms[Random.Range(1, dungeonRooms.Count)];

        for (int i = 0; i < room.ConnectedRooms.Length; i++)
        {
            if (room.ConnectedRooms[i] == null) return room;
        }

        return GetOpenRoom();
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

    private Vector3 SnapPosition(Vector3 input, float factor = 5)
    {
        if (factor == 0) throw new UnityException("Cannot divide by 0!");

        float x = Mathf.Round(input.x / factor) * factor;
        float y = Mathf.Round(input.y / factor) * factor;
        float z = Mathf.Round(input.z / factor) * factor;

        return new Vector3(x, y, z);
    }

    private void DrawDebugBox(Vector3 pos, Quaternion rot, Vector3 scale, Color c, float duration = 15f)
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

    private Vector2Int FindDungeonBounds()
    {
        Physics.SyncTransforms();

        Vector2 bottomLeft = Vector2.zero;
        Vector2 topRight = Vector2.zero;

        for (int i = 0; i < dungeonRooms.Count; i++)
        {
            var roomX = dungeonRooms[i].Transform.position.x;
            var roomZ = dungeonRooms[i].Transform.position.z;

            var halfWidth = dungeonRooms[i].RoomDimensions.x * 0.5f;
            var halfLength = dungeonRooms[i].RoomDimensions.y * 0.5f;

            if (roomX - halfWidth < bottomLeft.x) bottomLeft.x = roomX - halfWidth;
            if (roomX + halfWidth > topRight.x) topRight.x = roomX + halfWidth;

            if (roomZ - halfLength < bottomLeft.y) bottomLeft.y = roomZ - halfLength;
            if (roomZ + halfLength > topRight.y) topRight.y = roomZ + halfLength;
        }

        bottomLeft.x += 2.5f;
        bottomLeft.y += 2.5f;

        topRight.x -= 2.5f;
        topRight.y -= 2.5f;

        int width = Mathf.RoundToInt(Mathf.Abs(bottomLeft.x - topRight.x) / 5) + 1;
        int length = Mathf.RoundToInt(Mathf.Abs(bottomLeft.y - topRight.y) / 5) + 1;

        DuelManager.instance.Battlefield.CreateGrid(new Vector3(bottomLeft.x, 0, bottomLeft.y), new Vector2Int(width, length));

        return new Vector2Int(width, length);
    }
}

public enum Direction { Up, Down, Left, Right}

public struct DungeonFeatures
{
    public int minRooms;
    public int maxRooms;

    public int minCombats;
    public int maxCombats;

    public DungeonFeatures(int minMain, int maxMain, int minCombat, int maxCombat)
    {
        minRooms = minMain;
        maxRooms = maxMain;

        minCombats = minCombat;
        maxCombats = maxCombat;
    }
}