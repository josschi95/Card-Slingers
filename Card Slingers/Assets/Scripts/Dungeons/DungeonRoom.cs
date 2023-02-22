using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonRoom : MonoBehaviour
{
    public bool TESTING_NO_ENCOUNTER = true;

    [SerializeField] private Vector2 _fullDimensions;
    [SerializeField] private Vector2Int _boardDimensions;

    [SerializeField] private DungeonRoom[] connectedRooms = new DungeonRoom[4]; // up/down/left/right
    [SerializeField] private Transform[] connectionNodes = new Transform[4];
    [SerializeField] private Waypoint[] connectedWaypoints;
    [Space]
    [SerializeField] private GameObject[] _entranceParents;
    [SerializeField] private GameObject[] _closedEntranceParents;
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

    public void OnConfirmLayout()
    {
        //Disable all unconnected nodes
        //Enable walls in place of the unconnected Nodes
        for (int i = 0; i < connectedRooms.Length; i++)
        {
            _entranceParents[i].SetActive(connectedRooms[i] != null);
            _closedEntranceParents[i].SetActive(connectedRooms[i] == null);
        }
    }

    public void OnRoomEntered()
    {
        if (TESTING_NO_ENCOUNTER)
        {
            PlayerController.SetDestination(transform.position);
            return;
        }

        if (encounter != null) encounter.TriggerCombat();
        else
        {
            PlayerController.SetDestination(transform.position);
        }
    }
}
