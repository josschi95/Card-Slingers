using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private DungeonRoom startRoomPrefab;
    [SerializeField] private DungeonRoom[] dungeonRoomPrefabs;

    [Space]
    
    [SerializeField] private int minMainLineRooms;
    [SerializeField] private int maxMainLineRooms;

    [Space]

    [SerializeField] private int minBonusRooms;
    [SerializeField] private int maxBonusRooms;

    [Space]

    [SerializeField] private List<DungeonRoom> dungeonRooms;
    [SerializeField] private List<DungeonRoom> mainLineRooms;

    private void Start()
    {
        GenerateDungeon();
    }

    public void GenerateDungeon()
    {
        dungeonRooms = new List<DungeonRoom>();

        int mainRoomsToSpawn = Random.Range(minMainLineRooms, maxMainLineRooms + 1);
        int bonusRoomsToSpawn = Random.Range(minBonusRooms, maxBonusRooms + 1);

        var startRoom = Instantiate(startRoomPrefab, Vector3.zero, Quaternion.identity);
        dungeonRooms.Add(startRoom);

        //StartCoroutine(SpawnRooms_MethodA(mainRoomsToSpawn, bonusRoomsToSpawn));
        StartCoroutine(SpawnRooms_MethodA(mainRoomsToSpawn, bonusRoomsToSpawn));

        Debug.LogWarning("Now I need to set up the starting node to connect to the first room in the dungeon.");
    }

    private IEnumerator SpawnRooms_MethodA(int mainLineRoomsToSpawn, int bonusRooms)
    {
        while(mainLineRoomsToSpawn > 0)
        {
            var roomPrefab = dungeonRoomPrefabs[Random.Range(0, dungeonRoomPrefabs.Length)];

            var lastRoom = dungeonRooms[dungeonRooms.Count - 1];

            var newRoomOffset = Vector3.zero;
            newRoomOffset.x = lastRoom.RoomDimensions.x * 0.5f + roomPrefab.RoomDimensions.x * 0.5f + 20;

            float vertChance = Random.value;
            var vertOffset = lastRoom.RoomDimensions.y * 0.5f + roomPrefab.RoomDimensions.y * 0.5f + 20;

            //Adjust up or down so it doesn't skew too much in the same direction
            if (lastRoom.gameObject.transform.position.z > 50) vertChance = 0.1f;
            else if (lastRoom.gameObject.transform.position.z < -50) vertChance = 0.9f;

            if (vertChance < 0.2f) newRoomOffset.z = -vertOffset;
            else if (vertChance < 0.4f) newRoomOffset.z = -vertOffset * 0.5f;
            else if (vertOffset > 0.6) newRoomOffset.z = vertOffset * 0.5f;
            else if (vertChance > 0.8) newRoomOffset.z = vertOffset;

            //Rounds it to a multiple of 5 for easy connection via tiles
            newRoomOffset = SnapPosition(newRoomOffset);

            var newRoom = Instantiate(roomPrefab, lastRoom.transform.position + newRoomOffset, Quaternion.identity);
            
            dungeonRooms.Add(newRoom);
            mainLineRooms.Add(newRoom);

            ConnectRoomsInLine(lastRoom, newRoom);

            mainLineRoomsToSpawn--;

            yield return null;
        }

        //Main line has been created

        int retries = 0;
        while (bonusRooms > 0)
        {
            if (retries > 10) yield break; //just stop any infinite loops

            var roomPrefab = dungeonRoomPrefabs[Random.Range(0, dungeonRoomPrefabs.Length)];

            var roomToConnectTo = mainLineRooms[Random.Range(1, mainLineRooms.Count)];

            var direction = Direction.Up;
            if (roomToConnectTo.ConnectedRooms[(int)direction] != null) direction = Direction.Down;
            if (roomToConnectTo.ConnectedRooms[(int)direction] != null) continue;

            var newRoomOffset = Vector3.zero;
            var minZ = roomToConnectTo.RoomDimensions.y * 0.5f + roomPrefab.RoomDimensions.y * 0.5f + 20;
            if (direction == Direction.Up) newRoomOffset.z = minZ;
            else newRoomOffset.z = -minZ;

            
            //newRoomOffset.z = roomToConnectTo.RoomDimensions.y * 0.5f + roomPrefab.RoomDimensions.y * 0.5f + 20;

            float hertChance = Random.value;
            var hertOffset = roomToConnectTo.RoomDimensions.x * 0.5f + roomPrefab.RoomDimensions.x * 0.5f + 20;

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

            yield return null;
        }

        //Connect Waypoints
        ConnectRoomWaypoints();
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

        Debug.DrawLine(roomA.Nodes[indexA].position, roomB.Nodes[indexB].position, Color.green, int.MaxValue);
    }

    private void ConnectRoomsInLine(DungeonRoom roomA, DungeonRoom roomB)
    {
        int indexA = 0;
        int indexB = 0;

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

        for (int i = 0; i < roomA.ConnectedRooms.Length; i++)
        {
            if (roomA.ConnectedRooms[i] == roomB) indexA = i;
        }
        for (int i = 0; i < roomB.ConnectedRooms.Length; i++)
        {
            if (roomB.ConnectedRooms[i] == roomA) indexB = i;
        }


        Debug.DrawLine(roomA.Nodes[indexA].position, roomB.Nodes[indexB].position, Color.green, int.MaxValue);
    }

    private Vector3 SnapPosition(Vector3 input, float factor = 5)
    {
        if (factor == 0) throw new UnityException("Cannot divide by 0!");

        float x = Mathf.Round(input.x / factor) * factor;
        float y = Mathf.Round(input.y / factor) * factor;
        float z = Mathf.Round(input.z / factor) * factor;

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

                Direction direction = (Direction)i;
                Direction fromDirection = Direction.Up;

                for (int r = 0; r < connectedRoom.ConnectedRooms.Length; r++)
                {
                    if (connectedRoom.ConnectedRooms[r] == room)
                    {
                        fromDirection = (Direction)r;
                    }
                }

                room.SetConnectedRoom(connectedRoom, direction, fromDirection);
            }
        }
    }
}

public enum Direction { Up, Down, Left, Right}
