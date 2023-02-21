using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private Waypoint _startWaypoint;
    [SerializeField] private IntermediaryNode _intermediaryNode;
    [SerializeField] private DungeonRoom startRoomPrefab;
    [SerializeField] private DungeonRoom[] dungeonRoomPrefabs;

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

    private void Start()
    {
        GenerateDungeon(minMainLineRooms, maxMainLineRooms, minBonusRooms, maxBonusRooms);
    }

    public void GenerateDungeon(int minMain, int maxMain, int minBonus, int maxBonus)
    {
        dungeonRooms = new List<DungeonRoom>();

        int mainRoomsToSpawn = Random.Range(minMain, maxMain + 1);
        int bonusRoomsToSpawn = Random.Range(minBonus, maxBonus + 1);

        var startRoom = Instantiate(startRoomPrefab, Vector3.zero, Quaternion.identity);
        dungeonRooms.Add(startRoom);

        //Connects the starting waypoint to the start room //Player will just be placed at Vector3.zero
        //startRoom.RoomWaypoints[(int)Direction.Down].SetConnectedWaypoint(_startWaypoint);

        StartCoroutine(SpawnRooms_MethodA(mainRoomsToSpawn, bonusRoomsToSpawn));
    }

    private IEnumerator SpawnRooms_MethodA(int mainLineRoomsToSpawn, int bonusRooms)
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

            if (vertChance < 0.2f) newRoomOffset.z = -vertOffset;
            else if (vertChance < 0.4f) newRoomOffset.z = -vertOffset * 0.5f;
            else if (vertOffset > 0.6) newRoomOffset.z = vertOffset * 0.5f;
            else if (vertChance > 0.8) newRoomOffset.z = vertOffset;

            //Rounds it to a multiple of 5 for easy connection via tiles
            newRoomOffset = SnapPosition(newRoomOffset);

            var newRoom = Instantiate(roomPrefab, previousRoom.transform.position + newRoomOffset, Quaternion.identity);
            
            dungeonRooms.Add(newRoom);
            mainLineRooms.Add(newRoom);

            //if (previousRoom == dungeonRooms[0]) ConnectRoomsToNearest(previousRoom, newRoom);
            //else 
                ConnectRoomsInLine(previousRoom, newRoom);

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

            
            //newRoomOffset.z = roomToConnectTo.RoomDimensions.y * 0.5f + roomPrefab.RoomDimensions.y * 0.5f + 20;

            float hertChance = Random.value;
            var hertOffset = roomToConnectTo.RoomDimensions.x * 0.5f + roomPrefab.RoomDimensions.x * 0.5f + _minimumOffset;

            if (hertChance < 0.2f) newRoomOffset.x = -hertOffset;
            else if (hertChance < 0.4f) newRoomOffset.x = -hertOffset * 0.5f;
            else if (hertChance > 0.6) newRoomOffset.x = hertOffset * 0.5f;
            else if (hertChance > 0.8) newRoomOffset.x = hertOffset;

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

            ConnectRoomsToNearest(roomToConnectTo, newRoom);

            bonusRooms--;

            //Add a second bonus room onto the already existing bonus room
            if (Random.value < 0.15f)
            {

            }

            yield return null;
        }

        //Connect Waypoints
        ConnectRoomWaypoints();

        //Close off unused entrances
        for (int i = 0; i < dungeonRooms.Count; i++)
        {
            dungeonRooms[i].OnConfirmLayout();
        }

        var player = GameObject.Find("PlayerController");
        if (dungeonRooms[0].ConnectedRooms[0] != null) player.transform.eulerAngles = Vector3.zero;
        else if (dungeonRooms[0].ConnectedRooms[1] != null) player.transform.eulerAngles = new Vector3(0, 180, 0);
        else if (dungeonRooms[0].ConnectedRooms[2] != null) player.transform.eulerAngles = new Vector3(0, -90, 0);
        else if (dungeonRooms[0].ConnectedRooms[3] != null) player.transform.eulerAngles = new Vector3(0, 90, 0);
    }

    private void ConnectRoomsToNearest(DungeonRoom roomA, DungeonRoom roomB)
    {
        float lowestDistance = int.MaxValue;
        int indexA = 0;
        int indexB = 0;

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

        roomA.ConnectedRooms[indexA] = roomB;
        roomB.ConnectedRooms[indexB] = roomA;
    }

    private void ConnectRoomsInLine(DungeonRoom roomA, DungeonRoom roomB)
    {
        //Still might run into issues with replacing, but I don't think I will
        //room B is to the right of room A
        if (roomB.transform.position.x > roomA.transform.position.x)
        {
            roomB.ConnectedRooms[(int)Direction.Left] = roomA;
        }
        else
        {
            roomB.ConnectedRooms[(int)Direction.Right] = roomA;
        }

        //room A is higher than room B
        if (roomA.transform.position.z > roomB.transform.position.z)
        {
            roomA.ConnectedRooms[(int)Direction.Down] = roomB;
        }
        else
        {
            roomA.ConnectedRooms[(int)Direction.Up] = roomB;
        }
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

    private void ConnectRoomWaypoints()
    {
        foreach(DungeonRoom room in dungeonRooms)
        {
            for (int i = 0; i < room.ConnectedRooms.Length; i++)
            {
                if (room.ConnectedRooms[i] == null) continue;
                var connectedRoom = room.ConnectedRooms[i];

                Direction fromDirection = (Direction)i;
                Direction toDirection = Direction.Up;

                for (int r = 0; r < connectedRoom.ConnectedRooms.Length; r++)
                {
                    if (connectedRoom.ConnectedRooms[r] == room)
                    {
                        toDirection = (Direction)r;
                    }
                }

                //room.SetConnectedRoom(connectedRoom, fromDirection, toDirection);

                var intermediaryPos = Vector3.zero;
                var fromNode = room.Nodes[(int)fromDirection];
                var toNode = connectedRoom.Nodes[(int)toDirection];

                //Connection requires wrapping around the entire room
                if (fromDirection == toDirection)
                {
                    Debug.LogError("This should not happen.");
                }
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
                //Going between Up and Down
                else if ((int)fromDirection <=1 && (int)toDirection <= 1)
                {
                    //Need to place two intermediary nodes for an 'S' like hallway
                    if (fromNode.position.x != toNode.position.x)
                    {
                        float crossBarZValue = (fromNode.position.z + toNode.position.z) * 0.5f; //Find middle point

                        Vector3 firstIntermediatePos = SnapPosition(fromNode.position.x, 0, crossBarZValue);
                        Vector3 secondIntermediatePos = SnapPosition(toNode.position.x, 0, crossBarZValue);

                        var firstIntermediate = Instantiate(_intermediaryNode, firstIntermediatePos, Quaternion.identity);
                        var secondIntermediate = Instantiate(_intermediaryNode, secondIntermediatePos, Quaternion.identity);

                        firstIntermediate.SetAsIntermediate(secondIntermediate, room.RoomWaypoints[(int)fromDirection]);
                        secondIntermediate.SetAsIntermediate(firstIntermediate, connectedRoom.RoomWaypoints[(int)toDirection]);
                    }
                    //Else it's a straight line, no issue
                    else room.RoomWaypoints[(int)fromDirection].SetConnectedWaypoint(connectedRoom.RoomWaypoints[(int)toDirection]);
                }
                //Going between Left and Right
                else if ((int)fromDirection >= 2 && (int)toDirection >= 2)
                {
                    //Need to place two intermediary nodes for an 'S' like hallway
                    if (fromNode.position.z != toNode.position.z)
                    {
                        float crossBarXValue = (fromNode.position.z + toNode.position.z) * 0.5f; //Find middle point

                        Vector3 firstIntermediatePos = SnapPosition(crossBarXValue, 0, fromNode.position.z);
                        Vector3 secondIntermediatePos = SnapPosition(crossBarXValue, 0, toNode.position.z);

                        var firstIntermediate = Instantiate(_intermediaryNode, firstIntermediatePos, Quaternion.identity);
                        var secondIntermediate = Instantiate(_intermediaryNode, secondIntermediatePos, Quaternion.identity);

                        firstIntermediate.SetAsIntermediate(secondIntermediate, room.RoomWaypoints[(int)fromDirection]);
                        secondIntermediate.SetAsIntermediate(firstIntermediate, connectedRoom.RoomWaypoints[(int)toDirection]);
                    }
                    //Else it's a straight line, no issue
                    else room.RoomWaypoints[(int)fromDirection].SetConnectedWaypoint(connectedRoom.RoomWaypoints[(int)toDirection]);
                }
                else
                {
                    Debug.LogError("You missed some scenario to get here.");
                }

                if (intermediaryPos != Vector3.zero)
                {
                    var inter = Instantiate(_intermediaryNode, intermediaryPos, Quaternion.identity);
                    inter.SetAsIntermediate(room.RoomWaypoints[(int)fromDirection], connectedRoom.RoomWaypoints[(int)toDirection]);
                }

                /*switch (fromDirection)
                {
                    case Direction.Up:
                        switch (toDirection)
                        {
                            case Direction.Down:
                                if (room.Nodes[(int)fromDirection].position.x == connectedRoom.Nodes[(int)toDirection].position.x) Debug.Log("No Issue");
                                else Debug.LogWarning("Need two intermediary nodes.");
                                break;
                            case Direction.Left:
                                intermediaryPos.x = fromNode.position.x;
                                intermediaryPos.z = toNode.position.z;
                                break;
                            case Direction.Right:
                                intermediaryPos.x = fromNode.position.x;
                                intermediaryPos.z = toNode.position.z;
                                break;
                        }
                        break;
                    case Direction.Down:
                        switch (toDirection)
                        {
                            case Direction.Up:
                                if (room.Nodes[(int)fromDirection].position.x == connectedRoom.Nodes[(int)toDirection].position.x) Debug.Log("No Issue");
                                else Debug.LogWarning("Need two intermediary nodes.");
                                break;
                            case Direction.Left:
                                intermediaryPos.x = fromNode.position.x;
                                intermediaryPos.z = toNode.position.z;
                                break;
                            case Direction.Right:
                                intermediaryPos.x = fromNode.position.x;
                                intermediaryPos.z = toNode.position.z;
                                break;
                        }
                        break;
                    case Direction.Left:
                        switch (toDirection)
                        {
                            case Direction.Up:
                                intermediaryPos.x = toNode.position.x;
                                intermediaryPos.z = fromNode.position.z;
                                break;
                            case Direction.Down:
                                intermediaryPos.x = toNode.position.x;
                                intermediaryPos.z = fromNode.position.z;
                                break;
                            case Direction.Right:
                                if (room.Nodes[(int)fromDirection].position.z == connectedRoom.Nodes[(int)toDirection].position.z) Debug.Log("No Issue");
                                else Debug.LogWarning("Need two intermediary nodes.");
                                break;
                        }
                        break;
                    case Direction.Right:
                        switch (toDirection)
                        {
                            case Direction.Up:
                                intermediaryPos.x = toNode.position.x;
                                intermediaryPos.z = fromNode.position.z;
                                break;
                            case Direction.Down:
                                intermediaryPos.x = toNode.position.x;
                                intermediaryPos.z = fromNode.position.z;
                                break;
                            case Direction.Left:
                                if (room.Nodes[(int)fromDirection].position.z == connectedRoom.Nodes[(int)toDirection].position.z) Debug.Log("No Issue");
                                else Debug.LogWarning("Need two intermediary nodes.");
                                break;
                        }
                        break;
                }*/


            }
        }
    }
}

public enum Direction { Up, Down, Left, Right}
