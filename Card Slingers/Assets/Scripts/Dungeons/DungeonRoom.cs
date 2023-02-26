using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonRoom : MonoBehaviour
{
    [SerializeField] private CombatEncounter _encounter;
    [SerializeField] private Vector2 _fullDimensions;
    [SerializeField] private Vector2Int _boardDimensions;

    [Space]
    
    [SerializeField] private PathNode[] _nodes;
    [SerializeField] private GameObject[] _entranceParents;
    [SerializeField] private GameObject[] _closedEntranceParents;
    
    [Space]

    [SerializeField] private Transform _transform;
    [SerializeField] private Collider _collider;
    [SerializeField] private GameObject _fogOfWar;

    private DungeonRoom[] connectedRooms = new DungeonRoom[4]; // up/down/left/right

    [Space]

    [Space]

    private Vector3 _orientation;
    private bool _encounterTriggered;

    #region - Properties - 
    public CombatEncounter Encounter
    {
        get => _encounter;
        set
        {
            _encounter = value;
        }
    }
    public Vector2 RoomDimensions => _fullDimensions;
    public Vector2Int BoardDimensions => _boardDimensions;
    public PathNode[] Nodes => _nodes;
    public Transform Transform => _transform;
    public DungeonRoom[] ConnectedRooms
    {
        get => connectedRooms;
        set
        {
            connectedRooms = value;
        }
    }
    public Vector3 Orientation
    {
        get => _orientation;
        private set
        {
            _orientation = value;
        }
    }
    #endregion

    public bool TESTING_NO_ENCOUNTER;

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
        _collider.enabled = false;
        _fogOfWar.SetActive(true);
        for (int i = 0; i < connectedRooms.Length; i++)
        {
            _entranceParents[i].SetActive(connectedRooms[i] != null);
            _closedEntranceParents[i].SetActive(connectedRooms[i] == null);

            if (connectedRooms[i] != null && _nodes[i].ConnectedNode == null)
            {
                Debug.LogError("Lost Reference to Neighor Node " + transform.position + ", " + _nodes[i].direction);
            }
        }
    }

    public void OnRoomEntered(Direction direction)
    {
        PlayerController.instance.onRoomEntered?.Invoke(this);

        switch (direction)
        {
            case Direction.Up:
                _orientation = Vector3.up * 180;
                break;
            case Direction.Down:
                _orientation = Vector3.zero;
                break;
            case Direction.Left:
                _orientation = Vector3.up * 90;
                break;
            case Direction.Right:
                _orientation = Vector3.up * -90;
                break;
        }

        _fogOfWar.SetActive(false);

        if (TESTING_NO_ENCOUNTER)
        {
            PlayerController.SetDestination(transform.position);
            return;
        }

        if (_encounter != null && !_encounterTriggered) OnCombatEncounter();
        else PlayerController.SetDestination(transform.position);
    }

    private void OnCombatEncounter()
    {
        _encounter.TriggerCombat();
        _encounterTriggered = true;
        DuelManager.instance.onMatchStarted?.Invoke(_encounter);
    }
}
