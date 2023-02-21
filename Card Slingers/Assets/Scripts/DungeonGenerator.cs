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

        StartCoroutine(SpawnRooms(mainRoomsToSpawn, bonusRoomsToSpawn));
    }

    private IEnumerator SpawnRooms(int mainLineRoomsToSpawn, int bonusRooms)
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

            ConnectRooms(lastRoom, newRoom);

            mainLineRoomsToSpawn--;

            yield return null;
        }

        //Main line has been created

        //May want to create connections between main line before adding bonus rooms so I know where I can add them
        int retries = 0;
        while (bonusRooms > 0)
        {
            if (retries > 10) yield break; //just stop any infinite loops

            var roomPrefab = dungeonRoomPrefabs[Random.Range(0, dungeonRoomPrefabs.Length)];

            var roomToConnectTo = mainLineRooms[Random.Range(1, mainLineRooms.Count)];

            var direction = Direction.Up;
            bool roomIsValid = true;

            for (int i = 0; i < roomToConnectTo.ConnectedRooms.Length; i++)
            {
                if (roomToConnectTo.ConnectedRooms[i] == null)
                {
                    direction = (Direction)i;
                    break;
                }
                if (i == 3)
                {
                    //only should get here if all rooms are filledl
                    Debug.Log("Room not valid");
                    retries++;
                    roomIsValid = false;
                }
            }
            if (!roomIsValid) continue; //start over and try again

            Debug.LogWarning("Pickup Here. Need to make sure that I'm positioning the new room in the correct direction");

            var newRoomOffset = Vector3.zero;
            newRoomOffset.z = roomToConnectTo.RoomDimensions.y * 0.5f + roomPrefab.RoomDimensions.y * 0.5f + 20;

            float hertChance = Random.value;
            var hertOffset = roomToConnectTo.RoomDimensions.x * 0.5f + roomPrefab.RoomDimensions.x * 0.5f + 20;

            if (hertChance < 0.2f) newRoomOffset.x = -hertOffset;
            else if (hertChance < 0.4f) newRoomOffset.x = -hertOffset * 0.5f;
            else if (hertChance > 0.6) newRoomOffset.x = hertOffset * 0.5f;
            else if (hertChance > 0.8) newRoomOffset.x = hertOffset;

            //Rounds it to a multiple of 5 for easy connection via tiles
            newRoomOffset = SnapPosition(newRoomOffset);

            var newRoom = Instantiate(roomPrefab, roomToConnectTo.transform.position + newRoomOffset, Quaternion.identity);

            dungeonRooms.Add(newRoom);

            ConnectRooms(roomToConnectTo, newRoom);

            bonusRooms--;

            yield return null;
        }
    }

    private void ConnectRooms(DungeonRoom roomA, DungeonRoom roomB)
    {
        //Just find the two closest connection nodes
        //float lowestDistance = int.MaxValue;
        int indexA = 0;
        int indexB = 0;

        /*for (int a = 0; a < roomA.Nodes.Length; a++)
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
        }*/

        //roomA.ConnectedRooms[indexA] = roomB;
        //roomB.ConnectedRooms[indexB] = roomA;

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
}

public enum Direction { Up, Down, Left, Right}
