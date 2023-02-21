using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonRoom : MonoBehaviour
{
    [SerializeField] private Vector2 _fullDimensions;
    [SerializeField] private Vector2Int _boardDimensions;

    [SerializeField] private DungeonRoom[] connectedRooms = new DungeonRoom[4]; // up/down/left/right
    [SerializeField] private Transform[] connectionNodes = new Transform[4];
    [SerializeField] private Waypoint[] connectedWaypoints;
    [SerializeField] private CombatEncounter encounter;

    public Vector2Int BoardDimensions => _boardDimensions;
    public Vector2 RoomDimensions => _fullDimensions;
    public DungeonRoom[] ConnectedRooms => connectedRooms;
    public Transform[] Nodes => connectionNodes;
    public Waypoint[] RoomWaypoints => connectedWaypoints;

    private void Start()
    {
        for (int i = 0; i < connectedWaypoints.Length; i++)
        {
            connectedWaypoints[i].SetRoom(this);
        }
    }

    public void SetConnectedRoom(DungeonRoom room, Direction direction, Direction fromDirection)
    {
        connectedWaypoints[(int)direction].SetConnectedWaypoint(room.connectedWaypoints[(int)fromDirection]);
    }

    public void OnRoomEntered()
    {
        PlayerController.SetDestination(transform.position);
        return;

        if (encounter != null) encounter.TriggerCombat();
        else
        {
            PlayerController.SetDestination(transform.position);
        }
    }
}
