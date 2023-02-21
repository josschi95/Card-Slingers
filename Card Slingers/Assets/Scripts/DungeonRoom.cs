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

    private void Start()
    {
        var points = GetComponentsInChildren<Waypoint>();
        connectedWaypoints = new Waypoint[points.Length];
        connectedWaypoints = points;

        for (int i = 0; i < connectedWaypoints.Length; i++)
        {
            connectedWaypoints[i].SetRoom(this);
        }
    }

    public void OnRoomEntered()
    {
        if (encounter != null) encounter.TriggerCombat();
        else
        {
            PlayerController.SetDestination(transform.position);
        }
    }
}
