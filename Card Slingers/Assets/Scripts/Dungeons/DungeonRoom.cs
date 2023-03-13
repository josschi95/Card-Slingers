using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonRoom : MonoBehaviour
{
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
    private bool _containsEnemies = false;

    #region - Properties - 
    public bool ContainsEnemies
    {
        get => _containsEnemies;
        set
        {
            _containsEnemies = value;
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

    public void CloseEmptyEntrances()
    {
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

    public void OnConfirmLayout()
    {
        //_collider.enabled = false;
        //_fogOfWar.SetActive(true);

        var colls = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colls.Length; i++)
        {
            //colls[i].enabled = false;
        }
    }
}
