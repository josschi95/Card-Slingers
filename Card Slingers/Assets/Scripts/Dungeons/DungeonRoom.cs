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
    [SerializeField] private Waypoint[] _waypoints;
    [Space]
    [SerializeField] private GameObject[] _entranceParents;
    [SerializeField] private GameObject[] _closedEntranceParents;
    [SerializeField] private CombatEncounter encounter;
    [SerializeField] private GameObject _fogOfWar;
    [SerializeField] private GameObject TEST_ORIENTATION_MARKER;

    public Vector2Int BoardDimensions => _boardDimensions;
    public Vector2 RoomDimensions => _fullDimensions;
    public DungeonRoom[] ConnectedRooms => connectedRooms;
    public Transform[] Nodes => connectionNodes;
    public Waypoint[] Waypoints => _waypoints;

    private void OnEnable()
    {
        for (int i = 0; i < _waypoints.Length; i++)
        {
            _waypoints[i].SetRoom(this);
            //_waypoints[i].SetConnectedWaypoint(null);
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < connectedRooms.Length; i++)
        {
            if (connectedRooms[i] == null) continue;

            for (int x = 0; x < connectedRooms[i].ConnectedRooms.Length; x++)
            {
                if (connectedRooms[i].ConnectedRooms[x] == this)
                {
                    connectedRooms[i].ConnectedRooms[x] = null;
                }
            }
        }
    }

    public void OnConfirmLayout()
    {
        _fogOfWar.SetActive(true);
        for (int i = 0; i < connectedRooms.Length; i++)
        {
            _entranceParents[i].SetActive(connectedRooms[i] != null);
            _closedEntranceParents[i].SetActive(connectedRooms[i] == null);

            if (connectedRooms[i] != null && _waypoints[i].ConnectedNode == null)
            {
                Debug.LogError("Lost Reference to Neighor Node " + transform.position + ", " + _waypoints[i].direction);
            }
        }
    }

    public void OnRoomEntered(Direction direction)
    {
        switch (direction)
        {
            case Direction.Up:
                TEST_ORIENTATION_MARKER.transform.eulerAngles = Vector3.up * 180;
                break;
            case Direction.Down:
                TEST_ORIENTATION_MARKER.transform.eulerAngles = Vector3.zero;
                break;
            case Direction.Left:
                TEST_ORIENTATION_MARKER.transform.eulerAngles = Vector3.up * 90;
                break;
            case Direction.Right:
                TEST_ORIENTATION_MARKER.transform.eulerAngles = Vector3.up * -90;
                break;
        }

        _fogOfWar.SetActive(false);

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
